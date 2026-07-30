using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ProgressionJournal.Data.Resolvers;

public static class JournalExactShopCatalog
{
    private static readonly Lazy<Entry[]> Entries = new(CreateEntries);

    public static IReadOnlyList<JournalExactShopSource> GetSources(int targetItemId)
    {
        return Entries.Value
            .Where(entry => entry.TargetItemId == targetItemId)
            .Select(CreateSource)
            .ToArray();
    }

    internal static JournalExactShopSource[] GetAllSources()
    {
        return Entries.Value.Select(CreateSource).ToArray();
    }

    private static JournalExactShopSource CreateSource(Entry entry)
    {
        return new JournalExactShopSource(
            entry.SourceNpcType,
            Lang.GetNPCNameValue(entry.SourceNpcType),
            entry.TargetItemId,
            entry.Conditions.Select(ResolveCondition).ToArray(),
            entry.Provenance);
    }

    private static JournalExactShopCondition ResolveCondition(ConditionBuilder condition)
    {
        return condition.Kind switch
        {
            ConditionKind.AfterProgression => new JournalExactShopCondition(
                "ProgressionJournal.AfterProgression",
                Language.GetTextValue(
                    "Mods.ProgressionJournal.UI.FishingProgressionCondition",
                    condition.Arguments),
                condition.StageId),
            ConditionKind.WorldOption => new JournalExactShopCondition(
                "ProgressionJournal.WorldOption",
                Language.GetTextValue(
                    "Mods.ProgressionJournal.UI.SelectedItemWorldOptionCondition",
                    condition.Arguments),
                string.Empty),
            ConditionKind.ThoriumTrackerContract => new JournalExactShopCondition(
                "Mods.ThoriumMod.Conditions.CompletedMonsterContract",
                Language.GetTextValue(
                    "Mods.ProgressionJournal.UI.ThoriumDamselTrackerContractCondition"),
                string.Empty),
            ConditionKind.CustomCurrency => new JournalExactShopCondition(
                "ProgressionJournal.CustomCurrency",
                Language.GetTextValue((string)condition.Arguments[0]),
                string.Empty),
            _ => new JournalExactShopCondition(string.Empty, string.Empty, string.Empty)
        };
    }

    private static Entry[] CreateEntries()
    {
        List<EntryBuilder> builders = [];
        AddAAModClassic(builders);
        AddThorium(builders);
        return builders
            .Select(TryCreateEntry)
            .OfType<Entry>()
            .ToArray();
    }

    private static void AddAAModClassic(ICollection<EntryBuilder> builders)
    {
        const string provenance = "AAModClassic 1.0.17 installed assembly ModNPC.ModifyActiveShop IL";
        const string largeLetter = "AAModClassic/LargeLetter";
        AddShop(builders, provenance, largeLetter, "ApawnBag");
        foreach (var item in new[] { "FazerBag", "ShoxBag", "BegBag" })
        {
            AddShop(builders, provenance, largeLetter, item);
        }

        foreach (var item in new[]
                 {
                     "CCBag", "CerberusBag", "BlazenBag", "AvesBag", "DellyBag", "TiedBag",
                     "HallamBag", "TailsBag"
                 })
        {
            AddShop(builders, provenance, largeLetter, item);
        }

        AddShop(
            builders,
            provenance,
            largeLetter,
            "PlanterrorBag",
            new ConditionBuilder(ConditionKind.WorldOption, ["secret world option #2"]));
        foreach (var item in new[] { "BigEBag", "DallinBag", "MoonBag", "GibsBag", "CharlieBag" })
        {
            AddShop(builders, provenance, largeLetter, item);
        }

        AddShop(
            builders,
            provenance,
            largeLetter,
            "PineBreaker",
            new ConditionBuilder(
                ConditionKind.AfterProgression,
                ["Hardmode"],
                "wall-of-flesh"));
        foreach (var item in new[] { "FuryForger", "GameRaider", "AleisterStaff" })
        {
            AddShop(
                builders,
                provenance,
                largeLetter,
                item,
                new ConditionBuilder(
                    ConditionKind.AfterProgression,
                    ["Plantera"],
                    "plantera"));
        }

        foreach (var item in new[]
                 {
                     "ExtravagantLongsword", "TimeTeller", "CursedSickle", "Demise", "DuckstepLauncher",
                     "ConflagrateStaff", "Ethereal", "MobianBuster", "GentlemansRapier", "GibsFemur",
                     "Skullshot", "ScytheOfTheGrimReaper", "Prismeow", "MagicAcorn", "Placeholder",
                     "PoniumStaff", "SkrallStaff", "SockStaff", "SoulSiphon", "StormRifle", "TitanAxe",
                     "UmbralReaper", "BladeOfNight"
                 })
        {
            AddShop(
                builders,
                provenance,
                largeLetter,
                item,
                new ConditionBuilder(
                    ConditionKind.AfterProgression,
                    ["Moon Lord"],
                    "moon-lord"));
        }

        const string goblinSlayer = "AAModClassic/GoblinSlayer";
        AddShop(builders, provenance, goblinSlayer, "GoblinSlayersHelmet");
        AddShop(builders, provenance, goblinSlayer, "GoblinSlayersChestplate");
        AddShop(builders, provenance, goblinSlayer, "GoblinSlayersLeggings");
        AddShop(
            builders,
            provenance,
            goblinSlayer,
            "OldOneCharm",
            new ConditionBuilder(
                ConditionKind.CustomCurrency,
                ["Mods.ProgressionJournal.UI.AAModClassicDefenderMedalShopCondition"]));
        AddShop(
            builders,
            provenance,
            goblinSlayer,
            "EnergyConduit",
            new ConditionBuilder(
                ConditionKind.CustomCurrency,
                ["Mods.ProgressionJournal.UI.AAModClassicMartianCreditShopCondition"]));
    }

