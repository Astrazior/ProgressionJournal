using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ProgressionJournal.Data.Resolvers;

public static class JournalExactDropCatalog
{
    private static readonly Lazy<Entry[]> Entries = new(CreateEntries);

    public static IReadOnlyList<JournalExactDropSource> GetSources(int targetItemId)
    {
        return Entries.Value
            .Where(entry => entry.TargetItemId == targetItemId)
            .Select(ToSource)
            .ToArray();
    }

    public static IReadOnlyList<JournalExactDropSource> GetAllNpcDrops()
    {
        return Entries.Value
            .Where(static entry => entry.SourceNpcType.HasValue)
            .Select(ToSource)
            .ToArray();
    }

    public static IReadOnlyList<JournalExactDropSource> GetAllGlobalDrops()
    {
        return Entries.Value
            .Where(static entry => entry is { IncludeInSnapshot: true, SourceNpcType: null, SourceItemId: null })
            .Select(ToSource)
            .ToArray();
    }

    public static IReadOnlyList<JournalExactDropSource> GetAllItemDrops()
    {
        return Entries.Value
            .Where(static entry => entry is { IncludeInSnapshot: true, SourceItemId: not null })
            .Select(ToSource)
            .ToArray();
    }

    private static JournalExactDropSource ToSource(Entry entry)
    {
        return new JournalExactDropSource(
            entry.SourceName,
            entry.SourceNpcType,
            entry.SourceItemId,
            entry.TargetItemId,
            entry.DropRate,
            entry.StackMin,
            entry.StackMax,
            entry.Conditions
                .Select(static condition => new JournalExactDropCondition(
                    condition.Type,
                    ResolveConditionDescription(condition)))
                .ToArray(),
            entry.Provenance);
    }

    private static string ResolveConditionDescription(ConditionBuilder condition)
    {
        return condition.Kind switch
        {
            ConditionKind.ExpertMode => Language.GetTextValue(
                "Mods.ProgressionJournal.UI.SelectedItemExpertModeCondition"),
            ConditionKind.RightClickTile => Language.GetTextValue(
                "Mods.ProgressionJournal.UI.SelectedItemRightClickTileCondition",
                condition.Arguments),
            ConditionKind.RightClickTileWithItem => Language.GetTextValue(
                "Mods.ProgressionJournal.UI.SelectedItemRightClickTileWithItemCondition",
                condition.Arguments),
            ConditionKind.BothEventsActive => Language.GetTextValue(
                "Mods.ProgressionJournal.UI.SelectedItemBothEventsActiveCondition",
                condition.Arguments),
            ConditionKind.NoNpcAlive => Language.GetTextValue(
                "Mods.ProgressionJournal.UI.SelectedItemNoNpcAliveCondition",
                condition.Arguments),
            ConditionKind.ChargedTile => Language.GetTextValue(
                "Mods.ProgressionJournal.UI.SelectedItemChargedTileCondition",
                condition.Arguments),
            ConditionKind.Hardmode => Language.GetTextValue(
                "Mods.ProgressionJournal.UI.FishingWorldHardmode"),
            ConditionKind.BloodMoon => Language.GetTextValue(
                "Mods.ProgressionJournal.UI.FishingWorldBloodMoon"),
            ConditionKind.Biome => Language.GetTextValue(
                "Mods.ProgressionJournal.UI.FishingBiomeCondition",
                condition.Arguments),
            ConditionKind.BelowSurface => Language.GetTextValue(
                "Mods.ProgressionJournal.UI.SelectedItemBelowSurfaceCondition"),
            ConditionKind.AfterProgression => Language.GetTextValue(
                "Mods.ProgressionJournal.UI.FishingProgressionCondition",
                condition.Arguments),
            ConditionKind.Event => Language.GetTextValue(
                "Mods.ProgressionJournal.UI.NpcSpawnEventCondition",
                condition.Arguments),
            ConditionKind.AfterAllMechanicalBosses => Language.GetTextValue(
                "Mods.ProgressionJournal.UI.SelectedItemAfterAllMechanicalBossesCondition"),
            ConditionKind.SpecialWorldGate => Language.GetTextValue(
                "Mods.ProgressionJournal.UI.SelectedItemSpecialWorldGateCondition",
                condition.Arguments),
            ConditionKind.ZenithWorld => Language.GetTextValue(
                "Mods.ProgressionJournal.UI.SelectedItemZenithWorldCondition"),
            _ => string.Empty
        };
    }

