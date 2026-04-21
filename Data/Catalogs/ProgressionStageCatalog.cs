using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace ProgressionJournal.Data.Catalogs;

public static class ProgressionStageCatalog
{
	private static readonly IReadOnlyList<ProgressionStage> OrderedStages =
	[
		new (ProgressionStageId.PreBoss, "Mods.ProgressionJournal.Stages.PreBoss", static () => true),
		new (ProgressionStageId.PostKingSlime, "Mods.ProgressionJournal.Stages.PostKingSlime", static () => NPC.downedSlimeKing),
		new (ProgressionStageId.PostEyeOfCthulhu, "Mods.ProgressionJournal.Stages.PostEyeOfCthulhu", static () => NPC.downedBoss1),
		new (ProgressionStageId.PostWorldEvil, "Mods.ProgressionJournal.Stages.PostWorldEvil", static () => NPC.downedBoss2),
		new (ProgressionStageId.PostQueenBee, "Mods.ProgressionJournal.Stages.PostQueenBee", static () => NPC.downedQueenBee),
		new (ProgressionStageId.PostSkeletron, "Mods.ProgressionJournal.Stages.PostSkeletron", static () => NPC.downedBoss3),
		new (ProgressionStageId.PostDeerclops, "Mods.ProgressionJournal.Stages.PostDeerclops", static () => NPC.downedDeerclops),
		new (ProgressionStageId.HardmodeEntry, "Mods.ProgressionJournal.Stages.HardmodeEntry", static () => Main.hardMode),
		new (ProgressionStageId.PostQueenSlime, "Mods.ProgressionJournal.Stages.PostQueenSlime", static () => NPC.downedQueenSlime),
		new (ProgressionStageId.PostOneMechBoss, "Mods.ProgressionJournal.Stages.PostOneMechBoss", static () => NPC.downedMechBossAny),
		new (ProgressionStageId.PostThreeMechBosses, "Mods.ProgressionJournal.Stages.PostThreeMechBosses", static () => NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3),
		new (ProgressionStageId.PostPlantera, "Mods.ProgressionJournal.Stages.PostPlantera", static () => NPC.downedPlantBoss),
		new (ProgressionStageId.PostDukeFishron, "Mods.ProgressionJournal.Stages.PostDukeFishron", static () => NPC.downedFishron),
		new (ProgressionStageId.PostEmpressOfLight, "Mods.ProgressionJournal.Stages.PostEmpressOfLight", static () => NPC.downedEmpressOfLight),
		new (ProgressionStageId.PostGolem, "Mods.ProgressionJournal.Stages.PostGolem", static () => NPC.downedGolemBoss),
		new (ProgressionStageId.PostCelestialPillars, "Mods.ProgressionJournal.Stages.PostCelestialPillars", static () => NPC.downedAncientCultist),
		new (ProgressionStageId.PostMoonLord, "Mods.ProgressionJournal.Stages.PostMoonLord", static () => NPC.downedMoonlord)
	];

	private static readonly Dictionary<ProgressionStageId, ProgressionStage> StagesById =
		OrderedStages.ToDictionary(stage => stage.Id);
	private static readonly Dictionary<ProgressionStageId, int> StageOrderIndices =
		OrderedStages.Select((stage, index) => new KeyValuePair<ProgressionStageId, int>(stage.Id, index))
			.ToDictionary(pair => pair.Key, pair => pair.Value);

	public static IReadOnlyList<ProgressionStage> All => OrderedStages;

	public static ProgressionStage Get(ProgressionStageId stageId) => StagesById[stageId];

	public static int GetStageOrderIndex(ProgressionStageId stageId) => StageOrderIndices[stageId];

	public static bool IsAvailable(ProgressionStageId stageId, bool progressionModeEnabled)
	{
		return !progressionModeEnabled || Get(stageId).IsUnlocked();
	}

	public static IReadOnlyList<ProgressionStageId> GetAvailableStageIds(bool progressionModeEnabled)
	{
		return progressionModeEnabled
			? OrderedStages.Where(static stage => stage.IsUnlocked()).Select(static stage => stage.Id).ToArray()
			: OrderedStages.Select(static stage => stage.Id).ToArray();
	}

	public static ProgressionStageId GetCurrentStageId()
	{
		var current = OrderedStages[0];

		foreach (var stage in OrderedStages) {
			if (stage.IsUnlocked()) {
				current = stage;
			}
		}

		return current.Id;
	}
}

