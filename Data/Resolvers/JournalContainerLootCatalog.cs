using Terraria.ID;
using Terraria.ModLoader;

namespace ProgressionJournal.Data.Resolvers;

public static class JournalContainerLootCatalog
{
    private static readonly Lazy<IReadOnlyList<Entry>> Entries = new(CreateEntries);
    private static readonly Lazy<IReadOnlyList<JournalContainerLootCatalogProblem>> Problems = new(CreateProblems);

    private static readonly Dictionary<string, SourcePresentation> SourcePresentations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Terraria/SurfaceWoodenChest"] = new SourcePresentation("Surface Wooden Chest", ["Terraria/Chest"]),
        ["Terraria/UndergroundWoodenChest"] = new SourcePresentation("Underground Wooden Chest", ["Terraria/Chest"]),
        ["Terraria/UndergroundGoldChest"] = new SourcePresentation("Underground Gold Chest", ["Terraria/GoldChest"]),
        ["Terraria/IceChest"] = new SourcePresentation("Ice Chest", ["Terraria/IceChest", "Terraria/FrozenChest"]),
        ["Terraria/IvyChest"] = new SourcePresentation("Ivy Chest", ["Terraria/IvyChest"]),
        ["Terraria/WaterChest"] = new SourcePresentation("Water Chest", ["Terraria/WaterChest"]),
        ["Terraria/SkywareChest"] = new SourcePresentation("Skyware Chest", ["Terraria/SkywareChest"]),
        ["Terraria/SandstoneChest"] = new SourcePresentation("Sandstone Chest", ["Terraria/DesertChest"]),
        ["Terraria/LivingWoodChest"] = new SourcePresentation("Living Wood Chest", ["Terraria/LivingWoodChest", "Terraria/Chest"]),
        ["Terraria/WebCoveredChest"] = new SourcePresentation("Web Covered Chest", ["Terraria/WebCoveredChest", "Terraria/Chest"]),
        ["Terraria/PyramidChest"] = new SourcePresentation("Pyramid Chest", ["Terraria/GoldChest"]),
        ["Terraria/EnchantedSwordShrine"] = new SourcePresentation("Enchanted Sword Shrine", ["Terraria/EnchantedSword"]),
        ["Terraria/DungeonGoldChest"] = new SourcePresentation("Dungeon Gold Chest", ["Terraria/GoldChest"]),
        ["Terraria/ShadowChest"] = new SourcePresentation("Shadow Chest", ["Terraria/ShadowChest"]),
        ["Terraria/JungleBiomeChest"] = new SourcePresentation("Jungle Biome Chest", ["Terraria/JungleChest", "Terraria/GoldChest", "Terraria/Chest"]),
        ["Terraria/CorruptionBiomeChest"] = new SourcePresentation("Corruption Biome Chest", ["Terraria/CorruptionChest", "Terraria/GoldChest", "Terraria/Chest"]),
        ["Terraria/CrimsonBiomeChest"] = new SourcePresentation("Crimson Biome Chest", ["Terraria/CrimsonChest", "Terraria/GoldChest", "Terraria/Chest"]),
        ["Terraria/HallowedBiomeChest"] = new SourcePresentation("Hallowed Biome Chest", ["Terraria/HallowedChest", "Terraria/GoldChest", "Terraria/Chest"]),
        ["Terraria/FrozenBiomeChest"] = new SourcePresentation("Frozen Biome Chest", ["Terraria/FrozenChest", "Terraria/IceChest", "Terraria/GoldChest", "Terraria/Chest"]),
        ["Terraria/DesertBiomeChest"] = new SourcePresentation("Desert Biome Chest", ["Terraria/DungeonDesertChest", "Terraria/GoldChest", "Terraria/Chest"]),
        ["CalamityMod/AbyssTreasureChest"] = new SourcePresentation("Abyss Treasure Chest", ["CalamityMod/AbyssTreasureChest", "CalamityMod/AncientTreasureChest"]),
        ["CalamityMod/AstralChest"] = new SourcePresentation("Astral Chest", ["CalamityMod/AstralChest"]),
        ["ThoriumMod/AquaticDepthsBiomeChest"] = new SourcePresentation("Aquatic Depths Biome Chest", ["ThoriumMod/AquaticDepthsBiomeChest", "ThoriumMod/DepthChest"]),
        ["ThoriumMod/UnderworldBiomeChest"] = new SourcePresentation("Underworld Biome Chest", ["ThoriumMod/UnderworldBiomeChest"]),
    };

    public static IReadOnlyList<JournalContainerLootSource> GetSources(int targetItemId)
    {
        return Entries.Value
            .Where(entry => entry.TargetItemId == targetItemId)
            .Select(static entry => new JournalContainerLootSource(
                entry.SourceReference,
                entry.SourceDisplayName,
                entry.SourceItemId,
                entry.DropRate,
                entry.StackMin,
                entry.StackMax,
                entry.Provenance))
            .ToArray();
    }

    public static IReadOnlyList<JournalContainerLootDrop> GetAllDrops()
    {
        return Entries.Value
            .Select(static entry => new JournalContainerLootDrop(
                entry.SourceReference,
                entry.SourceDisplayName,
                entry.SourceItemId,
                entry.TargetReference,
                entry.DropRate,
                entry.StackMin,
                entry.StackMax,
                entry.Provenance))
            .ToArray();
    }

    public static IReadOnlyList<JournalContainerLootCatalogProblem> GetProblems() => Problems.Value;

    private static Entry[] CreateEntries()
    {
        return CreateBuilders()
            .Select(TryCreateEntry)
            .OfType<Entry>()
            .GroupBy(static entry => new { entry.SourceReference, entry.TargetItemId, entry.StackMin, entry.StackMax })
            .Select(static group => group.First())
            .ToArray();
    }

    private static JournalContainerLootCatalogProblem[] CreateProblems()
    {
        return CreateBuilders()
            .Where(static builder => TryCreateEntry(builder) is null)
            .Select(static builder => new JournalContainerLootCatalogProblem(
                builder.SourceReference,
                builder.SourceItemReferences.ToArray(),
                builder.TargetReference,
                builder.Provenance))
            .ToArray();
    }

    private static List<EntryBuilder> CreateBuilders()
    {
        List<EntryBuilder> builders = [];
        AddVanilla(builders);
        AddCalamity(builders);
        AddThorium(builders);
        AddFargo(builders);
        return builders;
    }

    private static void AddVanilla(ICollection<EntryBuilder> builders)
    {
        AddEqualPool(
            builders,
            "terraria-wiki.gg/chests",
            "Terraria/SurfaceWoodenChest",
            "Surface Wooden Chest",
            ["Terraria/Chest"],
            [
                "Terraria/Spear",
                "Terraria/Blowpipe",
                "Terraria/WoodenBoomerang",
                "Terraria/WandofSparking",
                "Terraria/Aglet",
                "Terraria/ClimbingClaws",
                "Terraria/Radar",
                "Terraria/GuideToPlantFiberCordage"
            ]);

        AddEqualPool(
            builders,
            "terraria-wiki.gg/gold-chest",
            "Terraria/UndergroundGoldChest",
            "Underground Gold Chest",
            ["Terraria/GoldChest"],
            [
                "Terraria/BandofRegeneration",
                "Terraria/MagicMirror",
                "Terraria/CloudinaBottle",
                "Terraria/HermesBoots",
                "Terraria/ShoeSpikes",
                "Terraria/FlareGun",
                "Terraria/Mace"
            ]);

        AddDrop(
            builders,
            "terraria-wiki.gg/lava-charm; terraria-wiki.gg/gold-chest",
            "Terraria/UndergroundGoldChest",
            "Underground Gold Chest",
            ["Terraria/GoldChest"],
            "Terraria/LavaCharm",
            1f / 20f);

        AddEqualPool(
            builders,
            "terraria-wiki.gg/chests",
            "Terraria/UndergroundWoodenChest",
            "Underground Wooden Chest",
            ["Terraria/Chest"],
            [
                "Terraria/BandofRegeneration",
                "Terraria/MagicMirror",
                "Terraria/CloudinaBottle",
                "Terraria/HermesBoots",
                "Terraria/ShoeSpikes",
                "Terraria/FlareGun",
                "Terraria/Mace"
            ]);

        AddEqualPool(
            builders,
            "terraria-wiki.gg/ice-chest",
            "Terraria/IceChest",
            "Ice Chest",
            ["Terraria/IceChest", "Terraria/FrozenChest"],
            [
                "Terraria/IceBlade",
                "Terraria/IceBoomerang",
                "Terraria/IceSkates",
                "Terraria/FlurryBoots",
                "Terraria/BlizzardinaBottle",
                "Terraria/SnowballCannon",
                "Terraria/IceMirror"
            ]);

        AddEqualPool(
            builders,
            "terraria-wiki.gg/ivy-chest",
            "Terraria/IvyChest",
            "Ivy Chest",
            ["Terraria/IvyChest"],
            [
                "Terraria/Boomstick",
                "Terraria/FeralClaws",
                "Terraria/AnkletoftheWind",
                "Terraria/StaffofRegrowth",
                "Terraria/FiberglassFishingPole",
                "Terraria/FlowerBoots"
            ]);

        AddEqualPool(
            builders,
            "terraria-wiki.gg/water-chest",
            "Terraria/WaterChest",
            "Water Chest",
            ["Terraria/WaterChest"],
            [
                "Terraria/Trident",
                "Terraria/Flipper",
                "Terraria/BreathingReed",
                "Terraria/WaterWalkingBoots"
            ]);

        AddEqualPool(
            builders,
            "terraria-wiki.gg/skyware-chest",
            "Terraria/SkywareChest",
            "Skyware Chest",
            ["Terraria/SkywareChest"],
            [
                "Terraria/Starfury",
                "Terraria/ShinyRedBalloon",
                "Terraria/LuckyHorseshoe",
                "Terraria/CelestialMagnet"
            ]);
        AddDrop(
            builders,
            "terraria-wiki.gg/fledgling-wings",
            "Terraria/SkywareChest",
            "Skyware Chest",
            ["Terraria/SkywareChest"],
            "Terraria/CreativeWings",
            1f / 40f);

        AddEqualPool(
            builders,
            "terraria-wiki.gg/sandstone-chest",
            "Terraria/SandstoneChest",
            "Sandstone Chest",
            ["Terraria/DesertChest", "Terraria/GoldChest", "Terraria/Chest"],
            [
                "Terraria/ThunderSpear",
                "Terraria/ThunderStaff",
                "Terraria/MagicConch",
                "Terraria/MysticCoilSnake",
                "Terraria/AncientChisel",
                "Terraria/SandBoots",
                "Terraria/CatBast"
            ]);

        AddDrop(
            builders,
            "terraria-wiki.gg/living-wood-chest",
            "Terraria/LivingWoodChest",
            "Living Wood Chest",
            ["Terraria/LivingWoodChest", "Terraria/Chest"],
            "Terraria/BabyBirdStaff",
            1f / 3f);
        AddDrop(
            builders,
            "terraria-wiki.gg/living-wood-chest",
            "Terraria/LivingWoodChest",
            "Living Wood Chest",
            ["Terraria/LivingWoodChest", "Terraria/Chest"],
            "Terraria/LivingWoodWand",
            2f / 3f);
        AddDrop(
            builders,
            "terraria-wiki.gg/living-wood-chest",
            "Terraria/LivingWoodChest",
            "Living Wood Chest",
            ["Terraria/LivingWoodChest", "Terraria/Chest"],
            "Terraria/LeafWand",
            2f / 3f);
        AddDrop(
            builders,
            "terraria-wiki.gg/web-covered-chest",
            "Terraria/WebCoveredChest",
            "Web Covered Chest",
            ["Terraria/WebCoveredChest", "Terraria/Chest"],
            "Terraria/WebSlinger",
            1f);

        AddEqualPool(
            builders,
            "terraria-wiki.gg/pyramid",
            "Terraria/PyramidChest",
            "Pyramid Chest",
            ["Terraria/GoldChest"],
            [
                "Terraria/FlyingCarpet",
                "Terraria/SandstorminaBottle"
            ]);

        AddDrop(
            builders,
            "terraria-wiki.gg/enchanted-sword-shrine",
            "Terraria/EnchantedSwordShrine",
            "Enchanted Sword Shrine",
            ["Terraria/EnchantedSword"],
            "Terraria/EnchantedSword",
            0.98f);
        AddDrop(
            builders,
            "terraria-wiki.gg/terragrim",
            "Terraria/EnchantedSwordShrine",
            "Enchanted Sword Shrine",
            ["Terraria/EnchantedSword"],
            "Terraria/Terragrim",
            0.02f);

        AddEqualPool(
            builders,
            "terraria-wiki.gg/locked-gold-chest",
            "Terraria/DungeonGoldChest",
            "Dungeon Gold Chest",
            ["Terraria/GoldChest"],
            [
                "Terraria/Muramasa",
                "Terraria/CobaltShield",
                "Terraria/AquaScepter",
                "Terraria/BlueMoon",
                "Terraria/MagicMissile",
                "Terraria/Valor",
                "Terraria/Handgun"
            ]);

        AddEqualPool(
            builders,
            "terraria-wiki.gg/shadow-chest",
            "Terraria/ShadowChest",
            "Shadow Chest",
            ["Terraria/ShadowChest"],
            [
                "Terraria/DarkLance",
                "Terraria/Sunfury",
                "Terraria/FlowerofFire",
                "Terraria/Flamelash",
                "Terraria/HellwingBow"
            ]);

        AddDrop(
            builders,
            "terraria-wiki.gg/biome-chests",
            "Terraria/JungleBiomeChest",
            "Jungle Biome Chest",
            ["Terraria/JungleChest", "Terraria/GoldChest", "Terraria/Chest"],
            "Terraria/PiranhaGun",
            1f);
        AddDrop(
            builders,
            "terraria-wiki.gg/biome-chests",
            "Terraria/CorruptionBiomeChest",
            "Corruption Biome Chest",
            ["Terraria/CorruptionChest", "Terraria/GoldChest", "Terraria/Chest"],
            "Terraria/ScourgeoftheCorruptor",
            1f);
        AddDrop(
            builders,
            "terraria-wiki.gg/biome-chests",
            "Terraria/CrimsonBiomeChest",
            "Crimson Biome Chest",
            ["Terraria/CrimsonChest", "Terraria/GoldChest", "Terraria/Chest"],
            "Terraria/VampireKnives",
            1f);
        AddDrop(
            builders,
            "terraria-wiki.gg/biome-chests",
            "Terraria/HallowedBiomeChest",
            "Hallowed Biome Chest",
            ["Terraria/HallowedChest", "Terraria/GoldChest", "Terraria/Chest"],
            "Terraria/RainbowGun",
            1f);
        AddDrop(
            builders,
            "terraria-wiki.gg/biome-chests",
            "Terraria/FrozenBiomeChest",
            "Frozen Biome Chest",
            ["Terraria/FrozenChest", "Terraria/IceChest", "Terraria/GoldChest", "Terraria/Chest"],
            "Terraria/StaffoftheFrostHydra",
            1f);
        AddDrop(
            builders,
            "terraria-wiki.gg/biome-chests",
            "Terraria/DesertBiomeChest",
            "Desert Biome Chest",
            ["Terraria/DungeonDesertChest", "Terraria/GoldChest", "Terraria/Chest"],
            "Terraria/StormTigerStaff",
            1f);
    }

    private static void AddCalamity(ICollection<EntryBuilder> builders)
    {
        AddEqualPool(
            builders,
            "CalamityModPublic/World/Abyss.cs; calamitymod-wiki.gg/ancient-treasure-chest",
            "CalamityMod/AbyssTreasureChest",
            "Abyss Treasure Chest",
            ["CalamityMod/AbyssTreasureChest", "CalamityMod/AncientTreasureChest"],
            [
                "CalamityMod/IronBoots",
                "CalamityMod/DepthCharm",
                "CalamityMod/AnechoicPlating",
                "CalamityMod/Archerfish",
                "CalamityMod/BallOFugu",
                "CalamityMod/HerringStaff",
                "CalamityMod/BlackAnurian",
                "CalamityMod/Lionfish",
                "CalamityMod/StrangeOrb",
                "CalamityMod/TorrentialTear"
            ]);

        AddDrop(
            builders,
            "calamitymod-wiki.gg/biome-chest",
            "CalamityMod/AstralChest",
            "Astral Chest",
            ["CalamityMod/AstralChest"],
            "CalamityMod/HeavenfallenStardisk",
            1f);
    }

    private static void AddThorium(ICollection<EntryBuilder> builders)
    {
        AddDrop(
            builders,
            "thoriummod-wiki.gg/biome-chests",
            "ThoriumMod/AquaticDepthsBiomeChest",
            "Aquatic Depths Biome Chest",
            ["ThoriumMod/AquaticDepthsBiomeChest", "ThoriumMod/DepthChest"],
            "ThoriumMod/Fishbone",
            1f);
        AddDrop(
            builders,
            "thoriummod-wiki.gg/1.7.2.5; thoriummod-wiki.gg/pharaohs-slab",
            "Terraria/DesertBiomeChest",
            "Desert Biome Chest",
            ["Terraria/DungeonDesertChest", "Terraria/GoldChest", "Terraria/Chest"],
            "ThoriumMod/PharaohsSlab",
            1f);
        AddDrop(
            builders,
            "thoriummod-wiki.gg/biome-chests",
            "ThoriumMod/UnderworldBiomeChest",
            "Underworld Biome Chest",
            ["ThoriumMod/UnderworldBiomeChest"],
            "ThoriumMod/PhoenixStaff",
            1f);
    }

    private static void AddFargo(ICollection<EntryBuilder> builders)
    {
    }

    private static Entry? TryCreateEntry(EntryBuilder builder)
    {
        if (!TryResolveItemReference(builder.TargetReference, out var targetItemId))
        {
            return null;
        }

        var sourceItemId = TryResolveAnyItemReference(builder.SourceItemReferences, out var resolvedSourceItemId)
            ? resolvedSourceItemId
            : ItemID.None;

        return new Entry(
            builder.SourceReference,
            builder.SourceDisplayName,
            sourceItemId,
            targetItemId,
            GetItemReference(targetItemId),
            builder.DropRate,
            builder.StackMin,
            builder.StackMax,
            builder.Provenance);
    }

    private static void AddEqualPool(
        ICollection<EntryBuilder> builders,
        string provenance,
        string sourceReference,
        string sourceDisplayName,
        IReadOnlyList<string> sourceItemReferences,
        IReadOnlyList<string> targetReferences)
    {
        var rate = 1f / targetReferences.Count;
        foreach (var targetReference in targetReferences)
        {
            AddDrop(builders, provenance, sourceReference, sourceDisplayName, sourceItemReferences, targetReference, rate);
        }
    }

    private static void AddDrop(
        ICollection<EntryBuilder> builders,
        string provenance,
        string sourceReference,
        string sourceDisplayName,
        IReadOnlyList<string> sourceItemReferences,
        string targetReference,
        float dropRate,
        int stackMin = 1,
        int stackMax = 1)
    {
        var sourcePresentation = ResolveSourcePresentation(sourceReference, sourceDisplayName, sourceItemReferences);
        builders.Add(new EntryBuilder(
            sourceReference,
            sourcePresentation.DisplayName,
            sourcePresentation.ItemReferences,
            targetReference,
            dropRate,
            stackMin,
            stackMax,
            provenance));
    }

    private static SourcePresentation ResolveSourcePresentation(
        string sourceReference,
        string fallbackDisplayName,
        IReadOnlyList<string> fallbackItemReferences)
    {
        return SourcePresentations.TryGetValue(sourceReference, out var presentation)
            ? presentation
            : new SourcePresentation(fallbackDisplayName, fallbackItemReferences);
    }

    private static bool TryResolveAnyItemReference(IReadOnlyList<string> references, out int itemId)
    {
        foreach (var reference in references)
        {
            if (TryResolveItemReference(reference, out itemId))
            {
                return true;
            }
        }

        itemId = ItemID.None;
        return false;
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

    private static string GetItemReference(int itemId)
    {
        var modItem = ItemLoader.GetItem(itemId);
        return modItem is null
            ? $"Terraria/{ItemID.Search.GetName(itemId)}"
            : $"{modItem.Mod.Name}/{modItem.Name}";
    }

    private sealed record SourcePresentation(
        string DisplayName,
        IReadOnlyList<string> ItemReferences);

    private sealed record EntryBuilder(
        string SourceReference,
        string SourceDisplayName,
        IReadOnlyList<string> SourceItemReferences,
        string TargetReference,
        float DropRate,
        int StackMin,
        int StackMax,
        string Provenance);

    private sealed record Entry(
        string SourceReference,
        string SourceDisplayName,
        int SourceItemId,
        int TargetItemId,
        string TargetReference,
        float DropRate,
        int StackMin,
        int StackMax,
        string Provenance);
}

public sealed record JournalContainerLootSource(
    string SourceReference,
    string SourceDisplayName,
    int SourceItemId,
    float DropRate,
    int StackMin,
    int StackMax,
    string Provenance);

public sealed record JournalContainerLootDrop(
    string SourceItem,
    string SourceDisplayName,
    int SourceItemId,
    string TargetItem,
    float DropRate,
    int StackMin,
    int StackMax,
    string Provenance);

public sealed record JournalContainerLootCatalogProblem(
    string SourceReference,
    string[] SourceItems,
    string TargetItem,
    string Provenance);
