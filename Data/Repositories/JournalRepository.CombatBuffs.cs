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
        JournalBuffCategory.Basic,
        JournalBuffCategory.Potion,
        JournalBuffCategory.Eternal,
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
                && entry.Category is JournalBuffCategory.Station or JournalBuffCategory.Passive)
            .OrderBy(entry => GetCombatBuffCategoryOrder(entry.Category))
            .ThenByDescending(entry => entry.IsClassSpecific)
            .ToArray();
    }

    public static IReadOnlyList<JournalCombatBuffEntry> GetConsumableCombatBuffEntries(ProgressionStageId stageId, CombatClass combatClass)
    {
        return CombatBuffEntries.Value
            .Where(entry => entry.AppliesToClass(combatClass)
                && entry.AppliesToStage(stageId)
                && (entry.Category == JournalBuffCategory.Basic
                    || entry.Category == JournalBuffCategory.Potion
                    || entry.Category == JournalBuffCategory.Eternal
                    || entry.Category == JournalBuffCategory.Food
                    || entry.Category == JournalBuffCategory.Flask))
            .GroupBy(entry => entry.Category)
            .OrderBy(group => GetCombatBuffCategoryOrder(group.Key))
            .SelectMany(group => group.Key == JournalBuffCategory.Food ? group : group.Reverse())
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
            Buff("bastStatue", JournalBuffCategory.Passive, CombatClass.All, ItemID.CatBast, ProgressionStageId.PreBoss),
            ClassSpecificBuff("starInABottle", JournalBuffCategory.Passive, CombatClass.Magic, ItemID.StarinaBottle, ProgressionStageId.PreBoss),

            Buff("bottledWaterBasic", JournalBuffCategory.Basic, CombatClass.All, ItemID.BottledWater, ProgressionStageId.PreBoss),
            Buff("lesserHealingPotionBasic", JournalBuffCategory.Basic, CombatClass.All, ItemID.LesserHealingPotion, ProgressionStageId.PreBoss),
            Buff("bottledHoneyBasic", JournalBuffCategory.Basic, CombatClass.All, ItemID.BottledHoney, ProgressionStageId.PreBoss),
            Buff("healingPotionBasic", JournalBuffCategory.Basic, CombatClass.All, ItemID.HealingPotion, ProgressionStageId.PreBoss),
            Buff("restorationPotionBasic", JournalBuffCategory.Basic, CombatClass.All, ItemID.RestorationPotion, ProgressionStageId.PreBoss),
            Buff("strangeBrewBasic", JournalBuffCategory.Basic, CombatClass.All, ItemID.StrangeBrew, ProgressionStageId.PreBoss),
            ClassSpecificBuff("aleBasic", JournalBuffCategory.Basic, CombatClass.Melee | CombatClass.Summoner, ItemID.Ale, ProgressionStageId.PreBoss),
            Buff("greaterHealingPotionBasic", JournalBuffCategory.Basic, CombatClass.All, ItemID.GreaterHealingPotion, ProgressionStageId.HardmodeEntry),
            Buff("superHealingPotionBasic", JournalBuffCategory.Basic, CombatClass.All, ItemID.SuperHealingPotion, ProgressionStageId.PostCelestialPillars),
            ClassSpecificBuff("lesserManaPotionBasic", JournalBuffCategory.Basic, CombatClass.Magic, ItemID.LesserManaPotion, ProgressionStageId.PreBoss),
            ClassSpecificBuff("manaPotionBasic", JournalBuffCategory.Basic, CombatClass.Magic, ItemID.ManaPotion, ProgressionStageId.PreBoss),
            ClassSpecificBuff("greaterManaPotionBasic", JournalBuffCategory.Basic, CombatClass.Magic, ItemID.GreaterManaPotion, ProgressionStageId.HardmodeEntry),
            ClassSpecificBuff("superManaPotionBasic", JournalBuffCategory.Basic, CombatClass.Magic, ItemID.SuperManaPotion, ProgressionStageId.HardmodeEntry),

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

            Buff("lifeCrystal", JournalBuffCategory.Eternal, CombatClass.All, ItemID.LifeCrystal, ProgressionStageId.PreBoss),
            Buff("manaCrystal", JournalBuffCategory.Eternal, CombatClass.All, ItemID.ManaCrystal, ProgressionStageId.PreBoss),
            Buff("peddlersSatchel", JournalBuffCategory.Eternal, CombatClass.All, ItemID.PeddlersSatchel, ProgressionStageId.PreBoss),
            Buff("combatBook", JournalBuffCategory.Eternal, CombatClass.All, ItemID.CombatBook, ProgressionStageId.PreBoss),
            Buff("torchGodsFavor", JournalBuffCategory.Eternal, CombatClass.All, ItemID.TorchGodsFavor, ProgressionStageId.PreBoss),
            Buff("aegisCrystal", JournalBuffCategory.Eternal, CombatClass.All, ItemID.AegisCrystal, ProgressionStageId.PreBoss),
            Buff("artisanLoaf", JournalBuffCategory.Eternal, CombatClass.All, ItemID.ArtisanLoaf, ProgressionStageId.PreBoss),
            Buff("arcaneCrystal", JournalBuffCategory.Eternal, CombatClass.All, ItemID.ArcaneCrystal, ProgressionStageId.PreBoss),
            Buff("galaxyPearl", JournalBuffCategory.Eternal, CombatClass.All, ItemID.GalaxyPearl, ProgressionStageId.PreBoss),
            Buff("ambrosia", JournalBuffCategory.Eternal, CombatClass.All, ItemID.Ambrosia, ProgressionStageId.PreBoss),
            Buff("gummyWorm", JournalBuffCategory.Eternal, CombatClass.All, ItemID.GummyWorm, ProgressionStageId.PreBoss),
            Buff("demonHeart", JournalBuffCategory.Eternal, CombatClass.All, ItemID.DemonHeart, ProgressionStageId.HardmodeEntry),
            Buff("combatBookVolumeTwo", JournalBuffCategory.Eternal, CombatClass.All, ItemID.CombatBookVolumeTwo, ProgressionStageId.HardmodeEntry),
            Buff("lifeFruit", JournalBuffCategory.Eternal, CombatClass.All, ItemID.LifeFruit, ProgressionStageId.PostOneMechBoss),
            Buff("aegisFruit", JournalBuffCategory.Eternal, CombatClass.All, ItemID.AegisFruit, ProgressionStageId.PostOneMechBoss),
            Buff("minecartPowerup", JournalBuffCategory.Eternal, CombatClass.All, ItemID.MinecartPowerup, ProgressionStageId.PostThreeMechBosses),

            Buff(
                "foodBuffProgression",
                JournalBuffCategory.Food,
                CombatClass.All,
                [
                    NamedBuffGroup("Mods.ProgressionJournal.UI.AnyFoodExquisitelyStuffed", BuffID.WellFed3, ItemID.Bacon),
                    NamedBuffGroup("Mods.ProgressionJournal.UI.AnyFoodPlentySatisfied", BuffID.WellFed2, ItemID.SeafoodDinner),
                    NamedBuffGroup("Mods.ProgressionJournal.UI.AnyFoodWellFed", BuffID.WellFed, ItemID.ApplePie)
                ],
                ProgressionStageId.PreBoss),

            ClassSpecificBuff("poisonFlask", JournalBuffCategory.Flask, CombatClass.Melee | CombatClass.Summoner, ItemID.FlaskofPoison, ProgressionStageId.PostQueenBee),
            ClassSpecificBuff("fireFlask", JournalBuffCategory.Flask, CombatClass.Melee | CombatClass.Summoner, ItemID.FlaskofFire, ProgressionStageId.PostQueenBee),
            ClassSpecificBuff("ichorFlask", JournalBuffCategory.Flask, CombatClass.Melee | CombatClass.Summoner, ItemID.FlaskofIchor, ProgressionStageId.HardmodeEntry),
            ClassSpecificBuff("cursedFlamesFlask", JournalBuffCategory.Flask, CombatClass.Melee | CombatClass.Summoner, ItemID.FlaskofCursedFlames, ProgressionStageId.HardmodeEntry),
            ClassSpecificBuff("goldFlask", JournalBuffCategory.Flask, CombatClass.Melee | CombatClass.Summoner, ItemID.FlaskofGold, ProgressionStageId.HardmodeEntry),
            ClassSpecificBuff("venomFlask", JournalBuffCategory.Flask, CombatClass.Melee | CombatClass.Summoner, ItemID.FlaskofVenom, ProgressionStageId.PostPlantera),
            ClassSpecificBuff("nanitesFlask", JournalBuffCategory.Flask, CombatClass.Melee | CombatClass.Summoner, ItemID.FlaskofNanites, ProgressionStageId.PostPlantera)
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

    private static JournalCombatBuffEntry Buff(
        string key,
        JournalBuffCategory category,
        CombatClass classes,
        IEnumerable<JournalItemGroup> itemGroups,
        ProgressionStageId availableFrom,
        ProgressionStageId? availableUntil = null)
    {
        return new JournalCombatBuffEntry(key, category, classes, itemGroups, availableFrom, availableUntil);
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
