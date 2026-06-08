using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.Utilities;

namespace ProgressionJournal.Data.Resolvers;

internal static class JournalFishingSourceResolver
{
    private const int RandomSeedCount = 64;

    private static readonly object SyncRoot = new();
    private static Dictionary<int, HashSet<ProbeContext>>? _catalog;

    private delegate void RollFishingItemDelegate(Projectile projectile, ref FishingAttempt attempt);

    private enum ProbeLiquid
    {
        Water,
        Lava,
        Honey
    }

    private sealed record ProbeBiome(string? LocalizationKey, Action<Player> Apply);

    private sealed record ProbeWorld(
        bool Hardmode,
        bool BloodMoon,
        bool DownedSkeletron,
        bool Remix,
        bool NotTheBees);

    private readonly record struct ProbeContext(
        ProbeLiquid Liquid,
        int BiomeIndex,
        int Depth,
        int WorldIndex);

    private static readonly ProbeBiome[] Biomes =
    [
        new(null, static _ => { }),
        new("Bestiary_Biomes.Ocean", static player => player.ZoneBeach = true),
        new("Bestiary_Biomes.Desert", static player => player.ZoneDesert = true),
        new("Bestiary_Biomes.Snow", static player => player.ZoneSnow = true),
        new("Bestiary_Biomes.Jungle", static player => player.ZoneJungle = true),
        new("Bestiary_Biomes.TheCorruption", static player => player.ZoneCorrupt = true),
        new("Bestiary_Biomes.Crimson", static player => player.ZoneCrimson = true),
        new("Bestiary_Biomes.TheHallow", static player => player.ZoneHallow = true),
        new("Bestiary_Biomes.TheDungeon", static player => player.ZoneDungeon = true),
        new("Bestiary_Biomes.UndergroundMushroom", static player => player.ZoneGlowshroom = true)
    ];

    private static readonly string[] DepthLocalizationKeys =
    [
        "Bestiary_Biomes.Sky",
        "Bestiary_Biomes.Surface",
        "Bestiary_Biomes.Underground",
        "Bestiary_Biomes.Caverns",
        "Bestiary_Biomes.TheUnderworld"
    ];

    private static readonly ProbeWorld[] Worlds =
    [
        new(false, false, false, false, false),
        new(false, false, true, false, false),
        new(true, false, true, false, false),
        new(false, true, true, false, false),
        new(true, true, true, false, false),
        new(false, false, false, true, false),
        new(false, false, false, false, true)
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
        if (!catalog.TryGetValue(itemId, out var contexts) || contexts.Count == 0)
        {
            return sources;
        }

        sources.Add(new JournalFishingSource(BuildConditions(contexts)));
        return sources;
    }

    private static Dictionary<int, HashSet<ProbeContext>> GetCatalog()
    {
        lock (SyncRoot)
        {
            return _catalog ??= BuildCatalog();
        }
    }

