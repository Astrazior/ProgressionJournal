using System.Collections.Generic;
using Terraria.ID;

namespace ProgressionJournal.Data.Repositories;

public static partial class JournalRepository
{
    private static void AddArmorEntries(List<JournalEntry> entries)
    {
        entries.AddRange(
        [
            Set("moltenArmorPostWorldEvil", JournalItemCategory.Armor, CombatClass.Melee,
                            ItemID.MoltenHelmet, ItemID.MoltenBreastplate, ItemID.MoltenGreaves,
                            Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended)),

            Set("shadowOrCrimsonArmorPostWorldEvil", JournalItemCategory.Armor, CombatClass.All,
                            Group(ItemID.ShadowHelmet, ItemID.CrimsonHelmet),
                            Group(ItemID.ShadowScalemail, ItemID.CrimsonScalemail),
                            Group(ItemID.ShadowGreaves, ItemID.CrimsonGreaves),
                            Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Additional)),

            Set("goldOrPlatinumArmor", JournalItemCategory.Armor, CombatClass.Melee,
                            Group(ItemID.GoldHelmet, ItemID.PlatinumHelmet),
                            Group(ItemID.GoldChainmail, ItemID.PlatinumChainmail),
                            Group(ItemID.GoldGreaves, ItemID.PlatinumGreaves),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Set("goldOrPlatinumArmorRangedPreBoss", JournalItemCategory.Armor, CombatClass.Ranged,
                            Group(ItemID.GoldHelmet, ItemID.PlatinumHelmet),
                            Group(ItemID.GoldChainmail, ItemID.PlatinumChainmail),
                            Group(ItemID.GoldGreaves, ItemID.PlatinumGreaves),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            new JournalEntry("amethystOrTopazOrSapphireRobePreBoss", JournalItemCategory.Armor, CombatClass.Magic,
                            [Group(ItemID.AmethystRobe, ItemID.TopazRobe, ItemID.SapphireRobe)],
                            [Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)]),

            new JournalEntry("emeraldOrRubyOrAmberRobePreBoss", JournalItemCategory.Armor, CombatClass.Magic,
                            [Group(ItemID.EmeraldRobe, ItemID.RubyRobe, ItemID.AmberRobe)],
                            [Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)]),

            Entry("diamondRobePreBoss", JournalItemCategory.Armor, CombatClass.Magic, ItemID.DiamondRobe,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            new JournalEntry("mysticRobeAndMagicHatPreBoss", JournalItemCategory.Armor, CombatClass.Magic,
                            [Group(2279), Group(ItemID.MagicHat)],
                            [Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)]),

            Entry("wizardHatPreBoss", JournalItemCategory.Armor, CombatClass.Magic, ItemID.WizardHat,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Set("jungleArmorPreBoss", JournalItemCategory.Armor, CombatClass.Magic,
                            Group(ItemID.JungleHat, ItemID.AncientCobaltHelmet),
                            Group(ItemID.JungleShirt, ItemID.AncientCobaltBreastplate),
                            Group(ItemID.JunglePants, ItemID.AncientCobaltLeggings),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Set("meteorArmorPostWorldEvil", JournalItemCategory.Armor, CombatClass.Magic,
                            ItemID.MeteorHelmet, ItemID.MeteorSuit, ItemID.MeteorLeggings,
                            Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended)),

            Entry("flinxFurCoatPreBoss", JournalItemCategory.Armor, CombatClass.Summoner, ItemID.FlinxFurCoat,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Set("obsidianArmorPostWorldEvil", JournalItemCategory.Armor, CombatClass.Summoner,
                            ItemID.ObsidianHelm, ItemID.ObsidianShirt, ItemID.ObsidianPants,
                            Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended)),

            Set("beeArmorPostWorldEvil", JournalItemCategory.Armor, CombatClass.Summoner,
                            ItemID.BeeHeadgear, ItemID.BeeBreastplate, ItemID.BeeGreaves,
                            Eval(ProgressionStageId.PostQueenBee, RecommendationTier.Recommended)),

            Set("ancientShadowArmorPreBoss", JournalItemCategory.Armor, CombatClass.All,
                            ItemID.AncientShadowHelmet, ItemID.AncientShadowScalemail, ItemID.AncientShadowGreaves,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Set("ninjaArmorPreBoss", JournalItemCategory.Armor, CombatClass.Melee,
                            ItemID.NinjaHood, ItemID.NinjaShirt, ItemID.NinjaPants,
                            Eval(ProgressionStageId.PostKingSlime, RecommendationTier.Recommended)),

            Set("fossilArmorPreBoss", JournalItemCategory.Armor, CombatClass.Ranged,
                            ItemID.FossilHelm, ItemID.FossilShirt, ItemID.FossilPants,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Set("ninjaArmorRangedPreBoss", JournalItemCategory.Armor, CombatClass.Ranged,
                            ItemID.NinjaHood, ItemID.NinjaShirt, ItemID.NinjaPants,
                            Eval(ProgressionStageId.PostKingSlime, RecommendationTier.Additional)),

            Set("necroArmorPostSkeletron", JournalItemCategory.Armor, CombatClass.Ranged,
                            ItemID.NecroHelmet, ItemID.NecroBreastplate, ItemID.NecroGreaves,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Recommended)),

            Set("gladiatorArmorPreBoss", JournalItemCategory.Armor, CombatClass.Melee,
                            ItemID.GladiatorHelmet, ItemID.GladiatorBreastplate, ItemID.GladiatorLeggings,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("giPreBoss", JournalItemCategory.Armor, CombatClass.Melee, ItemID.Gi,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Set("silverOrTungstenArmorPreBoss", JournalItemCategory.Armor, CombatClass.All,
                            Group(ItemID.SilverHelmet, ItemID.TungstenHelmet),
                            Group(ItemID.SilverChainmail, ItemID.TungstenChainmail),
                            Group(ItemID.SilverGreaves, ItemID.TungstenGreaves),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Set("ironOrLeadArmorPreBoss", JournalItemCategory.Armor, CombatClass.All,
                            Group(ItemID.IronHelmet, ItemID.LeadHelmet),
                            Group(ItemID.IronChainmail, ItemID.LeadChainmail),
                            Group(ItemID.IronGreaves, ItemID.LeadGreaves),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Set("cactusArmorPreBoss", JournalItemCategory.Armor, CombatClass.All,
                            ItemID.CactusHelmet, ItemID.CactusBreastplate, ItemID.CactusLeggings,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Set("copperOrTinArmorPreBoss", JournalItemCategory.Armor, CombatClass.All,
                            Group(ItemID.CopperHelmet, ItemID.TinHelmet),
                            Group(ItemID.CopperChainmail, ItemID.TinChainmail),
                            Group(ItemID.CopperGreaves, ItemID.TinGreaves),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Set("snowOrPinkSnowArmorPreBoss", JournalItemCategory.Armor, CombatClass.All,
                            Group(803, 978),
                            Group(804, 979),
                            Group(805, 980),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            new JournalEntry("rainArmorPreBoss", JournalItemCategory.Armor, CombatClass.All, [ItemID.RainHat, ItemID.RainCoat],
                            [Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)]),

            Set("earlyWoodArmorPreBoss", JournalItemCategory.Armor, CombatClass.All,
                            Group(ItemID.WoodHelmet, ItemID.BorealWoodHelmet, ItemID.RichMahoganyHelmet, ItemID.PalmWoodHelmet, ItemID.EbonwoodHelmet, ItemID.ShadewoodHelmet, ItemID.AshWoodHelmet),
                            Group(ItemID.WoodBreastplate, ItemID.BorealWoodBreastplate, ItemID.RichMahoganyBreastplate, ItemID.PalmWoodBreastplate, ItemID.EbonwoodBreastplate, ItemID.ShadewoodBreastplate, ItemID.AshWoodBreastplate),
                            Group(ItemID.WoodGreaves, ItemID.BorealWoodGreaves, ItemID.RichMahoganyGreaves, ItemID.PalmWoodGreaves, ItemID.EbonwoodGreaves, ItemID.ShadewoodGreaves, ItemID.AshWoodGreaves),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Set("palladiumOrCobaltArmorRangedHardmodeEntry", JournalItemCategory.Armor, CombatClass.Ranged,
                            Group(ItemID.PalladiumMask, ItemID.CobaltMask),
                            Group(ItemID.PalladiumBreastplate, ItemID.CobaltBreastplate),
                            Group(ItemID.PalladiumLeggings, ItemID.CobaltLeggings),
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Set("orichalcumOrMythrilArmorRangedHardmodeEntry", JournalItemCategory.Armor, CombatClass.Ranged,
                            Group(ItemID.OrichalcumMask, ItemID.MythrilHat),
                            Group(ItemID.OrichalcumBreastplate, ItemID.MythrilChainmail),
                            Group(ItemID.OrichalcumLeggings, ItemID.MythrilGreaves),
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Set("adamantiteOrTitaniumArmorRangedHardmodeEntry", JournalItemCategory.Armor, CombatClass.Ranged,
                            Group(ItemID.AdamantiteMask, ItemID.TitaniumHelmet),
                            Group(ItemID.AdamantiteBreastplate, ItemID.TitaniumBreastplate),
                            Group(ItemID.AdamantiteLeggings, ItemID.TitaniumLeggings),
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            Set("frostArmorRangedHardmodeEntry", JournalItemCategory.Armor, CombatClass.Ranged,
                            ItemID.FrostHelmet, ItemID.FrostBreastplate, ItemID.FrostLeggings,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            EventSet("huntressArmorPostOneMechBoss", JournalItemCategory.Armor, CombatClass.Ranged, JournalEventCategory.OldOnesArmy,
                            ItemID.HuntressWig, ItemID.HuntressJerkin, ItemID.HuntressPants,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Additional)),

            Set("hallowedArmorRangedPostOneMechBoss", JournalItemCategory.Armor, CombatClass.Ranged,
                            ItemID.HallowedHelmet, ItemID.HallowedPlateMail, ItemID.HallowedGreaves,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

            Set("cobaltOrPalladiumArmorMagicHardmodeEntry", JournalItemCategory.Armor, CombatClass.Magic,
                            Group(ItemID.CobaltHat, ItemID.PalladiumHeadgear),
                            Group(ItemID.CobaltBreastplate, ItemID.PalladiumBreastplate),
                            Group(ItemID.CobaltLeggings, ItemID.PalladiumLeggings),
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Set("orichalcumOrMythrilArmorMagicHardmodeEntry", JournalItemCategory.Armor, CombatClass.Magic,
                            Group(ItemID.OrichalcumHeadgear, ItemID.MythrilHood),
                            Group(ItemID.OrichalcumBreastplate, ItemID.MythrilChainmail),
                            Group(ItemID.OrichalcumLeggings, ItemID.MythrilGreaves),
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Set("adamantiteOrTitaniumArmorMagicHardmodeEntry", JournalItemCategory.Armor, CombatClass.Magic,
                            Group(ItemID.AdamantiteHeadgear, ItemID.TitaniumHeadgear),
                            Group(ItemID.AdamantiteBreastplate, ItemID.TitaniumBreastplate),
                            Group(ItemID.AdamantiteLeggings, ItemID.TitaniumLeggings),
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            Set("forbiddenArmorMagicHardmodeEntry", JournalItemCategory.Armor, CombatClass.Magic,
                            3776, 3777, 3778,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            EventSet("apprenticeArmorPostOneMechBoss", JournalItemCategory.Armor, CombatClass.Magic, JournalEventCategory.OldOnesArmy,
                            ItemID.ApprenticeHat, ItemID.ApprenticeRobe, ItemID.ApprenticeTrousers,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Additional)),

            Set("hallowedArmorMagicPostOneMechBoss", JournalItemCategory.Armor, CombatClass.Magic,
                            ItemID.HallowedHeadgear, ItemID.HallowedPlateMail, ItemID.HallowedGreaves,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

            Set("chlorophyteArmorMagicPostThreeMechBosses", JournalItemCategory.Armor, CombatClass.Magic,
                            ItemID.ChlorophyteHeadgear, ItemID.ChlorophytePlateMail, ItemID.ChlorophyteGreaves,
                            Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.NotRecommended)),

            Set("spectreArmorPostPlantera", JournalItemCategory.Armor, CombatClass.Magic,
                            Group(ItemID.SpectreHood, ItemID.SpectreMask),
                            Group(ItemID.SpectreRobe),
                            Group(ItemID.SpectrePants),
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            EventSet("darkArtistArmorPostGolem", JournalItemCategory.Armor, CombatClass.Magic, JournalEventCategory.OldOnesArmy,
                            ItemID.ApprenticeAltHead, ItemID.ApprenticeAltShirt, ItemID.ApprenticeAltPants,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended)),

            Set("nebulaArmorPostMoonLord", JournalItemCategory.Armor, CombatClass.Magic,
                            ItemID.NebulaHelmet, ItemID.NebulaBreastplate, ItemID.NebulaLeggings,
                            Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Recommended)),

            Set("cobaltOrPalladiumArmorSummonerHardmodeEntry", JournalItemCategory.Armor, CombatClass.Summoner,
                            Group(ItemID.CobaltHelmet, ItemID.PalladiumMask),
                            Group(ItemID.CobaltBreastplate, ItemID.PalladiumBreastplate),
                            Group(ItemID.CobaltLeggings, ItemID.PalladiumLeggings),
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Set("mythrilOrOrichalcumArmorSummonerHardmodeEntry", JournalItemCategory.Armor, CombatClass.Summoner,
                            Group(ItemID.MythrilHelmet, ItemID.OrichalcumMask),
                            Group(ItemID.MythrilChainmail, ItemID.OrichalcumBreastplate),
                            Group(ItemID.MythrilGreaves, ItemID.OrichalcumLeggings),
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Set("adamantiteOrTitaniumArmorSummonerHardmodeEntry", JournalItemCategory.Armor, CombatClass.Summoner,
                            Group(ItemID.AdamantiteHelmet, ItemID.TitaniumMask),
                            Group(ItemID.AdamantiteBreastplate, ItemID.TitaniumBreastplate),
                            Group(ItemID.AdamantiteLeggings, ItemID.TitaniumLeggings),
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Set("forbiddenArmorSummonerHardmodeEntry", JournalItemCategory.Armor, CombatClass.Summoner,
                            3776, 3777, 3778,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Set("spiderArmorHardmodeEntry", JournalItemCategory.Armor, CombatClass.Summoner,
                            ItemID.SpiderMask, ItemID.SpiderBreastplate, ItemID.SpiderGreaves,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            new JournalEntry("dd2ArmorPostOneMechBoss", JournalItemCategory.Armor, CombatClass.Summoner,
                            [
                                Group(ItemID.ApprenticeHat, ItemID.MonkBrows, ItemID.HuntressWig, ItemID.SquireGreatHelm),
                                Group(ItemID.ApprenticeRobe, ItemID.MonkShirt, ItemID.HuntressJerkin, ItemID.SquirePlating),
                                Group(ItemID.ApprenticeTrousers, ItemID.MonkPants, ItemID.HuntressPants, ItemID.SquireGreaves)
                            ],
                            [Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Additional)],
                            JournalEventCategory.OldOnesArmy),

            Set("hallowedArmorSummonerPostOneMechBoss", JournalItemCategory.Armor, CombatClass.Summoner,
                            ItemID.HallowedHood, ItemID.HallowedPlateMail, ItemID.HallowedGreaves,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

            Set("tikiArmorPostPlantera", JournalItemCategory.Armor, CombatClass.Summoner,
                            ItemID.TikiMask, ItemID.TikiShirt, ItemID.TikiPants,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            EventSet("spookyArmorPostPlantera", JournalItemCategory.Armor, CombatClass.Summoner, JournalEventCategory.PumpkinMoon,
                            ItemID.SpookyHelmet, ItemID.SpookyBreastplate, ItemID.SpookyLeggings,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            new JournalEntry("dd2ArmorPostGolem", JournalItemCategory.Armor, CombatClass.Summoner,
                            [
                                Group(ItemID.ApprenticeAltHead, ItemID.MonkAltHead, ItemID.HuntressAltHead, ItemID.SquireAltHead),
                                Group(ItemID.ApprenticeAltShirt, ItemID.MonkAltShirt, ItemID.HuntressAltShirt, ItemID.SquireAltShirt),
                                Group(ItemID.ApprenticeAltPants, ItemID.MonkAltPants, ItemID.HuntressAltPants, ItemID.SquireAltPants)
                            ],
                            [Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended)],
                            JournalEventCategory.OldOnesArmy),

            Set("stardustArmorPostMoonLord", JournalItemCategory.Armor, CombatClass.Summoner,
                            ItemID.StardustHelmet, ItemID.StardustBreastplate, ItemID.StardustLeggings,
                            Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Recommended)),

            Set("palladiumArmorHardmodeEntry", JournalItemCategory.Armor, CombatClass.Melee,
                            ItemID.PalladiumHelmet, ItemID.PalladiumBreastplate, ItemID.PalladiumLeggings,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            Set("cobaltArmorHardmodeEntry", JournalItemCategory.Armor, CombatClass.Melee,
                            ItemID.CobaltHelmet, ItemID.CobaltBreastplate, ItemID.CobaltLeggings,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Set("pearlwoodArmorHardmodeEntry", JournalItemCategory.Armor, CombatClass.All,
                            ItemID.PearlwoodHelmet, ItemID.PearlwoodBreastplate, ItemID.PearlwoodGreaves,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Set("orichalcumOrMythrilArmorHardmodeEntry", JournalItemCategory.Armor, CombatClass.Melee,
                            Group(ItemID.OrichalcumHelmet, ItemID.MythrilHelmet),
                            Group(ItemID.OrichalcumBreastplate, ItemID.MythrilChainmail),
                            Group(ItemID.OrichalcumLeggings, ItemID.MythrilGreaves),
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Set("adamantiteArmorHardmodeEntry", JournalItemCategory.Armor, CombatClass.Melee,
                            ItemID.AdamantiteHelmet, ItemID.AdamantiteBreastplate, ItemID.AdamantiteLeggings,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Set("titaniumArmorHardmodeEntry", JournalItemCategory.Armor, CombatClass.Melee,
                            ItemID.TitaniumHelmet, ItemID.TitaniumBreastplate, ItemID.TitaniumLeggings,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            Set("crystalAssassinArmorHardmodeEntry", JournalItemCategory.Armor, CombatClass.All,
                            ItemID.CrystalNinjaHelmet, ItemID.CrystalNinjaChestplate, ItemID.CrystalNinjaLeggings,
                            Eval(ProgressionStageId.PostQueenSlime, RecommendationTier.NotRecommended)),

            Set("frostArmorHardmodeEntry", JournalItemCategory.Armor, CombatClass.Melee,
                            ItemID.FrostHelmet, ItemID.FrostBreastplate, ItemID.FrostLeggings,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Set("hallowedArmorPostOneMechBoss", JournalItemCategory.Armor, CombatClass.Melee,
                            ItemID.HallowedMask, ItemID.HallowedPlateMail, ItemID.HallowedGreaves,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

            EventSet("monkArmorPostOneMechBoss", JournalItemCategory.Armor, CombatClass.Melee, JournalEventCategory.OldOnesArmy,
                            ItemID.MonkBrows, ItemID.MonkShirt, ItemID.MonkPants,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Additional)),

            EventSet("squireArmorPostOneMechBoss", JournalItemCategory.Armor, CombatClass.Melee, JournalEventCategory.OldOnesArmy,
                            ItemID.SquireGreatHelm, ItemID.SquirePlating, ItemID.SquireGreaves,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Additional)),

            Set("chlorophyteArmorPostThreeMechBosses", JournalItemCategory.Armor, CombatClass.Melee,
                            ItemID.ChlorophyteMask, ItemID.ChlorophytePlateMail, ItemID.ChlorophyteGreaves,
                            Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.Additional)),

            Set("chlorophyteArmorRangedPostThreeMechBosses", JournalItemCategory.Armor, CombatClass.Ranged,
                            ItemID.ChlorophyteHeadgear, ItemID.ChlorophytePlateMail, ItemID.ChlorophyteGreaves,
                            Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.Additional)),

            Set("turtleArmorPostThreeMechBosses", JournalItemCategory.Armor, CombatClass.Melee,
                            ItemID.TurtleHelmet, ItemID.TurtleScaleMail, ItemID.TurtleLeggings,
                            Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.Recommended)),

            Set("beetleArmorPostGolem", JournalItemCategory.Armor, CombatClass.Melee,
                            Group(ItemID.BeetleHelmet), Group(ItemID.BeetleScaleMail, ItemID.BeetleShell), Group(ItemID.BeetleLeggings),
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended)),

            EventSet("redRidingArmorPostGolem", JournalItemCategory.Armor, CombatClass.Ranged, JournalEventCategory.OldOnesArmy,
                            ItemID.HuntressAltHead, ItemID.HuntressAltShirt, ItemID.HuntressAltPants,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Additional)),

            EventSet("valhallaKnightArmorPostGolem", JournalItemCategory.Armor, CombatClass.Melee, JournalEventCategory.OldOnesArmy,
                            Group(3871), Group(3872), Group(3873),
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended)),

            Set("shroomiteArmorPostPlantera", JournalItemCategory.Armor, CombatClass.Ranged,
                            Group(ItemID.ShroomiteHeadgear, ItemID.ShroomiteMask, ItemID.ShroomiteHelmet),
                            Group(ItemID.ShroomiteBreastplate),
                            Group(ItemID.ShroomiteLeggings),
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            Set("pumpkinArmorPreBoss", JournalItemCategory.Armor, CombatClass.All,
                            ItemID.PumpkinHelmet, ItemID.PumpkinBreastplate, ItemID.PumpkinLeggings,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Set("vortexArmorPostMoonLord", JournalItemCategory.Armor, CombatClass.Ranged,
                            ItemID.VortexHelmet, ItemID.VortexBreastplate, ItemID.VortexLeggings,
                            Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Recommended)),

            Set("solarFlareArmorPostMoonLord", JournalItemCategory.Armor, CombatClass.Melee,
                            ItemID.SolarFlareHelmet, ItemID.SolarFlareBreastplate, ItemID.SolarFlareLeggings,
                            Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Recommended)),
        ]);
    }
}

