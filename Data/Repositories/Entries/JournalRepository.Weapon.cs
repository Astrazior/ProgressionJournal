using System.Collections.Generic;
using Terraria.ID;

namespace ProgressionJournal.Data;

public static partial class JournalRepository
{
    private static void AddWeaponEntries(List<JournalEntry> entries)
    {
        entries.AddRange(
        [
            Entry("woodenSwordsPreBoss", JournalItemCategory.Weapon, CombatClass.Melee,
                            Group(ItemID.WoodenSword, ItemID.BorealWoodSword, ItemID.RichMahoganySword, ItemID.PalmWoodSword, ItemID.EbonwoodSword, ItemID.ShadewoodSword, ItemID.AshWoodSword),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("woodenBoomerangPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.WoodenBoomerang,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("woodenYoyoPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.WoodYoyo,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("goldOrPlatinumBroadsword", JournalItemCategory.Weapon, CombatClass.Melee,
                            Group(ItemID.GoldBroadsword, ItemID.PlatinumBroadsword),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("discomfortOrArteryPreBoss", JournalItemCategory.Weapon, CombatClass.Melee,
                            Group(ItemID.CorruptYoyo, ItemID.CrimsonYoyo),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("lightsBaneOrBloodButchererPreBoss", JournalItemCategory.Weapon, CombatClass.Melee,
                            Group(ItemID.LightsBane, ItemID.BloodButcherer),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("warAxeOfNightOrBloodButchererPreBoss", JournalItemCategory.Weapon, CombatClass.Melee,
                            Group(ItemID.WarAxeoftheNight, ItemID.TheRottedFork),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("code1PostEye", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Code1,
                            Eval(ProgressionStageId.PostEyeOfCthulhu, RecommendationTier.Additional)),

            Entry("phasebladesPostWorldEvil", JournalItemCategory.Weapon, CombatClass.Melee,
                            Group(ItemID.BluePhaseblade, ItemID.RedPhaseblade, ItemID.GreenPhaseblade, ItemID.PurplePhaseblade, ItemID.WhitePhaseblade, ItemID.YellowPhaseblade),
                            Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Additional)),

            Entry("beekeeperPostWorldEvil", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.BeeKeeper,
                            Eval(ProgressionStageId.PostQueenBee, RecommendationTier.Additional)),

            Entry("hiveFivePostWorldEvil", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.HiveFive,
                            Eval(ProgressionStageId.PostQueenBee, RecommendationTier.Additional)),

            Entry("flamarangPostWorldEvil", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Flamarang,
                            Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended)),

            Entry("fieryGreatswordPostWorldEvil", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.FieryGreatsword,
                            Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended)),

            Entry("muramasaPostSkeletron", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Muramasa,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.NotRecommended)),

            Entry("sunfuryPostSkeletron", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Sunfury,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.NotRecommended)),

            Entry("cascadePostSkeletron", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Cascade,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Additional)),

            Entry("valorPostSkeletron", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Valor,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.NotRecommended)),

            Entry("blueMoonPostSkeletron", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.BlueMoon,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Useless)),

            Entry("darkLancePostSkeletron", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.DarkLance,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Recommended)),

