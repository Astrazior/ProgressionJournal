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
            .Where(static entry => entry is
            {
                IncludeInSnapshot: true,
                SourceNpcType: null,
                SourceItemId: null,
                SourceReference: null
            })
            .Select(ToSource)
            .ToArray();
    }

    public static IReadOnlyList<JournalExactDropSource> GetAllWorldDrops()
    {
        return Entries.Value
            .Where(static entry => entry is
            {
                IncludeInSnapshot: true,
                SourceNpcType: null,
                SourceItemId: null,
                SourceReference: not null
            })
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
            entry.ShowDropRate,
            entry.Conditions
                .Select(static condition => new JournalExactDropCondition(
                    condition.Type,
                    ResolveConditionDescription(condition)))
                .ToArray(),
            entry.Provenance,
            entry.SourceReference);
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
            ConditionKind.LocalizationKey => Language.GetTextValue(
                condition.Type,
                condition.Arguments),
            _ => string.Empty
        };
    }

    private static Entry[] CreateEntries()
    {
        List<EntryBuilder> builders = [];
        AddVanillaTreeFruits(builders);
        AddAAModClassic(builders);
        AddCalamity(builders);
        AddThorium(builders);
        return builders
            .Select(TryCreateEntry)
            .OfType<Entry>()
            .ToArray();
    }

    private static void AddVanillaTreeFruits(ICollection<EntryBuilder> builders)
    {
        const string provenance = "Terraria 1.4 vanilla tree fruit drops; terraria.wiki.gg/wiki/Fruits";
        AddVanillaTreeFruitGroup(
            builders,
            provenance,
            "Mods.ProgressionJournal.UI.VanillaForestFruitTreeSource",
            "Terraria/Apple",
            "Terraria/Apricot",
            "Terraria/Grapefruit",
            "Terraria/Lemon",
            "Terraria/Peach");
        AddVanillaTreeFruitGroup(
            builders,
            provenance,
            "Mods.ProgressionJournal.UI.VanillaBorealFruitTreeSource",
            "Terraria/Cherry",
            "Terraria/Plum");
        AddVanillaTreeFruitGroup(
            builders,
            provenance,
            "Mods.ProgressionJournal.UI.VanillaEbonwoodFruitTreeSource",
            "Terraria/BlackCurrant",
            "Terraria/Elderberry");
        AddVanillaTreeFruitGroup(
            builders,
            provenance,
            "Mods.ProgressionJournal.UI.VanillaShadewoodFruitTreeSource",
            "Terraria/BloodOrange",
            "Terraria/Rambutan");
        AddVanillaTreeFruitGroup(
            builders,
            provenance,
            "Mods.ProgressionJournal.UI.VanillaMahoganyFruitTreeSource",
            "Terraria/Mango",
            "Terraria/Pineapple");
        AddVanillaTreeFruitGroup(
            builders,
            provenance,
            "Mods.ProgressionJournal.UI.VanillaPalmFruitTreeSource",
            "Terraria/Banana",
            "Terraria/Coconut");
        AddVanillaTreeFruitGroup(
            builders,
            provenance,
            "Mods.ProgressionJournal.UI.VanillaPearlwoodFruitTreeSource",
            "Terraria/Dragonfruit",
            "Terraria/Starfruit");
        AddVanillaTreeFruitGroup(
            builders,
            provenance,
            "Mods.ProgressionJournal.UI.VanillaAshFruitTreeSource",
            "Terraria/Pomegranate",
            "Terraria/SpicyPepper");
    }

    private static void AddVanillaTreeFruitGroup(
        ICollection<EntryBuilder> builders,
        string provenance,
        string sourceLocalizationKey,
        params string[] targetReferences)
    {
        var sourceName = Language.GetTextValue(sourceLocalizationKey);
        ConditionBuilder[] conditions =
        [
            new(
                "Mods.ProgressionJournal.UI.VanillaFruitTreeCuttingCondition",
                ConditionKind.LocalizationKey,
                [sourceName])
        ];
        foreach (var targetReference in targetReferences)
        {
            AddWorld(
                builders,
                provenance,
                sourceName,
                sourceItemReference: null,
                targetReference,
                conditions,
                showDropRate: false);
        }
    }

    private static void AddThorium(ICollection<EntryBuilder> builders)
    {
        AddWorld(
            builders,
            "Thorium Mod 1.7.2.6 OceanCrystal tile source",
            "Ocean Crystal",
            sourceItemReference: null,
            "ThoriumMod/CrystalWave",
            [
                new ConditionBuilder(
                    "Mods.ProgressionJournal.UI.ThoriumCrystalWaveOceanCrystalCondition",
                    ConditionKind.LocalizationKey,
                    [])
            ],
            sourceReference: "ThoriumMod/OceanCrystal",
            includeInSnapshot: true);
    }

    private static void AddCalamity(ICollection<EntryBuilder> builders)
    {
        const string acidwoodFruitProvenance =
            "CalamityMod 2.2.1 Acidwood tree shaking; calamitymod.wiki.gg/wiki/Plants";
        ConditionBuilder[] acidwoodFruitConditions =
        [
            new(
                "Mods.ProgressionJournal.UI.AcidwoodFruitTreeShakingCondition",
                ConditionKind.LocalizationKey,
                [])
        ];
        var acidwoodTreeSourceName =
            Language.GetTextValue("Mods.ProgressionJournal.UI.AcidwoodTreeShakingSource");
        AddWorld(
            builders,
            acidwoodFruitProvenance,
            acidwoodTreeSourceName,
            sourceItemReference: null,
            "CalamityMod/Jackfruit",
            acidwoodFruitConditions,
            dropRate: 1f / 31f,
            sourceReference: "CalamityMod/AcidwoodTreeShaking",
            includeInSnapshot: true);
        AddWorld(
            builders,
            acidwoodFruitProvenance,
            acidwoodTreeSourceName,
            sourceItemReference: null,
            "CalamityMod/Salak",
            acidwoodFruitConditions,
            dropRate: 1f / 31f,
            sourceReference: "CalamityMod/AcidwoodTreeShaking",
            includeInSnapshot: true);

        const string provenance =
            "CalamityMod 2.2.1 Divine Swine interaction; calamitymod.wiki.gg/wiki/Divine_Swine";
        AddModNpc(
            builders,
            provenance,
            "CalamityMod/DivineSwine",
            "CalamityMod/DeliciousMeat",
            1f,
            conditions:
            [
                new ConditionBuilder(
                    "Mods.ProgressionJournal.UI.DeliciousMeatDivineSwineOfferingCondition",
                    ConditionKind.LocalizationKey,
                    []),
                new ConditionBuilder(
                    "Mods.ProgressionJournal.UI.DeliciousMeatDivineSwineAvailabilityCondition",
                    ConditionKind.LocalizationKey,
                    [])
            ]);

        const string gluttonyBlenderProvenance =
            "CalamityMod 2.2.1 Gluttony Blender interaction; calamitymod.wiki.gg/wiki/Gluttony_Blender";
        AddWorld(
            builders,
            gluttonyBlenderProvenance,
            sourceName: string.Empty,
            "CalamityMod/GluttonyBlender",
            "CalamityMod/QualitySlop",
            [
                new ConditionBuilder(
                    "Mods.ProgressionJournal.UI.QualitySlopGluttonyBlenderInteractionCondition",
                    ConditionKind.LocalizationKey,
                    []),
                new ConditionBuilder(
                    "Mods.ProgressionJournal.UI.QualitySlopGluttonyBlenderChanceCondition",
                    ConditionKind.LocalizationKey,
                    []),
                new ConditionBuilder(
                    "Mods.ProgressionJournal.UI.GluttonyBlenderAvailabilityCondition",
                    ConditionKind.LocalizationKey,
                    [])
            ],
            dropRate: 0.005f,
            showDropRate: false,
            includeInSnapshot: true);
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
        AddAAModClassicReviewSources(builders);
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

        ConditionBuilder[] developerBagCondition =
        [
            new ConditionBuilder(
                "Mods.ProgressionJournal.UI.AAModClassicDeveloperBagCondition",
                ConditionKind.LocalizationKey,
                [])
        ];
        foreach (var (source, target) in new[]
                 {
                     ("AvesBag", "AvesWings"),
                     ("BigEBag", "BigEWings"),
                     ("BlazenBag", "BlazenWings"),
                     ("CharlieBag", "CharlieWings"),
                     ("GibsBag", "GibsWings"),
                     ("GroxBag", "GroxWings"),
                     ("MoonBag", "MoonWings")
                 })
        {
            AddWorld(
                builders,
                "AAModClassic 1.0.17 developer-bag RightClick IL",
                sourceName: string.Empty,
                $"AAModClassic/{source}",
                $"AAModClassic/{target}",
                developerBagCondition,
                showDropRate: false,
                includeInSnapshot: true);
        }

        ConditionBuilder[] developerPageCondition =
        [
            new ConditionBuilder(
                "Mods.ProgressionJournal.UI.AAModClassicDeveloperPageCondition",
                ConditionKind.LocalizationKey,
                [])
        ];
        foreach (var source in postPlanteraSources
                     .Concat(postMoonLordSources)
                     .Concat(superAncientSources)
                     .Concat(rareSuperAncientSources))
        {
            AddWorld(
                builders,
                "AAModClassic 1.0.17 ZAAPlayer.DropDevArmor IL",
                sourceName: string.Empty,
                $"AAModClassic/{source}",
                "AAModClassic/APageOfTheRuneBook",
                developerPageCondition,
                showDropRate: false,
                includeInSnapshot: true);
        }
    }

    private static void AddAAModClassicReviewSources(ICollection<EntryBuilder> builders)
    {
        const string provenance = "AAModClassic 1.0.17 installed assembly IL";
        AddWorld(
            builders,
            provenance,
            "Ocean floor",
            sourceItemReference: null,
            "Terraria/Starfish",
            [
                new ConditionBuilder(
                    "Mods.ProgressionJournal.UI.AAModClassicStarfishOceanFloorCondition",
                    ConditionKind.LocalizationKey,
                    [])
            ],
            sourceReference: "Terraria/OceanFloorPickup",
            includeInSnapshot: true);
        AddWorld(
            builders,
            provenance,
            "Prism Ore",
            sourceItemReference: null,
            "AAModClassic/Prism",
            [
                new ConditionBuilder(
                    "Mods.ProgressionJournal.UI.AAModClassicPrismOreCondition",
                    ConditionKind.LocalizationKey,
                    [])
            ],
            sourceReference: "AAModClassic/PrismOre_Tile",
            includeInSnapshot: true);

        ConditionBuilder[] equinoxOreCondition =
        [
            new ConditionBuilder(
                "Mods.ProgressionJournal.UI.AAModClassicEquinoxOreCondition",
                ConditionKind.LocalizationKey,
                [])
        ];
        AddWorld(
            builders,
            provenance,
            "Radium Ore",
            sourceItemReference: null,
            "AAModClassic/RadiumOre",
            equinoxOreCondition,
            0.5f,
            sourceReference: "AAModClassic/RadiumOre_Tile",
            includeInSnapshot: true);
        AddWorld(
            builders,
            provenance,
            "Radium Ore",
            sourceItemReference: null,
            "AAModClassic/DarkmatterOre",
            equinoxOreCondition,
            0.5f,
            sourceReference: "AAModClassic/RadiumOre_Tile",
            includeInSnapshot: true);
        AddWorld(
            builders,
            provenance,
            "Mire foliage",
            sourceItemReference: null,
            "AAModClassic/BlackLotus",
            [
                new ConditionBuilder(
                    "Mods.ProgressionJournal.UI.AAModClassicBlackLotusCondition",
                    ConditionKind.LocalizationKey,
                    [])
            ],
            sourceReference: "AAModClassic/MireFoliage_Tile",
            includeInSnapshot: true);

        ConditionBuilder[] ashProofVestCondition =
        [
            new ConditionBuilder(
                "Mods.ProgressionJournal.UI.AAModClassicAshProofVestCondition",
                ConditionKind.LocalizationKey,
                [])
        ];
        AddWorld(
            builders,
            provenance,
            sourceName: string.Empty,
            "AAModClassic/AshProofVest3",
            "AAModClassic/AshProofVest2",
            ashProofVestCondition,
            1f / 3600f,
            includeInSnapshot: true);
        AddWorld(
            builders,
            provenance,
            sourceName: string.Empty,
            "AAModClassic/AshProofVest2",
            "AAModClassic/AshProofVest1",
            ashProofVestCondition,
            1f / 3600f,
            includeInSnapshot: true);
        AddWorld(
            builders,
            provenance,
            sourceName: string.Empty,
            "AAModClassic/AshProofVest1",
            "AAModClassic/AshProofVest0",
            ashProofVestCondition,
            1f / 3600f,
            includeInSnapshot: true);
        AddWorld(
            builders,
            provenance,
            sourceName: string.Empty,
            "AAModClassic/GoblinTinkererDoll",
            "AAModClassic/SoulStone",
            [
                new ConditionBuilder(
                    "Mods.ProgressionJournal.UI.AAModClassicSoulStoneCondition",
                    ConditionKind.LocalizationKey,
                    [])
            ],
            showDropRate: false,
            includeInSnapshot: true);
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
            ShowDropRate: true,
            conditions ?? [],
            provenance,
            IncludeInSnapshot: true,
            SourceReference: null));
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
            ShowDropRate: true,
            conditions ?? [],
            provenance,
            IncludeInSnapshot: true,
            SourceReference: null));
    }

    private static void AddModNpc(
        ICollection<EntryBuilder> builders,
        string provenance,
        string sourceNpcReference,
        string targetReference,
        float dropRate,
        int stackMin = 1,
        int stackMax = 1,
        ConditionBuilder[]? conditions = null)
    {
        if (!TryResolveNpcReference(sourceNpcReference, out var sourceNpcType))
        {
            return;
        }

        AddNpc(
            builders,
            provenance,
            sourceNpcType,
            targetReference,
            dropRate,
            stackMin,
            stackMax,
            conditions);
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
            ShowDropRate: true,
            conditions ?? [],
            provenance,
            IncludeInSnapshot: true,
            SourceReference: null));
    }

    private static void AddWorld(
        ICollection<EntryBuilder> builders,
        string provenance,
        string sourceName,
        string? sourceItemReference,
        string targetReference,
        ConditionBuilder[] conditions,
        float dropRate = 1f,
        bool showDropRate = true,
        string? sourceReference = null,
        bool includeInSnapshot = false)
    {
        builders.Add(new EntryBuilder(
            sourceName,
            SourceNpcType: null,
            sourceItemReference,
            targetReference,
            dropRate,
            1,
            1,
            showDropRate,
            conditions,
            provenance,
            includeInSnapshot,
            sourceReference));
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
            builder.ShowDropRate,
            builder.Conditions,
            builder.Provenance,
            builder.IncludeInSnapshot,
            builder.SourceReference);
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

    private static bool TryResolveNpcReference(string reference, out int npcType)
    {
        npcType = NPCID.None;
        var separator = reference.IndexOf('/');
        if (separator <= 0 || separator >= reference.Length - 1)
        {
            return false;
        }

        var modName = reference[..separator];
        var npcName = reference[(separator + 1)..];
        if (!ModContent.TryFind<ModNPC>($"{modName}/{npcName}", out var modNpc))
        {
            return false;
        }

        npcType = modNpc.Type;
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
        bool ShowDropRate,
        ConditionBuilder[] Conditions,
        string Provenance,
        bool IncludeInSnapshot,
        string? SourceReference);

    private sealed record Entry(
        string SourceName,
        int? SourceNpcType,
        int? SourceItemId,
        int TargetItemId,
        float DropRate,
        int StackMin,
        int StackMax,
        bool ShowDropRate,
        ConditionBuilder[] Conditions,
        string Provenance,
        bool IncludeInSnapshot,
        string? SourceReference);

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
        ZenithWorld,
        LocalizationKey
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
    bool ShowDropRate,
    JournalExactDropCondition[] Conditions,
    string Provenance,
    string? SourceReference)
{
    public JournalExactDropSource(
        string sourceName,
        int? sourceNpcType,
        int? sourceItemId,
        int targetItemId,
        float dropRate,
        int stackMin,
        int stackMax,
        JournalExactDropCondition[] conditions,
        string provenance)
        : this(
            sourceName,
            sourceNpcType,
            sourceItemId,
            targetItemId,
            dropRate,
            stackMin,
            stackMax,
            ShowDropRate: true,
            conditions,
            provenance,
            SourceReference: null)
    {
    }
}

public sealed record JournalExactDropCondition(string Type, string Description);
