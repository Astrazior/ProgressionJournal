using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;

namespace ProgressionJournal.Data;

public static class JournalRepository
{
	private static readonly Lazy<IReadOnlyList<JournalEntry>> Entries = new(BuildEntries);
	private static readonly Lazy<IReadOnlyList<JournalPreset>> Presets = new(BuildPresets);

	public static IReadOnlyList<JournalStageEntry> GetEntries(ProgressionStageId stageId, CombatClass combatClass)
	{
		return Entries.Value
			.Where(entry => entry.AppliesToClass(combatClass) && entry.TryGetEvaluation(stageId, out _))
			.Select(entry => new JournalStageEntry(entry, entry.GetEvaluation(stageId)))
			.OrderBy(entry => GetTierOrder(entry.Evaluation.Tier))
			.ThenBy(entry => GetCategoryOrder(entry.Entry.Category))
			.ThenBy(entry => GetDisplayOrderOverride(stageId, entry.Entry.Key))
			.ThenBy(entry => entry.Entry.GetDisplayName(), StringComparer.CurrentCultureIgnoreCase)
			.ToArray();
	}

	public static IReadOnlyList<JournalPreset> GetPresets(ProgressionStageId stageId, CombatClass combatClass)
	{
		return Presets.Value
			.Where(preset => preset.StageId == stageId && preset.CombatClass == combatClass)
			.ToArray();
	}

