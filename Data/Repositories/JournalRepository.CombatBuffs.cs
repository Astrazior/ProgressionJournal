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
        JournalBuffCategory.Potion,
        JournalBuffCategory.Food
    ];

    public static IReadOnlyList<JournalCombatBuffEntry> GetCombatBuffEntries(ProgressionStageId stageId, CombatClass combatClass)
    {
        return CombatBuffEntries.Value
            .Where(entry => entry.AppliesToClass(combatClass) && entry.AppliesToStage(stageId))
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
            Buff("campfire", JournalBuffCategory.Station, CombatClass.All, ItemID.Campfire, ProgressionStageId.PreBoss),
            Buff("heartLantern", JournalBuffCategory.Station, CombatClass.All, ItemID.HeartLantern, ProgressionStageId.PreBoss),
            Buff("sunflower", JournalBuffCategory.Station, CombatClass.All, ItemID.Sunflower, ProgressionStageId.PreBoss),
            Buff("sharpeningStation", JournalBuffCategory.Station, CombatClass.Melee, ItemID.SharpeningStation, ProgressionStageId.PostWorldEvil),
            Buff("ammoBox", JournalBuffCategory.Station, CombatClass.Ranged, ItemID.AmmoBox, ProgressionStageId.PostEyeOfCthulhu),
            Buff("bewitchingTable", JournalBuffCategory.Station, CombatClass.Summoner, ItemID.BewitchingTable, ProgressionStageId.PostSkeletron),
            Buff("crystalBall", JournalBuffCategory.Station, CombatClass.Magic, ItemID.CrystalBall, ProgressionStageId.HardmodeEntry),
            Buff("warTable", JournalBuffCategory.Station, CombatClass.Summoner, ItemID.WarTable, ProgressionStageId.PostOneMechBoss),

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
