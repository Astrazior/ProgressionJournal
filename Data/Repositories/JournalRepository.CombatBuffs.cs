using System.Collections.Generic;
using System.Linq;
using Terraria.ID;

namespace ProgressionJournal.Data.Repositories;

public static partial class JournalRepository
{
    private const CombatClass NonSummonerClasses = CombatClass.Melee | CombatClass.Ranged | CombatClass.Magic;

    private static readonly JournalBuffCategory[] CombatBuffCategoryOrder =
    [
        JournalBuffCategory.Station,
        JournalBuffCategory.Passive,
        JournalBuffCategory.Potion,
        JournalBuffCategory.Food,
        JournalBuffCategory.Flask
    ];

    public static IReadOnlyList<JournalCombatBuffEntry> GetCombatBuffEntries(ProgressionStageId stageId, CombatClass combatClass)
    {
        return CombatBuffEntries.Value
            .Where(entry => entry.AppliesToClass(combatClass) && entry.AppliesToStage(stageId))
            .OrderBy(entry => GetCombatBuffCategoryOrder(entry.Category))
            .ThenByDescending(entry => entry.IsClassSpecific)
            .ToArray();
    }

    public static IReadOnlyList<JournalCombatBuffEntry> GetPersistentCombatBuffEntries(ProgressionStageId stageId, CombatClass combatClass)
    {
        return CombatBuffEntries.Value
            .Where(entry => entry.AppliesToClass(combatClass)
                && entry.AppliesToStage(stageId)
                && (entry.Category == JournalBuffCategory.Station || entry.Category == JournalBuffCategory.Passive))
            .OrderBy(entry => GetCombatBuffCategoryOrder(entry.Category))
            .ThenByDescending(entry => entry.IsClassSpecific)
            .ToArray();
    }

    public static IReadOnlyList<JournalCombatBuffEntry> GetConsumableCombatBuffEntries(ProgressionStageId stageId, CombatClass combatClass)
    {
        return CombatBuffEntries.Value
            .Where(entry => entry.AppliesToClass(combatClass)
                && entry.AppliesToStage(stageId)
                && (entry.Category == JournalBuffCategory.Potion || entry.Category == JournalBuffCategory.Food || entry.Category == JournalBuffCategory.Flask))
            .OrderBy(entry => GetCombatBuffCategoryOrder(entry.Category))
            .ThenByDescending(entry => entry.IsClassSpecific)
            .ToArray();
    }

    private static int GetCombatBuffCategoryOrder(JournalBuffCategory category)
    {
        for (var index = 0; index < CombatBuffCategoryOrder.Length; index++)
        {
            if (CombatBuffCategoryOrder[index] == category)
            {
                return index;
            }
        }

        return CombatBuffCategoryOrder.Length;
    }