	private static IReadOnlyList<JournalEntry> BuildEntries()
	{
		return
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
				OptionalBossRequirementId.QueenBee,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Additional)),

			Entry("hiveFivePostWorldEvil", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.HiveFive,
				OptionalBossRequirementId.QueenBee,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Additional)),

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
				OptionalBossRequirementId.Deerclops,
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Useless)),

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

			Entry("flareAmmoPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged,
				Group(ItemID.Flare, ItemID.BlueFlare, ItemID.SpelunkerFlare, ItemID.ShimmerFlare),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

			Entry("seedAmmoPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.Seed,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

			Entry("musketBallPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.MusketBall,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("silverOrTungstenBulletPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged,
				Group(ItemID.SilverBullet, ItemID.TungstenBullet),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("poisonDartPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.PoisonDart,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("woodenArrowPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.WoodenArrow,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

			Entry("flamingArrowPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.FlamingArrow,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("unholyArrowPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.UnholyArrow,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("jestersArrowPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.JestersArrow,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("hellfireArrowPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.HellfireArrow,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("boneArrowPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.BoneArrow,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("frostburnArrowPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.FrostburnArrow,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("shimmerArrowPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.ShimmerArrow,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("meteorShotPostWorldEvil", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.MeteorShot,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended)),

			Entry("cursedArrowHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.CursedArrow,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("holyArrowHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.HolyArrow,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("ichorArrowHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.IchorArrow,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("endlessQuiverHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.EndlessQuiver,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

			Entry("crystalBulletHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.CrystalBullet,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("endlessMusketPouchHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.EndlessMusketPouch,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

			Entry("cursedBulletHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.CursedBullet,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

			Entry("ichorBulletHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.IchorBullet,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

			Entry("partyBulletHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.PartyBullet,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

			Entry("explodingBulletHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.ExplodingBullet,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

			Entry("goldenBulletHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.GoldenBullet,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

			Entry("highVelocityBulletPostOneMechBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.HighVelocityBullet,
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Additional)),

			Entry("chlorophyteArrowPostThreeMechBosses", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.ChlorophyteArrow,
				Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.Additional)),

			Entry("chlorophyteBulletPostThreeMechBosses", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.ChlorophyteBullet,
				Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.Recommended)),

			Entry("venomArrowPostPlantera", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.VenomArrow,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

			Entry("venomBulletPostPlantera", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.VenomBullet,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Additional)),

			Entry("nanoBulletPostPlantera", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.NanoBullet,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

			Entry("basicRocketSetPostPlantera", JournalItemCategory.ClassSpecific, CombatClass.Ranged,
				Group(ItemID.RocketI, ItemID.RocketII, ItemID.RocketIII, ItemID.RocketIV, ItemID.DryRocket, ItemID.WetRocket, ItemID.LavaRocket, ItemID.HoneyRocket),
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.NotRecommended)),

			Entry("miniNukeSetPostPlantera", JournalItemCategory.ClassSpecific, CombatClass.Ranged,
				Group(ItemID.MiniNukeI, ItemID.MiniNukeII),
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.NotRecommended)),

			Entry("clusterRocketSetPostGolem", JournalItemCategory.ClassSpecific, CombatClass.Ranged,
				Group(ItemID.ClusterRocketI, ItemID.ClusterRocketII),
				Eval(ProgressionStageId.PostGolem, RecommendationTier.NotRecommended)),

			Entry("moonlordBulletPostMoonLord", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.MoonlordBullet,
				Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Recommended)),

			Entry("moonlordArrowPostMoonLord", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.MoonlordArrow,
				Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Recommended)),

			Entry("leatherWhipPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Summoner, ItemID.BlandWhip,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("snapthornPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Summoner, ItemID.ThornWhip,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("spinalTapPostSkeletron", JournalItemCategory.ClassSpecific, CombatClass.Summoner, ItemID.BoneWhip,
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Recommended)),

			Entry("firecrackerHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Summoner, ItemID.FireWhip,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("coolWhipHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Summoner, ItemID.CoolWhip,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("durendalPostOneMechBoss", JournalItemCategory.ClassSpecific, CombatClass.Summoner, ItemID.SwordWhip,
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

			Entry("darkHarvestPostPlantera", JournalItemCategory.ClassSpecific, CombatClass.Summoner, ItemID.ScytheWhip,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Additional)),

			Entry("morningStarPostPlantera", JournalItemCategory.ClassSpecific, CombatClass.Summoner, ItemID.MaceWhip,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

			Entry("kaleidoscopePostPlantera", JournalItemCategory.ClassSpecific, CombatClass.Summoner, ItemID.RainbowWhip,
				OptionalBossRequirementId.EmpressOfLight,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

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
				OptionalBossRequirementId.QueenBee,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Additional)),

			Entry("impStaffPostWorldEvil", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.ImpStaff,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended)),

			Entry("houndiusShootiusPostSkeletron", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.HoundiusShootius,
				OptionalBossRequirementId.Deerclops,
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Additional)),

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
				OptionalBossRequirementId.QueenSlime,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

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
				OptionalBossRequirementId.DukeFishron,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

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
				OptionalBossRequirementId.EmpressOfLight,
				Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Recommended)),

			Entry("starCannonPostWorldEvil", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.StarCannon,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Additional)),

			Entry("blowgunPostWorldEvil", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Blowgun,
				OptionalBossRequirementId.QueenBee,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Additional)),

			Entry("theBeesKneesPostWorldEvil", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.BeesKnees,
				OptionalBossRequirementId.QueenBee,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended)),

			Entry("beenadePostWorldEvil", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Beenade,
				OptionalBossRequirementId.QueenBee,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended)),

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
				OptionalBossRequirementId.Deerclops,
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.NotRecommended)),

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
				OptionalBossRequirementId.QueenBee,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended)),

			Entry("aquaScepterPostSkeletron", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.AquaScepter,
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Additional)),

			Entry("magicMissilePostSkeletron", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.MagicMissile,
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Additional)),

			Entry("weatherPainPostSkeletron", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.WeatherPain,
				OptionalBossRequirementId.Deerclops,
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Additional)),

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
				OptionalBossRequirementId.DukeFishron,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Useless)),

			Entry("razorbladeTyphoonPostPlantera", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.RazorbladeTyphoon,
				OptionalBossRequirementId.DukeFishron,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

			Entry("toxicFlaskPostPlantera", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.ToxicFlask,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Useless)),

			Entry("nightglowPostPlantera", JournalItemCategory.Weapon, CombatClass.Magic, 4952,
				OptionalBossRequirementId.EmpressOfLight,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

			Entry("stellarTunePostPlantera", JournalItemCategory.Weapon, CombatClass.Magic, 4715,
				OptionalBossRequirementId.EmpressOfLight,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

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

			Entry("shieldOfCthulhuPostEye", JournalItemCategory.Accessory, CombatClass.All, ItemID.EoCShield,
				Eval(ProgressionStageId.PostEyeOfCthulhu, RecommendationTier.Recommended)),

			Entry("wormScarfOrBrainOfConfusionPostWorldEvil", JournalItemCategory.Accessory, CombatClass.All,
				Group(ItemID.WormScarf, ItemID.BrainOfConfusion),
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended)),

			Entry("honeyCombPostWorldEvil", JournalItemCategory.Accessory, CombatClass.All, ItemID.HoneyComb,
				OptionalBossRequirementId.QueenBee,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Additional)),

			Entry("stingerNecklacePostWorldEvil", JournalItemCategory.Accessory, CombatClass.All, ItemID.StingerNecklace,
				OptionalBossRequirementId.QueenBee,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended)),

			Entry("honeyBalloonPostWorldEvil", JournalItemCategory.Accessory, CombatClass.All, ItemID.HoneyBalloon,
				OptionalBossRequirementId.QueenBee,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Additional)),

			Entry("sweetheartNecklacePostWorldEvil", JournalItemCategory.Accessory, CombatClass.All, ItemID.SweetheartNecklace,
				OptionalBossRequirementId.QueenBee,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended)),

			Entry("hiveBackpackPostWorldEvil", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.HiveBackpack,
				OptionalBossRequirementId.QueenBee,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Useless)),

			Entry("pygmyNecklacePostWorldEvil", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.PygmyNecklace,
				OptionalBossRequirementId.QueenBee,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended)),

			Entry("cobaltShieldPostSkeletron", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.CobaltShield,
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Additional)),

			Entry("obsidianShieldPostSkeletron", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.ObsidianShield,
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Recommended)),

			Entry("boneGlovePostSkeletron", JournalItemCategory.Accessory, CombatClass.All, ItemID.BoneGlove,
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.NotRecommended)),

			Entry("boneHelmPostSkeletron", JournalItemCategory.Accessory, CombatClass.All, ItemID.BoneHelm,
				OptionalBossRequirementId.Deerclops,
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Additional)),

			Entry("nazarOrArmorPolishPostSkeletron", JournalItemCategory.Accessory, CombatClass.Melee,
				Group(ItemID.Nazar, ItemID.ArmorPolish),
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Useless)),

			Entry("celestialMagnetPreBoss", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.CelestialMagnet,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("magicCuffsPreBoss", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.MagicCuffs,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("celestialCuffsOrMagnetFlowerPreBoss", JournalItemCategory.Accessory, CombatClass.Magic,
				Group(ItemID.CelestialCuffs, ItemID.MagnetFlower),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("bandOfStarpowerPreBoss", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.BandofStarpower,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("manaRegenerationBandPreBoss", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.ManaRegenerationBand,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("naturesGiftPreBoss", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.NaturesGift,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

			Entry("manaFlowerPreBoss", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.ManaFlower,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("royalGelPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.RoyalGel,
				OptionalBossRequirementId.KingSlime,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

			Entry("magiluminescencePreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.Magiluminescence,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			new JournalEntry("bootsProgressionPreBoss", JournalItemCategory.Accessory, CombatClass.All,
				[
					Group(ItemID.HermesBoots, ItemID.FlurryBoots, ItemID.SailfishBoots, ItemID.SandBoots),
					Group(ItemID.RocketBoots),
					Group(ItemID.SpectreBoots),
					Group(ItemID.LightningBoots),
					Group(ItemID.FrostsparkBoots),
					Group(ItemID.TerrasparkBoots)
				],
				[Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)]),

			Entry("amphibianBootsPreBoss", JournalItemCategory.Accessory, CombatClass.All,
				Group(ItemID.AmphibianBoots, ItemID.FairyBoots),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("sharkToothNecklacePreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.SharkToothNecklace,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("panicNecklacePreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.PanicNecklace,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("sandstormOrBlizzardBottlePreBoss", JournalItemCategory.Accessory, CombatClass.All,
				Group(ItemID.SandstorminaBottle, ItemID.BlizzardinaBottle, ItemID.SharkronBalloon),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("balloonBundlesPreBoss", JournalItemCategory.Accessory, CombatClass.All,
				Group(ItemID.BundleofBalloons, ItemID.HorseshoeBundle),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("feralClawsPreBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.FeralClaws,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("feralClawsSummonerPreBoss", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.FeralClaws,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("apprenticeScarfOrSquireShieldPreBoss", JournalItemCategory.Accessory, CombatClass.Summoner,
				Group(ItemID.ApprenticeScarf, ItemID.SquireShield),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("magmaStonePreBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.MagmaStone,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("bezoarPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.Bezoar,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("obsidianRosePreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.ObsidianRose,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("lavaCharmOrMoltenCharmPreBoss", JournalItemCategory.Accessory, CombatClass.All,
				Group(ItemID.LavaCharm, ItemID.MoltenCharm),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("obsidianSkullLinePreBoss", JournalItemCategory.Accessory, CombatClass.All,
				Group(ItemID.ObsidianSkull, ItemID.ObsidianSkullRose, ItemID.MoltenSkullRose),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("bandOfRegenerationPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.BandofRegeneration,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("fledglingWingsPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.CreativeWings,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("ankletOfTheWindPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.AnkletoftheWind,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("horseshoeBalloonsPreBoss", JournalItemCategory.Accessory, CombatClass.All,
				Group(ItemID.WhiteHorseshoeBalloon, ItemID.BlueHorseshoeBalloon, ItemID.YellowHorseshoeBalloon, ItemID.BalloonHorseshoeHoney, ItemID.BalloonHorseshoeSharkron, ItemID.BalloonHorseshoeFart),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("cloudInBottle", JournalItemCategory.Accessory, CombatClass.All,
				Group(ItemID.CloudinaBottle, ItemID.TsunamiInABottle, ItemID.FartinaJar),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("stringsPreBoss", JournalItemCategory.Accessory, CombatClass.Melee,
				Group(ItemID.WhiteString, ItemID.BlueString, ItemID.GreenString, ItemID.BrownString, ItemID.OrangeString, ItemID.YellowString, ItemID.RedString, ItemID.PurpleString, ItemID.BlackString, ItemID.RainbowString, ItemID.PinkString, ItemID.SkyBlueString, ItemID.CyanString, ItemID.LimeString, ItemID.TealString, ItemID.VioletString),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("counterweightsPreBoss", JournalItemCategory.Accessory, CombatClass.Melee,
				Group(ItemID.BlackCounterweight, ItemID.BlueCounterweight, ItemID.GreenCounterweight, ItemID.PurpleCounterweight, ItemID.RedCounterweight, ItemID.YellowCounterweight),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("luckyHorseshoePreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.LuckyHorseshoe,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("agletPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.Aglet,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("flyingCarpetPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.FlyingCarpet,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("shinyRedBalloonPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.ShinyRedBalloon,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

			Entry("balloonPufferfishPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.BalloonPufferfish,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("shacklePreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.Shackle,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

			Entry("frogWebbingPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.FrogWebbing,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("frogGearPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.FrogGear,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("frogLegPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.FrogLeg,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

			Entry("frogFlipperPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.FrogFlipper,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

			Entry("climbingClawsPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.ClimbingClaws,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

			Entry("shoeSpikesPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.ShoeSpikes,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

			Entry("tigerClimbingGearPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.TigerClimbingGear,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

			Entry("breathingReedPreBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.BreathingReed,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

			Entry("umbrellaPreBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.Umbrella,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

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
				OptionalBossRequirementId.QueenBee,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended)),

			Set("ancientShadowArmorPreBoss", JournalItemCategory.Armor, CombatClass.All,
				ItemID.AncientShadowHelmet, ItemID.AncientShadowScalemail, ItemID.AncientShadowGreaves,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Set("ninjaArmorPreBoss", JournalItemCategory.Armor, CombatClass.Melee,
				ItemID.NinjaHood, ItemID.NinjaShirt, ItemID.NinjaPants,
				OptionalBossRequirementId.KingSlime,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Set("fossilArmorPreBoss", JournalItemCategory.Armor, CombatClass.Ranged,
				ItemID.FossilHelm, ItemID.FossilShirt, ItemID.FossilPants,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Set("ninjaArmorRangedPreBoss", JournalItemCategory.Armor, CombatClass.Ranged,
				ItemID.NinjaHood, ItemID.NinjaShirt, ItemID.NinjaPants,
				OptionalBossRequirementId.KingSlime,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

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

			Entry("postPlanteraWingsPostPlantera", JournalItemCategory.Accessory, CombatClass.All,
				Group(ItemID.MothronWings, ItemID.FestiveWings, ItemID.SpookyWings, ItemID.TatteredFairyWings, ItemID.LeafWings, 823, 1866),
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

			Entry("fishronWingsPostPlantera", JournalItemCategory.Accessory, CombatClass.All, ItemID.FishronWings,
				OptionalBossRequirementId.DukeFishron,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

			Entry("empressWingsPostPlantera", JournalItemCategory.Accessory, CombatClass.All, ItemID.EmpressFlightBooster,
				OptionalBossRequirementId.EmpressOfLight,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

			Entry("empressActualWingsPostPlantera", JournalItemCategory.Accessory, CombatClass.All, 4823,
				OptionalBossRequirementId.EmpressOfLight,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

			Entry("frozenShieldOrHeroShieldPostPlantera", JournalItemCategory.Accessory, CombatClass.Melee,
				Group(ItemID.FrozenShield, ItemID.HeroShield),
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

			Entry("blackBeltPostPlantera", JournalItemCategory.Accessory, CombatClass.All, ItemID.BlackBelt,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

			Entry("masterNinjaGearPostPlantera", JournalItemCategory.Accessory, CombatClass.All, ItemID.MasterNinjaGear,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

			Entry("northPolePostPlantera", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.NorthPole,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Additional)),

			Entry("piercingStarlightPostPlantera", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.PiercingStarlight,
				OptionalBossRequirementId.EmpressOfLight,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Additional)),

			Entry("flaironPostPlantera", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Flairon,
				OptionalBossRequirementId.DukeFishron,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Additional)),

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

			Entry("sporeSacPostPlantera", JournalItemCategory.Accessory, CombatClass.All, ItemID.SporeSac,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.NotRecommended)),

			Entry("herculesBeetlePostPlantera", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.HerculesBeetle,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

			Entry("papyrusScarabPostPlantera", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.PapyrusScarab,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

			Entry("paladinsShieldPostPlantera", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.PaladinsShield,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Additional)),

			Entry("tabiPostPlantera", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.Tabi,
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

			Entry("moonLordWingsPostMoonLord", JournalItemCategory.Accessory, CombatClass.All,
				Group(ItemID.LongRainbowTrailWings, ItemID.WingsSolar, ItemID.WingsNebula, ItemID.WingsVortex, ItemID.WingsStardust),
				Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Recommended)),

			Entry("destroyerEmblemPostGolem", JournalItemCategory.Accessory, CombatClass.All, ItemID.DestroyerEmblem,
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended)),

			Entry("eyeOfTheGolemMeleePostGolem", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.EyeoftheGolem,
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Additional)),

			Entry("eyeOfTheGolemRangedPostGolem", JournalItemCategory.Accessory, CombatClass.Ranged, ItemID.EyeoftheGolem,
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Additional)),

			Entry("eyeOfTheGolemMagicPostGolem", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.EyeoftheGolem,
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Additional)),

			Entry("eyeOfTheGolemSummonerPostGolem", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.EyeoftheGolem,
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Useless)),

			Entry("scopeLinePostPlantera", JournalItemCategory.Accessory, CombatClass.Ranged,
				Group(ItemID.RifleScope, ItemID.SniperScope, ItemID.ReconScope),
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

			Entry("sunStonePostGolem", JournalItemCategory.Accessory, CombatClass.All, ItemID.SunStone,
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Additional)),

			Entry("celestialStonePostGolem", JournalItemCategory.Accessory, CombatClass.All, ItemID.CelestialStone,
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended)),

			Entry("celestialShellPostGolem", JournalItemCategory.Accessory, CombatClass.All, ItemID.CelestialShell,
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended)),

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

			Entry("volatileGelatinHardmodeEntry", JournalItemCategory.Accessory, CombatClass.All, ItemID.VolatileGelatin,
				OptionalBossRequirementId.QueenSlime,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

			Entry("ankhIngredientsHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee,
				Group(ItemID.Blindfold, ItemID.Megaphone, ItemID.TrifoldMap, ItemID.FastClock, ItemID.Vitamins, ItemID.PocketMirror),
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

			Entry("ankhIntermediatesHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee,
				Group(ItemID.CountercurseMantra, ItemID.MedicatedBandage, ItemID.ThePlan, ItemID.ArmorBracing, ItemID.ReflectiveShades),
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

			Entry("frozenTurtleShellHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.FrozenTurtleShell,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("frozenTurtleShellRangedHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Ranged, ItemID.FrozenTurtleShell,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

			Entry("crossNecklaceHardmodeEntry", JournalItemCategory.Accessory, CombatClass.All, ItemID.CrossNecklace,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

			Entry("moonCharmHardmodeEntry", JournalItemCategory.Accessory, CombatClass.All, ItemID.MoonCharm,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("earlyHardmodeWingsHardmodeEntry", JournalItemCategory.Accessory, CombatClass.All,
				Group(493, 492, 761, 2494, 822, 785),
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("philosophersStoneHardmodeEntry", JournalItemCategory.Accessory, CombatClass.All, ItemID.PhilosophersStone,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

			Entry("charmofMythsHardmodeEntry", JournalItemCategory.Accessory, CombatClass.All, ItemID.CharmofMyths,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("ankhCharmHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.AnkhCharm,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

			Entry("ankhShieldHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.AnkhShield,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("arcaneFlowerHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.ArcaneFlower,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("manaCloakHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.ManaCloak,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("sorcererEmblemHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.SorcererEmblem,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("putridScentMagicHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.PutridScent,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("frozenTurtleShellMagicHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.FrozenTurtleShell,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

			Entry("titanGloveSummonerHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.TitanGlove,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("berserkerGloveSummonerHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.BerserkerGlove,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

			Entry("powerGloveSummonerHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.PowerGlove,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("mechanicalGloveSummonerHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.MechanicalGlove,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("fireGauntletSummonerHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.FireGauntlet,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("putridScentSummonerHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.PutridScent,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("frozenTurtleShellSummonerHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.FrozenTurtleShell,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

			Entry("hiveBackpackSummonerHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.HiveBackpack,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("starCloakOrBeeCloakHardmodeEntry", JournalItemCategory.Accessory, CombatClass.All,
				Group(ItemID.StarCloak, ItemID.BeeCloak),
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

			Entry("starVeilHardmodeEntry", JournalItemCategory.Accessory, CombatClass.All, ItemID.StarVeil,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("warriorEmblemHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.WarriorEmblem,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("celestialEmblemPostThreeMechBosses", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.CelestialEmblem,
				Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.Recommended)),

			Entry("avengerEmblemPostOneMechBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.AvengerEmblem,
				Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.Recommended)),

			Entry("titanGloveHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.TitanGlove,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("berserkerGloveHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.BerserkerGlove,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("powerGloveHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.PowerGlove,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("mechanicalGlovePostOneMechBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.MechanicalGlove,
				Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.Recommended)),

			Entry("fireGauntletPostOneMechBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.FireGauntlet,
				Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.Recommended)),

			Entry("yoyoGloveHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.YoYoGlove,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("yoyoBagHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.YoyoBag,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("fleshKnucklesHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.FleshKnuckles,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("putridScentHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Ranged, ItemID.PutridScent,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("hivePackPostWorldEvil", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.HiveBackpack,
				OptionalBossRequirementId.QueenBee,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended)),

			Entry("huntressBucklerOrMonkBeltPostOneMechBoss", JournalItemCategory.Accessory, CombatClass.Summoner,
				Group(ItemID.HuntressBuckler, ItemID.MonkBelt),
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Useless)),

			Entry("moonStonePostOneMechBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.MoonStone,
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

			Entry("neptunesShellPostOneMechBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.NeptunesShell,
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Additional)),

			Entry("moonShellPostOneMechBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.MoonShell,
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

			Entry("shinyStonePostGolem", JournalItemCategory.Accessory, CombatClass.All, ItemID.ShinyStone,
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Useless)),

			Entry("postOneMechBossWingsPostOneMechBoss", JournalItemCategory.Accessory, CombatClass.All,
				Group(ItemID.Jetpack, ItemID.BatWings, ItemID.BeeWings, ItemID.ButterflyWings, ItemID.FlameWings),
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

			Entry("rangerEmblemHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Ranged, ItemID.RangerEmblem,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("quiverLineHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Ranged,
				Group(ItemID.MagicQuiver, ItemID.MoltenQuiver, ItemID.StalkersQuiver),
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

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
				OptionalBossRequirementId.DukeFishron,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

			Entry("eventidePostPlantera", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.FairyQueenRangedItem,
				OptionalBossRequirementId.EmpressOfLight,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

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

			Set("huntressArmorPostOneMechBoss", JournalItemCategory.Armor, CombatClass.Ranged,
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

			Set("apprenticeArmorPostOneMechBoss", JournalItemCategory.Armor, CombatClass.Magic,
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

			Set("darkArtistArmorPostGolem", JournalItemCategory.Armor, CombatClass.Magic,
				ItemID.ApprenticeAltHead, ItemID.ApprenticeAltShirt, ItemID.ApprenticeAltPants,
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended)),

			Set("nebulaArmorPostMoonLord", JournalItemCategory.Armor, CombatClass.Magic,
				ItemID.NebulaHelmet, ItemID.NebulaBreastplate, ItemID.NebulaLeggings,
				Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Recommended)),

			Set("cobaltOrPalladiumArmorSummonerHardmodeEntry", JournalItemCategory.Armor, CombatClass.Summoner,
				Group(ItemID.CobaltHelmet, ItemID.CobaltMask, ItemID.CobaltHat, ItemID.PalladiumHelmet, ItemID.PalladiumMask, ItemID.PalladiumHeadgear),
				Group(ItemID.CobaltBreastplate, ItemID.PalladiumBreastplate),
				Group(ItemID.CobaltLeggings, ItemID.PalladiumLeggings),
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

			Set("mythrilOrOrichalcumArmorSummonerHardmodeEntry", JournalItemCategory.Armor, CombatClass.Summoner,
				Group(ItemID.MythrilHelmet, ItemID.MythrilHood, ItemID.OrichalcumHelmet, ItemID.OrichalcumMask, ItemID.OrichalcumHeadgear),
				Group(ItemID.MythrilChainmail, ItemID.OrichalcumBreastplate),
				Group(ItemID.MythrilGreaves, ItemID.OrichalcumLeggings),
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

			Set("adamantiteOrTitaniumArmorSummonerHardmodeEntry", JournalItemCategory.Armor, CombatClass.Summoner,
				Group(ItemID.AdamantiteHelmet, ItemID.AdamantiteMask, ItemID.AdamantiteHeadgear, ItemID.TitaniumHelmet, ItemID.TitaniumMask, ItemID.TitaniumHeadgear),
				Group(ItemID.AdamantiteBreastplate, ItemID.TitaniumBreastplate),
				Group(ItemID.AdamantiteLeggings, ItemID.TitaniumLeggings),
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

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
				[Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Additional)]),

			Set("hallowedArmorSummonerPostOneMechBoss", JournalItemCategory.Armor, CombatClass.Summoner,
				ItemID.HallowedHood, ItemID.HallowedPlateMail, ItemID.HallowedGreaves,
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

			Set("tikiArmorPostPlantera", JournalItemCategory.Armor, CombatClass.Summoner,
				ItemID.TikiMask, ItemID.TikiShirt, ItemID.TikiPants,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

			Set("spookyArmorPostPlantera", JournalItemCategory.Armor, CombatClass.Summoner,
				ItemID.SpookyHelmet, ItemID.SpookyBreastplate, ItemID.SpookyLeggings,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

			new JournalEntry("dd2ArmorPostGolem", JournalItemCategory.Armor, CombatClass.Summoner,
				[
					Group(ItemID.ApprenticeAltHead, ItemID.MonkAltHead, ItemID.HuntressAltHead, ItemID.SquireAltHead),
					Group(ItemID.ApprenticeAltShirt, ItemID.MonkAltShirt, ItemID.HuntressAltShirt, ItemID.SquireAltShirt),
					Group(ItemID.ApprenticeAltPants, ItemID.MonkAltPants, ItemID.HuntressAltPants, ItemID.SquireAltPants)
				],
				[Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended)]),

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
				OptionalBossRequirementId.QueenSlime,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

			Set("frostArmorHardmodeEntry", JournalItemCategory.Armor, CombatClass.Melee,
				ItemID.FrostHelmet, ItemID.FrostBreastplate, ItemID.FrostLeggings,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Set("hallowedArmorPostOneMechBoss", JournalItemCategory.Armor, CombatClass.Melee,
				ItemID.HallowedMask, ItemID.HallowedPlateMail, ItemID.HallowedGreaves,
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

			Set("monkArmorPostOneMechBoss", JournalItemCategory.Armor, CombatClass.Melee,
				ItemID.MonkBrows, ItemID.MonkShirt, ItemID.MonkPants,
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Additional)),

			Set("squireArmorPostOneMechBoss", JournalItemCategory.Armor, CombatClass.Melee,
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

			Set("redRidingArmorPostGolem", JournalItemCategory.Armor, CombatClass.Ranged,
				ItemID.HuntressAltHead, ItemID.HuntressAltShirt, ItemID.HuntressAltPants,
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Additional)),

			Set("valhallaKnightArmorPostGolem", JournalItemCategory.Armor, CombatClass.Melee,
				Group(3871), Group(3872), Group(3873),
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended)),

			Set("shroomiteArmorPostPlantera", JournalItemCategory.Armor, CombatClass.Ranged,
				Group(ItemID.ShroomiteHeadgear, ItemID.ShroomiteMask, ItemID.ShroomiteHelmet),
				Group(ItemID.ShroomiteBreastplate),
				Group(ItemID.ShroomiteLeggings),
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

			Set("vortexArmorPostMoonLord", JournalItemCategory.Armor, CombatClass.Ranged,
				ItemID.VortexHelmet, ItemID.VortexBreastplate, ItemID.VortexLeggings,
				Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Recommended)),

			Set("solarFlareArmorPostMoonLord", JournalItemCategory.Armor, CombatClass.Melee,
				ItemID.SolarFlareHelmet, ItemID.SolarFlareBreastplate, ItemID.SolarFlareLeggings,
				Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Recommended))
		];
	}

	private static IReadOnlyList<JournalPreset> BuildPresets()
	{
		return Array.Empty<JournalPreset>();
	}

	private static JournalEntry Entry(
		string key,
		JournalItemCategory category,
		CombatClass classes,
		JournalItemGroup itemGroup,
		params StageEvaluation[] evaluations)
	{
		return new JournalEntry(key, category, classes, [itemGroup], evaluations);
	}

	private static JournalEntry Entry(
		string key,
		JournalItemCategory category,
		CombatClass classes,
		int itemId,
		params StageEvaluation[] evaluations)
	{
		return new JournalEntry(key, category, classes, [itemId], evaluations);
	}

	private static JournalEntry Entry(
		string key,
		JournalItemCategory category,
		CombatClass classes,
		JournalItemGroup itemGroup,
		OptionalBossRequirementId optionalBossRequirement,
		params StageEvaluation[] evaluations)
	{
		return new JournalEntry(key, category, classes, [itemGroup], evaluations, optionalBossRequirement);
	}

	private static JournalEntry Entry(
		string key,
		JournalItemCategory category,
		CombatClass classes,
		int itemId,
		OptionalBossRequirementId optionalBossRequirement,
		params StageEvaluation[] evaluations)
	{
		return new JournalEntry(key, category, classes, [itemId], evaluations, optionalBossRequirement);
	}

	private static JournalEntry Set(
		string key,
		JournalItemCategory category,
		CombatClass classes,
		JournalItemGroup firstGroup,
		JournalItemGroup secondGroup,
		JournalItemGroup thirdGroup,
		params StageEvaluation[] evaluations)
	{
		return new JournalEntry(key, category, classes, [firstGroup, secondGroup, thirdGroup], evaluations);
	}

	private static JournalEntry Set(
		string key,
		JournalItemCategory category,
		CombatClass classes,
		int firstItemId,
		int secondItemId,
		int thirdItemId,
		params StageEvaluation[] evaluations)
	{
		return new JournalEntry(key, category, classes, [firstItemId, secondItemId, thirdItemId], evaluations);
	}

	private static JournalEntry Set(
		string key,
		JournalItemCategory category,
		CombatClass classes,
		JournalItemGroup firstGroup,
		JournalItemGroup secondGroup,
		JournalItemGroup thirdGroup,
		OptionalBossRequirementId optionalBossRequirement,
		params StageEvaluation[] evaluations)
	{
		return new JournalEntry(key, category, classes, [firstGroup, secondGroup, thirdGroup], evaluations, optionalBossRequirement);
	}

	private static JournalEntry Set(
		string key,
		JournalItemCategory category,
		CombatClass classes,
		int firstItemId,
		int secondItemId,
		int thirdItemId,
		OptionalBossRequirementId optionalBossRequirement,
		params StageEvaluation[] evaluations)
	{
		return new JournalEntry(key, category, classes, [firstItemId, secondItemId, thirdItemId], evaluations, optionalBossRequirement);
	}

	private static StageEvaluation Eval(ProgressionStageId stageId, RecommendationTier tier)
	{
		return new StageEvaluation(stageId, tier);
	}

	private static JournalItemGroup Group(params int[] itemIds)
	{
		return new JournalItemGroup(itemIds);
	}

	private static int GetTierOrder(RecommendationTier tier) => tier switch
	{
		RecommendationTier.Recommended => 0,
		RecommendationTier.Additional => 1,
		RecommendationTier.NotRecommended => 2,
		RecommendationTier.Useless => 3,
		_ => int.MaxValue
	};

	private static int GetDisplayOrderOverride(ProgressionStageId stageId, string entryKey)
	{
		if (stageId == ProgressionStageId.PreBoss) {
			return entryKey switch
			{
				"sandstormOrBlizzardBottlePreBoss" => 0,
				"balloonBundlesPreBoss" => 1,
				_ => int.MaxValue
			};
		}

		return int.MaxValue;
	}

	private static int GetCategoryOrder(JournalItemCategory category) => category switch
	{
		JournalItemCategory.Weapon => 0,
		JournalItemCategory.ClassSpecific => 1,
		JournalItemCategory.Armor => 2,
		JournalItemCategory.Accessory => 3,
		_ => int.MaxValue
	};
}