    private static Entry[] CreateEntries()
    {
        List<EntryBuilder> builders = [];
        AddAAModClassic(builders);
        return builders
            .Select(TryCreateEntry)
            .OfType<Entry>()
            .ToArray();
    }

    private static void AddAAModClassic(ICollection<EntryBuilder> builders)
    {
        const string globalNpcSource = "AAModClassic 1.0.12 AAModGlobalNPC.OnKill IL; public precursor: github.com/DiamondWalker/Ancients-Awakened-Patch@2ef96c5";
        AddNpc(builders, globalNpcSource, NPCID.EyeofCthulhu, "AAModClassic/CthulhusBlade", 1f / 4f);

        foreach (var npcType in new[] { 31, 294, 296, 295 })
        {
            AddNpc(builders, globalNpcSource, npcType, "AAModClassic/AquaLance", 0.01f);
        }

        AddNpc(builders, globalNpcSource, 139, "AAModClassic/EnergyCell", 1f, 3, 11);
        AddNpc(builders, globalNpcSource, NPCID.TheDestroyer, "AAModClassic/EnergyCell", 1f, 8, 15);
        AddNpc(builders, globalNpcSource, NPCID.SkeletronPrime, "AAModClassic/EnergyCell", 1f, 8, 15);
        AddNpc(builders, globalNpcSource, NPCID.TheDestroyer, "AAModClassic/LaserRifle", 0.34f);
        AddNpc(builders, globalNpcSource, NPCID.SkeletronPrime, "AAModClassic/LaserRifle", 0.34f);
        AddNpc(builders, globalNpcSource, NPCID.WallofFlesh, "AAModClassic/HKMP5", 0.10f);
        AddNpc(builders, globalNpcSource, 395, "AAModClassic/AlienRifle", 0.12f);
        AddNpc(builders, globalNpcSource, 395, "AAModClassic/EnergyConduit", 0.03f);
        AddNpc(builders, globalNpcSource, NPCID.CursedSkull, "AAModClassic/SkullWand", 0.12f);
        AddNpc(builders, globalNpcSource, NPCID.Vulture, "AAModClassic/VultureFeather", 1f, 1, 2);
        AddNpc(builders, globalNpcSource, NPCID.Drippler, "AAModClassic/BloodyMary", 0.005f);
        AddNpc(builders, globalNpcSource, NPCID.EyeofCthulhu, "AAModClassic/CthulhusBlade", 0.25f);
        AddNpc(builders, globalNpcSource, NPCID.QueenBee, "AAModClassic/BugSwatter", 0.01f);

        foreach (var npcType in new[] { 292, 291, 293 })
        {
            AddNpc(builders, globalNpcSource, npcType, "AAModClassic/M79Parts", 1f / 50f);
        }

        AddNpc(builders, globalNpcSource, NPCID.AngryNimbus, "AAModClassic/ElectricityShard", 1f / 6f);
        AddNpc(builders, globalNpcSource, 24, "AAModClassic/DevilSilk", 1f, 2, 2);
        AddNpc(builders, globalNpcSource, 62, "AAModClassic/DevilSilk", 1f, 4, 4);
        AddNpc(builders, globalNpcSource, 66, "AAModClassic/DevilSilk", 1f, 5, 5);
        AddNpc(builders, globalNpcSource, 156, "AAModClassic/PureEvil", 1f / 3f);
        AddNpc(builders, globalNpcSource, NPCID.Plantera, "AAModClassic/PlanteraPetal", 1f, 30, 39);
        AddNpc(builders, globalNpcSource, NPCID.DukeFishron, "AAModClassic/Seashroom", 0.10f);

        ConditionBuilder hardmode = new(
            "ProgressionJournal.Hardmode",
            ConditionKind.Hardmode,
            []);
        ConditionBuilder belowSurface = new(
            "ProgressionJournal.BelowSurface",
            ConditionKind.BelowSurface,
            []);
        ConditionBuilder afterPlantera = new(
            "ProgressionJournal.AfterProgression",
            ConditionKind.AfterProgression,
            ["Plantera"]);
        AddGlobal(builders, globalNpcSource, "Any enemy", "AAModClassic/ShinyCharm", 1f / 8192f);
        AddGlobal(
            builders,
            globalNpcSource,
            "Any enemy in the underground Mire",
            "AAModClassic/SoulOfSpite",
            1f / 5f,
            conditions: [hardmode, new ConditionBuilder("ProgressionJournal.Biome", ConditionKind.Biome, ["Mire"]), belowSurface]);
        AddGlobal(
            builders,
            globalNpcSource,
            "Any enemy in the underground Inferno",
            "AAModClassic/SoulOfSmite",
            1f / 5f,
            conditions: [hardmode, new ConditionBuilder("ProgressionJournal.Biome", ConditionKind.Biome, ["Inferno"]), belowSurface]);
        AddGlobal(
            builders,
            globalNpcSource,
            "Any enemy in the Mire",
            "AAModClassic/MireKey",
            1f / 2500f,
            conditions: [hardmode, new ConditionBuilder("ProgressionJournal.Biome", ConditionKind.Biome, ["Mire"])]);
        AddGlobal(
            builders,
            globalNpcSource,
            "Any enemy in the Inferno",
            "AAModClassic/InfernoKey",
            1f / 2500f,
            conditions: [hardmode, new ConditionBuilder("ProgressionJournal.Biome", ConditionKind.Biome, ["Inferno"])]);
        AddGlobal(
            builders,
            globalNpcSource,
            "Any enemy in the Void",
            "AAModClassic/VoidKey",
            1f / 1250f,
            conditions: [hardmode, new ConditionBuilder("ProgressionJournal.Biome", ConditionKind.Biome, ["Void"])]);
        AddGlobal(
            builders,
            globalNpcSource,
            "Any enemy in the Terrarium",
            "AAModClassic/TerraPrism",
            1f / 100f,
            conditions: [hardmode, new ConditionBuilder("ProgressionJournal.Biome", ConditionKind.Biome, ["Terrarium"]), afterPlantera]);
        AddGlobal(
            builders,
            globalNpcSource,
            "Any enemy in the Inferno or Mire",
            "AAModClassic/ChaosPrism",
            1f / 100f,
            conditions: [hardmode, new ConditionBuilder("ProgressionJournal.Biome", ConditionKind.Biome, ["Inferno or Mire"]), afterPlantera]);
        AddGlobal(
            builders,
            globalNpcSource,
            "Any enemy",
            "AAModClassic/BloodRune",
            1f / 8f,
            conditions: [new ConditionBuilder("ProgressionJournal.BloodMoon", ConditionKind.BloodMoon, [])]);
        AddGlobal(
            builders,
            globalNpcSource,
            "Pirate Invasion enemies",
            "AAModClassic/PirateBooty",
            15f / 64f,
            stackMax: 2,
            conditions: [new ConditionBuilder("ProgressionJournal.Event", ConditionKind.Event, ["Pirate Invasion"])]);
        AddGlobal(
            builders,
            globalNpcSource,
            "Solar Eclipse enemies",
            "AAModClassic/MonsterSoul",
            1f / 8f,
            conditions: [new ConditionBuilder("ProgressionJournal.Event", ConditionKind.Event, ["Solar Eclipse"])]);
        AddGlobal(
            builders,
            globalNpcSource,
            "Pumpkin Moon enemies",
            "AAModClassic/HalloweenTreat",
            1f / 8f,
            conditions: [new ConditionBuilder("ProgressionJournal.Event", ConditionKind.Event, ["Pumpkin Moon"])]);
        AddGlobal(
            builders,
            globalNpcSource,
            "Frost Moon enemies",
            "AAModClassic/ChristmasCheer",
            1f / 8f,
            conditions: [new ConditionBuilder("ProgressionJournal.Event", ConditionKind.Event, ["Frost Moon"])]);
        AddGlobal(
            builders,
            globalNpcSource,
            "Martian Madness enemies",
            "AAModClassic/MartianCredit",
            1f / 8f,
            conditions: [new ConditionBuilder("ProgressionJournal.Event", ConditionKind.Event, ["Martian Madness"])]);
        AddGlobal(
            builders,
            globalNpcSource,
            "Goblin Army enemies",
            "AAModClassic/GoblinSoul",
            1f / 20f,
            conditions:
            [
                new ConditionBuilder("ProgressionJournal.Event", ConditionKind.Event, ["Goblin Army"]),
                new ConditionBuilder("ProgressionJournal.AfterProgression", ConditionKind.AfterProgression, ["the first defeated Goblin Army"])
            ]);

        ConditionBuilder afterAllMechs = new(
            "ProgressionJournal.AfterAllMechanicalBosses",
            ConditionKind.AfterAllMechanicalBosses,
            []);
        AddNpc(
            builders,
            globalNpcSource,
            166,
            "AAModClassic/HeroRelics",
            0.40f,
            2,
            3,
            [afterAllMechs]);
        AddNpc(
            builders,
            globalNpcSource,
            162,
            "AAModClassic/HeroRelics",
            0.40f,
            2,
            3,
            [afterAllMechs]);
        ConditionBuilder afterSkeletron = new(
            "ProgressionJournal.AfterProgression",
            ConditionKind.AfterProgression,
            ["Skeletron"]);
        AddNpc(builders, globalNpcSource, 197, "AAModClassic/VikingRelic", 1f / 3f, 1, 2, [afterSkeletron]);
        AddNpc(builders, globalNpcSource, 167, "AAModClassic/VikingRelic", 1f / 3f, 1, 2, [afterSkeletron]);
        AddNpc(
            builders,
            globalNpcSource,
            471,
            "AAModClassic/GoblinTinkererDoll",
            1f / 4f,
            conditions:
            [
                new ConditionBuilder(
                    "ProgressionJournal.SpecialWorldGate",
                    ConditionKind.SpecialWorldGate,
                    ["all Ancients defeated", "Apocalyptic world option enabled"])
            ]);

        ConditionBuilder[] expertMode = [new(
            "Terraria.GameContent.ItemDropRules.Conditions+IsExpert",
            ConditionKind.ExpertMode,
            [])];
        foreach (var npcType in new[] { 195, 196, 52 })
        {
            AddNpc(
                builders,
                globalNpcSource,
                npcType,
                "AAModClassic/AncientGoldLeggings",
                1f / 20f,
                conditions: expertMode);
        }

        foreach (var npcType in new[] { 45, 172 })
        {
            AddNpc(
                builders,
                globalNpcSource,
                npcType,
                "AAModClassic/AncientGoldChestplate",
                1f / 20f,
                conditions: expertMode);
        }

        const string tileSource = "AAModClassic 1.0.12 installed assembly ModTile.RightClick IL";
        AddWorld(
            builders,
            tileSource,
            "Aleister's Book tile",
            "AAModClassic/AleisterBook",
            "AAModClassic/AleisterBook",
            [new ConditionBuilder("ProgressionJournal.RightClickTile", ConditionKind.RightClickTile, ["Aleister's Book tile"])]);
        AddWorld(
            builders,
            tileSource,
            "Worm Altar",
            sourceItemReference: null,
            "AAModClassic/EquinoxWorm",
            [
                new ConditionBuilder("ProgressionJournal.RightClickTile", ConditionKind.RightClickTileWithItem, ["Worm Altar", "Worm Idol"]),
                new ConditionBuilder("ProgressionJournal.BothEventsActive", ConditionKind.BothEventsActive, ["Star and Gravity"]),
                new ConditionBuilder("ProgressionJournal.NoNpcAlive", ConditionKind.NoNpcAlive, ["Worm Spawn"])
            ]);
        AddWorld(
            builders,
            tileSource,
            "Core Activator",
            sourceItemReference: null,
            "AAModClassic/TerraPrism",
            [
                new ConditionBuilder("ProgressionJournal.RightClickTile", ConditionKind.RightClickTile, ["Core Activator"]),
                new ConditionBuilder("ProgressionJournal.ChargedTile", ConditionKind.ChargedTile, ["Core Activator"])
            ]);

        AddDeveloperBagSources(builders);
    }

