using System.Collections;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
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
    private static readonly FieldInfo? ModBiomeFlagsField = typeof(Player).GetField(
        "modBiomeFlags",
        BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly PropertyInfo? ModBiomeIndexProperty = typeof(ModBiome).GetProperty(
        "ZeroIndexType",
        BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly MethodInfo? SpawnHelperResetMethod = typeof(NPCLoader).Assembly
        .GetType("Terraria.ModLoader.Utilities.NPCSpawnHelper")
        ?.GetMethod("Reset", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    private static readonly MethodInfo? SpawnHelperDoChecksMethod = typeof(NPCLoader).Assembly
        .GetType("Terraria.ModLoader.Utilities.NPCSpawnHelper")
        ?.GetMethod("DoChecks", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    private static readonly FieldInfo? NpcLoaderNpcsField = typeof(NPCLoader).GetField(
        "npcs",
        BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly FieldInfo? EditSpawnPoolHookField = typeof(NPCLoader).GetField(
        "HookEditSpawnPool",
        BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly FieldInfo? DefaultSpawnRateField = typeof(NPC).GetField(
        "defaultSpawnRate",
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
        Action<Player> Apply)
    {
        public int WallType { get; init; }
    }

    private sealed class SpawnArena
    {
        private readonly Dictionary<long, Tile> _originalTiles = [];

        public void Prepare(SpawnEnvironment environment, int depth, bool ocean)
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
        Func<bool>? IsAvailable = null);

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
        IReadOnlySet<int> VanillaProgressionStages,
        IReadOnlyList<SpawnEnvironment> Environments,
        IReadOnlyList<SpawnEvent> Events,
        IReadOnlyList<StaticBooleanFlag> CustomEventFlags);

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
        int NetMode);

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
            return new JournalNpcSpawnAvailability(
                npcType,
                observed: false,
                earliestStageIndex: -1,
                earliestStageName: string.Empty,
                conditions: []);
        }

        var stageEvidence = contexts
            .Where(context => catalog.Environments[context.EnvironmentIndex].ModBiome is null)
            .ToArray();
        if (stageEvidence.Length == 0)
        {
            return new JournalNpcSpawnAvailability(
                npcType,
                observed: false,
                earliestStageIndex: -1,
                earliestStageName: string.Empty,
                conditions: []);
        }

        var earliestStageIndex = stageEvidence.Min(static context => context.StageIndex);
        var earliestStageName = earliestStageIndex >= 0 && earliestStageIndex < catalog.StageNames.Count
            ? catalog.StageNames[earliestStageIndex]
            : string.Empty;
        return new JournalNpcSpawnAvailability(
            npcType,
            observed: true,
            earliestStageIndex,
            earliestStageName,
            BuildConditions(catalog, stageEvidence, earliestStageIndex, earliestStageName));
    }

    public static IReadOnlyList<string> GetConditions(int npcType)
    {
        return GetAvailability(npcType).Conditions;
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
        var player = GetProbePlayer();
        var environments = CreateEnvironments();
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
            events,
            customEventFlags);
        if (player is null)
        {
            return catalog;
        }

        var playerState = CapturePlayerState(player);
        var worldState = CaptureWorldState();
        var originalNpcReferences = Main.npc.ToArray();
        var originalPlayerReferences = Main.player.ToArray();
        var spawnArena = new SpawnArena();

        try
        {
            PreparePlayers(player);
            Main.netMode = NetmodeID.SinglePlayer;
            foreach (var context in CreateContexts(catalog))
            {
                progression.Reset();
                progression.Apply(context.StageIndex);
                ApplyContext(catalog, player, context);
                if (!(catalog.Events[context.EventIndex].IsAvailable?.Invoke() ?? true))
                {
                    continue;
                }
                var spawnInfo = CreateSpawnInfo(catalog, player, context);

                var spawnRate = 600;
                var maxSpawns = 5;
                try
                {
                    NPCLoader.EditSpawnRate(player, ref spawnRate, ref maxSpawns);
                }
                catch
                {
                    continue;
                }

                if (spawnRate <= 0 || maxSpawns <= 0)
                {
                    continue;
                }

                ObserveExactSpawnPool(catalog.Observations, spawnInfo, context);
                ObserveChosenSpawn(catalog.Observations, spawnInfo, context);
                ObserveDd2WaveEnemies(catalog.Observations, context);

                if (!ShouldRunFullSpawn(context, catalog)) continue;
                spawnArena.Prepare(
                    catalog.Environments[context.EnvironmentIndex],
                    context.Depth,
                    context.EnvironmentIndex == 1);
                ObserveFullSpawn(catalog.Observations, player, context);
            }
        }
        catch
        {
            return catalog;
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
        }

        return catalog;
    }

    private static IReadOnlyList<SpawnEnvironment> CreateEnvironments()
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
            new(Language.GetTextValue("Bestiary_Biomes.TheHallow"), TileID.HallowedGrass, null, static p => p.ZoneHallow = true),
            new(Language.GetTextValue("Bestiary_Biomes.TheDungeon"), TileID.BlueDungeonBrick, null, static p => p.ZoneDungeon = true)
            {
                WallType = WallID.BlueDungeonUnsafe
            },
            new(Language.GetTextValue("Bestiary_Biomes.UndergroundMushroom"), TileID.MushroomGrass, null, static p => p.ZoneGlowshroom = true),
            new(Language.GetTextValue("Bestiary_Biomes.Meteor"), TileID.Meteorite, null, static p => p.ZoneMeteor = true),
            new(Language.GetTextValue("Bestiary_Biomes.Graveyard"), TileID.Stone, null, static p => p.ZoneGraveyard = true),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnGranite"), TileID.Granite, null, static _ => { }),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnMarble"), TileID.Marble, null, static _ => { }),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnSpiderCave"), TileID.Stone, null, static _ => { })
            {
                WallType = WallID.SpiderUnsafe
            },
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnTemple"), TileID.LihzahrdBrick, null, static _ => { })
            {
                WallType = WallID.LihzahrdBrickUnsafe
            },
            new(Language.GetTextValue("Bestiary_Biomes.UndergroundDesert"), TileID.Sandstone, null, static p => p.ZoneDesert = true),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnSolarPillar"), TileID.Stone, null, static p => p.ZoneTowerSolar = true),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnVortexPillar"), TileID.Stone, null, static p => p.ZoneTowerVortex = true),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnNebulaPillar"), TileID.Stone, null, static p => p.ZoneTowerNebula = true),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnStardustPillar"), TileID.Stone, null, static p => p.ZoneTowerStardust = true)
        };

        var profile = JournalRuntimeProgressionScenarios.CurrentProfile;
        var relevantMods = profile is not null
            ? profile.Document.RequiredMods
                .Select(static requirement => requirement.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : [];
        foreach (var biome in ModContent.GetContent<ModBiome>()
                     .Where(biome => relevantMods.Count == 0 || relevantMods.Contains(biome.Mod.Name))
                     .OrderBy(static biome => biome.FullName, StringComparer.OrdinalIgnoreCase))
        {
            environments.Add(new SpawnEnvironment(
                biome.DisplayName.Value,
                TileID.Stone,
                biome,
                static _ => { }));
        }

        return environments;
    }

    private static IReadOnlyList<SpawnEvent> CreateEvents(
        IReadOnlyCollection<StaticBooleanFlag> customEventFlags)
    {
        List<SpawnEvent> events =
        [
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnNoEvent"), static () => { }),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.FishingWorldNight"), static () => Main.dayTime = false),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.FishingWorldBloodMoon"), static () =>
            {
                Main.dayTime = false;
                Main.bloodMoon = true;
            }),
            new(
                Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnEclipse"),
                static () => Main.eclipse = true,
                static () => NPC.downedMechBossAny),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnRain"), static () => Main.raining = true),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnSandstorm"), static () =>
            {
                Sandstorm.Happening = true;
                Sandstorm.Severity = 1f;
            }),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnSlimeRain"), static () => Main.slimeRain = true),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnGoblinArmy"), static () => ApplyInvasion(InvasionID.GoblinArmy)),
            new(
                Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnFrostLegion"),
                static () => ApplyInvasion(InvasionID.SnowLegion),
                static () => Main.hardMode),
            new(
                Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnPirates"),
                static () => ApplyInvasion(InvasionID.PirateInvasion),
                static () => Main.hardMode),
            new(
                Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnMartians"),
                static () => ApplyInvasion(InvasionID.MartianMadness),
                static () => NPC.downedGolemBoss),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnPumpkinMoon"), static () =>
            {
                Main.dayTime = false;
                Main.pumpkinMoon = true;
            }, static () => NPC.downedPlantBoss),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnFrostMoon"), static () =>
            {
                Main.dayTime = false;
                Main.snowMoon = true;
            }, static () => NPC.downedPlantBoss),
            new(Language.GetTextValue("Mods.ProgressionJournal.UI.NpcSpawnOldOnesArmy"), static () =>
            {
                DD2Event.Ongoing = true;
                Dd2FindProperDifficultyMethod?.Invoke(null, null);
            }, static () => NPC.downedBoss2)
        ];
        // A discovered "active" flag proves how to enter an event, but not when
        // the event becomes available. Keep those NPCs unknown until the profile
        // declares the event stage or a structured prerequisite is available.
        events.AddRange(customEventFlags.Select(flag =>
            new SpawnEvent(
                flag.Name,
                () => flag.Set(true),
                static () => false)));
        return events;
    }

    private static IReadOnlyList<StaticBooleanFlag> CreateCustomEventFlags()
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
            .SelectMany(mod => mod.Code.GetTypes())
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

        for (var environmentIndex = 0; environmentIndex < catalog.Environments.Count; environmentIndex++)
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

            for (var eventIndex = 1; eventIndex < catalog.Events.Count; eventIndex++)
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
            for (var environmentIndex = 0; environmentIndex < catalog.Environments.Count; environmentIndex++)
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
                foreach (var eventIndex in Enumerable.Range(1, catalog.Events.Count - 1))
                {
                    AddVariants(
                        stageIndex,
                        environmentIndex,
                        GetEnvironmentDepths(environmentIndex).First(),
                        eventIndex,
                        includeSafe: false);
                }
            }

            for (var eventIndex = 1; eventIndex < catalog.Events.Count; eventIndex++)
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

        var (x, y) = GetCoordinates(context.Depth, context.EnvironmentIndex == 1);
        player.Center = new Vector2(x * 16f, y * 16f);
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
        Dictionary<int, HashSet<SpawnContext>> observations,
        NPCSpawnInfo spawnInfo,
        SpawnContext context)
    {
        try
        {
            SpawnHelperResetMethod?.Invoke(null, null);
            SpawnHelperDoChecksMethod?.Invoke(null, [spawnInfo]);
            var pool = new Dictionary<int, float> { [0] = 1f };
            if (NpcLoaderNpcsField?.GetValue(null) is IEnumerable<ModNPC> modNpcs)
            {
                foreach (var modNpc in modNpcs)
                {
                    var weight = modNpc.SpawnChance(spawnInfo);
                    if (weight > 0f)
                    {
                        pool[modNpc.Type] = weight;
                    }
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
                    AddObservation(observations, npcType, context);
                }
            }
        }
        catch
        {
            // Unknown third-party spawn code leaves this scenario unproven.
        }
    }

    private static IEnumerable<GlobalNPC> GetEditSpawnPoolGlobals()
    {
        var hookList = EditSpawnPoolHookField?.GetValue(null);
        var hookGlobalsField = hookList?.GetType().GetField(
            "hookGlobals",
            BindingFlags.Instance | BindingFlags.NonPublic);
        return hookGlobalsField?.GetValue(hookList) as GlobalNPC[] ?? [];
    }

    private static void ObserveChosenSpawn(
        Dictionary<int, HashSet<SpawnContext>> observations,
        NPCSpawnInfo spawnInfo,
        SpawnContext context)
    {
        try
        {
            for (var seed = 0; seed < 8; seed++)
            {
                Main.rand = new UnifiedRandom(seed);
                if (NPCLoader.ChooseSpawn(spawnInfo) is { } npcType and > 0
                    && IsOrdinaryNpc(npcType))
                {
                    AddObservation(observations, npcType, context);
                }
            }
        }
        catch
        {
            // Keep unknown when the real selection boundary cannot be executed.
        }
    }

    private static void ObserveDd2WaveEnemies(
        Dictionary<int, HashSet<SpawnContext>> observations,
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
                        AddObservation(observations, npcType, context);
                    }
                }
            }
        }
        catch
        {
            // A changed event implementation remains unproven instead of being guessed.
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

    private static void ObserveFullSpawn(
        Dictionary<int, HashSet<SpawnContext>> observations,
        Player player,
        SpawnContext context)
    {
        try
        {
            var previousDefaultSpawnRate = DefaultSpawnRateField?.GetValue(null);
            var previousNoSpawnCycle = NoSpawnCycleField?.GetValue(null);
            try
            {
                DefaultSpawnRateField?.SetValue(null, 1);
                var seedCount = context is { EnvironmentIndex: 0, EventIndex: 0 }
                    ? FocusedFullSpawnSeedCount
                    : FullSpawnSeedCount;
                for (var seed = 0; seed < seedCount; seed++)
                {
                    PrepareNpcArray();
                    NoSpawnCycleField?.SetValue(null, false);
                    Main.rand = new UnifiedRandom(seed);
                    player.active = true;
                    NPC.SpawnNPC();
                    foreach (var npc in Main.npc.Where(static npc => npc is { active: true }))
                    {
                        if (IsOrdinaryNpc(npc.type))
                        {
                            AddObservation(observations, npc.type, context);
                        }
                    }
                }
            }
            finally
            {
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
        catch
        {
            // Vanilla availability remains unknown for scenarios the full spawn method rejects.
        }
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

    private static IReadOnlyList<string> BuildConditions(
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
        if (environmentIndexes.Length < catalog.Environments.Count)
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
        return conditions;
    }

    private static void AppendBooleanCondition(
        ICollection<string> conditions,
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
            Main.netMode);
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
    }

    private static Player? GetProbePlayer()
    {
        if (Main.myPlayer >= 0 && Main.myPlayer < Main.player.Length
            && Main.player[Main.myPlayer] is { active: true } localPlayer)
        {
            return localPlayer;
        }

        return Main.player.FirstOrDefault(static player => player is { active: true });
    }

    private static BitArray GetModBiomeFlags(Player player)
    {
        return ModBiomeFlagsField?.GetValue(player) as BitArray ?? new BitArray(0);
    }

    private static int GetModBiomeIndex(ModBiome biome)
    {
        return ModBiomeIndexProperty?.GetValue(biome) is int index ? index : -1;
    }
}