            Entry("nightsEdgePostSkeletron", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.NightsEdge,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Recommended)),

            Entry("lucyTheAxePostSkeletron", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.LucyTheAxe,
                            Eval(ProgressionStageId.PostDeerclops, RecommendationTier.Useless)),

            Entry("tragicUmbrellaPostSkeletron", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.TragicUmbrella,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Useless)),

            Entry("bladeOfGrassPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.BladeofGrass,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("starfuryPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Starfury,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("thunderSpearPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.ThunderSpear,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("thornChakramPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.ThornChakram,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("trimarangPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Trimarang,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("amazonPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.JungleYoyo,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("enchantedSwordPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.EnchantedSword,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("iceBladePreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.IceBlade,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("ballOHurtOrRottedForkPreBoss", JournalItemCategory.Weapon, CombatClass.Melee,
                            Group(ItemID.BallOHurt, ItemID.TheRottedFork),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("theMeatballPostWorldEvil", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.TheMeatball,
                            Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.NotRecommended)),

            Entry("tentacleSpikePreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.TentacleSpike,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("batBatPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.BatBat,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("gladiusPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Gladius,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("tridentPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Trident,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("terragrimPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Terragrim,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("iceBoomerangPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.IceBoomerang,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("enchantedBoomerangPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.EnchantedBoomerang,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("shroomerangPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Shroomerang,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("macePreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Mace,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("chainKnifePreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.ChainKnife,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("flamingMacePreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.FlamingMace,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("rallyPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Rally,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("copperOrTinShortswordPreBoss", JournalItemCategory.Weapon, CombatClass.Melee,
                            Group(ItemID.CopperShortsword, ItemID.TinShortsword),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("goldOrPlatinumShortswordPreBoss", JournalItemCategory.Weapon, CombatClass.Melee,
                            Group(ItemID.GoldShortsword, ItemID.PlatinumShortsword),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("ironOrLeadShortswordPreBoss", JournalItemCategory.Weapon, CombatClass.Melee,
                            Group(ItemID.IronShortsword, ItemID.LeadShortsword),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("silverOrTungstenShortswordPreBoss", JournalItemCategory.Weapon, CombatClass.Melee,
                            Group(ItemID.SilverShortsword, ItemID.TungstenShortsword),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("copperOrTinBroadswordPreBoss", JournalItemCategory.Weapon, CombatClass.Melee,
                            Group(ItemID.CopperBroadsword, ItemID.TinBroadsword),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("ironOrLeadBroadswordPreBoss", JournalItemCategory.Weapon, CombatClass.Melee,
                            Group(ItemID.IronBroadsword, ItemID.LeadBroadsword),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("silverOrTungstenBroadswordPreBoss", JournalItemCategory.Weapon, CombatClass.Melee,
                            Group(ItemID.SilverBroadsword, ItemID.TungstenBroadsword),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("cactusSwordPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.CactusSword,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("spearPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Spear,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("swordfishPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Swordfish,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("katanaPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Katana,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("exoticScimitarPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.DyeTradersScimitar,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("purpleClubberfishPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.PurpleClubberfish,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("boneSwordPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.BoneSword,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("falconBladePreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.FalconBlade,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("zombieArmPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.ZombieArm,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("mandibleBladePreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.AntlionClaw,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("stylishScissorsPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.StylistKilLaKillScissorsIWish,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("woodenBowsPreBoss", JournalItemCategory.Weapon, CombatClass.Ranged,
                            Group(ItemID.WoodenBow, ItemID.BorealWoodBow, ItemID.RichMahoganyBow, ItemID.PalmWoodBow, ItemID.EbonwoodBow, ItemID.ShadewoodBow, ItemID.AshWoodBow),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("copperOrTinBowsPreBoss", JournalItemCategory.Weapon, CombatClass.Ranged,
                            Group(ItemID.CopperBow, ItemID.TinBow),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("ironOrLeadBowsPreBoss", JournalItemCategory.Weapon, CombatClass.Ranged,
                            Group(ItemID.IronBow, ItemID.LeadBow),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("silverOrTungstenBowsPreBoss", JournalItemCategory.Weapon, CombatClass.Ranged,
                            Group(ItemID.SilverBow, ItemID.TungstenBow),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("goldOrPlatinumBowsPreBoss", JournalItemCategory.Weapon, CombatClass.Ranged,
                            Group(ItemID.GoldBow, ItemID.PlatinumBow),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("demonOrTendonBowPreBoss", JournalItemCategory.Weapon, CombatClass.Ranged,
                            Group(ItemID.DemonBow, ItemID.TendonBow),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("bloodRainBowPreBoss", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.BloodRainBow,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("blowpipePreBoss", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Blowpipe,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("minisharkPreBoss", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Minishark,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("boomstickPreBoss", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Boomstick,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("revolverPreBoss", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Revolver,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("flintlockPistolPreBoss", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.FlintlockPistol,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("undertakerPreBoss", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.TheUndertaker,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("snowballCannonPreBoss", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.SnowballCannon,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("harpoonPreBoss", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Harpoon,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("musketPreBoss", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Musket,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("sandgunPreBoss", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Sandgun,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("paintballGunPreBoss", JournalItemCategory.Weapon, CombatClass.Ranged, 3350,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("grenadePreBoss", JournalItemCategory.Weapon, CombatClass.Ranged,
                            Group(ItemID.Grenade, ItemID.StickyGrenade, ItemID.BouncyGrenade),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("throwingWeaponsPreBoss", JournalItemCategory.Weapon, CombatClass.Ranged,
                            Group(ItemID.Shuriken, ItemID.ThrowingKnife, ItemID.BoneDagger, ItemID.PoisonedKnife, ItemID.SpikyBall, ItemID.Snowball, ItemID.Javelin, ItemID.MolotovCocktail, ItemID.FrostDaggerfish),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("boneJavelinPreBoss", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.BoneJavelin,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("paperAirplanesPreBoss", JournalItemCategory.Weapon, CombatClass.Ranged,
                            Group(ItemID.PaperAirplaneA, ItemID.PaperAirplaneB),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("abigailsFlowerPreBoss", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.AbigailsFlower,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("finchStaffPreBoss", JournalItemCategory.Weapon, CombatClass.Summoner, 4281,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("flinxStaffPreBoss", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.FlinxStaff,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("slimeStaffPreBoss", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.SlimeStaff,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("vampireFrogStaffPreBoss", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.VampireFrogStaff,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("hornetStaffPostWorldEvil", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.HornetStaff,
                            Eval(ProgressionStageId.PostQueenBee, RecommendationTier.Additional)),

            Entry("impStaffPostWorldEvil", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.ImpStaff,
                            Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended)),

            Entry("houndiusShootiusPostSkeletron", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.HoundiusShootius,
                            Eval(ProgressionStageId.PostDeerclops, RecommendationTier.Additional)),

            Entry("earlySentryWandsPreBoss", JournalItemCategory.Weapon, CombatClass.Summoner,
                            Group(ItemID.DD2LightningAuraT1Popper, ItemID.DD2FlameburstTowerT1Popper, ItemID.DD2BallistraTowerT1Popper, ItemID.DD2ExplosiveTrapT1Popper),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("queenSpiderStaffHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.QueenSpiderStaff,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("spiderStaffHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.SpiderStaff,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            Entry("sanguineStaffHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.SanguineStaff,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("pirateStaffHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.PirateStaff,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Entry("bladeStaffHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.Smolstar,
                            Eval(ProgressionStageId.PostQueenSlime, RecommendationTier.Recommended)),

            Entry("opticStaffPostOneMechBoss", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.OpticStaff,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

            Entry("dd2SentryTier2PostOneMechBoss", JournalItemCategory.Weapon, CombatClass.Summoner,
                            Group(ItemID.DD2LightningAuraT2Popper, ItemID.DD2FlameburstTowerT2Popper, ItemID.DD2BallistraTowerT2Popper, ItemID.DD2ExplosiveTrapT2Popper),
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

            Entry("pygmyStaffPostPlantera", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.PygmyStaff,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.NotRecommended)),

            Entry("staffOfTheFrostHydraPostPlantera", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.StaffoftheFrostHydra,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            Entry("desertTigerStaffPostPlantera", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.StormTigerStaff,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            Entry("deadlySphereStaffPostPlantera", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.DeadlySphereStaff,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            Entry("ravenStaffPostPlantera", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.RavenStaff,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Useless)),

            Entry("tempestStaffPostPlantera", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.TempestStaff,
                            Eval(ProgressionStageId.PostDukeFishron, RecommendationTier.Recommended)),

            Entry("xenoStaffPostGolem", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.XenoStaff,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended)),

            Entry("dd2SentryTier3PostGolem", JournalItemCategory.Weapon, CombatClass.Summoner,
                            Group(ItemID.DD2LightningAuraT3Popper, ItemID.DD2FlameburstTowerT3Popper, ItemID.DD2BallistraTowerT3Popper, ItemID.DD2ExplosiveTrapT3Popper),
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended)),

            Entry("stardustCellOrDragonStaffPostCelestialPillars", JournalItemCategory.Weapon, CombatClass.Summoner,
                            Group(ItemID.StardustCellStaff, ItemID.StardustDragonStaff),
                            Eval(ProgressionStageId.PostCelestialPillars, RecommendationTier.Recommended)),

            Entry("lunarPortalStaffPostMoonLord", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.MoonlordTurretStaff,
                            Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Recommended)),

            Entry("rainbowCrystalStaffPostMoonLord", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.RainbowCrystalStaff,
                            Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Recommended)),

            Entry("terraprismaPostMoonLord", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.EmpressBlade,
                            Eval(ProgressionStageId.PostEmpressOfLight, RecommendationTier.Recommended)),

            Entry("starCannonPostWorldEvil", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.StarCannon,
                            Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Additional)),

            Entry("blowgunPostWorldEvil", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Blowgun,
                            Eval(ProgressionStageId.PostQueenBee, RecommendationTier.Additional)),

            Entry("theBeesKneesPostWorldEvil", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.BeesKnees,
                            Eval(ProgressionStageId.PostQueenBee, RecommendationTier.Recommended)),

            Entry("beenadePostWorldEvil", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Beenade,
                            Eval(ProgressionStageId.PostQueenBee, RecommendationTier.Recommended)),

            Entry("aleThrowingGlovePostWorldEvil", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.AleThrowingGlove,
                            Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Useless)),

            Entry("moltenFuryPostWorldEvil", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.MoltenFury,
                            Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Additional)),

            Entry("hellwingBowPostSkeletron", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.HellwingBow,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.NotRecommended)),

            Entry("quadBarrelShotgunPostSkeletron", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.QuadBarrelShotgun,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Additional)),

            Entry("handgunPostSkeletron", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Handgun,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Additional)),

            Entry("phoenixBlasterPostSkeletron", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.PhoenixBlaster,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Recommended)),

            Entry("pewMaticHornPostSkeletron", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.PewMaticHorn,
                            Eval(ProgressionStageId.PostDeerclops, RecommendationTier.NotRecommended)),

            Entry("bonePostSkeletron", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Bone,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Useless)),

            Entry("amethystOrTopazOrSapphireStaffPreBoss", JournalItemCategory.Weapon, CombatClass.Magic,
                            Group(ItemID.AmethystStaff, ItemID.TopazStaff, ItemID.SapphireStaff),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("wandOfSparkingOrWandOfFrostingPreBoss", JournalItemCategory.Weapon, CombatClass.Magic,
                            Group(ItemID.WandofSparking, ItemID.WandofFrosting),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("emeraldOrRubyOrAmberStaffPreBoss", JournalItemCategory.Weapon, CombatClass.Magic,
                            Group(ItemID.EmeraldStaff, ItemID.RubyStaff, ItemID.AmberStaff),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("diamondStaffPreBoss", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.DiamondStaff,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("thunderZapperPreBoss", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.ThunderStaff,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("vilethornPreBoss", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.Vilethorn,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("demonScythePreBoss", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.DemonScythe,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("crimsonRodPreBoss", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.CrimsonRod,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("beeGunPostWorldEvil", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.BeeGun,
                            Eval(ProgressionStageId.PostQueenBee, RecommendationTier.Recommended)),

            Entry("aquaScepterPostSkeletron", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.AquaScepter,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Additional)),

            Entry("magicMissilePostSkeletron", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.MagicMissile,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Additional)),

            Entry("weatherPainPostSkeletron", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.WeatherPain,
                            Eval(ProgressionStageId.PostDeerclops, RecommendationTier.Additional)),

            Entry("flamelashPostSkeletron", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.Flamelash,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Recommended)),

            Entry("flowerOfFirePostSkeletron", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.FlowerofFire,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Recommended)),

            Entry("grayZapinatorPostSkeletron", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.ZapinatorGray,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Recommended)),

            Entry("spaceGunPostSkeletron", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.SpaceGun,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Recommended)),

            Entry("bookOfSkullsPostSkeletron", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.BookofSkulls,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Additional)),

            Entry("waterBoltPostSkeletron", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.WaterBolt,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.NotRecommended)),

            Entry("skyFractureHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.SkyFracture,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            Entry("meteorStaffHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.MeteorStaff,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("crystalSerpentHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.CrystalSerpent,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            Entry("crystalVileShardHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.CrystalVileShard,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Entry("lifeDrainHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.SoulDrain,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("venomStaffHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.PoisonStaff,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            Entry("frostStaffHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.FrostStaff,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            Entry("flowerOfFrostHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.FlowerofFrost,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("clingerStaffHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.ClingerStaff,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("nimbusRodHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.NimbusRod,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("unholyTridentPostOneMechBoss", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.UnholyTrident,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Additional)),

            Entry("rainbowRodPostOneMechBoss", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.RainbowRod,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

            Entry("tomeOfInfiniteWisdomPostOneMechBoss", JournalItemCategory.Weapon, CombatClass.Magic, 3852,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

            Entry("poisonStaffPostThreeMechBosses", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.VenomStaff,
                            Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.Recommended)),

            Entry("nettleBurstPostPlantera", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.NettleBurst,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Useless)),

            Entry("shadowbeamStaffPostPlantera", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.ShadowbeamStaff,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.NotRecommended)),

            Entry("infernoForkPostPlantera", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.InfernoFork,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            Entry("spectreStaffPostPlantera", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.SpectreStaff,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Additional)),

            Entry("batScepterPostPlantera", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.BatScepter,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            Entry("razorpinePostPlantera", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.Razorpine,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            Entry("blizzardStaffPostPlantera", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.BlizzardStaff,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Additional)),

            Entry("resonanceScepterPostPlantera", JournalItemCategory.Weapon, CombatClass.Magic, 5065,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Additional)),

            Entry("rainbowGunPostPlantera", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.RainbowGun,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Additional)),

            Entry("magnetSpherePostPlantera", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.MagnetSphere,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            Entry("staffOfEarthPostGolem", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.StaffofEarth,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Useless)),

            Entry("betsysWrathPostGolem", JournalItemCategory.Weapon, CombatClass.Magic, 3870,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.NotRecommended)),

            Entry("laserRifleHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.LaserRifle,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("orangeZapinatorHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.ZapinatorOrange,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            Entry("cursedFlamesOrGoldenShowerHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Magic,
                            Group(ItemID.CursedFlames, ItemID.GoldenShower),
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("crystalStormHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.CrystalStorm,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("iceRodHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.IceRod,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Entry("magicDaggerHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.MagicDagger,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("medusaHeadHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.MedusaHead,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Entry("spiritFlameHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.SpiritFlame,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("shadowflameHexDollHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.ShadowFlameHexDoll,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            Entry("bloodThornHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Magic, 4270,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Entry("magicalHarpPostOneMechBoss", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.MagicalHarp,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.NotRecommended)),

            Entry("waspGunPostPlantera", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.WaspGun,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Additional)),

            Entry("leafBlowerPostPlantera", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.LeafBlower,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.NotRecommended)),

            Entry("bubbleGunPostPlantera", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.BubbleGun,
                            Eval(ProgressionStageId.PostDukeFishron, RecommendationTier.Useless)),

            Entry("razorbladeTyphoonPostPlantera", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.RazorbladeTyphoon,
                            Eval(ProgressionStageId.PostDukeFishron, RecommendationTier.Recommended)),

            Entry("toxicFlaskPostPlantera", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.ToxicFlask,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Useless)),

            Entry("nightglowPostPlantera", JournalItemCategory.Weapon, CombatClass.Magic, 4952,
                            Eval(ProgressionStageId.PostEmpressOfLight, RecommendationTier.Recommended)),

            Entry("stellarTunePostPlantera", JournalItemCategory.Weapon, CombatClass.Magic, 4715,
                            Eval(ProgressionStageId.PostEmpressOfLight, RecommendationTier.Recommended)),

            Entry("heatRayPostGolem", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.HeatRay,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Additional)),

            Entry("laserMachinegunPostGolem", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.LaserMachinegun,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.NotRecommended)),

            Entry("chargedBlasterCannonPostGolem", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.ChargedBlasterCannon,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended)),

            Entry("nebulaArcanumPostCelestialPillars", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.NebulaArcanum,
                            Eval(ProgressionStageId.PostCelestialPillars, RecommendationTier.Recommended)),

            Entry("nebulaBlazePostCelestialPillars", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.NebulaBlaze,
                            Eval(ProgressionStageId.PostCelestialPillars, RecommendationTier.Recommended)),

            Entry("lunarFlarePostMoonLord", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.LunarFlareBook,
                            Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Recommended)),

            Entry("lastPrismPostMoonLord", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.LastPrism,
                            Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Recommended)),

            Entry("bananarangHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Bananarang,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("slapHandHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.SlapHand,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Entry("pearlwoodSwordHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.PearlwoodSword,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Entry("classyCaneHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.TaxCollectorsStickOfDoom,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Entry("arkhalisHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Arkhalis,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Entry("hardmodeOreSwordsHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee,
                            Group(ItemID.CobaltSword, ItemID.PalladiumSword, ItemID.OrichalcumSword, ItemID.MythrilSword, ItemID.TitaniumSword, ItemID.AdamantiteSword),
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Entry("phasesabersHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee,
                            Group(ItemID.BluePhasesaber, ItemID.GreenPhasesaber, ItemID.RedPhasesaber, ItemID.PurplePhasesaber, ItemID.WhitePhasesaber, ItemID.YellowPhasesaber, ItemID.OrangePhasesaber),
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("fetidBaghnakhsHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.FetidBaghnakhs,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("valkyrieYoyoOrRedsThrowHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee,
                            Group(ItemID.ValkyrieYoyo, ItemID.RedsYoyo),
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("formatCHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.FormatC,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Entry("gradientHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Gradient,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("chikHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Chik,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Entry("helFireHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.HelFire,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            Entry("amarokHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Amarok,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            Entry("code2PostOneMechBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Code2,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.NotRecommended)),

            Entry("brandOfTheInfernoPostOneMechBoss", JournalItemCategory.Weapon, CombatClass.Melee, 3823,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.NotRecommended)),

            Entry("lightDiscPostOneMechBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.LightDisc,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

            Entry("ghastlyGlaivePostOneMechBoss", JournalItemCategory.Weapon, CombatClass.Melee, 3836,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

            Entry("yeletsPostOneMechBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Yelets,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Additional)),

            Entry("excaliburPostOneMechBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Excalibur,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

            Entry("gungnirPostOneMechBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Gungnir,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Additional)),

            Entry("hallowJoustingLancePostOneMechBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.HallowJoustingLance,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Useless)),

            Entry("sleepyOctopodPostOneMechBoss", JournalItemCategory.Weapon, CombatClass.Melee, 3835,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Useless)),

            Entry("trueExcaliburPostThreeMechBosses", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.TrueExcalibur,
                            Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.Recommended)),

            Entry("trueNightsEdgePostThreeMechBosses", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.TrueNightsEdge,
                            Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.Recommended)),

            Entry("chlorophytePartisanPostThreeMechBosses", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.ChlorophytePartisan,
                            Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.NotRecommended)),

            Entry("chlorophyteClaymorePostThreeMechBosses", JournalItemCategory.Weapon, CombatClass.Melee,
                            Group(ItemID.ChlorophyteClaymore, ItemID.ChlorophyteSaber),
                            Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.Useless)),

            Entry("deathSicklePostThreeMechBosses", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.DeathSickle,
                            Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.Additional)),

            Entry("terraBladePostPlantera", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.TerraBlade,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            Entry("theHorsemansBladePostPlantera", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.TheHorsemansBlade,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            Entry("vampireKnivesOrScourgeOfTheCorruptorPostPlantera", JournalItemCategory.Weapon, CombatClass.Melee,
                            Group(ItemID.VampireKnives, ItemID.ScourgeoftheCorruptor),
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            Entry("northPolePostPlantera", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.NorthPole,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Additional)),

            Entry("piercingStarlightPostPlantera", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.PiercingStarlight,
                            Eval(ProgressionStageId.PostEmpressOfLight, RecommendationTier.Additional)),

            Entry("flaironPostPlantera", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Flairon,
                            Eval(ProgressionStageId.PostDukeFishron, RecommendationTier.Additional)),

            Entry("krakenPostPlantera", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Kraken,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Additional)),

            Entry("christmasTreeSwordPostPlantera", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.ChristmasTreeSword,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Additional)),

            Entry("theEyeOfCthulhuPostPlantera", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.TheEyeOfCthulhu,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.NotRecommended)),

            Entry("paladinsHammerPostPlantera", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.PaladinsHammer,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.NotRecommended)),

            Entry("flowerPowPostPlantera", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.FlowerPow,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.NotRecommended)),

            Entry("seedlerPostPlantera", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Seedler,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.NotRecommended)),

            Entry("keybrandPostPlantera", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Keybrand,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Useless)),

            Entry("shadowJoustingLancePostPlantera", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.ShadowJoustingLance,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Useless)),

            Entry("psychoKnifePostPlantera", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.PsychoKnife,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Useless)),

            Entry("butchersChainsawPostPlantera", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.ButchersChainsaw,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Useless)),

            Entry("theAxePostPlantera", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.TheAxe,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Useless)),

            Entry("flyingDragonPostGolem", JournalItemCategory.Weapon, CombatClass.Melee, 3827,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended)),

            Entry("possessedHatchetPostGolem", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.PossessedHatchet,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Useless)),

            Entry("influxWaverPostGolem", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.InfluxWaver,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.NotRecommended)),

            Entry("golemFistPostGolem", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.GolemFist,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Useless)),

            Entry("skyDragonsFuryPostGolem", JournalItemCategory.Weapon, CombatClass.Melee, 3858,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Useless)),

            Entry("dayBreakPostCelestialPillars", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.DayBreak,
                            Eval(ProgressionStageId.PostCelestialPillars, RecommendationTier.Additional)),

            Entry("solarEruptionPostCelestialPillars", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.SolarEruption,
                            Eval(ProgressionStageId.PostCelestialPillars, RecommendationTier.Recommended)),

            Entry("meowmerePostMoonLord", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Meowmere,
                            Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Additional)),

            Entry("starWrathPostMoonLord", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.StarWrath,
                            Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Additional)),

            Entry("terrarianPostMoonLord", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Terrarian,
                            Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Additional)),

            Entry("zenithPostMoonLord", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Zenith,
                            Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Recommended)),

            Entry("chainGuillotinesHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.ChainGuillotines,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            Entry("iceSickleHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.IceSickle,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("frostbrandHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Frostbrand,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            Entry("obsidianSwordfishHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.ObsidianSwordfish,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("cutlassHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Cutlass,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Entry("hamBatHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.HamBat,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("hardmodeOrePolearmsHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee,
                            Group(ItemID.CobaltNaginata, ItemID.PalladiumPike, ItemID.OrichalcumHalberd, ItemID.MythrilHalberd, ItemID.AdamantiteGlaive, ItemID.TitaniumTrident),
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("sergeantUnitedShieldHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.BouncingShield,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("dripplerCripplerHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.DripplerFlail,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Entry("bladetongueHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Bladetongue,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("flyingKnifeHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.FlyingKnife,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("breakerBladeHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.BreakerBlade,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Entry("joustingLanceHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.JoustingLance,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Entry("beamSwordHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.BeamSword,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Entry("shadowFlameKnifeHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.ShadowFlameKnife,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("daoofPowHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.DaoofPow,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Entry("anchorHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Anchor,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Entry("koCannonHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.KOCannon,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Entry("daedalusStormbowHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.DaedalusStormbow,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("marrowHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Marrow,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("shadowFlameBowHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.ShadowFlameBow,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            Entry("iceBowHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.IceBow,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("uziHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Uzi,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("onyxBlasterHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.OnyxBlaster,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("clockworkAssaultRifleHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.ClockworkAssaultRifle,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            Entry("dartGunPairHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Ranged,
                            Group(ItemID.DartRifle, ItemID.DartPistol),
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("toxikarpHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Toxikarp,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            Entry("coinGunHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.CoinGun,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("pearlwoodBowHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.PearlwoodBow,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Entry("gatligatorHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Gatligator,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("shotgunHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Shotgun,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("cobaltOrPalladiumRepeaterHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Ranged,
                            Group(ItemID.CobaltRepeater, ItemID.PalladiumRepeater),
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("mythrilOrOrichalcumRepeaterHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Ranged,
                            Group(ItemID.MythrilRepeater, ItemID.OrichalcumRepeater),
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("adamantiteOrTitaniumRepeaterHardmodeEntry", JournalItemCategory.Weapon, CombatClass.Ranged,
                            Group(ItemID.AdamantiteRepeater, ItemID.TitaniumRepeater),
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            Entry("dd2PhoenixBowPostOneMechBoss", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.DD2PhoenixBow,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

            Entry("hallowedRepeaterPostOneMechBoss", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.HallowedRepeater,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Additional)),

            Entry("superStarCannonPostOneMechBoss", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.SuperStarCannon,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Additional)),

            Entry("megasharkPostOneMechBoss", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Megashark,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

            Entry("flamethrowerPostOneMechBoss", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Flamethrower,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

            Entry("pulseBowPostThreeMechBosses", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.PulseBow,
                            Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.Additional)),

            Entry("chlorophyteShotbowPostThreeMechBosses", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.ChlorophyteShotbow,
                            Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.Recommended)),

            Entry("tsunamiPostPlantera", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Tsunami,
                            Eval(ProgressionStageId.PostDukeFishron, RecommendationTier.Recommended)),

            Entry("eventidePostPlantera", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.FairyQueenRangedItem,
                            Eval(ProgressionStageId.PostEmpressOfLight, RecommendationTier.Recommended)),

            Entry("stakeLauncherPostPlantera", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.StakeLauncher,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            Entry("piranhaGunPostPlantera", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.PiranhaGun,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Additional)),

            Entry("venusMagnumPostPlantera", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.VenusMagnum,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Additional)),

            Entry("tacticalShotgunPostPlantera", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.TacticalShotgun,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Useless)),

            Entry("sniperRiflePostPlantera", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.SniperRifle,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.NotRecommended)),

            Entry("candyCornRiflePostPlantera", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.CandyCornRifle,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.NotRecommended)),

            Entry("chainGunPostPlantera", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.ChainGun,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            Entry("elfMelterPostPlantera", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.ElfMelter,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            Entry("grenadeLauncherPostPlantera", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.GrenadeLauncher,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.NotRecommended)),

            Entry("proximityMineLauncherPostPlantera", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.ProximityMineLauncher,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Useless)),

            Entry("rocketLauncherPostPlantera", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.RocketLauncher,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.NotRecommended)),

            Entry("nailGunPostPlantera", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.NailGun,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.NotRecommended)),

            Entry("jackOLanternLauncherPostPlantera", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.JackOLanternLauncher,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.NotRecommended)),

            Entry("snowmanCannonPostPlantera", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.SnowmanCannon,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            Entry("dd2BetsyBowPostGolem", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.DD2BetsyBow,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Additional)),

            Entry("xenopopperPostGolem", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Xenopopper,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.NotRecommended)),

            Entry("styngerPostGolem", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Stynger,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Useless)),

            Entry("electrosphereLauncherPostGolem", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.ElectrosphereLauncher,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Additional)),

            Entry("fireworksLauncherPostGolem", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.FireworksLauncher,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Useless)),

            Entry("phantasmPostCelestialPillars", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Phantasm,
                            Eval(ProgressionStageId.PostCelestialPillars, RecommendationTier.Recommended)),

            Entry("vortexBeaterPostCelestialPillars", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.VortexBeater,
                            Eval(ProgressionStageId.PostCelestialPillars, RecommendationTier.Recommended)),

            Entry("sdmgPostMoonLord", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.SDMG,
                            Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Recommended)),

            Entry("celeb2PostMoonLord", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Celeb2,
                            Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Recommended)),
        ]);
    }
}