    private static void AddDeveloperBagSources(ICollection<EntryBuilder> builders)
    {
        const string provenance = "AAModClassic 1.0.12 treasure-bag RightClick and ZAAPlayer.DropDevArmor IL";
        string[] developerBags =
        [
            "HallamBag", "BigEBag", "BegBag", "MaskanoBag", "CharlieBag", "TailsBag",
            "DellyBag", "DallinBag", "AvesBag", "TiedBag", "MoonBag", "GroxBag", "CCBag",
            "GibsBag", "ApawnBag", "MikpinBag", "FargoBag", "BlazenBag", "CerberusBag",
            "PlutoBag", "VoidEyeBag", "AnarchyBag", "ShoxBag"
        ];
        string[] preHardmodeSources =
        [
            "DesertDjinnTreasureBag", "MushroomMonarchTreasureBag", "TruffleToadTreasureBag",
            "FeudalFungusTreasureBag", "SagittariusTreasureBag", "SubzeroSerpentTreasureBag",
            "HydraTreasureBag", "BroodmotherTreasureBag", "GripsOfChaosTreasureBag"
        ];
        string[] hardmodeSources =
        [
            "AnubisTreasureBag", "TechnoTruffleTreasureBag", "RetrieverTreasureBag",
            "RaiderUltimaTreasureBag", "OrthrusXTreasureBag"
        ];
        string[] postPlanteraSources = ["GreedTreasureBag", "AthenaTreasureBag"];
        string[] postMoonLordSources =
        [
            "AnubisATreasureBag", "ZeroTreasureBag", "YamataTreasureBag", "AkumaTreasureBag",
            "GreedATreasureBag", "SistersOfDiscordTreasureBag", "RajahRabbitTreasureBag",
            "AthenaATreasureBag"
        ];
        string[] superAncientSources = ["RajahRabbitATreasureBag"];
        string[] rareSuperAncientSources =
        [
            "ShenDoragonTreasureBag", "SoulOfCthulhuTreasureBag", "InfinityZeroTreasureBag"
        ];

        foreach (var developerBag in developerBags)
        {
            var target = $"AAModClassic/{developerBag}";
            AddItemSources(builders, provenance, preHardmodeSources, target, 1f / 250f);
            AddItemSources(builders, provenance, hardmodeSources, target, 1f / 260f);
            AddItemSources(builders, provenance, postPlanteraSources, target, 1f / 290f);
            AddItemSources(builders, provenance, postMoonLordSources, target, 1f / 330f);
            AddItem(builders, provenance, "EquinoxWormsTreasureBag", target, 1f / 660f);
            AddItemSources(builders, provenance, superAncientSources, target, 1f / 330f);
            AddItemSources(builders, provenance, rareSuperAncientSources, target, 1f / 3300f);
        }

        ConditionBuilder[] zenithWorld =
        [
            new ConditionBuilder("ProgressionJournal.ZenithWorld", ConditionKind.ZenithWorld, [])
        ];
        const string planterrorBag = "AAModClassic/PlanterrorBag";
        AddItemSources(builders, provenance, hardmodeSources, planterrorBag, 1f / 260f, zenithWorld);
        AddItemSources(builders, provenance, postPlanteraSources, planterrorBag, 1f / 290f, zenithWorld);
        AddItemSources(builders, provenance, postMoonLordSources, planterrorBag, 1f / 330f, zenithWorld);
        AddItem(builders, provenance, "EquinoxWormsTreasureBag", planterrorBag, 1f / 660f, zenithWorld);
        AddItemSources(builders, provenance, superAncientSources, planterrorBag, 1f / 330f, zenithWorld);
        AddItemSources(builders, provenance, rareSuperAncientSources, planterrorBag, 1f / 3300f, zenithWorld);

        const string monochromeApple = "AAModClassic/MonochromeApple";
        AddItemSources(builders, provenance, hardmodeSources, monochromeApple, 1f / 260f);
        AddItemSources(builders, provenance, postPlanteraSources, monochromeApple, 1f / 290f);
        AddItemSources(builders, provenance, postMoonLordSources, monochromeApple, 1f / 330f);
        AddItem(builders, provenance, "EquinoxWormsTreasureBag", monochromeApple, 1f / 660f);
        AddItemSources(builders, provenance, superAncientSources, monochromeApple, 1f / 330f);
        AddItemSources(builders, provenance, rareSuperAncientSources, monochromeApple, 1f / 3300f);

        const string furyForger = "AAModClassic/FuryForger";
        AddItemSources(builders, provenance, postPlanteraSources, furyForger, 1f / 290f);

        const string aleisterStaff = "AAModClassic/AleisterStaff";
        AddItemSources(builders, provenance, postMoonLordSources, aleisterStaff, 1f / 330f);
        AddItem(builders, provenance, "EquinoxWormsTreasureBag", aleisterStaff, 1f / 660f);
        AddItemSources(builders, provenance, superAncientSources, aleisterStaff, 1f / 330f);
        AddItemSources(builders, provenance, rareSuperAncientSources, aleisterStaff, 1f / 3300f);

        const string extravagantTerratool = "AAModClassic/ExtravagantTerratool";
        AddItemSources(builders, provenance, superAncientSources, extravagantTerratool, 1f / 330f);
        AddItemSources(builders, provenance, rareSuperAncientSources, extravagantTerratool, 1f / 3300f);
    }

