using System.Collections;
using System.Reflection;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Creative;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace ProgressionJournal.Data.Resolvers;

internal static class JournalTownNpcAvailabilityResolver
{
    private static readonly object SyncRoot = new();
    private static readonly HashSet<string> LoggedProbeFailures = new(StringComparer.Ordinal);
    private static readonly FieldInfo? ModBiomeFlagsField = typeof(Player).GetField(
        "modBiomeFlags",
        BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly PropertyInfo? ModBiomeIndexProperty = typeof(ModBiome).GetProperty(
        "ZeroIndexType",
        BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly int[] VanillaTownSpawnTypes =
    [
        17, 18, 19, 20, 22, 38, 54, 107, 108, 124, 142, 160, 178, 207, 208,
        209, 227, 228, 229, 353, 369, 441, 550, 588, 633, 637, 638, 656, 663,
        670, 678, 679, 680, 681, 682, 683, 684
    ];
    private static Catalog? _catalog;
    private static string _catalogKey = string.Empty;

    private sealed record InventoryScenario(string Name, IReadOnlyList<int> Items, bool HasLifeCrystal);

    private sealed record TownProbeScenario(
        int StageIndex,
        bool SpecialUnlocks,
        bool SpecialWorld,
        bool Inventory,
        bool TownPopulation,
        InventoryScenario InventoryScenario,
        int ProgressionVariantIndex = -1);

    private sealed record ShopKey(int NpcType, string ShopName, int ItemId);

    private sealed record Catalog(
        Dictionary<int, JournalTownNpcAvailability> Npcs,
        Dictionary<ShopKey, int> ShopStages,
        IReadOnlyList<string> StageNames);

    private readonly record struct PlayerState(
        bool Active,
        int StatLifeMax,
        int StatLifeMax2,
        Item[] Inventory,
        Item[] Dye,
        Item[] MiscDyes,
        bool Beach,
        bool Desert,
        bool Snow,
        bool Jungle,
        bool Corrupt,
        bool Crimson,
        bool Hallow,
        bool Dungeon,
        bool Glowshroom,
        bool Graveyard,
        bool AdjWater,
        bool AdjLava,
        bool AdjHoney,
        BitArray ModBiomeFlags);

    private sealed class StaticBooleanFlag(FieldInfo field)
    {
        private readonly bool _originalValue = (bool)(field.GetValue(null) ?? false);

        public string Name { get; } = field.Name;

        public void Set(bool value) => field.SetValue(null, value);

        public void Restore() => field.SetValue(null, _originalValue);
    }

    private sealed record StaticFieldState(FieldInfo Field, object? Value);

    public static JournalTownNpcAvailability GetAvailability(int npcType)
    {
        var catalog = GetCatalog();
        return catalog.Npcs.TryGetValue(npcType, out var availability)
            ? availability
            : new JournalTownNpcAvailability(
                npcType,
                observed: false,
                earliestStageIndex: -1,
                earliestStageName: string.Empty,
                requiresSpecialUnlock: false,
                requiresInventory: false,
                requiresTownPopulation: false);
    }

    public static IReadOnlyList<string> GetShopConditions(int npcType, string shopName, int itemId)
    {
        var catalog = GetCatalog();
        if (!catalog.Npcs.TryGetValue(npcType, out var availability))
        {
            return [];
        }

        var conditions = BuildAvailabilityConditions(availability);
        var key = new ShopKey(npcType, shopName, itemId);
        if (catalog.ShopStages.TryGetValue(key, out var shopStageIndex)
            && shopStageIndex > availability.EarliestStageIndex
            && shopStageIndex >= 0
            && shopStageIndex < catalog.StageNames.Count)
        {
            conditions.Add(Language.GetTextValue(
                "Mods.ProgressionJournal.UI.ShopObservedAfterCondition",
                catalog.StageNames[shopStageIndex]));
        }

        return conditions;
    }

    internal static bool TryGetShopStage(
        int npcType,
        string shopName,
        int itemId,
        out int stageIndex,
        out string stageName)
    {
        var catalog = GetCatalog();
        if (catalog.ShopStages.TryGetValue(new ShopKey(npcType, shopName, itemId), out stageIndex))
        {
            stageName = stageIndex >= 0 && stageIndex < catalog.StageNames.Count
                ? catalog.StageNames[stageIndex]
                : string.Empty;
            return true;
        }

        stageIndex = -1;
        stageName = string.Empty;
        return false;
    }

    private static Catalog GetCatalog()
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
        return $"{Main.worldID}:{profileId}:{mods}";
    }

    private static Catalog BuildCatalog()
    {
        var updateTownNpcAvailability = GetTownNpcAvailabilityMethod();
        var player = GetProbePlayer();
        using var progression = new JournalRuntimeProgressionScenarios();
        var stageNames = progression.StageNames;
        var emptyCatalog = new Catalog([], [], stageNames);
        if (updateTownNpcAvailability is null || player is null)
        {
            return emptyCatalog;
        }

        var townNpcTypes = EnumerateTownNpcTypes();
        var inventoryScenarios = CreateInventoryScenarios();
        var observations = new Dictionary<int, List<TownProbeScenario>>();
        var originalNpcReferences = Main.npc.ToArray();
        var originalPlayerReferences = Main.player.ToArray();
        var playerState = CapturePlayerState(player);
        var previousTownNpcCanSpawn = Main.townNPCCanSpawn;
        var previousCheckForSpawns = Main.checkForSpawns;
        var previousPrioritizedTownNpcType = WorldGen.prioritizedTownNPCType;
        var previousNetMode = Main.netMode;
        var previousDesiredWorldTilesUpdateRate = Main.desiredWorldTilesUpdateRate;
        var previousBestiaryTracker = Main.BestiaryTracker;
        var previousDayTime = Main.dayTime;
        var previousTime = Main.time;
        var previousBloodMoon = Main.bloodMoon;
        var previousEclipse = Main.eclipse;
        var previousRaining = Main.raining;
        var previousInvasionType = Main.invasionType;
        var previousInvasionSize = Main.invasionSize;
        var previousInvasionDelay = Main.invasionDelay;
        var previousMoonPhase = Main.moonPhase;
        var previousRandom = Main.rand;
        var previousXMas = Main.xMas;
        var previousHalloween = Main.halloween;
        var previousTenthAnniversaryWorld = Main.tenthAnniversaryWorld;
        var previousRemixWorld = Main.remixWorld;
        var previousGenuineParty = BirthdayParty.GenuineParty;
        var freezeTime = CreativePowerManager.Instance.GetPower<CreativePowers.FreezeTime>();
        var previousFreezeTime = freezeTime.Enabled;
        var specialFlags = CreateSpecialUnlockFlags();

        try
        {
            PreparePlayers(player);
            Main.netMode = NetmodeID.SinglePlayer;
            Main.desiredWorldTilesUpdateRate = 1d;
            freezeTime.SetPowerInfo(false);
            Main.townNPCCanSpawn = new bool[Math.Max(NPCLoader.NPCCount, Main.townNPCCanSpawn.Length)];

            for (var stageIndex = 0; stageIndex < progression.Count; stageIndex++)
            {
                RunTownScenario(
                    updateTownNpcAvailability,
                    progression,
                    player,
                    townNpcTypes,
                    observations,
                    new TownProbeScenario(
                        stageIndex,
                        SpecialUnlocks: false,
                        SpecialWorld: false,
                        Inventory: false,
                        TownPopulation: false,
                        inventoryScenarios[0]),
                    specialFlags,
                    useMaxBestiary: false);

                foreach (var inventoryScenario in inventoryScenarios.Skip(1))
                {
                    RunTownScenario(
                        updateTownNpcAvailability,
                        progression,
                        player,
                        townNpcTypes,
                        observations,
                        new TownProbeScenario(
                            stageIndex,
                            SpecialUnlocks: false,
                            SpecialWorld: false,
                            Inventory: true,
                            TownPopulation: false,
                            inventoryScenario),
                        specialFlags,
                        useMaxBestiary: false);
                }

                RunTownScenario(
                    updateTownNpcAvailability,
                    progression,
                    player,
                    townNpcTypes,
                    observations,
                    new TownProbeScenario(
                        stageIndex,
                        SpecialUnlocks: true,
                        SpecialWorld: true,
                        Inventory: true,
                        TownPopulation: false,
                        inventoryScenarios[1]),
                    specialFlags,
                    useMaxBestiary: true);

                RunTownScenario(
                    updateTownNpcAvailability,
                    progression,
                    player,
                    townNpcTypes,
                    observations,
                    new TownProbeScenario(
                        stageIndex,
                        SpecialUnlocks: true,
                        SpecialWorld: true,
                        Inventory: true,
                        TownPopulation: true,
                        inventoryScenarios[1]),
                    specialFlags,
                    useMaxBestiary: true);
            }

            ObserveTransientTownNpcSystems(
                progression,
                player,
                townNpcTypes,
                observations,
                inventoryScenarios[1]);

            var availability = townNpcTypes.ToDictionary(
                static npcType => npcType,
                npcType => CreateAvailability(npcType, observations, stageNames));
            var shopStages = BuildShopStages(
                progression,
                player,
                observations,
                inventoryScenarios);
            return new Catalog(availability, shopStages, stageNames);
        }
        catch (Exception exception)
        {
            LogDebugOnce(
                "catalog",
                $"Failed to build the runtime town NPC availability catalog for profile '{JournalRuntimeProgressionScenarios.CurrentProfile?.Id ?? "<none>"}'.",
                exception);
            return emptyCatalog;
        }
        finally
        {
            for (var index = 0; index < Main.npc.Length && index < originalNpcReferences.Length; index++)
            {
                Main.npc[index] = originalNpcReferences[index];
            }

            for (var index = 0; index < Main.player.Length && index < originalPlayerReferences.Length; index++)
            {
                Main.player[index] = originalPlayerReferences[index];
            }

            RestorePlayerState(player, playerState);
            Main.townNPCCanSpawn = previousTownNpcCanSpawn;
            Main.checkForSpawns = previousCheckForSpawns;
            WorldGen.prioritizedTownNPCType = previousPrioritizedTownNpcType;
            Main.netMode = previousNetMode;
            Main.desiredWorldTilesUpdateRate = previousDesiredWorldTilesUpdateRate;
            Main.BestiaryTracker = previousBestiaryTracker;
            Main.dayTime = previousDayTime;
            Main.time = previousTime;
            Main.bloodMoon = previousBloodMoon;
            Main.eclipse = previousEclipse;
            Main.raining = previousRaining;
            Main.invasionType = previousInvasionType;
            Main.invasionSize = previousInvasionSize;
            Main.invasionDelay = previousInvasionDelay;
            Main.moonPhase = previousMoonPhase;
            Main.rand = previousRandom;
            Main.xMas = previousXMas;
            Main.halloween = previousHalloween;
            Main.tenthAnniversaryWorld = previousTenthAnniversaryWorld;
            Main.remixWorld = previousRemixWorld;
            BirthdayParty.GenuineParty = previousGenuineParty;
            freezeTime.SetPowerInfo(previousFreezeTime);
            foreach (var flag in specialFlags)
            {
                flag.Restore();
            }
        }
    }

    private static void ObserveTransientTownNpcSystems(
        JournalRuntimeProgressionScenarios progression,
        Player player,
        IReadOnlyCollection<int> townNpcTypes,
        IDictionary<int, List<TownProbeScenario>> observations,
        InventoryScenario inventoryScenario)
    {
        var profile = JournalRuntimeProgressionScenarios.CurrentProfile;
        var relevantMods = profile is not null
            ? profile.Document.RequiredMods
                .Select(static requirement => requirement.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : [];
        var systems = ModContent.GetContent<ModSystem>()
            .Where(system => relevantMods.Count == 0 || relevantMods.Contains(system.Mod.Name))
            .Where(static system =>
            {
                var method = system.GetType().GetMethod(
                    nameof(ModSystem.PreUpdateWorld),
                    BindingFlags.Instance | BindingFlags.Public);
                if (method?.DeclaringType == typeof(ModSystem))
                {
                    return false;
                }

                var name = system.GetType().FullName ?? system.GetType().Name;
                return name.Contains("Spawn", StringComparison.OrdinalIgnoreCase)
                       || name.Contains("Town", StringComparison.OrdinalIgnoreCase)
                       || name.Contains("Travel", StringComparison.OrdinalIgnoreCase)
                       || name.Contains("Merchant", StringComparison.OrdinalIgnoreCase)
                       || name.Contains("Salesman", StringComparison.OrdinalIgnoreCase);
            })
            .ToArray();
        if (systems.Length == 0)
        {
            return;
        }

        var knownTownNpcTypes = townNpcTypes.ToHashSet();
        for (var stageIndex = 0; stageIndex < progression.Count; stageIndex++)
        {
            foreach (var system in systems)
            {
                for (var seed = 0; seed < 8; seed++)
                {
                    for (var variantIndex = 0;
                         variantIndex < progression.GetVariantCount(stageIndex);
                         variantIndex++)
                    {
                        progression.Reset();
                        progression.Apply(stageIndex, variantIndex);
                        PrepareTransientTown(
                            player,
                            inventoryScenario,
                            system,
                            townNpcTypes);
                        var staticState = CaptureStaticFieldState(system.GetType());
                        try
                        {
                            Main.eclipse = false;
                            Main.invasionType = 0;
                            Main.invasionSize = 0;
                            Main.invasionDelay = 0;
                            Main.dayTime = true;
                            Main.time = 0d;
                            system.PreUpdateWorld();

                            var beforeTypes = Main.npc
                                .Select(static npc => npc is { active: true } ? npc.type : -1)
                                .ToArray();
                            Main.dayTime = false;
                            Main.time = 0d;
                            Main.rand = new UnifiedRandom(seed);
                            system.PreUpdateWorld();

                            for (var index = 0; index < Main.npc.Length; index++)
                            {
                                var npc = Main.npc[index];
                                if (npc is not { active: true, townNPC: true }
                                    || npc.type == beforeTypes[index]
                                    || !knownTownNpcTypes.Contains(npc.type))
                                {
                                    continue;
                                }

                                if (!observations.TryGetValue(npc.type, out var values))
                                {
                                    values = [];
                                    observations[npc.type] = values;
                                }

                                values.Add(new TownProbeScenario(
                                    stageIndex,
                                    SpecialUnlocks: false,
                                    SpecialWorld: false,
                                    Inventory: true,
                                    TownPopulation: true,
                                    inventoryScenario,
                                    ProgressionVariantIndex: variantIndex));
                            }
                        }
                        catch (Exception exception)
                        {
                            LogDebugOnce(
                                "transient-town-system",
                                $"Failed to probe transient town NPC system '{system.GetType().FullName}' "
                                + $"at stage {stageIndex}, variant {variantIndex}, and seed {seed}.",
                                exception);
                        }
                        finally
                        {
                            RestoreStaticFieldState(staticState);
                        }
                    }
                }
            }
        }
    }

    private static void PrepareTransientTown(
        Player player,
        InventoryScenario inventoryScenario,
        ModSystem system,
        IReadOnlyCollection<int> townNpcTypes)
    {
        PrepareNpcArray(populatedTown: true);
        var slot = Array.FindIndex(Main.npc, static npc => npc is not { active: true });
        if (slot < 0)
        {
            slot = Main.npc.Length;
        }

        var systemName = system.GetType().Name;
        foreach (var npcType in townNpcTypes.Where(static type => type >= NPCID.Count))
        {
            var modNpc = NPCLoader.GetNPC(npcType);
            if (modNpc is null
                || modNpc.Mod != system.Mod
                || systemName.Contains(modNpc.Name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            AddNpc(ref slot, npcType);
        }

        ApplyInventoryScenario(player, inventoryScenario);
        foreach (var npc in Main.npc.Where(static npc => npc is { active: true, townNPC: true }))
        {
            npc.homeless = false;
            npc.homeTileX = Main.spawnTileX;
            npc.homeTileY = Main.spawnTileY;
        }
    }

    private static List<StaticFieldState> CaptureStaticFieldState(Type type)
    {
        const BindingFlags flags =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var result = new List<StaticFieldState>();
        foreach (var field in type.GetFields(flags).Where(static field => !field.IsInitOnly))
        {
            try
            {
                result.Add(new StaticFieldState(field, field.GetValue(null)));
            }
            catch (Exception exception)
            {
                LogDebugOnce(
                    "capture-static-field",
                    $"Failed to capture static field '{field.DeclaringType?.FullName}.{field.Name}' "
                    + "while probing transient town NPC systems.",
                    exception);
            }
        }

        return result;
    }

    private static void RestoreStaticFieldState(IEnumerable<StaticFieldState> states)
    {
        foreach (var state in states)
        {
            try
            {
                state.Field.SetValue(null, state.Value);
            }
            catch (Exception exception)
            {
                LogDebugOnce(
                    "restore-static-field",
                    $"Failed to restore static field '{state.Field.DeclaringType?.FullName}.{state.Field.Name}' "
                    + "after probing transient town NPC systems.",
                    exception);
            }
        }
    }

    private static MethodInfo? GetTownNpcAvailabilityMethod()
    {
        return typeof(Main).GetMethod(
            "UpdateTime_SpawnTownNPCs",
            BindingFlags.Static | BindingFlags.NonPublic);
    }

    private static int[] EnumerateTownNpcTypes()
    {
        return VanillaTownSpawnTypes
            .Concat(Enumerable.Range(
                NPCID.Count,
                Math.Max(0, NPCLoader.NPCCount - NPCID.Count))
                .Where(ContentSamples.NpcsByNetId.ContainsKey)
                .Where(type => ContentSamples.NpcsByNetId[type].townNPC))
            .Where(type => type >= 0 && type < NPCLoader.NPCCount)
            .Distinct()
            .ToArray();
    }

    private static List<InventoryScenario> CreateInventoryScenarios()
    {
        var result = new List<InventoryScenario>
        {
            new("empty", [], false),
            new(
                "vanilla town requirements",
                [
                    ItemID.SilverCoin,
                    ItemID.Musket,
                    ItemID.Bomb,
                    ItemID.StrangePlant1
                ],
                true)
        };
        var profile = JournalRuntimeProgressionScenarios.CurrentProfile;
        var relevantMods = profile is not null
            ? profile.Document.RequiredMods
                .Select(static requirement => requirement.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : [];
        var modItems = Enumerable.Range(ItemID.Count, Math.Max(0, ItemLoader.ItemCount - ItemID.Count))
            .Where(itemId => ItemLoader.GetItem(itemId)?.Mod is { } mod
                             && (relevantMods.Count == 0 || relevantMods.Contains(mod.Name)))
            .ToArray();
        for (var index = 0; index < modItems.Length; index += 58)
        {
            result.Add(new InventoryScenario(
                $"mod item batch {index / 58 + 1}",
                modItems.Skip(index).Take(58).ToArray(),
                true));
        }

        return result;
    }

    private static void RunTownScenario(
        MethodInfo updateTownNpcAvailability,
        JournalRuntimeProgressionScenarios progression,
        Player player,
        IReadOnlyCollection<int> townNpcTypes,
        IDictionary<int, List<TownProbeScenario>> observations,
        TownProbeScenario scenario,
        IReadOnlyCollection<StaticBooleanFlag> specialFlags,
        bool useMaxBestiary)
    {
        for (var variantIndex = 0;
             variantIndex < progression.GetVariantCount(scenario.StageIndex);
             variantIndex++)
        {
            progression.Reset();
            progression.Apply(scenario.StageIndex, variantIndex);
            foreach (var flag in specialFlags)
            {
                flag.Set(scenario.SpecialUnlocks);
            }
            Main.xMas = scenario.SpecialWorld;
            Main.halloween = scenario.SpecialWorld;
            Main.tenthAnniversaryWorld = scenario.SpecialWorld;
            Main.remixWorld = scenario.SpecialWorld;
            BirthdayParty.GenuineParty = scenario.SpecialWorld;

            PrepareNpcArray(scenario.TownPopulation);
            ApplyInventoryScenario(player, scenario.InventoryScenario);
            Main.BestiaryTracker = useMaxBestiary
                ? CreateCompletedBestiaryTracker()
                : new BestiaryUnlocksTracker();
            Array.Fill(Main.townNPCCanSpawn, false);
            Main.checkForSpawns = 7200;
            WorldGen.prioritizedTownNPCType = 0;

            updateTownNpcAvailability.Invoke(null, null);

            foreach (var npcType in townNpcTypes)
            {
                if (npcType >= 0
                    && npcType < Main.townNPCCanSpawn.Length
                    && Main.townNPCCanSpawn[npcType])
                {
                    if (!observations.TryGetValue(npcType, out var values))
                    {
                        values = [];
                        observations[npcType] = values;
                    }

                    values.Add(scenario with { ProgressionVariantIndex = variantIndex });
                }
            }
        }
    }

    private static JournalTownNpcAvailability CreateAvailability(
        int npcType,
        IReadOnlyDictionary<int, List<TownProbeScenario>> observations,
        IReadOnlyList<string> stageNames)
    {
        if (!observations.TryGetValue(npcType, out var values) || values.Count == 0)
        {
            return new JournalTownNpcAvailability(
                npcType,
                observed: false,
                earliestStageIndex: -1,
                earliestStageName: string.Empty,
                requiresSpecialUnlock: false,
                requiresInventory: false,
                requiresTownPopulation: false);
        }

        var stageEvidence = values
            .Where(static value => value is { SpecialUnlocks: false, SpecialWorld: false })
            .ToArray();
        if (stageEvidence.Length == 0)
        {
            var special = values
                .OrderBy(static value => value.StageIndex)
                .ThenBy(static value => value.Inventory)
                .ThenBy(static value => value.TownPopulation)
                .First();
            return new JournalTownNpcAvailability(
                npcType,
                observed: false,
                earliestStageIndex: -1,
                earliestStageName: string.Empty,
                requiresSpecialUnlock: true,
                requiresInventory: special.Inventory,
                requiresTownPopulation: special.TownPopulation);
        }

        var best = stageEvidence
            .OrderBy(static value => value.StageIndex)
            .ThenBy(static value => value.Inventory)
            .ThenBy(static value => value.TownPopulation)
            .First();
        return new JournalTownNpcAvailability(
            npcType,
            observed: true,
            best.StageIndex,
            best.StageIndex >= 0 && best.StageIndex < stageNames.Count
                ? stageNames[best.StageIndex]
                : string.Empty,
            best.SpecialUnlocks || best.SpecialWorld,
            best.Inventory,
            best.TownPopulation);
    }

    private static Dictionary<ShopKey, int> BuildShopStages(
        JournalRuntimeProgressionScenarios progression,
        Player player,
        IReadOnlyDictionary<int, List<TownProbeScenario>> observations,
        IReadOnlyList<InventoryScenario> inventoryScenarios)
    {
        var result = new Dictionary<ShopKey, int>();
        var shops = NPCShopDatabase.AllShops.ToArray();
        var environmentScenarios = CreateShopEnvironmentScenarios();

        for (var stageIndex = 0; stageIndex < progression.Count; stageIndex++)
        {
            for (var variantIndex = 0;
                 variantIndex < progression.GetVariantCount(stageIndex);
                 variantIndex++)
            {
                progression.Reset();
                progression.Apply(stageIndex, variantIndex);

                foreach (var environment in environmentScenarios)
                {
                    ResetShopEnvironment(player);
                    ApplyInventoryScenario(player, inventoryScenarios[1]);
                    environment(player);
                    ObserveShops(
                        shops,
                        observations,
                        progression,
                        result,
                        stageIndex,
                        variantIndex);
                }

                foreach (var inventoryScenario in inventoryScenarios.Skip(2))
                {
                    ResetShopEnvironment(player);
                    ApplyInventoryScenario(player, inventoryScenario);
                    ObserveShops(
                        shops,
                        observations,
                        progression,
                        result,
                        stageIndex,
                        variantIndex);
                }
            }
        }

        return result;
    }

    private static void ObserveShops(
        IReadOnlyCollection<AbstractNPCShop> shops,
        IReadOnlyDictionary<int, List<TownProbeScenario>> observations,
        JournalRuntimeProgressionScenarios progression,
        IDictionary<ShopKey, int> result,
        int stageIndex,
        int variantIndex)
    {
        foreach (var shop in shops)
        {
            if (shop.ActiveEntries.All(entry =>
                    entry.Item is null
                    || entry.Item.IsAir
                    || result.ContainsKey(new ShopKey(
                        shop.NpcType,
                        shop.Name,
                        entry.Item.type))))
            {
                continue;
            }

            if (!HasCompatibleNpcObservation(
                    observations,
                    shop.NpcType,
                    progression,
                    stageIndex,
                    variantIndex))
            {
                continue;
            }

            try
            {
                var npc = new NPC();
                npc.SetDefaults(shop.NpcType);
                npc.active = true;
                var items = new List<Item>();
                shop.FillShop(items, npc);
                foreach (var item in items.Where(static item => item is not null && !item.IsAir))
                {
                    var key = new ShopKey(shop.NpcType, shop.Name, item.type);
                    if (!result.TryGetValue(key, out var existing) || stageIndex < existing)
                    {
                        result[key] = stageIndex;
                    }
                }
            }
            catch (Exception exception)
            {
                LogDebugOnce(
                    "shop",
                    $"Failed to inspect shop '{shop.Name}' for NPC type {shop.NpcType} at stage {stageIndex}.",
                    exception);
            }
        }
    }

    private static bool HasCompatibleNpcObservation(
        IReadOnlyDictionary<int, List<TownProbeScenario>> observations,
        int npcType,
        JournalRuntimeProgressionScenarios progression,
        int stageIndex,
        int variantIndex)
    {
        return observations.TryGetValue(npcType, out var scenarios)
            && scenarios.Any(scenario =>
                scenario is { SpecialUnlocks: false, SpecialWorld: false }
                && scenario.StageIndex <= stageIndex
                && progression.IsVariantContinuation(
                    stageIndex,
                    variantIndex,
                    scenario.StageIndex,
                    scenario.ProgressionVariantIndex));
    }

    private static List<Action<Player>> CreateShopEnvironmentScenarios()
    {
        var scenarios = new List<Action<Player>>
        {
            static _ => { },
            static _ => Main.dayTime = false,
            static _ =>
            {
                Main.dayTime = false;
                Main.bloodMoon = true;
            },
            static _ => Main.eclipse = true,
            static _ => Main.raining = true,
            static _ => Main.xMas = true,
            static _ => Main.halloween = true,
            static _ => Main.tenthAnniversaryWorld = true,
            static _ => Main.remixWorld = true,
            static _ => BirthdayParty.GenuineParty = true,
            static player => player.ZoneBeach = true,
            static player => player.ZoneDesert = true,
            static player => player.ZoneSnow = true,
            static player => player.ZoneJungle = true,
            static player => player.ZoneCorrupt = true,
            static player => player.ZoneCrimson = true,
            static player => player.ZoneHallow = true,
            static player => player.ZoneDungeon = true,
            static player => player.ZoneGlowshroom = true,
            static player => player.ZoneGraveyard = true,
            static player => player.adjWater = true,
            static player => player.adjLava = true,
            static player => player.adjHoney = true
        };

        for (var moonPhase = 0; moonPhase < 8; moonPhase++)
        {
            var capturedMoonPhase = moonPhase;
            scenarios.Add(_ =>
            {
                Main.dayTime = false;
                Main.moonPhase = capturedMoonPhase;
            });
        }

        scenarios.AddRange(from biome in ModContent.GetContent<ModBiome>()
            select GetModBiomeIndex(biome)
            into index
            where index >= 0
            select (Action<Player>)(player =>
            {
                var flags = GetModBiomeFlags(player);
                if (index < flags.Length)
                {
                    flags[index] = true;
                }
            }));

        return scenarios;
    }

    private static List<string> BuildAvailabilityConditions(JournalTownNpcAvailability availability)
    {
        if (!availability.Observed)
        {
            return [];
        }

        var conditions = new List<string>();
        if (availability.EarliestStageIndex > 0
            && !string.IsNullOrWhiteSpace(availability.EarliestStageName))
        {
            conditions.Add(Language.GetTextValue(
                "Mods.ProgressionJournal.UI.TownNpcAvailableAfterCondition",
                availability.EarliestStageName));
        }

        if (availability.RequiresSpecialUnlock)
        {
            conditions.Add(Language.GetTextValue(
                "Mods.ProgressionJournal.UI.TownNpcSpecialUnlockCondition"));
        }

        if (availability.RequiresInventory)
        {
            conditions.Add(Language.GetTextValue(
                "Mods.ProgressionJournal.UI.TownNpcInventoryCondition"));
        }

        if (availability.RequiresTownPopulation)
        {
            conditions.Add(Language.GetTextValue(
                "Mods.ProgressionJournal.UI.TownNpcPopulationCondition"));
        }

        return conditions;
    }

    private static void PreparePlayers(Player probePlayer)
    {
        for (var index = 0; index < Main.player.Length; index++)
        {
            if (index == probePlayer.whoAmI)
            {
                Main.player[index] = probePlayer;
                probePlayer.active = true;
            }
            else
            {
                Main.player[index] = new Player
                {
                    whoAmI = index,
                    active = false
                };
            }
        }
    }

    private static void PrepareNpcArray(bool populatedTown)
    {
        for (var index = 0; index < Main.npc.Length; index++)
        {
            Main.npc[index] = new NPC();
        }

        var slot = 0;
        AddNpc(ref slot, NPCID.OldMan);
        if (!populatedTown)
        {
            return;
        }

        foreach (var npcType in Enumerable.Range(0, NPCID.Count)
                     .Where(ContentSamples.NpcsByNetId.ContainsKey)
                     .Where(type => ContentSamples.NpcsByNetId[type].townNPC)
                     .Where(static type => type is not NPCID.OldMan and not NPCID.Princess))
        {
            AddNpc(ref slot, npcType);
        }
    }

    private static void AddNpc(ref int slot, int npcType)
    {
        if (slot >= Main.npc.Length)
        {
            return;
        }

        var npc = new NPC();
        npc.SetDefaults(npcType);
        npc.active = true;
        npc.townNPC = true;
        npc.homeless = true;
        npc.whoAmI = slot;
        Main.npc[slot++] = npc;
    }

    private static void ApplyInventoryScenario(Player player, InventoryScenario scenario)
    {
        player.inventory = CreateEmptyItems(59);
        player.dye = CreateEmptyItems(10);
        player.miscDyes = CreateEmptyItems(5);
        player.statLifeMax = scenario.HasLifeCrystal ? 120 : 100;
        player.statLifeMax2 = player.statLifeMax;

        for (var index = 0; index < scenario.Items.Count && index < 58; index++)
        {
            var item = new Item();
            item.SetDefaults(scenario.Items[index]);
            item.stack = scenario.Items[index] == ItemID.SilverCoin
                ? 50
                : Math.Max(1, item.maxStack);
            player.inventory[index] = item;
        }
    }

    private static Item[] CreateEmptyItems(int count)
    {
        return Enumerable.Range(0, count).Select(static _ => new Item()).ToArray();
    }

    private static void ResetShopEnvironment(Player player)
    {
        Main.dayTime = true;
        Main.bloodMoon = false;
        Main.eclipse = false;
        Main.raining = false;
        Main.moonPhase = 0;
        Main.xMas = false;
        Main.halloween = false;
        Main.tenthAnniversaryWorld = false;
        Main.remixWorld = false;
        BirthdayParty.GenuineParty = false;
        player.ZoneBeach = false;
        player.ZoneDesert = false;
        player.ZoneSnow = false;
        player.ZoneJungle = false;
        player.ZoneCorrupt = false;
        player.ZoneCrimson = false;
        player.ZoneHallow = false;
        player.ZoneDungeon = false;
        player.ZoneGlowshroom = false;
        player.ZoneGraveyard = false;
        player.adjWater = false;
        player.adjLava = false;
        player.adjHoney = false;
        GetModBiomeFlags(player).SetAll(false);
    }

    private static StaticBooleanFlag[] CreateSpecialUnlockFlags()
    {
        const BindingFlags flags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        return typeof(NPC)
            .GetFields(flags)
            .Where(static field => field.FieldType == typeof(bool) && !field.IsInitOnly)
            .Where(static field =>
                field.Name.StartsWith("saved", StringComparison.Ordinal)
                || field.Name.StartsWith("unlocked", StringComparison.Ordinal)
                || field.Name.StartsWith("bought", StringComparison.Ordinal))
            .Select(static field => new StaticBooleanFlag(field))
            .ToArray();
    }

    private static BestiaryUnlocksTracker CreateCompletedBestiaryTracker()
    {
        var tracker = new BestiaryUnlocksTracker();
        foreach (var creditId in ContentSamples.NpcBestiaryCreditIdsByNpcNetIds.Values
                     .Where(static value => !string.IsNullOrWhiteSpace(value))
                     .Distinct(StringComparer.Ordinal))
        {
            tracker.Kills.SetKillCountDirectly(creditId, 9999);
            tracker.Sights.SetWasSeenDirectly(creditId);
        }

        return tracker;
    }

    private static PlayerState CapturePlayerState(Player player)
    {
        return new PlayerState(
            player.active,
            player.statLifeMax,
            player.statLifeMax2,
            player.inventory,
            player.dye,
            player.miscDyes,
            player.ZoneBeach,
            player.ZoneDesert,
            player.ZoneSnow,
            player.ZoneJungle,
            player.ZoneCorrupt,
            player.ZoneCrimson,
            player.ZoneHallow,
            player.ZoneDungeon,
            player.ZoneGlowshroom,
            player.ZoneGraveyard,
            player.adjWater,
            player.adjLava,
            player.adjHoney,
            (BitArray)GetModBiomeFlags(player).Clone());
    }

    private static void RestorePlayerState(Player player, PlayerState state)
    {
        player.active = state.Active;
        player.statLifeMax = state.StatLifeMax;
        player.statLifeMax2 = state.StatLifeMax2;
        player.inventory = state.Inventory;
        player.dye = state.Dye;
        player.miscDyes = state.MiscDyes;
        player.ZoneBeach = state.Beach;
        player.ZoneDesert = state.Desert;
        player.ZoneSnow = state.Snow;
        player.ZoneJungle = state.Jungle;
        player.ZoneCorrupt = state.Corrupt;
        player.ZoneCrimson = state.Crimson;
        player.ZoneHallow = state.Hallow;
        player.ZoneDungeon = state.Dungeon;
        player.ZoneGlowshroom = state.Glowshroom;
        player.ZoneGraveyard = state.Graveyard;
        player.adjWater = state.AdjWater;
        player.adjLava = state.AdjLava;
        player.adjHoney = state.AdjHoney;
        ModBiomeFlagsField?.SetValue(player, (BitArray)state.ModBiomeFlags.Clone());
    }

    private static Player? GetProbePlayer()
    {
        if (Main.myPlayer < 0 || Main.myPlayer >= Main.player.Length)
            return Main.player.FirstOrDefault(static player => player is { active: true });
        var localPlayer = Main.LocalPlayer;
        return localPlayer is { active: true } ? localPlayer : Main.player.FirstOrDefault(static player => player is { active: true });
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
