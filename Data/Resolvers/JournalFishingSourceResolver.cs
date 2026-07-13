using System.Collections;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace ProgressionJournal.Data.Resolvers;

internal static class JournalFishingSourceResolver
{
    private const int DefaultRandomSeedCount = 32;
    private const int ScenarioRandomSeedCount = 32;
    private const int EquipmentRandomSeedCount = 8;
    private const int ForcedRaritySeedCount = 64;

    private static readonly object SyncRoot = new();
    private static readonly HashSet<string> LoggedProbeFailures = new(StringComparer.Ordinal);
    private static readonly FieldInfo? ModBiomeFlagsField = typeof(Player).GetField(
        "modBiomeFlags",
        BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly PropertyInfo? ModBiomeIndexProperty = typeof(ModBiome).GetProperty(
        "ZeroIndexType",
        BindingFlags.Instance | BindingFlags.NonPublic);
    private static FishingCatalog? _catalog;
    private static string _catalogKey = string.Empty;

    private enum ProbeLiquid
    {
        Water,
        Lava,
        Honey
    }

    private sealed record ProbeEnvironment(
        string DisplayName,
        bool IsOcean,
        ModBiome? ModBiome,
        ModWaterStyle? WaterStyle,
        Action<Player> ApplyVanillaBiome);

    private sealed record ProbeEquipment(int PoleItemId, int BaitItemId, int RandomSeedCount);

    private sealed record ProbeProgression(
        string DisplayName,
        ProbeProgressionVariant[] Variants);

    private sealed record ProbeProgressionVariant(
        bool Hardmode,
        bool DownedSkeletron,
        bool CombatBookUsed,
        bool UnlockedSlimeRed,
        HashSet<string> ConditionKeys);

    private sealed record ProbeWorld(
        bool Hardmode = false,
        bool BloodMoon = false,
        bool DayTime = true,
        bool DownedSkeletron = false,
        bool CombatBookUsed = false,
        bool UnlockedSlimeRed = false,
        bool Remix = false,
        bool NotTheBees = false);

    private sealed record FishingPipeline(
        MethodInfo RollDropLevels,
        MethodInfo RollEnemySpawns,
        MethodInfo RollItemDrop);

    private sealed record FishingCatalog(
        Dictionary<int, HashSet<ProbeContext>> ItemContexts,
        Dictionary<int, HashSet<ProbeContext>> NpcContexts,
        IReadOnlyList<ProbeEnvironment> Environments,
        ProbeEquipment[] Equipment,
        IReadOnlyList<ProbeProgression> Progression);

    private readonly record struct ProbeContext(
        ProbeLiquid Liquid,
        int EnvironmentIndex,
        int Depth,
        int WorldIndex,
        int EquipmentIndex,
        int ProgressionIndex,
        int ProgressionVariantIndex = 0);

    private readonly record struct PlayerState(
        Vector2 Position,
        Vector2 Velocity,
        bool Beach,
        bool Desert,
        bool Snow,
        bool Jungle,
        bool Corrupt,
        bool Crimson,
        bool Hallow,
        bool Dungeon,
        bool Glowshroom,
        BitArray ModBiomeFlags);

    private static readonly string[] DepthLocalizationKeys =
    [
        "Bestiary_Biomes.Sky",
        "Bestiary_Biomes.Surface",
        "Bestiary_Biomes.Underground",
        "Bestiary_Biomes.Caverns",
        "Bestiary_Biomes.TheUnderworld"
    ];

    private static readonly int[] FishingLevels = [50, 200, 400];

    private static readonly ProbeWorld[] Worlds =
    [
        new(),
        new(DayTime: false),
        new(Hardmode: true),
        new(Hardmode: true, DayTime: false),
        new(BloodMoon: true, DayTime: false),
        new(Hardmode: true, BloodMoon: true, DayTime: false),
        new(DownedSkeletron: true),
        new(Hardmode: true, DownedSkeletron: true),
        new(CombatBookUsed: true),
        new(Hardmode: true, CombatBookUsed: true),
        new(BloodMoon: true, DayTime: false, UnlockedSlimeRed: true),
        new(Hardmode: true, BloodMoon: true, DayTime: false, UnlockedSlimeRed: true),
        new(Remix: true),
        new(Hardmode: true, Remix: true),
        new(NotTheBees: true),
        new(Hardmode: true, NotTheBees: true)
    ];

    public static IReadOnlyList<JournalFishingSource> FindSources(int itemId)
    {
        var sources = new List<JournalFishingSource>();

        if (Main.anglerQuestItemNetIDs?.Contains(itemId) == true)
        {
            sources.Add(new JournalFishingSource(
                [Language.GetTextValue("Mods.ProgressionJournal.UI.FishingAnglerQuestCondition")]));
        }

        var catalog = GetCatalog();
        AppendObservedSource(sources, catalog, catalog.ItemContexts, itemId);
        return sources;
    }

    internal static JournalFishingAvailability GetItemAvailability(int itemId)
    {
        var catalog = GetCatalog();
        if (Main.anglerQuestItemNetIDs?.Contains(itemId) == true)
        {
            return new JournalFishingAvailability(
                observed: true,
                earliestStageIndex: 0,
                earliestStageName: catalog.Progression.Count > 0
                    ? catalog.Progression[0].DisplayName
                    : string.Empty,
                conditions:
                [
                    Language.GetTextValue("Mods.ProgressionJournal.UI.FishingAnglerQuestCondition")
                ]);
        }

        return CreateAvailability(catalog, catalog.ItemContexts, itemId);
    }

    internal static JournalFishingAvailability GetNpcAvailability(int npcId)
    {
        var catalog = GetCatalog();
        return CreateAvailability(catalog, catalog.NpcContexts, npcId);
    }

    internal static IReadOnlyList<JournalFishingSource> FindNpcSources(int npcId)
    {
        var sources = new List<JournalFishingSource>();
        var catalog = GetCatalog();
        AppendObservedSource(sources, catalog, catalog.NpcContexts, npcId);
        return sources;
    }

    private static void AppendObservedSource(
        List<JournalFishingSource> sources,
        FishingCatalog catalog,
        IReadOnlyDictionary<int, HashSet<ProbeContext>> observations,
        int contentId)
    {
        if (observations.TryGetValue(contentId, out var contexts) && contexts.Count > 0)
        {
            sources.Add(new JournalFishingSource(BuildConditions(catalog, contexts)));
        }
    }

    private static JournalFishingAvailability CreateAvailability(
        FishingCatalog catalog,
        IReadOnlyDictionary<int, HashSet<ProbeContext>> observations,
        int contentId)
    {
        if (!observations.TryGetValue(contentId, out var contexts) || contexts.Count == 0)
        {
            return new JournalFishingAvailability(
                observed: false,
                earliestStageIndex: -1,
                earliestStageName: string.Empty,
                conditions: []);
        }

        var effectiveStageIndexes = contexts
            .Select(context => GetEvidenceProgressionIndex(catalog, context))
            .Where(static index => index >= 0)
            .ToArray();
        if (effectiveStageIndexes.Length == 0)
        {
            return new JournalFishingAvailability(
                observed: true,
                earliestStageIndex: -1,
                earliestStageName: string.Empty,
                conditions: BuildConditions(catalog, contexts));
        }

        var earliestStageIndex = effectiveStageIndexes.Min();
        var earliestStageName = earliestStageIndex >= 0
            && earliestStageIndex < catalog.Progression.Count
            ? catalog.Progression[earliestStageIndex].DisplayName
            : string.Empty;
        return new JournalFishingAvailability(
            observed: true,
            earliestStageIndex,
            earliestStageName,
            BuildConditions(catalog, contexts));
    }

    private static FishingCatalog GetCatalog()
    {
        lock (SyncRoot)
        {
            var key = BuildCatalogKey();
            if (_catalog is not null && string.Equals(_catalogKey, key, StringComparison.Ordinal)) return _catalog;
            LoggedProbeFailures.Clear();
            _catalog = BuildCatalog();
            _catalogKey = key;

            return _catalog;
        }
    }

    private static string BuildCatalogKey()
    {
        var profileId = JournalRuntimeProgressionScenarios.CurrentProfile?.Id ?? string.Empty;
        var mods = string.Join(
            ";",
            ModLoader.Mods
                .Where(static mod => mod.Code is not null)
                .OrderBy(static mod => mod.Name, StringComparer.OrdinalIgnoreCase)
                .Select(static mod => $"{mod.Name}@{mod.Version}"));
        return $"{Main.worldID}:{Main.maxTilesX}x{Main.maxTilesY}:{profileId}:{mods}";
    }

    private static FishingCatalog BuildCatalog()
    {
        var pipeline = CreatePipeline();
        var player = GetProbePlayer();
        var environments = CreateEnvironments();
        var equipment = CreateEquipment();
        using var progression = new JournalRuntimeProgressionScenarios();
        var progressionScenarios = progression.StageNames
            .Select(static name => new ProbeProgression(
                name,
                [new ProbeProgressionVariant(false, false, false, false, [])]))
            .ToArray();
        var catalog = new FishingCatalog([], [], environments, equipment, progressionScenarios);
        if (pipeline is null || player is null)
        {
            return catalog;
        }

        var previousRandom = Main.rand;
        var previousHardmode = Main.hardMode;
        var previousBloodMoon = Main.bloodMoon;
        var previousDayTime = Main.dayTime;
        var previousRemix = Main.remixWorld;
        var previousNotTheBees = Main.notTheBeesWorld;
        var previousWaterStyle = Main.waterStyle;
        var previousDownedSkeletron = NPC.downedBoss3;
        var previousCombatBookUsed = NPC.combatBookWasUsed;
        var previousUnlockedSlimeRed = NPC.unlockedSlimeRedSpawn;
        var previousPlayerState = CapturePlayerState(player);

        try
        {
            progressionScenarios = progression.StageNames
                .Select((name, index) =>
                {
                    return new ProbeProgression(
                        name,
                        Enumerable.Range(0, progression.GetVariantCount(index))
                            .Select(variantIndex =>
                            {
                                progression.Reset();
                                ApplyWorld(new ProbeWorld());
                                progression.Apply(index, variantIndex);
                                return new ProbeProgressionVariant(
                                    Main.hardMode,
                                    NPC.downedBoss3,
                                    NPC.combatBookWasUsed,
                                    NPC.unlockedSlimeRedSpawn,
                                    progression.GetVariantConditionKeys(index, variantIndex));
                            })
                            .ToArray());
                })
                .ToArray();
            catalog = catalog with { Progression = progressionScenarios };

            var projectile = new Projectile
            {
                owner = player.whoAmI
            };

            foreach (var (context, randomSeedCount) in CreateProbeContexts(catalog))
            {
                progression.Reset();
                ApplyWorld(Worlds[context.WorldIndex]);
                progression.Apply(context.ProgressionIndex, context.ProgressionVariantIndex);
                ApplyEnvironment(player, environments[context.EnvironmentIndex]);
                ProbeContextDrops(
                    catalog,
                    pipeline,
                    player,
                    projectile,
                    context,
                    randomSeedCount);
            }
        }
        catch (Exception exception)
        {
            LogDebugOnce(
                "catalog",
                $"Failed to build the runtime fishing source catalog for profile '{JournalRuntimeProgressionScenarios.CurrentProfile?.Id ?? "<none>"}'.",
                exception);
            return new FishingCatalog([], [], environments, equipment, progressionScenarios);
        }
        finally
        {
            Main.rand = previousRandom;
            Main.hardMode = previousHardmode;
            Main.bloodMoon = previousBloodMoon;
            Main.dayTime = previousDayTime;
            Main.remixWorld = previousRemix;
            Main.notTheBeesWorld = previousNotTheBees;
            Main.waterStyle = previousWaterStyle;
            NPC.downedBoss3 = previousDownedSkeletron;
            NPC.combatBookWasUsed = previousCombatBookUsed;
            NPC.unlockedSlimeRedSpawn = previousUnlockedSlimeRed;
            RestorePlayerState(player, previousPlayerState);
        }

        return catalog;
    }

    private static IReadOnlyList<(ProbeContext Context, int RandomSeedCount)> CreateProbeContexts(
        FishingCatalog catalog)
    {
        var contexts = new Dictionary<ProbeContext, int>();

        for (var environmentIndex = 0; environmentIndex < catalog.Environments.Count; environmentIndex++)
        {
            for (var depth = 0; depth < DepthLocalizationKeys.Length; depth++)
            {
                for (var liquid = ProbeLiquid.Water; liquid <= ProbeLiquid.Honey; liquid++)
                {
                    Add(
                        new ProbeContext(liquid, environmentIndex, depth, 0, 0, 0),
                        DefaultRandomSeedCount);
                }
            }
        }

        for (var worldIndex = 1; worldIndex < Worlds.Length; worldIndex++)
        {
            for (var environmentIndex = 0; environmentIndex < catalog.Environments.Count; environmentIndex++)
            {
                Add(
                    new ProbeContext(ProbeLiquid.Water, environmentIndex, 1, worldIndex, 0, 0),
                    ScenarioRandomSeedCount);
            }

            for (var liquid = ProbeLiquid.Water; liquid <= ProbeLiquid.Honey; liquid++)
            {
                Add(
                    new ProbeContext(liquid, 0, 1, worldIndex, 0, 0),
                    ScenarioRandomSeedCount);
            }
        }

        for (var progressionIndex = 0; progressionIndex < catalog.Progression.Count; progressionIndex++)
        {
            for (var environmentIndex = 0; environmentIndex < catalog.Environments.Count; environmentIndex++)
            {
                Add(
                    new ProbeContext(
                        ProbeLiquid.Water,
                        environmentIndex,
                        1,
                        0,
                        0,
                        progressionIndex),
                    ScenarioRandomSeedCount);
            }

            for (var depth = 0; depth < DepthLocalizationKeys.Length; depth++)
            {
                Add(
                    new ProbeContext(
                        ProbeLiquid.Water,
                        0,
                        depth,
                        0,
                        0,
                        progressionIndex),
                    ScenarioRandomSeedCount);
            }

            for (var liquid = ProbeLiquid.Water; liquid <= ProbeLiquid.Honey; liquid++)
            {
                Add(
                    new ProbeContext(
                        liquid,
                        0,
                        1,
                        0,
                        0,
                        progressionIndex),
                    ScenarioRandomSeedCount);
            }
        }

        for (var equipmentIndex = 1; equipmentIndex < catalog.Equipment.Length; equipmentIndex++)
        {
            Add(
                new ProbeContext(
                    ProbeLiquid.Water,
                    0,
                    1,
                    0,
                    equipmentIndex,
                    0),
                catalog.Equipment[equipmentIndex].RandomSeedCount);
            if (catalog.Progression.Count > 1)
            {
                Add(
                    new ProbeContext(
                        ProbeLiquid.Water,
                        0,
                        1,
                        0,
                        equipmentIndex,
                        catalog.Progression.Count - 1),
                    catalog.Equipment[equipmentIndex].RandomSeedCount);
            }
        }

        return contexts
            .Select(static pair => (pair.Key, pair.Value))
            .ToArray();

        void Add(ProbeContext context, int randomSeedCount)
        {
            var variants = catalog.Progression[context.ProgressionIndex].Variants;
            for (var variantIndex = 0; variantIndex < variants.Length; variantIndex++)
            {
                var variantContext = context with { ProgressionVariantIndex = variantIndex };
                if (!contexts.TryGetValue(variantContext, out var existing) || randomSeedCount > existing)
                {
                    contexts[variantContext] = randomSeedCount;
                }
            }
        }
    }

    private static FishingPipeline? CreatePipeline()
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic;
        var methods = typeof(Projectile).GetMethods(flags);
        var rollDropLevels = methods.FirstOrDefault(static method =>
            method.Name == "FishingCheck_RollDropLevels");
        var rollEnemySpawns = methods.FirstOrDefault(static method =>
            method.Name == "FishingCheck_RollEnemySpawns");
        var rollItemDrop = methods.FirstOrDefault(static method =>
            method.Name == "FishingCheck_RollItemDrop");
        return rollDropLevels is null || rollEnemySpawns is null || rollItemDrop is null
            ? null
            : new FishingPipeline(rollDropLevels, rollEnemySpawns, rollItemDrop);
    }