    private static void AddItemSources(
        ICollection<EntryBuilder> builders,
        string provenance,
        IEnumerable<string> sourceNames,
        string targetReference,
        float dropRate,
        ConditionBuilder[]? conditions = null)
    {
        foreach (var sourceName in sourceNames)
        {
            AddItem(builders, provenance, sourceName, targetReference, dropRate, conditions);
        }
    }

    private static void AddItem(
        ICollection<EntryBuilder> builders,
        string provenance,
        string sourceName,
        string targetReference,
        float dropRate,
        ConditionBuilder[]? conditions = null)
    {
        builders.Add(new EntryBuilder(
            SourceName: string.Empty,
            SourceNpcType: null,
            $"AAModClassic/{sourceName}",
            targetReference,
            dropRate,
            1,
            1,
            conditions ?? [],
            provenance,
            IncludeInSnapshot: true));
    }

    private static void AddNpc(
        ICollection<EntryBuilder> builders,
        string provenance,
        int sourceNpcType,
        string targetReference,
        float dropRate,
        int stackMin = 1,
        int stackMax = 1,
        ConditionBuilder[]? conditions = null)
    {
        builders.Add(new EntryBuilder(
            Lang.GetNPCNameValue(sourceNpcType),
            sourceNpcType,
            SourceItemReference: null,
            targetReference,
            dropRate,
            stackMin,
            stackMax,
            conditions ?? [],
            provenance,
            IncludeInSnapshot: true));
    }