    private static void AddShop(
        ICollection<EntryBuilder> builders,
        string provenance,
        string sourceNpcReference,
        string targetItemName,
        params ConditionBuilder[] conditions)
    {
        builders.Add(new EntryBuilder(
            sourceNpcReference,
            $"AAModClassic/{targetItemName}",
            conditions,
            provenance));
    }

    private static void AddThorium(ICollection<EntryBuilder> builders)
    {
        const string provenance =
            "ThoriumMod 1.7.2.6 installed shop snapshots: world-evil-selected "
            + "Grim Pedestal variants share the Damsel of Distress Tracker contract";
        ConditionBuilder[] conditions =
        [
            new(ConditionKind.ThoriumTrackerContract, [])
        ];
        foreach (var target in new[]
                 {
                     "ThoriumMod/GrimPedestal",
                     "ThoriumMod/GrimPedestalCrimson"
                 })
        {
            builders.Add(new EntryBuilder(
                "ThoriumMod/Tracker",
                target,
                conditions,
                provenance));
        }
    }

    private static Entry? TryCreateEntry(EntryBuilder builder)
    {
        if (!TryResolveNpcReference(builder.SourceNpcReference, out var sourceNpcType)
            || !TryResolveItemReference(builder.TargetItemReference, out var targetItemId))
        {
            return null;
        }

        return new Entry(
            sourceNpcType,
            targetItemId,
            builder.Conditions,
            builder.Provenance);
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
        if (!ModContent.TryFind<ModItem>($"{modName}/{itemName}", out var modItem))
        {
            return false;
        }

        itemId = modItem.Type;
        return true;
    }

    private sealed record EntryBuilder(
        string SourceNpcReference,
        string TargetItemReference,
        ConditionBuilder[] Conditions,
        string Provenance);

    private sealed record Entry(
        int SourceNpcType,
        int TargetItemId,
        ConditionBuilder[] Conditions,
        string Provenance);

    private sealed record ConditionBuilder(
        ConditionKind Kind,
        object[] Arguments,
        string StageId = "");

    private enum ConditionKind
    {
        AfterProgression,
        WorldOption,
        ThoriumTrackerContract,
        CustomCurrency
    }
}

public sealed record JournalExactShopSource(
    int NpcType,
    string NpcName,
    int TargetItemId,
    JournalExactShopCondition[] Conditions,
    string Provenance);

public sealed record JournalExactShopCondition(
    string Type,
    string Description,
    string StageId)
{
    public JournalExactShopCondition(string type, string description)
        : this(type, description, string.Empty)
    {
    }
}
