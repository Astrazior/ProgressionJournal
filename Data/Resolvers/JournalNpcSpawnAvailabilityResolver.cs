using System.Collections;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace ProgressionJournal.Data.Resolvers;

internal static class JournalNpcSpawnAvailabilityResolver
{
    private const int FullSpawnSeedCount = 48;
    private const int FocusedFullSpawnSeedCount = 192;

    private static readonly object SyncRoot = new();
    private static readonly MethodInfo? PlayerLoaderSetupPlayerMethod = typeof(PlayerLoader).GetMethod(
        "SetupPlayer",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    private static readonly FieldInfo? ModBiomeFlagsField = typeof(Player).GetField(
        "modBiomeFlags",
        BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly PropertyInfo? ModBiomeIndexProperty = typeof(ModBiome).GetProperty(
        "ZeroIndexType",
        BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly MethodInfo? PlayerGetModPlayerMethod = typeof(Player).GetMethods(
            BindingFlags.Instance | BindingFlags.Public)
        .FirstOrDefault(static method => method.Name == nameof(Player.GetModPlayer)
            && method.IsGenericMethodDefinition
            && method.GetGenericArguments().Length == 1
            && method.GetParameters().Length == 0);
    private static readonly MethodInfo? SpawnHelperResetMethod = typeof(NPCLoader).Assembly
        .GetType("Terraria.ModLoader.Utilities.NPCSpawnHelper")
        ?.GetMethod("Reset", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    private static readonly MethodInfo? SpawnHelperDoChecksMethod = typeof(NPCLoader).Assembly
        .GetType("Terraria.ModLoader.Utilities.NPCSpawnHelper")
        ?.GetMethod("DoChecks", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    private static readonly FieldInfo? EditSpawnPoolHookField = typeof(NPCLoader).GetField(
        "HookEditSpawnPool",
        BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly FieldInfo? EditSpawnRateHookField = typeof(NPCLoader).GetField(
        "HookEditSpawnRate",
        BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly FieldInfo? DefaultSpawnRateField = typeof(NPC).GetField(
        "defaultSpawnRate",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    private static readonly FieldInfo? SpawnRateField = typeof(NPC).GetField(
        "spawnRate",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    private static readonly FieldInfo? MaxSpawnsField = typeof(NPC).GetField(
        "maxSpawns",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    private static readonly FieldInfo? NoSpawnCycleField = typeof(NPC).GetField(
        "noSpawnCycle",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    private static readonly MethodInfo? Dd2FindProperDifficultyMethod = typeof(DD2Event).GetMethod(
        "FindProperDifficulty",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    private static readonly MethodInfo? Dd2GetEnemiesForWaveMethod = typeof(DD2Event).GetMethod(
        "GetEnemiesForWave",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    private static Catalog? _catalog;
    private static string _catalogKey = string.Empty;

    private sealed record SpawnEnvironment(
        string Name,
        int TileType,
        ModBiome? ModBiome,
        Action<Player> Apply,
        Func<bool>? IsAvailable = null)
    {
        public int WallType { get; init; }
    }

    private sealed record LegacyBiomeFlag(FieldInfo Field, ModPlayer ModPlayer, bool OriginalValue)
    {
        public void Set(bool value) => Field.SetValue(ModPlayer, value);
    }

    private sealed record LegacyBiomeFlagCatalog(
        Dictionary<int, LegacyBiomeFlag[]> ByEnvironment,
        LegacyBiomeFlag[] All);

    private sealed class SpawnArena
    {
        private readonly Dictionary<long, Tile> _originalTiles = [];

        public void Prepare(SpawnEnvironment environment, int depth, bool ocean, bool water)
        {
            var (centerX, centerY) = GetCoordinates(depth, ocean);
            var horizontalRadius = Math.Max(100, (int)(NPC.sWidth / 16f * 0.85f));
            var verticalRadius = Math.Max(60, (int)(NPC.sHeight / 16f * 0.85f));
            var safeRadius = Math.Max(35, (int)(NPC.sHeight / 16f * 0.52f));
            var floorY = Math.Clamp(
                centerY + Math.Max(safeRadius + 4, (int)(NPC.sHeight / 16f * 0.62f)),
                20,
                Main.maxTilesY - 10);
            var left = Math.Clamp(centerX - horizontalRadius, 10, Main.maxTilesX - 20);
            var right = Math.Clamp(centerX + horizontalRadius, left + 1, Main.maxTilesX - 10);
            var top = Math.Clamp(centerY - verticalRadius, 10, floorY - 1);
            var bottom = Math.Min(Main.maxTilesY - 10, floorY + 3);

            for (var x = left; x <= right; x++)
            {
                for (var y = top; y <= bottom; y++)
                {
                    Backup(x, y);
                    var tile = Main.tile[x, y];
                    tile.ClearEverything();
                    if (environment.WallType > 0)
                    {
                        tile.WallType = (ushort)environment.WallType;
                    }

                    if (water && y < floorY && x != left && x != right)
                    {
                        tile.LiquidAmount = byte.MaxValue;
                        tile.LiquidType = LiquidID.Water;
                    }

                    if (water && y < floorY && (x == left || x == right))
                    {
                        tile.ResetToType((ushort)environment.TileType);
                    }

                    if (y < floorY) continue;
                    tile.ResetToType((ushort)environment.TileType);
                    if (environment.WallType > 0)
                    {
                        tile.WallType = (ushort)environment.WallType;
                    }
                }
            }
        }

        public void Restore()
        {
            foreach (var (key, tile) in _originalTiles)
            {
                var x = (int)(key >> 32);
                var y = (int)key;
                Main.tile[x, y].CopyFrom(tile);
            }

            _originalTiles.Clear();
        }

        private void Backup(int x, int y)
        {
            var key = ((long)x << 32) | (uint)y;
            if (!_originalTiles.ContainsKey(key))
            {
                _originalTiles[key] = (Tile)Main.tile[x, y].Clone();
            }
        }
    }

    private sealed record SpawnEvent(
        string Name,
        Action Apply,
        Func<bool>? IsAvailable = null,
        string EventCategory = "");

    private sealed class StaticBooleanFlag(FieldInfo field)
    {
        private readonly bool _originalValue = (bool)(field.GetValue(null) ?? false);

        public string Name { get; } = $"{field.DeclaringType?.Name}.{field.Name}";

        public void Set(bool value) => field.SetValue(null, value);

        public void Restore() => field.SetValue(null, _originalValue);
    }

    private readonly record struct SpawnContext(
        int StageIndex,
        int EnvironmentIndex,
        int Depth,
        int EventIndex,
        bool Water,
        bool PlayerSafe,
        bool PlayerInTown,
        bool Invasion,
        bool Granite,
        bool Marble,
        bool SpiderCave,
        bool DesertCave,
        bool Lihzahrd,
        bool Sky);

    private sealed record Catalog(
        Dictionary<int, HashSet<SpawnContext>> Observations,
        IReadOnlyList<string> StageNames,
        HashSet<int> VanillaProgressionStages,
        SpawnEnvironment[] Environments,
        LegacyBiomeFlagCatalog LegacyBiomeFlags,
        SpawnEvent[] Events,
        StaticBooleanFlag[] CustomEventFlags,
        HashSet<string> Failures,
        ProbeCounters Counters);

    private sealed class ProbeCounters
    {
        public int CandidateNpcCount { get; init; }
        public int ModNpcTemplateCount { get; init; }
        public int ContextCount { get; set; }
        public int SpawnRateBlockedContextCount { get; set; }
        public HashSet<int> PositiveSpawnChanceTypes { get; } = [];
        public HashSet<int> ChosenSpawnTypes { get; } = [];
        public HashSet<int> FullSpawnTypes { get; } = [];
        public int FullSpawnContextCount { get; set; }
        public int FullSpawnAttemptCount { get; set; }
        public int FullSpawnSuccessfulAttemptCount { get; set; }
        public int FullSpawnedNpcInstanceCount { get; set; }
        public List<JournalNpcFullSpawnContextDiagnostics> FullSpawnContextDetails { get; } = [];
    }

    private readonly record struct PlayerState(
        bool Active,
        bool Dead,
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
        bool Meteor,
        bool Graveyard,
        bool TowerSolar,
        bool TowerVortex,
        bool TowerNebula,
        bool TowerStardust,
        BitArray ModBiomeFlags);

    private readonly record struct WorldState(
        bool DayTime,
        bool BloodMoon,
        bool Eclipse,
        bool Raining,
        bool SlimeRain,
        bool PumpkinMoon,
        bool SnowMoon,
        bool Xmas,
        bool Halloween,
        int InvasionType,
        double InvasionX,
        int InvasionSize,
        bool Sandstorm,
        bool Dd2Ongoing,
        int Dd2Difficulty,
        UnifiedRandom Random,
        int NetMode,
        int GameMode);

    public static JournalNpcSpawnAvailability GetAvailability(int npcType)
    {
        if (!IsOrdinaryNpc(npcType))
        {
            return new JournalNpcSpawnAvailability(
                npcType,
                observed: false,
                earliestStageIndex: -1,
                earliestStageName: string.Empty,
                conditions: []);
        }

        var catalog = GetCatalog();
        if (!catalog.Observations.TryGetValue(npcType, out var contexts) || contexts.Count == 0)
        {
            if (TryInferSimpleHardmodeSkyAvailability(npcType, out var inferredAvailability))
            {
                return inferredAvailability;
            }

            return new JournalNpcSpawnAvailability(
                npcType,
                observed: false,
                earliestStageIndex: -1,
                earliestStageName: string.Empty,
                conditions: []);
        }

        var stageEvidence = contexts.ToArray();

        var earliestStageIndex = stageEvidence.Min(static context => context.StageIndex);
        var earliestStageName = earliestStageIndex >= 0 && earliestStageIndex < catalog.StageNames.Count
            ? catalog.StageNames[earliestStageIndex]
            : string.Empty;
        return new JournalNpcSpawnAvailability(
            npcType,
            observed: true,
            earliestStageIndex,
            earliestStageName,
            BuildConditions(catalog, stageEvidence, earliestStageIndex, earliestStageName),
            GetEventCategories(catalog, stageEvidence));
    }

    public static IReadOnlyList<string> GetConditions(int npcType)
    {
        return GetAvailability(npcType).Conditions;
    }

    private static bool TryInferSimpleHardmodeSkyAvailability(
        int npcType,
        out JournalNpcSpawnAvailability availability)
    {
        availability = null!;
        if (!JournalStaticNpcSpawnConditionResolver.IsSimpleHardmodeSky(npcType))
        {
            return false;
        }

        var stageIndex = FindEarliestHardmodeStage();
        var profile = JournalRuntimeProgressionScenarios.CurrentProfile;
        if (stageIndex < 0 || profile is null || stageIndex >= profile.Stages.Count)
        {
            return false;
        }

        var stageName = profile.Stages[stageIndex].Name.Resolve();
        availability = new JournalNpcSpawnAvailability(
            npcType,
            observed: true,
            stageIndex,
            stageName,
            [
                Language.GetTextValue(
                    "Mods.ProgressionJournal.UI.NpcSpawnAvailableAfterCondition",
                    stageName),
                Language.GetTextValue(
                    "Mods.ProgressionJournal.UI.FishingDepthCondition",
                    Language.GetTextValue("Bestiary_Biomes.Sky"))
            ]);
        return true;
    }

    private static int FindEarliestHardmodeStage()
    {
        using var progression = new JournalRuntimeProgressionScenarios();
        for (var stageIndex = 0; stageIndex < progression.Count; stageIndex++)
        {
            for (var variantIndex = 0;
                 variantIndex < progression.GetVariantCount(stageIndex);
                 variantIndex++)
            {
                progression.Reset();
                progression.Apply(stageIndex, variantIndex);
                if (Main.hardMode)
                {
                    return stageIndex;
                }
            }
        }

        return -1;
    }

    internal static JournalNpcSpawnProbeDiagnostics GetDiagnostics()
    {
        var catalog = GetCatalog();
        return new JournalNpcSpawnProbeDiagnostics(
            catalog.Observations.Count,
            catalog.Counters.CandidateNpcCount,
            catalog.Counters.ModNpcTemplateCount,
            catalog.Counters.ContextCount,
            catalog.Counters.SpawnRateBlockedContextCount,
            catalog.Counters.PositiveSpawnChanceTypes.Count,
            catalog.Counters.ChosenSpawnTypes.Count,
            catalog.Counters.FullSpawnTypes.Count,
            catalog.Counters.FullSpawnContextCount,
            catalog.Counters.FullSpawnAttemptCount,
            catalog.Counters.FullSpawnSuccessfulAttemptCount,
            catalog.Counters.FullSpawnedNpcInstanceCount,
            catalog.Counters.FullSpawnContextDetails,
            catalog.Failures.Order(StringComparer.Ordinal).ToArray());
    }

    private static Catalog GetCatalog()
    {
        lock (SyncRoot)
        {
            var key = BuildCatalogKey();
            if (_catalog is not null && string.Equals(_catalogKey, key, StringComparison.Ordinal)) return _catalog;
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

    private static Catalog BuildCatalog()
    {
        var player = CreateProbePlayer();
        var environments = CreateEnvironments();
        var legacyBiomeFlags = CreateLegacyBiomeFlags(player, environments);
        var customEventFlags = CreateCustomEventFlags();
        var events = CreateEvents(customEventFlags);
        using var progression = new JournalRuntimeProgressionScenarios();
        var catalog = new Catalog(
            [],
            progression.StageNames,
            Enumerable.Range(0, progression.Count)
                .Where(progression.ChangesVanillaProgression)
                .Append(0)
                .ToHashSet(),
            environments,
            legacyBiomeFlags,
            events,
            customEventFlags,
            [],
            new ProbeCounters
            {
                CandidateNpcCount = Enumerable.Range(1, NPCLoader.NPCCount - 1).Count(IsOrdinaryNpc),
                ModNpcTemplateCount = ModContent.GetContent<ModNPC>().Count()
            });
        var playerState = CapturePlayerState(player);
        var worldState = CaptureWorldState();
        var originalNpcReferences = Main.npc.ToArray();
        var originalPlayerReferences = Main.player.ToArray();
        var spawnArena = new SpawnArena();

        try
        {
            PreparePlayers(player);
            Main.netMode = NetmodeID.SinglePlayer;
            // Profiles describe one progression path, independent of the world used for export.
            // Some mods derive defeat flags from different NPC variants in Expert mode, so a
            // probe must not inherit the export world's difficulty and lose normal-mode sources.
            Main.GameMode = 0;
            foreach (var context in CreateContexts(catalog))
            {
                for (var variantIndex = 0;
                     variantIndex < progression.GetVariantCount(context.StageIndex);
                     variantIndex++)
                {
                    try
                    {
                        progression.Reset();
                        progression.Apply(context.StageIndex, variantIndex);
                        var environment = catalog.Environments[context.EnvironmentIndex];
                        if (!(environment.IsAvailable?.Invoke() ?? true))
                        {
                            continue;
                        }
                        ApplyContext(catalog, player, context);
                        if (!(catalog.Events[context.EventIndex].IsAvailable?.Invoke() ?? true))
                        {
                            continue;
                        }
                        var spawnInfo = CreateSpawnInfo(catalog, player, context);
                        catalog.Counters.ContextCount++;

                        // SpawnChance/EditSpawnPool describe which NPC is valid for this context.
                        // EditSpawnRate only controls whether the shared spawn cycle runs at all and
                        // must not prevent the availability probe from reaching those per-NPC APIs.
                        ObserveExactSpawnPool(catalog, spawnInfo, context);
                        ObserveChosenSpawn(catalog, spawnInfo, context);
                        ObserveDd2WaveEnemies(catalog, context);

                        var spawnRate = 600;
                        var maxSpawns = 5;
                        NPCLoader.EditSpawnRate(player, ref spawnRate, ref maxSpawns);

                        if (spawnRate <= 0 || maxSpawns <= 0)
                        {
                            catalog.Counters.SpawnRateBlockedContextCount++;
                            continue;
                        }

                        if (!ShouldRunFullSpawn(context, catalog)) continue;
                        ObserveFullSpawnInTemporaryArena(catalog, spawnArena, player, context);
                    }
                    catch (Exception exception)
                    {
                        RecordFailure(catalog, "spawn scenario", exception);
                    }
                }
            }
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
            RestoreWorldState(worldState);
            spawnArena.Restore();
            foreach (var flag in customEventFlags)
            {
                flag.Restore();
            }

            foreach (var flag in legacyBiomeFlags.All)
            {
                flag.Set(flag.OriginalValue);
            }
        }

        return catalog;
    }

    private static SpawnEnvironment[] CreateEnvironments()
    {
        var environments = new List<SpawnEnvironment>
        {
            new(
                Language.GetTextValue("Mods.ProgressionJournal.UI.FishingBiomeDefault"),
                TileID.Stone,
                null,
                static _ => { }),
            new(Language.GetTextValue("Bestiary_Biomes.Ocean"), TileID.Sand, null, static p => p.ZoneBeach = true),
            new(Language.GetTextValue("Bestiary_Biomes.Desert"), TileID.Sand, null, static p => p.ZoneDesert = true),
            new(Language.GetTextValue("Bestiary_Biomes.Snow"), TileID.SnowBlock, null, static p => p.ZoneSnow = true),
            new(Language.GetTextValue("Bestiary_Biomes.Jungle"), TileID.JungleGrass, null, static p => p.ZoneJungle = true),
            new(Language.GetTextValue("Bestiary_Biomes.TheCorruption"), TileID.CorruptGrass, null, static p => p.ZoneCorrupt = true),
            new(Language.GetTextValue("Bestiary_Biomes.Crimson"), TileID.CrimsonGrass, null, static p => p.ZoneCrimson = true),
            new(
                Language.GetTextValue("Bestiary_Biomes.TheHallow"),
                TileID.HallowedGrass,
                null,
                static p => p.ZoneHallow = true,
                static () => Main.hardMode),
            new(
                Language.GetTextValue("Bestiary_Biomes.TheDungeon"),
                TileID.BlueDungeonBrick,
                null,
                static p => p.ZoneDungeon = true,
                static () => NPC.downedBoss3)
            {
                WallType = WallID.BlueDungeonUnsafe
            },
            new(Language.GetTextValue("Bestiary_Biomes.UndergroundMushroom"), TileID.MushroomGrass, null, static p => p.ZoneGlowshroom = true),
            new(
                Language.GetTextValue("Bestiary_Biomes.Meteor"),
                TileID.Meteorite,
                null,
                static p => p.ZoneMeteor = true,
                static () => NPC.downedBoss2),
            new(Language.GetTextValue("Bestiary_Biomes.Graveyard"), TileID.Stone, null, static p => p.ZoneGraveyard = true),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnGranite"), TileID.Granite, null, static _ => { }),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnMarble"), TileID.Marble, null, static _ => { }),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnSpiderCave"), TileID.Stone, null, static _ => { })
            {
                WallType = WallID.SpiderUnsafe
            },
            new(
                Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnTemple"),
                TileID.LihzahrdBrick,
                null,
                static _ => { },
                static () => NPC.downedPlantBoss)
            {
                WallType = WallID.LihzahrdBrickUnsafe
            },
            new(Language.GetTextValue("Bestiary_Biomes.UndergroundDesert"), TileID.Sandstone, null, static p => p.ZoneDesert = true),
            new(
                Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnSolarPillar"),
                TileID.Stone,
                null,
                static p => p.ZoneTowerSolar = true,
                static () => NPC.downedAncientCultist),
            new(
                Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnVortexPillar"),
                TileID.Stone,
                null,
                static p => p.ZoneTowerVortex = true,
                static () => NPC.downedAncientCultist),
            new(
                Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnNebulaPillar"),
                TileID.Stone,
                null,
                static p => p.ZoneTowerNebula = true,
                static () => NPC.downedAncientCultist),
            new(
                Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnStardustPillar"),
                TileID.Stone,
                null,
                static p => p.ZoneTowerStardust = true,
                static () => NPC.downedAncientCultist)
        };

        var profile = JournalRuntimeProgressionScenarios.CurrentProfile;
        var relevantMods = profile is not null
            ? profile.Document.RequiredMods
                .Select(static requirement => requirement.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : [];
        environments.AddRange(ModContent.GetContent<ModBiome>()
            .Where(biome => relevantMods.Count == 0 || relevantMods.Contains(biome.Mod.Name))
            .OrderBy(static biome => biome.FullName, StringComparer.OrdinalIgnoreCase)
            .Select(biome => new SpawnEnvironment(biome.DisplayName.Value, TileID.Stone, biome, static _ => { })));

        return environments.ToArray();
    }

    private static SpawnEvent[] CreateEvents(
        StaticBooleanFlag[] customEventFlags)
    {
        List<SpawnEvent> events =
        [
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnNoEvent"), static () => { }),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.FishingWorldNight"), static () => Main.dayTime = false),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.FishingWorldBloodMoon"), static () =>
            {
                Main.dayTime = false;
                Main.bloodMoon = true;
            }, EventCategory: "BloodMoon"),
            new(
                Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnEclipse"),
                static () => Main.eclipse = true,
                static () => NPC.downedMechBossAny,
                "SolarEclipse"),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnRain"), static () => Main.raining = true),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnSandstorm"), static () =>
            {
                Sandstorm.Happening = true;
                Sandstorm.Severity = 1f;
            }),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnSlimeRain"), static () => Main.slimeRain = true),
            new(
                Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnGoblinArmy"),
                static () => ApplyInvasion(InvasionID.GoblinArmy),
                EventCategory: "GoblinArmy"),
            new(
                Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnFrostLegion"),
                static () => ApplyInvasion(InvasionID.SnowLegion),
                static () => Main.hardMode),
            new(
                Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnPirates"),
                static () => ApplyInvasion(InvasionID.PirateInvasion),
                static () => Main.hardMode,
                "PirateInvasion"),
            new(
                Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnMartians"),
                static () => ApplyInvasion(InvasionID.MartianMadness),
                static () => NPC.downedGolemBoss,
                "MartianMadness"),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnPumpkinMoon"), static () =>
            {
                Main.dayTime = false;
                Main.pumpkinMoon = true;
            }, static () => NPC.downedPlantBoss, "PumpkinMoon"),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnFrostMoon"), static () =>
            {
                Main.dayTime = false;
                Main.snowMoon = true;
            }, static () => NPC.downedPlantBoss, "FrostMoon"),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnOldOnesArmy"), static () =>
            {
                DD2Event.Ongoing = true;
                Dd2FindProperDifficultyMethod?.Invoke(null, null);
            }, static () => NPC.downedBoss2, "OldOnesArmy")
        ];
        // A discovered "active" flag proves how to enter an event, but not when
        // the event becomes available. Keep those NPCs unknown until the profile
        // declares the event stage or a structured prerequisite is available.
        events.AddRange(customEventFlags.Select(flag =>
            new SpawnEvent(
                flag.Name,
                () => flag.Set(true),
                static () => false)));
        return events.ToArray();
    }

    private static StaticBooleanFlag[] CreateCustomEventFlags()
    {
        var profile = JournalRuntimeProgressionScenarios.CurrentProfile;
        var relevantMods = profile is not null
            ? profile.Document.RequiredMods
                .Select(static requirement => requirement.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : [];
        const BindingFlags flags =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        return ModLoader.Mods
            .Where(mod =>
                mod.Code is not null
                && (relevantMods.Count == 0 || relevantMods.Contains(mod.Name)))
            .SelectMany(mod => GetLoadableTypes(mod.Code))
            .Where(type =>
                type.Name.Contains("Event", StringComparison.OrdinalIgnoreCase)
                || type.Name.Contains("Invasion", StringComparison.OrdinalIgnoreCase))
            .SelectMany(type => type.GetFields(flags))
            .Where(static field =>
                field.FieldType == typeof(bool)
                && !field.IsInitOnly
                && (field.Name.Contains("Ongoing", StringComparison.OrdinalIgnoreCase)
                    || field.Name.Contains("Active", StringComparison.OrdinalIgnoreCase)
                    || field.Name.Contains("Happening", StringComparison.OrdinalIgnoreCase)))
            .Select(static field => new StaticBooleanFlag(field))
            .ToArray();
    }

    private static HashSet<SpawnContext> CreateContexts(Catalog catalog)
    {
        var contexts = new HashSet<SpawnContext>();

        for (var environmentIndex = 0; environmentIndex < catalog.Environments.Length; environmentIndex++)
        {
            for (var depth = 0; depth < 5; depth++)
            {
                AddVariants(
                    stageIndex: 0,
                    environmentIndex,
                    depth,
                    eventIndex: 0,
                    includeSafe: true);
            }

            for (var eventIndex = 1; eventIndex < catalog.Events.Length; eventIndex++)
            {
                AddVariants(
                    stageIndex: 0,
                    environmentIndex,
                    depth: 1,
                    eventIndex,
                    includeSafe: false);
            }
        }

        for (var stageIndex = 0; stageIndex < catalog.StageNames.Count; stageIndex++)
        {
            for (var environmentIndex = 0; environmentIndex < catalog.Environments.Length; environmentIndex++)
            {
                foreach (var depth in GetEnvironmentDepths(environmentIndex))
                {
                    AddVariants(
                        stageIndex,
                        environmentIndex,
                        depth,
                        eventIndex: 0,
                        includeSafe: true);
                }

                if (environmentIndex <= 0) continue;
                foreach (var eventIndex in Enumerable.Range(1, catalog.Events.Length - 1))
                {
                    AddVariants(
                        stageIndex,
                        environmentIndex,
                        GetEnvironmentDepths(environmentIndex).First(),
                        eventIndex,
                        includeSafe: false);
                }
            }

            for (var eventIndex = 1; eventIndex < catalog.Events.Length; eventIndex++)
            {
                AddVariants(
                    stageIndex,
                    environmentIndex: 0,
                    depth: 1,
                    eventIndex,
                    includeSafe: false);
            }
        }

        return contexts;

        void AddVariants(
            int stageIndex,
            int environmentIndex,
            int depth,
            int eventIndex,
            bool includeSafe)
        {
            var variantCount = includeSafe ? 3 : 2;
            for (var variant = 0; variant < variantCount; variant++)
            {
                contexts.Add(CreateContext(
                    stageIndex,
                    environmentIndex,
                    depth,
                    eventIndex,
                    variant));
            }
        }
    }

    private static IEnumerable<int> GetEnvironmentDepths(int environmentIndex)
    {
        return environmentIndex switch
        {
            1 or 17 or 18 or 19 or 20 => [1],
            8 or 9 or 12 or 13 or 14 or 15 or 16 => [3],
            _ => [1, 3]
        };
    }

    private static SpawnContext CreateContext(
        int stageIndex,
        int environmentIndex,
        int depth,
        int eventIndex,
        int variant)
    {
        return new SpawnContext(
            stageIndex,
            environmentIndex,
            depth,
            eventIndex,
            Water: variant % 3 == 1,
            PlayerSafe: variant % 3 == 2,
            PlayerInTown: variant % 3 == 2,
            Invasion: eventIndex is >= 7 and <= 10,
            Granite: environmentIndex == 12,
            Marble: environmentIndex == 13,
            SpiderCave: environmentIndex == 14,
            DesertCave: environmentIndex == 16,
            Lihzahrd: environmentIndex == 15,
            Sky: depth == 0);
    }

    private static void ApplyContext(Catalog catalog, Player player, SpawnContext context)
    {
        ResetWorldScenario(catalog);
        ResetPlayerZones(player);
        foreach (var flag in catalog.LegacyBiomeFlags.All)
        {
            flag.Set(false);
        }

        catalog.Events[context.EventIndex].Apply();
        var environment = catalog.Environments[context.EnvironmentIndex];
        environment.Apply(player);
        var modBiomeFlags = GetModBiomeFlags(player);
        if (environment.ModBiome is not null)
        {
            var biomeIndex = GetModBiomeIndex(environment.ModBiome);
            if (biomeIndex >= 0 && biomeIndex < modBiomeFlags.Length)
            {
                modBiomeFlags[biomeIndex] = true;
            }
        }

        if (catalog.LegacyBiomeFlags.ByEnvironment.TryGetValue(
                context.EnvironmentIndex,
                out var activeLegacyBiomeFlags))
        {
            foreach (var flag in activeLegacyBiomeFlags)
            {
                flag.Set(true);
            }
        }

        var (x, y) = GetCoordinates(context.Depth, context.EnvironmentIndex == 1);
        player.Center = new Vector2(x * 16f, y * 16f);
    }

    private static LegacyBiomeFlagCatalog CreateLegacyBiomeFlags(
        Player player,
        IReadOnlyList<SpawnEnvironment> environments)
    {
        var byEnvironment = new Dictionary<int, LegacyBiomeFlag[]>();
        var byField = new Dictionary<FieldInfo, LegacyBiomeFlag>();
        if (PlayerGetModPlayerMethod is null)
        {
            return new LegacyBiomeFlagCatalog(byEnvironment, []);
        }

        for (var environmentIndex = 0; environmentIndex < environments.Count; environmentIndex++)
        {
            var biome = environments[environmentIndex].ModBiome;
            var isBiomeActive = biome?.GetType().GetMethod(
                nameof(ModBiome.IsBiomeActive),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (isBiomeActive is null)
            {
                continue;
            }

            var flags = new List<LegacyBiomeFlag>();
            foreach (var field in JournalLegacyDirectDropAnalyzer.GetReferencedMembers(isBiomeActive)
                         .OfType<FieldInfo>()
                         .Where(static field => !field.IsStatic
                             && field.FieldType == typeof(bool)
                             && field.DeclaringType is not null
                             && typeof(ModPlayer).IsAssignableFrom(field.DeclaringType))
                         .Distinct())
            {
                if (!byField.TryGetValue(field, out var flag))
                {
                    try
                    {
                        var modPlayer = PlayerGetModPlayerMethod
                            .MakeGenericMethod(field.DeclaringType!)
                            .Invoke(player, null) as ModPlayer;
                        if (modPlayer is null)
                        {
                            continue;
                        }

                        flag = new LegacyBiomeFlag(
                            field,
                            modPlayer,
                            (bool)(field.GetValue(modPlayer) ?? false));
                        byField[field] = flag;
                    }
                    catch (Exception exception)
                    {
                        ProgressionJournal.Instance?.Logger.Debug(
                            $"Failed to bind legacy NPC probe biome flag "
                            + $"'{field.DeclaringType?.FullName}.{field.Name}'."
                            + $"{Environment.NewLine}{exception}");
                        continue;
                    }
                }

                flags.Add(flag);
            }

            if (flags.Count > 0)
            {
                byEnvironment[environmentIndex] = flags.ToArray();
            }
        }

        return new LegacyBiomeFlagCatalog(byEnvironment, byField.Values.ToArray());
    }

    private static NPCSpawnInfo CreateSpawnInfo(Catalog catalog, Player player, SpawnContext context)
    {
        var (x, y) = GetCoordinates(context.Depth, context.EnvironmentIndex == 1);
        return new NPCSpawnInfo
        {
            SpawnTileX = x,
            SpawnTileY = y,
            SpawnTileType = catalog.Environments[context.EnvironmentIndex].TileType,
            Player = player,
            PlayerFloorX = x,
            PlayerFloorY = y,
            Sky = context.Sky,
            Lihzahrd = context.Lihzahrd,
            PlayerSafe = context.PlayerSafe,
            Invasion = context.Invasion,
            Water = context.Water,
            Granite = context.Granite,
            Marble = context.Marble,
            SpiderCave = context.SpiderCave,
            PlayerInTown = context.PlayerInTown,
            DesertCave = context.DesertCave,
            SafeRangeX = false
        };
    }

    private static void ObserveExactSpawnPool(
        Catalog catalog,
        NPCSpawnInfo spawnInfo,
        SpawnContext context)
    {
        try
        {
            SpawnHelperResetMethod?.Invoke(null, null);
            SpawnHelperDoChecksMethod?.Invoke(null, [spawnInfo]);
            var pool = new Dictionary<int, float> { [0] = 1f };
            foreach (var modNpc in ModContent.GetContent<ModNPC>())
            {
                var weight = modNpc.SpawnChance(spawnInfo);
                if (weight > 0f)
                {
                    pool[modNpc.Type] = weight;
                    catalog.Counters.PositiveSpawnChanceTypes.Add(modNpc.Type);
                }
            }

            foreach (var globalNpc in GetEditSpawnPoolGlobals())
            {
                globalNpc.EditSpawnPool(pool, spawnInfo);
            }

            foreach (var (npcType, weight) in pool)
            {
                if (npcType > 0 && weight > 0f && IsOrdinaryNpc(npcType))
                {
                    AddObservation(catalog.Observations, npcType, context);
                }
            }
        }
        catch (Exception exception)
        {
            RecordFailure(catalog, "exact spawn pool", exception);
        }
    }

    private static IEnumerable<GlobalNPC> GetEditSpawnPoolGlobals()
    {
        return GetHookGlobals(EditSpawnPoolHookField);
    }

    private static IReadOnlyList<string> TraceSpawnRateHooks(Player player)
    {
        List<string> trace = [];
        var spawnRate = 1;
        var maxSpawns = 5;
        foreach (var globalNpc in GetHookGlobals(EditSpawnRateHookField))
        {
            var previousSpawnRate = spawnRate;
            var previousMaxSpawns = maxSpawns;
            globalNpc.EditSpawnRate(player, ref spawnRate, ref maxSpawns);
            if (spawnRate == previousSpawnRate && maxSpawns == previousMaxSpawns)
            {
                continue;
            }

            trace.Add(
                $"{globalNpc.GetType().FullName}: "
                + $"{previousSpawnRate}/{previousMaxSpawns} -> {spawnRate}/{maxSpawns}");
        }

        return trace;
    }

    private static (bool Disabled, float Multiplier) GetJourneySpawnRate(Player player)
    {
        if (!Main.GameModeInfo.IsJourneyMode)
        {
            return (false, 1f);
        }

        var power = CreativePowerManager.Instance
            .GetPower<CreativePowers.SpawnRateSliderPerPlayerPower>();
        if (power is null || !power.GetIsUnlocked())
        {
            return (false, 1f);
        }

        var disabled = power.GetShouldDisableSpawnsFor(player.whoAmI);
        return power.GetRemappedSliderValueFor(player.whoAmI, out var multiplier)
            ? (disabled, multiplier)
            : (disabled, 1f);
    }

    private static IEnumerable<GlobalNPC> GetHookGlobals(FieldInfo? hookField)
    {
        var hookList = hookField?.GetValue(null);
        var hookGlobalsField = GetHookGlobalsField(hookList);
        return hookGlobalsField?.GetValue(hookList) as GlobalNPC[] ?? [];
    }

    private static FieldInfo? GetHookGlobalsField(object? hookList)
    {
        return hookList?.GetType().GetField(
            "hookGlobals",
            BindingFlags.Instance | BindingFlags.NonPublic);
    }

    private static void ObserveChosenSpawn(
        Catalog catalog,
        NPCSpawnInfo spawnInfo,
        SpawnContext context)
    {
        try
        {
            for (var seed = 0; seed < 8; seed++)
            {
                Main.rand = new UnifiedRandom(seed);
                if (NPCLoader.ChooseSpawn(spawnInfo) is { } npcType and > 0
                    )
                {
                    catalog.Counters.ChosenSpawnTypes.Add(npcType);
                    if (IsOrdinaryNpc(npcType))
                    {
                        AddObservation(catalog.Observations, npcType, context);
                    }
                }
            }
        }
        catch (Exception exception)
        {
            RecordFailure(catalog, "chosen spawn", exception);
        }
    }

    private static void ObserveDd2WaveEnemies(
        Catalog catalog,
        SpawnContext context)
    {
        if (!DD2Event.Ongoing)
        {
            return;
        }

        try
        {
            for (var wave = 1; wave <= 7; wave++)
            {
                if (Dd2GetEnemiesForWaveMethod?.Invoke(null, [wave]) is not IEnumerable enemies)
                {
                    continue;
                }

                foreach (var value in enemies)
                {
                    var npcType = Convert.ToInt32(value);
                    if (npcType > 0)
                    {
                        AddObservation(catalog.Observations, npcType, context);
                    }
                }
            }
        }
        catch (Exception exception)
        {
            RecordFailure(catalog, "Old One's Army wave", exception);
        }
    }

    private static bool ShouldRunFullSpawn(SpawnContext context, Catalog catalog)
    {
        if (!catalog.VanillaProgressionStages.Contains(context.StageIndex))
        {
            return false;
        }

        if (context.EventIndex != 0)
        {
            return context is { EnvironmentIndex: 0, Depth: 1 };
        }

        return catalog.Environments[context.EnvironmentIndex].ModBiome is null;
    }

    private static void ObserveFullSpawnInTemporaryArena(
        Catalog catalog,
        SpawnArena spawnArena,
        Player player,
        SpawnContext context)
    {
        try
        {
            spawnArena.Prepare(
                catalog.Environments[context.EnvironmentIndex],
                context.Depth,
                context.EnvironmentIndex == 1,
                context.Water);
            ObserveFullSpawn(catalog, player, context);
        }
        finally
        {
            spawnArena.Restore();
        }
    }

    private static void ObserveFullSpawn(
        Catalog catalog,
        Player player,
        SpawnContext context)
    {
        try
        {
            catalog.Counters.FullSpawnContextCount++;
            var attemptCount = 0;
            var successfulAttemptCount = 0;
            var spawnedNpcInstanceCount = 0;
            var minimumSpawnRate = int.MaxValue;
            var maximumSpawnRate = int.MinValue;
            var minimumMaxSpawns = int.MaxValue;
            var maximumMaxSpawns = int.MinValue;
            HashSet<int> spawnedNpcTypes = [];
            var journeySpawnRate = GetJourneySpawnRate(player);
            var spawnRateHookTrace = TraceSpawnRateHooks(player);
            var previousDefaultSpawnRate = DefaultSpawnRateField?.GetValue(null);
            var previousNoSpawnCycle = NoSpawnCycleField?.GetValue(null);
            var editSpawnRateHookList = EditSpawnRateHookField?.GetValue(null);
            var editSpawnRateHookGlobalsField = GetHookGlobalsField(editSpawnRateHookList);
            var previousEditSpawnRateHooks = editSpawnRateHookGlobalsField?.GetValue(editSpawnRateHookList);
            try
            {
                DefaultSpawnRateField?.SetValue(null, 1);
                editSpawnRateHookGlobalsField?.SetValue(
                    editSpawnRateHookList,
                    Array.Empty<GlobalNPC>());
                var seedCount = context is { EnvironmentIndex: 0, EventIndex: 0 }
                    ? FocusedFullSpawnSeedCount
                    : FullSpawnSeedCount;
                for (var seed = 0; seed < seedCount; seed++)
                {
                    attemptCount++;
                    catalog.Counters.FullSpawnAttemptCount++;
                    PrepareNpcArray();
                    NoSpawnCycleField?.SetValue(null, false);
                    Main.rand = new UnifiedRandom(seed);
                    player.active = true;
                    NPC.SpawnNPC();
                    if (SpawnRateField?.GetValue(null) is int spawnRate)
                    {
                        minimumSpawnRate = Math.Min(minimumSpawnRate, spawnRate);
                        maximumSpawnRate = Math.Max(maximumSpawnRate, spawnRate);
                    }
                    if (MaxSpawnsField?.GetValue(null) is int maxSpawns)
                    {
                        minimumMaxSpawns = Math.Min(minimumMaxSpawns, maxSpawns);
                        maximumMaxSpawns = Math.Max(maximumMaxSpawns, maxSpawns);
                    }
                    var spawnedNpcs = Main.npc
                        .Where(static npc => npc is { active: true })
                        .ToArray();
                    if (spawnedNpcs.Length > 0)
                    {
                        successfulAttemptCount++;
                        spawnedNpcInstanceCount += spawnedNpcs.Length;
                        catalog.Counters.FullSpawnSuccessfulAttemptCount++;
                        catalog.Counters.FullSpawnedNpcInstanceCount += spawnedNpcs.Length;
                    }

                    foreach (var npc in spawnedNpcs)
                    {
                        spawnedNpcTypes.Add(npc.type);
                        catalog.Counters.FullSpawnTypes.Add(npc.type);
                        if (IsOrdinaryNpc(npc.type))
                        {
                            AddObservation(catalog.Observations, npc.type, context);
                        }
                    }
                }

                catalog.Counters.FullSpawnContextDetails.Add(
                    new JournalNpcFullSpawnContextDiagnostics(
                        context.StageIndex,
                        catalog.Environments[context.EnvironmentIndex].Name,
                        context.Depth,
                        catalog.Events[context.EventIndex].Name,
                        context.Water,
                        context.PlayerSafe,
                        context.PlayerInTown,
                        attemptCount,
                        successfulAttemptCount,
                        spawnedNpcInstanceCount,
                        player.nearbyActiveNPCs,
                        player.townNPCs,
                        minimumSpawnRate == int.MaxValue ? -1 : minimumSpawnRate,
                        maximumSpawnRate == int.MinValue ? -1 : maximumSpawnRate,
                        minimumMaxSpawns == int.MaxValue ? -1 : minimumMaxSpawns,
                        maximumMaxSpawns == int.MinValue ? -1 : maximumMaxSpawns,
                        Main.GameModeInfo.IsJourneyMode,
                        journeySpawnRate.Disabled,
                        journeySpawnRate.Multiplier,
                        spawnRateHookTrace,
                        spawnedNpcTypes.Order().ToArray()));
            }
            finally
            {
                if (editSpawnRateHookGlobalsField is not null)
                {
                    editSpawnRateHookGlobalsField.SetValue(
                        editSpawnRateHookList,
                        previousEditSpawnRateHooks);
                }
                if (previousDefaultSpawnRate is not null)
                {
                    DefaultSpawnRateField?.SetValue(null, previousDefaultSpawnRate);
                }
                if (previousNoSpawnCycle is not null)
                {
                    NoSpawnCycleField?.SetValue(null, previousNoSpawnCycle);
                }
            }
        }
        catch (Exception exception)
        {
            RecordFailure(catalog, "full vanilla spawn", exception);
        }
    }

    private static void RecordFailure(Catalog catalog, string operation, Exception exception)
    {
        var message = $"{operation}: {exception.GetType().Name}: {exception.Message}";
        if (!catalog.Failures.Add(message))
        {
            return;
        }

        ProgressionJournal.Instance?.Logger.Debug(
            $"NPC availability probe failed during {operation}.{Environment.NewLine}{exception}");
    }

    private static void AddObservation(
        Dictionary<int, HashSet<SpawnContext>> observations,
        int npcType,
        SpawnContext context)
    {
        if (!observations.TryGetValue(npcType, out var contexts))
        {
            contexts = [];
            observations[npcType] = contexts;
        }

        contexts.Add(context);
    }

    private static string[] BuildConditions(
        Catalog catalog,
        IReadOnlyCollection<SpawnContext> contexts,
        int earliestStageIndex,
        string earliestStageName)
    {
        var conditions = new List<string>();
        if (earliestStageIndex > 0 && !string.IsNullOrWhiteSpace(earliestStageName))
        {
            conditions.Add(Language.GetTextValue(
                "Mods.ProgressionJournal.UI.NpcSpawnAvailableAfterCondition",
                earliestStageName));
        }

        var environmentIndexes = contexts
            .Select(static context => context.EnvironmentIndex)
            .Distinct()
            .Order()
            .ToArray();
        if (environmentIndexes.Length < catalog.Environments.Length)
        {
            conditions.Add(Language.GetTextValue(
                "Mods.ProgressionJournal.UI.NpcSpawnBiomeCondition",
                string.Join(", ", environmentIndexes.Select(index => catalog.Environments[index].Name))));
        }

        var depths = contexts.Select(static context => context.Depth).Distinct().Order().ToArray();
        if (depths.Length < 5)
        {
            conditions.Add(Language.GetTextValue(
                "Mods.ProgressionJournal.UI.FishingDepthCondition",
                string.Join(", ", depths.Select(GetDepthName))));
        }

        var eventIndexes = contexts
            .Select(static context => context.EventIndex)
            .Distinct()
            .Order()
            .ToArray();
        if (!eventIndexes.Contains(0))
        {
            conditions.Add(Language.GetTextValue(
                "Mods.ProgressionJournal.UI.NpcSpawnEventCondition",
                string.Join(", ", eventIndexes.Select(index => catalog.Events[index].Name))));
        }

        AppendBooleanCondition(
            conditions,
            contexts,
            static context => context.Water,
            "Mods.ProgressionJournal.UI.NpcSpawnWaterCondition");
        AppendBooleanCondition(
            conditions,
            contexts,
            static context => context.PlayerSafe,
            "Mods.ProgressionJournal.UI.NpcSpawnSafeCondition");
        AppendBooleanCondition(
            conditions,
            contexts,
            static context => context.PlayerInTown,
            "Mods.ProgressionJournal.UI.NpcSpawnTownCondition");
        return conditions.ToArray();
    }

    private static string[] GetEventCategories(
        Catalog catalog,
        IReadOnlyCollection<SpawnContext> contexts)
    {
        var categories = contexts
            .Select(context => catalog.Events[context.EventIndex].EventCategory)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return categories.Length > 0 && categories.All(static category => category.Length > 0)
            ? categories
            : [];
    }

    private static void AppendBooleanCondition(
        List<string> conditions,
        IEnumerable<SpawnContext> contexts,
        Func<SpawnContext, bool> selector,
        string localizationKey)
    {
        if (contexts.All(selector))
        {
            conditions.Add(Language.GetTextValue(localizationKey));
        }
    }

    private static string GetDepthName(int depth)
    {
        var key = depth switch
        {
            0 => "Bestiary_Biomes.Sky",
            1 => "Bestiary_Biomes.Surface",
            2 => "Bestiary_Biomes.Underground",
            3 => "Bestiary_Biomes.Caverns",
            _ => "Bestiary_Biomes.TheUnderworld"
        };
        return Language.GetTextValue(key);
    }

    private static bool IsOrdinaryNpc(int npcType)
    {
        if (npcType <= 0 || !ContentSamples.NpcsByNetId.TryGetValue(npcType, out var npc))
        {
            return false;
        }

        return !npc.townNPC
               && npc is { boss: false, lifeMax: > 5, damage: > 0 }
               && !NPCID.Sets.ShouldBeCountedAsBoss[npcType]
               && !NPCID.Sets.BossBestiaryPriority.Contains(npcType);
    }

    private static void PreparePlayers(Player player)
    {
        for (var index = 0; index < Main.player.Length; index++)
        {
            if (index == player.whoAmI)
            {
                Main.player[index] = player;
                player.active = true;
                player.dead = false;
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

    private static void PrepareNpcArray()
    {
        for (var index = 0; index < Main.npc.Length; index++)
        {
            Main.npc[index] = new NPC
            {
                whoAmI = index
            };
        }
    }

    private static void ResetPlayerZones(Player player)
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
        player.ZoneMeteor = false;
        player.ZoneGraveyard = false;
        player.ZoneTowerSolar = false;
        player.ZoneTowerVortex = false;
        player.ZoneTowerNebula = false;
        player.ZoneTowerStardust = false;
        GetModBiomeFlags(player).SetAll(false);
    }

    private static void ResetWorldScenario(Catalog catalog)
    {
        Main.dayTime = true;
        Main.bloodMoon = false;
        Main.eclipse = false;
        Main.raining = false;
        Main.slimeRain = false;
        Main.pumpkinMoon = false;
        Main.snowMoon = false;
        Main.xMas = false;
        Main.halloween = false;
        Main.invasionType = InvasionID.None;
        Main.invasionX = 0d;
        Main.invasionSize = 0;
        Sandstorm.Happening = false;
        Sandstorm.Severity = 0f;
        DD2Event.Ongoing = false;
        DD2Event.OngoingDifficulty = 0;
        foreach (var flag in catalog.CustomEventFlags)
        {
            flag.Set(false);
        }
    }

    private static void ApplyInvasion(int invasionType)
    {
        Main.invasionType = invasionType;
        Main.invasionX = Main.spawnTileX;
        Main.invasionSize = 1000;
    }

    private static (int X, int Y) GetCoordinates(int depth, bool ocean)
    {
        var x = ocean ? Math.Min(200, Main.maxTilesX / 10) : Main.maxTilesX / 2;
        var y = depth switch
        {
            0 => Math.Max(20, (int)(Main.worldSurface * 0.25)),
            1 => Math.Max(20, (int)(Main.worldSurface * 0.75)),
            2 => Math.Max(20, (int)((Main.worldSurface + Main.rockLayer) * 0.5)),
            3 => Math.Min(Main.maxTilesY - 20, (int)Main.rockLayer + 100),
            _ => Main.maxTilesY - 120
        };
        return (x, y);
    }

    private static PlayerState CapturePlayerState(Player player)
    {
        return new PlayerState(
            player.active,
            player.dead,
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
            player.ZoneMeteor,
            player.ZoneGraveyard,
            player.ZoneTowerSolar,
            player.ZoneTowerVortex,
            player.ZoneTowerNebula,
            player.ZoneTowerStardust,
            (BitArray)GetModBiomeFlags(player).Clone());
    }

    private static void RestorePlayerState(Player player, PlayerState state)
    {
        player.active = state.Active;
        player.dead = state.Dead;
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
        player.ZoneMeteor = state.Meteor;
        player.ZoneGraveyard = state.Graveyard;
        player.ZoneTowerSolar = state.TowerSolar;
        player.ZoneTowerVortex = state.TowerVortex;
        player.ZoneTowerNebula = state.TowerNebula;
        player.ZoneTowerStardust = state.TowerStardust;
        ModBiomeFlagsField?.SetValue(player, (BitArray)state.ModBiomeFlags.Clone());
    }

    private static WorldState CaptureWorldState()
    {
        return new WorldState(
            Main.dayTime,
            Main.bloodMoon,
            Main.eclipse,
            Main.raining,
            Main.slimeRain,
            Main.pumpkinMoon,
            Main.snowMoon,
            Main.xMas,
            Main.halloween,
            Main.invasionType,
            Main.invasionX,
            Main.invasionSize,
            Sandstorm.Happening,
            DD2Event.Ongoing,
            DD2Event.OngoingDifficulty,
            Main.rand,
            Main.netMode,
            Main.GameMode);
    }

    private static void RestoreWorldState(WorldState state)
    {
        Main.dayTime = state.DayTime;
        Main.bloodMoon = state.BloodMoon;
        Main.eclipse = state.Eclipse;
        Main.raining = state.Raining;
        Main.slimeRain = state.SlimeRain;
        Main.pumpkinMoon = state.PumpkinMoon;
        Main.snowMoon = state.SnowMoon;
        Main.xMas = state.Xmas;
        Main.halloween = state.Halloween;
        Main.invasionType = state.InvasionType;
        Main.invasionX = state.InvasionX;
        Main.invasionSize = state.InvasionSize;
        Sandstorm.Happening = state.Sandstorm;
        DD2Event.Ongoing = state.Dd2Ongoing;
        DD2Event.OngoingDifficulty = state.Dd2Difficulty;
        Main.rand = state.Random;
        Main.netMode = state.NetMode;
        Main.GameMode = state.GameMode;
    }

    private static Player CreateProbePlayer()
    {
        var player = new Player
        {
            whoAmI = Math.Clamp(Main.myPlayer, 0, 254),
            active = true,
            dead = false
        };
        PlayerLoaderSetupPlayerMethod?.Invoke(null, [player]);
        player.ResetEffects();

        return player;
    }

    private static BitArray GetModBiomeFlags(Player player)
    {
        return ModBiomeFlagsField?.GetValue(player) as BitArray ?? new BitArray(0);
    }

    private static int GetModBiomeIndex(ModBiome biome)
    {
        return ModBiomeIndexProperty?.GetValue(biome) is int index ? index : -1;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            ProgressionJournal.Instance?.Logger.Debug(
                $"Some types could not be loaded while preparing NPC spawn probes."
                + $"{Environment.NewLine}{exception}");
            return exception.Types.OfType<Type>();
        }
    }
}

internal sealed record JournalNpcSpawnProbeDiagnostics(
    int ObservedNpcCount,
    int CandidateNpcCount,
    int ModNpcTemplateCount,
    int ContextCount,
    int SpawnRateBlockedContextCount,
    int PositiveSpawnChanceCount,
    int ChosenSpawnCount,
    int FullSpawnCount,
    int FullSpawnContextCount,
    int FullSpawnAttemptCount,
    int FullSpawnSuccessfulAttemptCount,
    int FullSpawnedNpcInstanceCount,
    IReadOnlyList<JournalNpcFullSpawnContextDiagnostics> FullSpawnContextDetails,
    IReadOnlyList<string> Failures);

internal sealed record JournalNpcFullSpawnContextDiagnostics(
    int StageIndex,
    string Environment,
    int Depth,
    string Event,
    bool Water,
    bool PlayerSafe,
    bool PlayerInTown,
    int Attempts,
    int SuccessfulAttempts,
    int SpawnedNpcInstances,
    float NearbyActiveNpcs,
    float TownNpcs,
    int MinimumSpawnRate,
    int MaximumSpawnRate,
    int MinimumMaxSpawns,
    int MaximumMaxSpawns,
    bool JourneyMode,
    bool JourneySpawnsDisabled,
    float JourneySpawnRateMultiplier,
    IReadOnlyList<string> SpawnRateHookTrace,
    IReadOnlyList<int> SpawnedNpcTypes);