    private static void AddGlobal(
        ICollection<EntryBuilder> builders,
        string provenance,
        string sourceName,
        string targetReference,
        float dropRate,
        int stackMin = 1,
        int stackMax = 1,
        ConditionBuilder[]? conditions = null)
    {
        builders.Add(new EntryBuilder(
            sourceName,
            SourceNpcType: null,
            SourceItemReference: null,
            targetReference,
            dropRate,
            stackMin,
            stackMax,
            conditions ?? [],
            provenance,
            IncludeInSnapshot: true));
    }

    private static void AddWorld(
        ICollection<EntryBuilder> builders,
        string provenance,
        string sourceName,
        string? sourceItemReference,
        string targetReference,
        ConditionBuilder[] conditions)
    {
        builders.Add(new EntryBuilder(
            sourceName,
            SourceNpcType: null,
            sourceItemReference,
            targetReference,
            1f,
            1,
            1,
            conditions,
            provenance,
            IncludeInSnapshot: false));
    }

    private static Entry? TryCreateEntry(EntryBuilder builder)
    {
        if (!TryResolveItemReference(builder.TargetReference, out var targetItemId))
        {
            return null;
        }

        int? sourceItemId = null;
        var sourceName = builder.SourceName;
        if (builder.SourceItemReference is not null
            && TryResolveItemReference(builder.SourceItemReference, out var resolvedSourceItemId))
        {
            sourceItemId = resolvedSourceItemId;
            sourceName = Lang.GetItemNameValue(resolvedSourceItemId);
        }

        return new Entry(
            sourceName,
            builder.SourceNpcType,
            sourceItemId,
            targetItemId,
            builder.DropRate,
            builder.StackMin,
            builder.StackMax,
            builder.Conditions,
            builder.Provenance,
            builder.IncludeInSnapshot);
    }

