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
			Entry("woodenSword", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.WoodenSword,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

			Entry("platinumBroadsword", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.PlatinumBroadsword,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostEyeOfCthulhu, RecommendationTier.Situational),
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.NotRecommended)),

			Entry("bladeOfGrass", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.BladeofGrass,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Recommended),
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Situational)),

			Entry("nightsEdge", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.NightsEdge,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Situational)),

			Entry("excalibur", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.Excalibur,
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Situational)),

			Entry("terraBlade", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.TerraBlade,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Situational)),

			Entry("goldBow", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.GoldBow,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostEyeOfCthulhu, RecommendationTier.Situational),
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.NotRecommended)),

			Entry("minishark", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Minishark,
				Eval(ProgressionStageId.PostEyeOfCthulhu, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Situational)),

			Entry("moltenFury", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.MoltenFury,
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Recommended),
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Situational)),

			Entry("megashark", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Megashark,
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Situational)),

			Entry("chlorophyteShotbow", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.ChlorophyteShotbow,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Situational)),

			Entry("diamondStaff", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.DiamondStaff,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostEyeOfCthulhu, RecommendationTier.Situational),
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.NotRecommended)),

			Entry("spaceGun", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.SpaceGun,
				Eval(ProgressionStageId.PostEyeOfCthulhu, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Situational)),

			Entry("demonScythe", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.DemonScythe,
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Recommended),
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Situational)),

			Entry("meteorStaff", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.MeteorStaff,
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Situational)),

			Entry("spectreStaff", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.SpectreStaff,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Situational)),

			Entry("flinxStaff", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.FlinxStaff,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostEyeOfCthulhu, RecommendationTier.Situational),
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.NotRecommended)),

			Entry("thornWhip", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.ThornWhip,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Recommended),
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Situational)),

			Entry("impStaff", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.ImpStaff,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Situational)),

			Entry("sanguineStaff", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.SanguineStaff,
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Situational)),

			Entry("opticStaff", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.OpticStaff,
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Situational)),

			Entry("xenoStaff", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.XenoStaff,
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Situational)),

			Entry("hermesBoots", JournalItemCategory.Accessory, CombatClass.All, ItemID.HermesBoots,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostEyeOfCthulhu, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Situational),
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

			Entry("cloudInBottle", JournalItemCategory.Accessory, CombatClass.All, ItemID.CloudinaBottle,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostEyeOfCthulhu, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Situational),
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.NotRecommended)),

			Entry("obsidianShield", JournalItemCategory.Accessory, CombatClass.All, ItemID.ObsidianShield,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Situational)),

			Entry("leafWings", JournalItemCategory.Accessory, CombatClass.All, ItemID.LeafWings,
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Situational)),

			Entry("ankhShield", JournalItemCategory.Accessory, CombatClass.All, ItemID.AnkhShield,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Situational)),

			Set("jungleArmor", JournalItemCategory.Armor, CombatClass.Magic,
				ItemID.JungleHat, ItemID.JungleShirt, ItemID.JunglePants,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostEyeOfCthulhu, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Situational)),

			Set("fossilArmor", JournalItemCategory.Armor, CombatClass.Ranged,
				ItemID.FossilHelm, ItemID.FossilShirt, ItemID.FossilPants,
				Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostEyeOfCthulhu, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Situational)),

			Set("beeArmor", JournalItemCategory.Armor, CombatClass.Summoner,
				ItemID.BeeHeadgear, ItemID.BeeBreastplate, ItemID.BeeGreaves,
				OptionalBossRequirementId.QueenBee,
				Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Recommended),
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Situational)),

			Set("necroArmor", JournalItemCategory.Armor, CombatClass.Ranged,
				ItemID.NecroHelmet, ItemID.NecroBreastplate, ItemID.NecroGreaves,
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Recommended),
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Situational)),

			Set("moltenArmor", JournalItemCategory.Armor, CombatClass.Melee,
				ItemID.MoltenHelmet, ItemID.MoltenBreastplate, ItemID.MoltenGreaves,
				Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Recommended),
				Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Situational)),

			Set("spiderArmor", JournalItemCategory.Armor, CombatClass.Summoner,
				ItemID.SpiderMask, ItemID.SpiderBreastplate, ItemID.SpiderGreaves,
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Situational)),

			Set("hallowedMeleeArmor", JournalItemCategory.Armor, CombatClass.Melee,
				ItemID.HallowedHelmet, ItemID.HallowedPlateMail, ItemID.HallowedGreaves,
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Situational)),

			Set("hallowedRangedArmor", JournalItemCategory.Armor, CombatClass.Ranged,
				ItemID.HallowedMask, ItemID.HallowedPlateMail, ItemID.HallowedGreaves,
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Situational)),

			Set("hallowedMagicArmor", JournalItemCategory.Armor, CombatClass.Magic,
				ItemID.HallowedHeadgear, ItemID.HallowedPlateMail, ItemID.HallowedGreaves,
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Situational)),

			Set("hallowedSummonerArmor", JournalItemCategory.Armor, CombatClass.Summoner,
				ItemID.HallowedHood, ItemID.HallowedPlateMail, ItemID.HallowedGreaves,
				Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Situational)),

			Set("turtleArmor", JournalItemCategory.Armor, CombatClass.Melee,
				ItemID.TurtleHelmet, ItemID.TurtleScaleMail, ItemID.TurtleLeggings,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Situational)),

			Set("shroomiteArmor", JournalItemCategory.Armor, CombatClass.Ranged,
				ItemID.ShroomiteMask, ItemID.ShroomiteBreastplate, ItemID.ShroomiteLeggings,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Situational)),

			Set("spectreArmor", JournalItemCategory.Armor, CombatClass.Magic,
				ItemID.SpectreHood, ItemID.SpectreRobe, ItemID.SpectrePants,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Situational)),

			Set("tikiArmor", JournalItemCategory.Armor, CombatClass.Summoner,
				ItemID.TikiMask, ItemID.TikiShirt, ItemID.TikiPants,
				Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended),
				Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Situational)),

			Entry("solarEruption", JournalItemCategory.Weapon, CombatClass.Melee, ItemID.SolarEruption,
				Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Recommended)),

			Entry("phantasm", JournalItemCategory.Weapon, CombatClass.Ranged, ItemID.Phantasm,
				Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Recommended)),

			Entry("nebulaBlaze", JournalItemCategory.Weapon, CombatClass.Magic, ItemID.NebulaBlaze,
				Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Recommended)),

			Entry("stardustDragonStaff", JournalItemCategory.Weapon, CombatClass.Summoner, ItemID.StardustDragonStaff,
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
		int itemId,
		params StageEvaluation[] evaluations)
	{
		return new JournalEntry(key, category, classes, [itemId], evaluations);
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

	private static int GetTierOrder(RecommendationTier tier) => tier switch
	{
		RecommendationTier.Recommended => 0,
		RecommendationTier.Situational => 1,
		RecommendationTier.NotRecommended => 2,
		RecommendationTier.Useless => 3,
		_ => int.MaxValue
	};
}