    private static IReadOnlyList<ProbeEnvironment> CreateEnvironments()
    {
        var environments = new List<ProbeEnvironment>
        {
            new(
                Language.GetTextValue("Mods.ProgressionJournal.UI.FishingBiomeDefault"),
                false,
                null,
                null,
                static _ => { }),
            new(
                Language.GetTextValue("Bestiary_Biomes.Ocean"),
                true,
                null,
                null,
                static player => player.ZoneBeach = true),
            new(
                Language.GetTextValue("Bestiary_Biomes.Desert"),
                false,
                null,
                null,
                static player => player.ZoneDesert = true),
            new(
                Language.GetTextValue("Bestiary_Biomes.Snow"),
                false,
                null,
                null,
                static player => player.ZoneSnow = true),
            new(
                Language.GetTextValue("Bestiary_Biomes.Jungle"),
                false,
                null,
                null,
                static player => player.ZoneJungle = true),
            new(
                Language.GetTextValue("Bestiary_Biomes.TheCorruption"),
                false,
                null,
                null,
                static player => player.ZoneCorrupt = true),
            new(
                Language.GetTextValue("Bestiary_Biomes.Crimson"),
                false,
                null,
                null,
                static player => player.ZoneCrimson = true),
            new(
                Language.GetTextValue("Bestiary_Biomes.TheHallow"),
                false,
                null,
                null,
                static player => player.ZoneHallow = true),
            new(
                Language.GetTextValue("Bestiary_Biomes.TheDungeon"),
                false,
                null,
                null,
                static player => player.ZoneDungeon = true),
            new(
                Language.GetTextValue("Bestiary_Biomes.UndergroundMushroom"),
                false,
                null,
                null,
                static player => player.ZoneGlowshroom = true)
        };

        var relevantMods = GetRelevantModNames();
        var modBiomes = ModContent.GetContent<ModBiome>()
            .Where(biome => relevantMods.Count == 0 || relevantMods.Contains(biome.Mod.Name))
            .OrderBy(static biome => biome.FullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        environments.AddRange(modBiomes.Select(static biome => new ProbeEnvironment(
            biome.DisplayName.Value,
            false,
            biome,
            biome.WaterStyle,
            static _ => { })));

        var representedWaterStyles = modBiomes
            .Select(static biome => biome.WaterStyle)
            .Where(static waterStyle => waterStyle is not null)
            .Select(static style => style!.Slot)
            .ToHashSet();
        environments.AddRange(ModContent.GetContent<ModWaterStyle>()
            .Where(style => relevantMods.Count == 0 || relevantMods.Contains(style.Mod.Name))
            .Where(style => !representedWaterStyles.Contains(style.Slot))
            .OrderBy(static style => style.FullName, StringComparer.OrdinalIgnoreCase)
            .Select(static waterStyle => new ProbeEnvironment(
                waterStyle.FullName,
                false,
                null,
                waterStyle,
                static _ => { })));

        return environments;
    }

    private static ProbeEquipment[] CreateEquipment()
    {
        const short defaultPole = ItemID.GoldenFishingRod;
        const short defaultBait = ItemID.MasterBait;
        var relevantMods = GetRelevantModNames();
        var poles = new List<int> { defaultPole };
        var baits = new List<int> { defaultBait };

        for (var itemId = ItemID.Count; itemId < ItemLoader.ItemCount; itemId++)
        {
            var item = ContentSamples.ItemsByType[itemId];
            var modName = item.ModItem?.Mod.Name;
            if (modName is null || (relevantMods.Count > 0 && !relevantMods.Contains(modName)))
            {
                continue;
            }

            if (item.fishingPole > 0)
            {
                poles.Add(itemId);
            }

            if (item.bait > 0)
            {
                baits.Add(itemId);
            }
        }

        var result = new List<ProbeEquipment>
        {
            new(defaultPole, defaultBait, DefaultRandomSeedCount)
        };
        result.AddRange(poles
            .Skip(1)
            .Select(pole => new ProbeEquipment(pole, defaultBait, EquipmentRandomSeedCount)));
        result.AddRange(baits
            .Skip(1)
            .Select(bait => new ProbeEquipment(defaultPole, bait, EquipmentRandomSeedCount)));

        return result.Distinct().ToArray();
    }

    private static HashSet<string> GetRelevantModNames()
    {
        var profile = JournalRuntimeProgressionScenarios.CurrentProfile;
        if (profile is null)
        {
            return [];
        }

        return profile.Document.RequiredMods
            .Select(static requirement => requirement.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static void ProbeContextDrops(
        FishingCatalog catalog,
        FishingPipeline pipeline,
        Player player,
        Projectile projectile,
        ProbeContext context,
        int randomSeedCount)
    {
        var equipment = catalog.Equipment[context.EquipmentIndex];

        foreach (var fishingLevel in FishingLevels)
        {
            for (var seed = 0; seed < randomSeedCount; seed++)
            {
                Main.rand = new UnifiedRandom(seed);
                var attempt = CreateAttempt(catalog, player, projectile, context, fishingLevel, equipment);
                InvokeRollDropLevels(pipeline.RollDropLevels, projectile, fishingLevel, ref attempt);
                RunAttempt(ref attempt, includeEnemySpawns: true);
            }

            for (var rarity = 0; rarity < 6; rarity++)
            {
                for (var seed = 0; seed < Math.Min(randomSeedCount, ForcedRaritySeedCount); seed++)
                {
                    Main.rand = new UnifiedRandom(seed);
                    var attempt = CreateAttempt(catalog, player, projectile, context, fishingLevel, equipment);
                    attempt.common = rarity == 0;
                    attempt.uncommon = rarity == 1;
                    attempt.rare = rarity == 2;
                    attempt.veryrare = rarity == 3;
                    attempt.legendary = rarity == 4;
                    attempt.crate = rarity == 5;
                    RunAttempt(ref attempt, includeEnemySpawns: false);
                }
            }
        }

        return;

        void RunAttempt(ref FishingAttempt attempt, bool includeEnemySpawns)
        {
            try
            {
                PlayerLoader.ModifyFishingAttempt(player, ref attempt);
                if (includeEnemySpawns)
                {
                    InvokeFishingAttempt(pipeline.RollEnemySpawns, projectile, ref attempt);
                }

                InvokeFishingAttempt(pipeline.RollItemDrop, projectile, ref attempt);

                var itemDrop = attempt.rolledItemDrop;
                var npcSpawn = attempt.rolledEnemySpawn;
                var sonar = new AdvancedPopupRequest();
                var sonarPosition = projectile.position;
                PlayerLoader.CatchFish(
                    player,
                    attempt,
                    ref itemDrop,
                    ref npcSpawn,
                    ref sonar,
                    ref sonarPosition);

                AddCaughtItem(catalog.ItemContexts, player, itemDrop, context);
                AddContext(catalog.NpcContexts, npcSpawn, context);
            }
            catch (Exception exception)
            {
                LogDebugOnce(
                    "fishing-attempt",
                    $"Failed to probe fishing context {context} at fishing level {attempt.fishingLevel} "
                    + $"with pole {equipment.PoleItemId}, bait {equipment.BaitItemId}, "
                    + $"and enemy-spawn probing set to {includeEnemySpawns}.",
                    exception);
            }
        }
    }

    private static void InvokeRollDropLevels(
        MethodInfo method,
        Projectile projectile,
        int fishingLevel,
        ref FishingAttempt attempt)
    {
        var parameters = method.GetParameters();
        var arguments = new object?[parameters.Length];
        var boolValues = new Queue<bool>(
        [
            attempt.common,
            attempt.uncommon,
            attempt.rare,
            attempt.veryrare,
            attempt.legendary,
            attempt.crate
        ]);
        for (var index = 0; index < parameters.Length; index++)
        {
            var type = parameters[index].ParameterType;
            arguments[index] = type == typeof(int)
                ? fishingLevel
                : type == typeof(bool).MakeByRefType() && boolValues.TryDequeue(out var value)
                    ? value
                    : type == typeof(Projectile)
                        ? projectile
                        : GetDefaultValue(parameters[index]);
        }

        method.Invoke(method.IsStatic ? null : projectile, arguments);
        var values = arguments
            .Where((_, index) => parameters[index].ParameterType == typeof(bool).MakeByRefType())
            .Select(static value => value is true)
            .ToArray();
        if (values.Length < 6) return;
        attempt.common = values[0];
        attempt.uncommon = values[1];
        attempt.rare = values[2];
        attempt.veryrare = values[3];
        attempt.legendary = values[4];
        attempt.crate = values[5];
    }

    private static void InvokeFishingAttempt(
        MethodInfo method,
        Projectile projectile,
        ref FishingAttempt attempt)
    {
        var parameters = method.GetParameters();
        var arguments = new object?[parameters.Length];
        for (var index = 0; index < parameters.Length; index++)
        {
            arguments[index] =
                parameters[index].ParameterType == typeof(FishingAttempt).MakeByRefType()
                    ? attempt
                    : parameters[index].ParameterType == typeof(Projectile)
                        ? projectile
                        : GetDefaultValue(parameters[index]);
        }

        method.Invoke(method.IsStatic ? null : projectile, arguments);
        var attemptIndex = Array.FindIndex(
            parameters,
            static parameter =>
                parameter.ParameterType == typeof(FishingAttempt).MakeByRefType());
        if (attemptIndex >= 0 && arguments[attemptIndex] is FishingAttempt updated)
        {
            attempt = updated;
        }
    }

    private static object? GetDefaultValue(ParameterInfo parameter)
    {
        if (parameter.HasDefaultValue)
        {
            return parameter.DefaultValue;
        }

        var type = parameter.ParameterType.IsByRef
            ? parameter.ParameterType.GetElementType()
            : parameter.ParameterType;
        return type is { IsValueType: true } ? Activator.CreateInstance(type) : null;
    }

    private static FishingAttempt CreateAttempt(
        FishingCatalog catalog,
        Player player,
        Projectile projectile,
        ProbeContext context,
        int fishingLevel,
        ProbeEquipment equipment)
    {
        var y = context.Depth switch
        {
            0 => Math.Max(10, (int)(Main.worldSurface * 0.25)),
            1 => Math.Max(10, (int)(Main.worldSurface * 0.75)),
            2 => Math.Max(10, (int)((Main.worldSurface + Main.rockLayer) * 0.5)),
            3 => Math.Min(Main.maxTilesY - 10, (int)Main.rockLayer + 100),
            _ => Main.maxTilesY - 100
        };
        var environment = catalog.Environments[context.EnvironmentIndex];
        var x = environment.IsOcean
            ? Math.Min(200, Main.maxTilesX / 10)
            : Main.maxTilesX / 2;
        player.Center = new Vector2(x * 16f, y * 16f);
        projectile.Center = player.Center;

        return new FishingAttempt
        {
            playerFishingConditions = new PlayerFishingConditions
            {
                FinalFishingLevel = fishingLevel,
                LevelMultipliers = 1f,
                Pole = CreateItem(equipment.PoleItemId),
                Bait = CreateItem(equipment.BaitItemId)
            },
            X = x,
            Y = y,
            bobberType = ProjectileID.BobberGolden,
            inLava = context.Liquid == ProbeLiquid.Lava,
            inHoney = context.Liquid == ProbeLiquid.Honey,
            CanFishInLava = true,
            waterTilesCount = 1001,
            waterNeededToFish = 300,
            fishingLevel = fishingLevel,
            questFish = -1,
            heightLevel = GetHeightLevel(y)
        };
    }

    private static Item CreateItem(int itemId)
    {
        var item = new Item();
        item.SetDefaults(itemId);
        item.stack = 1;
        return item;
    }

    private static void AddCaughtItem(
        Dictionary<int, HashSet<ProbeContext>> catalog,
        Player player,
        int itemId,
        ProbeContext context)
    {
        if (!JournalItemUtilities.IsValidItemId(itemId))
        {
            return;
        }

        var item = CreateItem(itemId);
        try
        {
            PlayerLoader.ModifyCaughtFish(player, item);
        }
        catch (Exception exception)
        {
            LogDebugOnce(
                "modify-caught-fish",
                $"Failed to apply ModifyCaughtFish while probing caught item {itemId} in fishing context {context}.",
                exception);
            return;
        }

        if (JournalItemUtilities.IsValidItemId(item.type))
        {
            AddContext(catalog, item.type, context);
        }
    }

    private static void AddContext(
        Dictionary<int, HashSet<ProbeContext>> catalog,
        int contentId,
        ProbeContext context)
    {
        if (contentId <= 0)
        {
            return;
        }

        if (!catalog.TryGetValue(contentId, out var contexts))
        {
            contexts = [];
            catalog[contentId] = contexts;
        }

        contexts.Add(context);
    }

    private static int GetHeightLevel(int y)
    {
        if (y < Main.worldSurface * 0.5)
        {
            return 0;
        }

        if (y < Main.worldSurface)
        {
            return 1;
        }

        if (!Main.remixWorld)
        {
            return y < Main.rockLayer
                ? 2
                : y < Main.maxTilesY - 300
                    ? 3
                    : 4;
        }

        if (y < Main.rockLayer)
        {
            return 3;
        }

        if (y >= Main.maxTilesY - 300)
        {
            return 4;
        }

        return Main.rand.NextBool(2) ? 1 : 2;
    }

    private static List<string> BuildConditions(
        FishingCatalog catalog,
        IReadOnlyCollection<ProbeContext> contexts)
    {
        var conditions = new List<string>();
        var liquids = contexts.Select(static context => context.Liquid).Distinct().Order().ToArray();
        var environmentIndexes = contexts
            .Select(static context => context.EnvironmentIndex)
            .Distinct()
            .Order()
            .ToArray();
        var depths = contexts.Select(static context => context.Depth).Distinct().Order().ToArray();
        var worlds = contexts
            .Select(static context => Worlds[context.WorldIndex])
            .Distinct()
            .ToArray();
        var progressionIndexes = contexts
            .Select(context => GetEvidenceProgressionIndex(catalog, context))
            .Where(static index => index >= 0)
            .Distinct()
            .Order()
            .ToArray();

        if (liquids.Length < Enum.GetValues<ProbeLiquid>().Length)
        {
            conditions.Add(Language.GetTextValue(
                "Mods.ProgressionJournal.UI.FishingLiquidCondition",
                string.Join(", ", liquids.Select(GetLiquidName))));
        }

        if (environmentIndexes.Length < catalog.Environments.Count)
        {
            conditions.Add(Language.GetTextValue(
                "Mods.ProgressionJournal.UI.FishingBiomeCondition",
                string.Join(
                    ", ",
                    environmentIndexes.Select(index => catalog.Environments[index].DisplayName))));
        }

        var waterStyles = environmentIndexes
            .Select(index => catalog.Environments[index].WaterStyle)
            .Where(static waterStyle => waterStyle is not null)
            .Select(static style => style!.FullName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (waterStyles.Length > 0
            && environmentIndexes.All(index => catalog.Environments[index].WaterStyle is not null))
        {
            conditions.Add(Language.GetTextValue(
                "Mods.ProgressionJournal.UI.FishingWaterStyleCondition",
                string.Join(", ", waterStyles)));
        }

        if (depths.Length < DepthLocalizationKeys.Length)
        {
            conditions.Add(Language.GetTextValue(
                "Mods.ProgressionJournal.UI.FishingDepthCondition",
                string.Join(", ", depths.Select(static depth => Language.GetTextValue(DepthLocalizationKeys[depth])))));
        }

        AppendProgressionCondition(conditions, catalog, progressionIndexes);
        AppendWorldConditions(conditions, worlds);
        return conditions;
    }

    private static ProbeWorld GetEffectiveWorld(
        FishingCatalog catalog,
        ProbeContext context)
    {
        var world = Worlds[context.WorldIndex];
        var progressionIndex = GetEffectiveProgressionIndex(catalog, context);
        if (progressionIndex < 0 || progressionIndex >= catalog.Progression.Count)
        {
            return world;
        }

        var origin = catalog.Progression[context.ProgressionIndex]
            .Variants[context.ProgressionVariantIndex];
        var progression = catalog.Progression[progressionIndex].Variants
            .FirstOrDefault(variant =>
                IsVariantContinuation(origin, variant)
                && SatisfiesWorld(world, variant));
        if (progression is null)
        {
            return world;
        }

        return world with
        {
            Hardmode = world.Hardmode || progression.Hardmode,
            DownedSkeletron = world.DownedSkeletron || progression.DownedSkeletron,
            CombatBookUsed = world.CombatBookUsed || progression.CombatBookUsed,
            UnlockedSlimeRed = world.UnlockedSlimeRed || progression.UnlockedSlimeRed
        };
    }

    private static int GetEffectiveProgressionIndex(
        FishingCatalog catalog,
        ProbeContext context)
    {
        var world = Worlds[context.WorldIndex];
        var origin = catalog.Progression[context.ProgressionIndex]
            .Variants[context.ProgressionVariantIndex];
        for (var index = context.ProgressionIndex; index < catalog.Progression.Count; index++)
        {
            if (catalog.Progression[index].Variants.Any(variant =>
                    IsVariantContinuation(origin, variant)
                    && SatisfiesWorld(world, variant)))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool IsVariantContinuation(
        ProbeProgressionVariant earlier,
        ProbeProgressionVariant current)
    {
        return earlier.ConditionKeys.IsSubsetOf(current.ConditionKeys);
    }

    private static bool SatisfiesWorld(ProbeWorld world, ProbeProgressionVariant progression)
    {
        return (!world.Hardmode || progression.Hardmode)
            && (!world.DownedSkeletron || progression.DownedSkeletron)
            && (!world.CombatBookUsed || progression.CombatBookUsed)
            && (!world.UnlockedSlimeRed || progression.UnlockedSlimeRed);
    }

    private static int GetEvidenceProgressionIndex(
        FishingCatalog catalog,
        ProbeContext context)
    {
        var hasSyntheticModEnvironment = catalog.Environments[context.EnvironmentIndex].ModBiome is not null;
        return hasSyntheticModEnvironment
            ? -1
            : GetEffectiveProgressionIndex(catalog, context);
    }

    private static void AppendProgressionCondition(
        List<string> conditions,
        FishingCatalog catalog,
        IReadOnlyCollection<int> progressionIndexes)
    {
        if (catalog.Progression.Count <= 1
            || progressionIndexes.Count == 0
            || progressionIndexes.Contains(0))
        {
            return;
        }

        var firstIndex = progressionIndexes.Min();
        var stageName = catalog.Progression[firstIndex].DisplayName;
        if (!string.IsNullOrWhiteSpace(stageName))
        {
            conditions.Add(Language.GetTextValue(
                "Mods.ProgressionJournal.UI.FishingProgressionCondition",
                stageName));
        }
    }

    private static void AppendWorldConditions(
        List<string> conditions,
        IReadOnlyCollection<ProbeWorld> worlds)
    {
        var worldConditions = new List<string>();

        if (worlds.All(static world => world.Hardmode))
        {
            worldConditions.Add(Language.GetTextValue("Mods.ProgressionJournal.UI.FishingWorldHardmode"));
        }
        else if (worlds.All(static world => !world.Hardmode))
        {
            worldConditions.Add(Language.GetTextValue("Mods.ProgressionJournal.UI.FishingWorldPreHardmode"));
        }

        if (worlds.All(static world => world.BloodMoon))
        {
            worldConditions.Add(Language.GetTextValue("Mods.ProgressionJournal.UI.FishingWorldBloodMoon"));
        }

        if (worlds.All(static world => world.DayTime))
        {
            worldConditions.Add(Language.GetTextValue("Mods.ProgressionJournal.UI.FishingWorldDay"));
        }
        else if (worlds.All(static world => !world.DayTime))
        {
            worldConditions.Add(Language.GetTextValue("Mods.ProgressionJournal.UI.FishingWorldNight"));
        }

        if (worlds.All(static world => world.DownedSkeletron))
        {
            worldConditions.Add(Language.GetTextValue("Mods.ProgressionJournal.UI.FishingWorldPostSkeletron"));
        }

        if (worlds.All(static world => world.CombatBookUsed))
        {
            worldConditions.Add(Language.GetTextValue("Mods.ProgressionJournal.UI.FishingWorldCombatBook"));
        }

        if (worlds.All(static world => world.Remix))
        {
            worldConditions.Add(Language.GetTextValue("Mods.ProgressionJournal.UI.FishingWorldRemix"));
        }

        if (worlds.All(static world => world.NotTheBees))
        {
            worldConditions.Add(Language.GetTextValue("Mods.ProgressionJournal.UI.FishingWorldNotTheBees"));
        }

        if (worldConditions.Count > 0)
        {
            conditions.Add(Language.GetTextValue(
                "Mods.ProgressionJournal.UI.FishingWorldCondition",
                string.Join(", ", worldConditions)));
        }
    }

    private static string GetLiquidName(ProbeLiquid liquid)
    {
        var key = liquid switch
        {
            ProbeLiquid.Water => "FishingLiquidWater",
            ProbeLiquid.Lava => "FishingLiquidLava",
            ProbeLiquid.Honey => "FishingLiquidHoney",
            _ => throw new ArgumentOutOfRangeException(nameof(liquid), liquid, null)
        };
        return Language.GetTextValue($"Mods.ProgressionJournal.UI.{key}");
    }

    private static Player? GetProbePlayer()
    {
        if (Main.myPlayer < 0 || Main.myPlayer >= Main.player.Length)
            return Main.player.FirstOrDefault(static player => player is { active: true });
        var localPlayer = Main.LocalPlayer;
        return localPlayer is { active: true } ? localPlayer : Main.player.FirstOrDefault(static player => player is { active: true });
    }

    private static void ApplyWorld(ProbeWorld world)
    {
        Main.hardMode = world.Hardmode;
        Main.bloodMoon = world.BloodMoon;
        Main.dayTime = world.DayTime;
        Main.remixWorld = world.Remix;
        Main.notTheBeesWorld = world.NotTheBees;
        NPC.downedBoss3 = world.DownedSkeletron;
        NPC.combatBookWasUsed = world.CombatBookUsed;
        NPC.unlockedSlimeRedSpawn = world.UnlockedSlimeRed;
    }

    private static void ApplyEnvironment(Player player, ProbeEnvironment environment)
    {
        ResetVanillaBiomes(player);
        var modBiomeFlags = GetModBiomeFlags(player);
        modBiomeFlags.SetAll(false);
        environment.ApplyVanillaBiome(player);
        var modBiomeIndex = environment.ModBiome is null
            ? -1
            : GetModBiomeIndex(environment.ModBiome);
        if (modBiomeIndex >= 0 && modBiomeIndex < modBiomeFlags.Length)
        {
            modBiomeFlags[modBiomeIndex] = true;
        }

        Main.waterStyle = environment.WaterStyle?.Slot ?? 0;
    }

    private static PlayerState CapturePlayerState(Player player)
    {
        return new PlayerState(
            player.position,
            player.velocity,
            player.ZoneBeach,
            player.ZoneDesert,
            player.ZoneSnow,
            player.ZoneJungle,
            player.ZoneCorrupt,
            player.ZoneCrimson,
            player.ZoneHallow,
            player.ZoneDungeon,
            player.ZoneGlowshroom,
            (BitArray)GetModBiomeFlags(player).Clone());
    }

    private static void RestorePlayerState(Player player, PlayerState state)
    {
        player.position = state.Position;
        player.velocity = state.Velocity;
        player.ZoneBeach = state.Beach;
        player.ZoneDesert = state.Desert;
        player.ZoneSnow = state.Snow;
        player.ZoneJungle = state.Jungle;
        player.ZoneCorrupt = state.Corrupt;
        player.ZoneCrimson = state.Crimson;
        player.ZoneHallow = state.Hallow;
        player.ZoneDungeon = state.Dungeon;
        player.ZoneGlowshroom = state.Glowshroom;
        ModBiomeFlagsField?.SetValue(player, (BitArray)state.ModBiomeFlags.Clone());
    }

    private static void ResetVanillaBiomes(Player player)
    {
        player.ZoneBeach = false;
        player.ZoneDesert = false;
        player.ZoneSnow = false;
        player.ZoneJungle = false;
        player.ZoneCorrupt = false;
        player.ZoneCrimson = false;
        player.ZoneHallow = false;
        player.ZoneDungeon = false;
        player.ZoneGlowshroom = false;
    }

    private static BitArray GetModBiomeFlags(Player player)
    {
        return ModBiomeFlagsField?.GetValue(player) as BitArray ?? new BitArray(0);
    }

    private static int GetModBiomeIndex(ModBiome biome)
    {
        return ModBiomeIndexProperty?.GetValue(biome) is int index ? index : -1;
    }

    private static void LogDebugOnce(string operation, string message, Exception exception)
    {
        var key = $"{operation}:{exception.GetType().FullName}:{exception.Message}";
        lock (SyncRoot)
        {
            if (!LoggedProbeFailures.Add(key))
            {
                return;
            }
        }

        ProgressionJournal.Instance?.Logger.Debug($"{message}{Environment.NewLine}{exception}");
    }

}