    private static bool TryResolveItemReference(string reference, out int itemId)
    {
        itemId = ItemID.None;
        var separator = reference.IndexOf('/');
        if (separator <= 0 || separator >= reference.Length - 1)
        {
            return false;
        }

        var modName = reference[..separator];
        var itemName = reference[(separator + 1)..];
        if (string.Equals(modName, "Terraria", StringComparison.OrdinalIgnoreCase))
        {
            return ItemID.Search.TryGetId(itemName, out itemId);
        }

        if (!ModContent.TryFind<ModItem>($"{modName}/{itemName}", out var modItem))
        {
            return false;
        }

        itemId = modItem.Type;
        return true;
    }

    private sealed record EntryBuilder(
        string SourceName,
        int? SourceNpcType,
        string? SourceItemReference,
        string TargetReference,
        float DropRate,
        int StackMin,
        int StackMax,
        ConditionBuilder[] Conditions,
        string Provenance,
        bool IncludeInSnapshot);

    private sealed record Entry(
        string SourceName,
        int? SourceNpcType,
        int? SourceItemId,
        int TargetItemId,
        float DropRate,
        int StackMin,
        int StackMax,
        ConditionBuilder[] Conditions,
        string Provenance,
        bool IncludeInSnapshot);

    private sealed record ConditionBuilder(string Type, ConditionKind Kind, object[] Arguments);

    private enum ConditionKind
    {
        ExpertMode,
        RightClickTile,
        RightClickTileWithItem,
        BothEventsActive,
        NoNpcAlive,
        ChargedTile,
        Hardmode,
        BloodMoon,
        Biome,
        BelowSurface,
        AfterProgression,
        Event,
        AfterAllMechanicalBosses,
        SpecialWorldGate,
        ZenithWorld
    }
}

public sealed record JournalExactDropSource(
    string SourceName,
    int? SourceNpcType,
    int? SourceItemId,
    int TargetItemId,
    float DropRate,
    int StackMin,
    int StackMax,
    JournalExactDropCondition[] Conditions,
    string Provenance);

public sealed record JournalExactDropCondition(string Type, string Description);
