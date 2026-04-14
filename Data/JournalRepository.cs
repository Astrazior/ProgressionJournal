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
			.ThenBy(entry => entry.Entry.Category)
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

			Entry("grenadePreBoss", JournalItemCategory.Weapon, CombatClass.Ranged,
				Group(ItemID.Grenade, ItemID.StickyGrenade, ItemID.BouncyGrenade),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("molotovCocktailPreBoss", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.MolotovCocktail,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

			Entry("throwingWeaponsPreBoss", JournalItemCategory.Weapon, CombatClass.Ranged,
				Group(ItemID.Shuriken, ItemID.ThrowingKnife, ItemID.PoisonedKnife, ItemID.SpikyBall, ItemID.Snowball, ItemID.Javelin),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

			Entry("boneJavelinPreBoss", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.BoneJavelin,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("frostDaggerfishPreBoss", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.FrostDaggerfish,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

			Entry("paperAirplanesPreBoss", JournalItemCategory.Weapon, CombatClass.Ranged,
				Group(ItemID.PaperAirplaneA, ItemID.PaperAirplaneB),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

			Entry("shieldOfCthulhuPostEye", JournalItemCategory.Accessory, CombatClass.All, ItemID.EoCShield,
				Eval(ProgressionStageId.PostEyeOfCthulhu, RecommendationTier.Recommended)),

			Entry("wormScarfOrBrainOfConfusionPostWorldEvil", JournalItemCategory.Accessory, CombatClass.All,
				Group(ItemID.WormScarf, ItemID.BrainOfConfusion),
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended)),

			Entry("honeyCombPostWorldEvil", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.HoneyComb,
				OptionalBossRequirementId.QueenBee,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Additional)),

			Entry("stingerNecklacePostWorldEvil", JournalItemCategory.Accessory, CombatClass.All, ItemID.StingerNecklace,
				OptionalBossRequirementId.QueenBee,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended)),

			Entry("honeyBalloonPostWorldEvil", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.HoneyBalloon,
				OptionalBossRequirementId.QueenBee,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Additional)),

			Entry("sweetheartNecklacePostWorldEvil", JournalItemCategory.Accessory, CombatClass.All, ItemID.SweetheartNecklace,
				OptionalBossRequirementId.QueenBee,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended)),

			Entry("hiveBackpackPostWorldEvil", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.HiveBackpack,
				OptionalBossRequirementId.QueenBee,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Useless)),

			Entry("cobaltShieldPostSkeletron", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.CobaltShield,
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Additional)),

			Entry("obsidianShieldPostSkeletron", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.ObsidianShield,
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Recommended)),

			Entry("boneGlovePostSkeletron", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.BoneGlove,
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.NotRecommended)),

			Entry("boneHelmPostSkeletron", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.BoneHelm,
				OptionalBossRequirementId.Deerclops,
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Additional)),

			Entry("nazarOrArmorPolishPostSkeletron", JournalItemCategory.Accessory, CombatClass.Melee,
				Group(ItemID.Nazar, ItemID.ArmorPolish),
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Useless)),

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

			Entry("amphibianBootsPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.AmphibianBoots,
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

			Set("shadowOrCrimsonArmorPostWorldEvil", JournalItemCategory.Armor, CombatClass.Melee,
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
				Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

			Set("snowOrPinkSnowArmorPreBoss", JournalItemCategory.Armor, CombatClass.All,
				Group(803, 978),
				Group(804, 979),
				Group(805, 980),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

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

			Entry("chlorophyteClaymorePostThreeMechBosses", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.ChlorophyteClaymore,
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

			Entry("sporeSacPostPlantera", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.SporeSac,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.NotRecommended)),

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

			Entry("volatileGelatinHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.VolatileGelatin,
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

			Entry("crossNecklaceHardmodeEntry", JournalItemCategory.Accessory, CombatClass.All, ItemID.CrossNecklace,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

			Entry("moonCharmHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.MoonCharm,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("earlyHardmodeWingsHardmodeEntry", JournalItemCategory.Accessory, CombatClass.All,
				Group(493, 492, 761, 2494, 822, 785),
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("philosophersStoneHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.PhilosophersStone,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

			Entry("charmofMythsHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.CharmofMyths,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("ankhCharmHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.AnkhCharm,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

			Entry("ankhShieldHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.AnkhShield,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

			Entry("warriorEmblemHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.WarriorEmblem,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

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

			Entry("putridScentHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.PutridScent,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

			Entry("moonStonePostOneMechBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.MoonStone,
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

			Entry("neptunesShellPostOneMechBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.NeptunesShell,
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Additional)),

			Entry("moonShellPostOneMechBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.MoonShell,
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

			Entry("postOneMechBossWingsPostOneMechBoss", JournalItemCategory.Accessory, CombatClass.All,
				Group(ItemID.Jetpack, ItemID.BatWings, ItemID.BeeWings, ItemID.ButterflyWings, ItemID.FlameWings),
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

			Set("palladiumArmorHardmodeEntry", JournalItemCategory.Armor, CombatClass.Melee,
				ItemID.PalladiumHelmet, ItemID.PalladiumBreastplate, ItemID.PalladiumLeggings,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

			Set("cobaltArmorHardmodeEntry", JournalItemCategory.Armor, CombatClass.Melee,
				ItemID.CobaltHelmet, ItemID.CobaltBreastplate, ItemID.CobaltLeggings,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

			Set("pearlwoodArmorHardmodeEntry", JournalItemCategory.Armor, CombatClass.Melee,
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

			Set("crystalAssassinArmorHardmodeEntry", JournalItemCategory.Armor, CombatClass.Melee,
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

			Set("turtleArmorPostThreeMechBosses", JournalItemCategory.Armor, CombatClass.Melee,
				ItemID.TurtleHelmet, ItemID.TurtleScaleMail, ItemID.TurtleLeggings,
				Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.Recommended)),

			Set("beetleArmorPostGolem", JournalItemCategory.Armor, CombatClass.Melee,
				Group(ItemID.BeetleHelmet), Group(ItemID.BeetleScaleMail, ItemID.BeetleShell), Group(ItemID.BeetleLeggings),
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended)),

			Set("valhallaKnightArmorPostGolem", JournalItemCategory.Armor, CombatClass.Melee,
				Group(3871), Group(3872), Group(3873),
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended)),

			Set("shinobiInfiltratorArmorPostGolem", JournalItemCategory.Armor, CombatClass.Melee,
				Group(3880), Group(3881), Group(3882),
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Additional)),

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
}