    private static Dictionary<int, HashSet<ProbeContext>> BuildCatalog()
    {
        var method = typeof(Projectile).GetMethod(
            "FishingCheck_RollItemDrop",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (method is null)
        {
            return [];
        }

        RollFishingItemDelegate rollFishingItem;
        try
        {
            rollFishingItem = method.CreateDelegate<RollFishingItemDelegate>();
        }
        catch
        {
            return [];
        }

        var playerIndex = FindProbePlayerIndex();
        if (playerIndex < 0)
        {
            return [];
        }

        var previousPlayer = Main.player[playerIndex];
        var previousRandom = Main.rand;
        var previousHardmode = Main.hardMode;
        var previousBloodMoon = Main.bloodMoon;
        var previousRemix = Main.remixWorld;
        var previousNotTheBees = Main.notTheBeesWorld;
        var previousDownedSkeletron = NPC.downedBoss3;
        var catalog = new Dictionary<int, HashSet<ProbeContext>>();

        try
        {
            var player = new Player
            {
                whoAmI = playerIndex
            };
            Main.player[playerIndex] = player;

            var projectile = new Projectile
            {
                owner = playerIndex
            };

            for (var worldIndex = 0; worldIndex < Worlds.Length; worldIndex++)
            {
                ApplyWorld(Worlds[worldIndex]);

                for (var biomeIndex = 0; biomeIndex < Biomes.Length; biomeIndex++)
                {
                    ResetBiomes(player);
                    Biomes[biomeIndex].Apply(player);

                    for (var depth = 0; depth < DepthLocalizationKeys.Length; depth++)
                    {
                        for (var liquid = ProbeLiquid.Water; liquid <= ProbeLiquid.Honey; liquid++)
                        {
                            var context = new ProbeContext(liquid, biomeIndex, depth, worldIndex);
                            ProbeContextDrops(catalog, rollFishingItem, projectile, context);
                        }
                    }
                }
            }
        }
        catch
        {
            return [];
        }
        finally
        {
            Main.player[playerIndex] = previousPlayer;
            Main.rand = previousRandom;
            Main.hardMode = previousHardmode;
            Main.bloodMoon = previousBloodMoon;
            Main.remixWorld = previousRemix;
            Main.notTheBeesWorld = previousNotTheBees;
            NPC.downedBoss3 = previousDownedSkeletron;
        }

        return catalog;
    }

    private static void ProbeContextDrops(
        Dictionary<int, HashSet<ProbeContext>> catalog,
        RollFishingItemDelegate rollFishingItem,
        Projectile projectile,
        ProbeContext context)
    {
        for (var rarity = 0; rarity < 5; rarity++)
        {
            for (var seed = 0; seed < RandomSeedCount; seed++)
            {
                Main.rand = new UnifiedRandom(seed);
                var attempt = CreateAttempt(context, rarity);
                rollFishingItem(projectile, ref attempt);

                if (!JournalItemUtilities.IsValidItemId(attempt.rolledItemDrop))
                {
                    continue;
                }

                if (!catalog.TryGetValue(attempt.rolledItemDrop, out var contexts))
                {
                    contexts = [];
                    catalog[attempt.rolledItemDrop] = contexts;
                }

                contexts.Add(context);
            }
        }
    }

    private static FishingAttempt CreateAttempt(ProbeContext context, int rarity)
    {
        var y = context.Depth switch
        {
            0 => Math.Max(10, (int)(Main.worldSurface * 0.25)),
            1 => Math.Max(10, (int)(Main.worldSurface * 0.75)),
            2 => Math.Max(10, (int)((Main.worldSurface + Main.rockLayer) * 0.5)),
            3 => Math.Min(Main.maxTilesY - 10, (int)Main.rockLayer + 100),
            _ => Main.maxTilesY - 100
        };

        return new FishingAttempt
        {
            X = Main.maxTilesX / 2,
            Y = y,
            common = rarity == 0,
            uncommon = rarity == 1,
            rare = rarity == 2,
            veryrare = rarity == 3,
            legendary = rarity == 4,
            crate = false,
            inLava = context.Liquid == ProbeLiquid.Lava,
            inHoney = context.Liquid == ProbeLiquid.Honey,
            CanFishInLava = true,
            waterTilesCount = 1000,
            waterNeededToFish = 300,
            fishingLevel = 200,
            questFish = -1,
            heightLevel = context.Depth,
            rolledItemDrop = 0,
            rolledEnemySpawn = 0
        };
    }

    private static IReadOnlyList<string> BuildConditions(IReadOnlyCollection<ProbeContext> contexts)
    {
        var conditions = new List<string>();
        var liquids = contexts.Select(static context => context.Liquid).Distinct().Order().ToArray();
        var biomes = contexts.Select(static context => context.BiomeIndex).Distinct().Order().ToArray();
        var depths = contexts.Select(static context => context.Depth).Distinct().Order().ToArray();
        var worlds = contexts.Select(static context => context.WorldIndex).Distinct().Order().ToArray();

        if (liquids.Length < Enum.GetValues<ProbeLiquid>().Length)
        {
            conditions.Add(Language.GetTextValue(
                "Mods.ProgressionJournal.UI.FishingLiquidCondition",
                string.Join(", ", liquids.Select(GetLiquidName))));
        }

        if (biomes.Length < Biomes.Length)
        {
            var biomeNames = biomes
                .Select(static index => Biomes[index].LocalizationKey)
                .Where(static key => key is not null)
                .Select(static key => Language.GetTextValue(key!))
                .ToArray();
            if (biomeNames.Length > 0)
            {
                conditions.Add(Language.GetTextValue(
                    "Mods.ProgressionJournal.UI.FishingBiomeCondition",
                    string.Join(", ", biomeNames)));
            }
        }

        if (depths.Length < DepthLocalizationKeys.Length)
        {
            conditions.Add(Language.GetTextValue(
                "Mods.ProgressionJournal.UI.FishingDepthCondition",
                string.Join(", ", depths.Select(static depth => Language.GetTextValue(DepthLocalizationKeys[depth])))));
        }

        AppendWorldConditions(conditions, worlds);
        return conditions;
    }

    private static void AppendWorldConditions(ICollection<string> conditions, IReadOnlyCollection<int> worldIndexes)
    {
        var worlds = worldIndexes.Select(static index => Worlds[index]).ToArray();
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

        if (worlds.All(static world => world.DownedSkeletron))
        {
            worldConditions.Add(Language.GetTextValue("Mods.ProgressionJournal.UI.FishingWorldPostSkeletron"));
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

    private static int FindProbePlayerIndex()
    {
        for (var index = Main.player.Length - 1; index >= 0; index--)
        {
            if (index != Main.myPlayer && (Main.player[index] is null || !Main.player[index].active))
            {
                return index;
            }
        }

        return -1;
    }

    private static void ApplyWorld(ProbeWorld world)
    {
        Main.hardMode = world.Hardmode;
        Main.bloodMoon = world.BloodMoon;
        Main.remixWorld = world.Remix;
        Main.notTheBeesWorld = world.NotTheBees;
        NPC.downedBoss3 = world.DownedSkeletron;
    }

    private static void ResetBiomes(Player player)
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
}
