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

			Entry("goldOrPlatinumBroadsword", JournalItemCategory.Weapon, CombatClass.Melee,
				Group(ItemID.GoldBroadsword, ItemID.PlatinumBroadsword),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

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

			Entry("rottedForkOrBallOHurtPreBoss", JournalItemCategory.Weapon, CombatClass.Melee,
				Group(ItemID.TheRottedFork, ItemID.BallOHurt),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("tentacleSpikePreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.TentacleSpike,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

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

			Entry("flamingMacePreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.FlamingMace,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

			Entry("rallyPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Rally,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

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
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("katanaPreBoss", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Katana,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

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

			Entry("hermesBoots", JournalItemCategory.Accessory, CombatClass.Melee,
				Group(ItemID.HermesBoots, ItemID.FlurryBoots, ItemID.SailfishBoots, ItemID.SandBoots),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("sharkToothNecklacePreBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.SharkToothNecklace,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("panicNecklacePreBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.PanicNecklace,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("sandstormOrBlizzardBottlePreBoss", JournalItemCategory.Accessory, CombatClass.Melee,
				Group(ItemID.SandstorminaBottle, ItemID.BlizzardinaBottle),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("feralClawsPreBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.FeralClaws,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("bandOfRegenerationPreBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.BandofRegeneration,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("fledglingWingsPreBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.CreativeWings,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("ankletOfTheWindPreBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.AnkletoftheWind,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("cloudInBottle", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.CloudinaBottle,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("luckyHorseshoePreBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.LuckyHorseshoe,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("agletPreBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.Aglet,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("flyingCarpetPreBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.FlyingCarpet,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Entry("shinyRedBalloonPreBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.ShinyRedBalloon,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

			Entry("shacklePreBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.Shackle,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

			Entry("frogLegPreBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.FrogLeg,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

			Entry("climbingClawsPreBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.ClimbingClaws,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

			Entry("shoeSpikesPreBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.ShoeSpikes,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

			Entry("flipperPreBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.Flipper,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

			Entry("breathingReedPreBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.BreathingReed,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

			Entry("umbrellaPreBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.Umbrella,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

			Set("goldOrPlatinumArmor", JournalItemCategory.Armor, CombatClass.Melee,
				Group(ItemID.GoldHelmet, ItemID.PlatinumHelmet),
				Group(ItemID.GoldChainmail, ItemID.PlatinumChainmail),
				Group(ItemID.GoldGreaves, ItemID.PlatinumGreaves),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Set("gladiatorArmorPreBoss", JournalItemCategory.Armor, CombatClass.Melee,
				ItemID.GladiatorHelmet, ItemID.GladiatorBreastplate, ItemID.GladiatorLeggings,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

			Entry("giPreBoss", JournalItemCategory.Armor, CombatClass.Melee, ItemID.Gi,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Set("silverOrTungstenArmorPreBoss", JournalItemCategory.Armor, CombatClass.Melee,
				Group(ItemID.SilverHelmet, ItemID.TungstenHelmet),
				Group(ItemID.SilverChainmail, ItemID.TungstenChainmail),
				Group(ItemID.SilverGreaves, ItemID.TungstenGreaves),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Set("ironOrLeadArmorPreBoss", JournalItemCategory.Armor, CombatClass.Melee,
				Group(ItemID.IronHelmet, ItemID.LeadHelmet),
				Group(ItemID.IronChainmail, ItemID.LeadChainmail),
				Group(ItemID.IronGreaves, ItemID.LeadGreaves),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

			Set("cactusArmorPreBoss", JournalItemCategory.Armor, CombatClass.Melee,
				ItemID.CactusHelmet, ItemID.CactusBreastplate, ItemID.CactusLeggings,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

			Set("copperOrTinArmorPreBoss", JournalItemCategory.Armor, CombatClass.Melee,
				Group(ItemID.CopperHelmet, ItemID.TinHelmet),
				Group(ItemID.CopperChainmail, ItemID.TinChainmail),
				Group(ItemID.CopperGreaves, ItemID.TinGreaves),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

			Set("snowOrPinkSnowArmorPreBoss", JournalItemCategory.Armor, CombatClass.Melee,
				Group(803, 978),
				Group(804, 979),
				Group(805, 980),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

			new JournalEntry("rainArmorPreBoss", JournalItemCategory.Armor, CombatClass.Melee, [ItemID.RainHat, ItemID.RainCoat],
				[Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)]),

			Set("earlyWoodArmorPreBoss", JournalItemCategory.Armor, CombatClass.Melee,
				Group(ItemID.WoodHelmet, ItemID.BorealWoodHelmet, ItemID.RichMahoganyHelmet, ItemID.PalmWoodHelmet, ItemID.EbonwoodHelmet, ItemID.ShadewoodHelmet, ItemID.AshWoodHelmet),
				Group(ItemID.WoodBreastplate, ItemID.BorealWoodBreastplate, ItemID.RichMahoganyBreastplate, ItemID.PalmWoodBreastplate, ItemID.EbonwoodBreastplate, ItemID.ShadewoodBreastplate, ItemID.AshWoodBreastplate),
				Group(ItemID.WoodGreaves, ItemID.BorealWoodGreaves, ItemID.RichMahoganyGreaves, ItemID.PalmWoodGreaves, ItemID.EbonwoodGreaves, ItemID.ShadewoodGreaves, ItemID.AshWoodGreaves),
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless))
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
}