    private static IReadOnlyList<JournalCombatBuffEntry> BuildCombatBuffEntries()
    {
        return
        [
            Buff("warTable", JournalBuffCategory.Station, CombatClass.All, ItemID.WarTable, ProgressionStageId.PreBoss),
            ClassSpecificBuff("sharpeningStation", JournalBuffCategory.Station, CombatClass.Melee | CombatClass.Summoner, ItemID.SharpeningStation, ProgressionStageId.PreBoss),
            ClassSpecificBuff("ammoBox", JournalBuffCategory.Station, CombatClass.Ranged, ItemID.AmmoBox, ProgressionStageId.PreBoss),
            Buff("bewitchingTable", JournalBuffCategory.Station, CombatClass.All, ItemID.BewitchingTable, ProgressionStageId.PostSkeletron),
            ClassSpecificBuff("crystalBall", JournalBuffCategory.Station, CombatClass.Magic, ItemID.CrystalBall, ProgressionStageId.HardmodeEntry),
            Buff("sliceOfCake", JournalBuffCategory.Station, CombatClass.All, ItemID.SliceOfCake, ProgressionStageId.PostWorldEvil),

            Buff("sunflower", JournalBuffCategory.Passive, CombatClass.All, ItemID.Sunflower, ProgressionStageId.PreBoss),
            Buff("heartLantern", JournalBuffCategory.Passive, CombatClass.All, ItemID.HeartLantern, ProgressionStageId.PreBoss),
            Buff("gardenGnome", JournalBuffCategory.Passive, CombatClass.All, ItemID.GardenGnome, ProgressionStageId.PreBoss),
            Buff("campfire", JournalBuffCategory.Passive, CombatClass.All, ItemID.Campfire, ProgressionStageId.PreBoss),
            Buff("enemyBanner", JournalBuffCategory.Passive, CombatClass.All, ItemID.ZombieBanner, ProgressionStageId.PreBoss),
            Buff("bastStatue", JournalBuffCategory.Passive, CombatClass.All, ItemID.CatBast, ProgressionStageId.PreBoss),
            ClassSpecificBuff("starInABottle", JournalBuffCategory.Passive, CombatClass.Magic, ItemID.StarinaBottle, ProgressionStageId.PreBoss),

            Buff("ironskinPotion", JournalBuffCategory.Potion, CombatClass.All, ItemID.IronskinPotion, ProgressionStageId.PreBoss),
            Buff("regenerationPotion", JournalBuffCategory.Potion, CombatClass.All, ItemID.RegenerationPotion, ProgressionStageId.PreBoss),
            Buff("swiftnessPotion", JournalBuffCategory.Potion, CombatClass.All, ItemID.SwiftnessPotion, ProgressionStageId.PreBoss),
            Buff("heartreachPotion", JournalBuffCategory.Potion, CombatClass.All, ItemID.HeartreachPotion, ProgressionStageId.PreBoss),
            Buff("warmthPotion", JournalBuffCategory.Potion, CombatClass.All, ItemID.WarmthPotion, ProgressionStageId.PreBoss),
            Buff("ragePotion", JournalBuffCategory.Potion, NonSummonerClasses, ItemID.RagePotion, ProgressionStageId.PreBoss),
            Buff("wrathPotion", JournalBuffCategory.Potion, CombatClass.All, ItemID.WrathPotion, ProgressionStageId.PreBoss),
            Buff("infernoPotion", JournalBuffCategory.Potion, CombatClass.All, ItemID.InfernoPotion, ProgressionStageId.PreBoss),
            Buff("endurancePotion", JournalBuffCategory.Potion, CombatClass.All, ItemID.EndurancePotion, ProgressionStageId.PreBoss),
            Buff("lifeforcePotion", JournalBuffCategory.Potion, CombatClass.All, ItemID.LifeforcePotion, ProgressionStageId.HardmodeEntry),
            Buff("thornsPotion", JournalBuffCategory.Potion, CombatClass.All, ItemID.ThornsPotion, ProgressionStageId.PreBoss),
            Buff("titanPotion", JournalBuffCategory.Potion, CombatClass.All, ItemID.TitanPotion, ProgressionStageId.PreBoss),
            ClassSpecificBuff("archeryPotion", JournalBuffCategory.Potion, CombatClass.Ranged, ItemID.ArcheryPotion, ProgressionStageId.PreBoss),
            ClassSpecificBuff("ammoReservationPotion", JournalBuffCategory.Potion, CombatClass.Ranged, ItemID.AmmoReservationPotion, ProgressionStageId.PreBoss),
            ClassSpecificBuff("magicPowerPotion", JournalBuffCategory.Potion, CombatClass.Magic, ItemID.MagicPowerPotion, ProgressionStageId.PreBoss),
            ClassSpecificBuff("manaRegenerationPotion", JournalBuffCategory.Potion, CombatClass.Magic, ItemID.ManaRegenerationPotion, ProgressionStageId.PreBoss),
            ClassSpecificBuff("summoningPotion", JournalBuffCategory.Potion, CombatClass.Summoner, ItemID.SummoningPotion, ProgressionStageId.PreBoss),

            Buff("wellFedFood", JournalBuffCategory.Food, CombatClass.All, Group(ItemID.ApplePie, ItemID.PumpkinPie), ProgressionStageId.PreBoss, ProgressionStageId.PostDeerclops),
            Buff("plentySatisfiedFood", JournalBuffCategory.Food, CombatClass.All, ItemID.SeafoodDinner, ProgressionStageId.HardmodeEntry),
            Buff("bacon", JournalBuffCategory.Food, CombatClass.All, ItemID.Bacon, ProgressionStageId.PostPlantera)
        ];
    }

    private static JournalCombatBuffEntry Buff(
        string key,
        JournalBuffCategory category,
        CombatClass classes,
        int itemId,
        ProgressionStageId availableFrom,
        ProgressionStageId? availableUntil = null)
    {
        return new JournalCombatBuffEntry(key, category, classes, [new JournalItemGroup([itemId])], availableFrom, availableUntil);
    }

    private static JournalCombatBuffEntry Buff(
        string key,
        JournalBuffCategory category,
        CombatClass classes,
        JournalItemGroup itemGroup,
        ProgressionStageId availableFrom,
        ProgressionStageId? availableUntil = null)
    {
        return new JournalCombatBuffEntry(key, category, classes, [itemGroup], availableFrom, availableUntil);
    }

    private static JournalCombatBuffEntry ClassSpecificBuff(
        string key,
        JournalBuffCategory category,
        CombatClass classes,
        int itemId,
        ProgressionStageId availableFrom,
        ProgressionStageId? availableUntil = null)
    {
        return new JournalCombatBuffEntry(key, category, classes, [new JournalItemGroup([itemId])], availableFrom, availableUntil, isClassSpecific: true);
    }

    private static JournalCombatBuffEntry ClassSpecificBuff(
        string key,
        JournalBuffCategory category,
        CombatClass classes,
        JournalItemGroup itemGroup,
        ProgressionStageId availableFrom,
        ProgressionStageId? availableUntil = null)
    {
        return new JournalCombatBuffEntry(key, category, classes, [itemGroup], availableFrom, availableUntil, isClassSpecific: true);
    }
}
