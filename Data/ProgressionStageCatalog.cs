using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace ProgressionJournal.Data;

public static class ProgressionStageCatalog
{
	private static readonly IReadOnlyList<ProgressionStage> OrderedStages =
	[
		new (ProgressionStageId.PreBoss, "Mods.ProgressionJournal.Stages.PreBoss", static () => true),
		new (ProgressionStageId.PostEyeOfCthulhu, "Mods.ProgressionJournal.Stages.PostEyeOfCthulhu", static () => NPC.downedBoss1),
		new (ProgressionStageId.PostWorldEvil, "Mods.ProgressionJournal.Stages.PostWorldEvil", static () => NPC.downedBoss2),
		new (ProgressionStageId.PostSkeletron, "Mods.ProgressionJournal.Stages.PostSkeletron", static () => NPC.downedBoss3),
		new (ProgressionStageId.HardmodeEntry, "Mods.ProgressionJournal.Stages.HardmodeEntry", static () => Main.hardMode),
		new (ProgressionStageId.PostMechBosses, "Mods.ProgressionJournal.Stages.PostMechBosses", static () => NPC.downedMechBossAny),
		new (ProgressionStageId.PostPlantera, "Mods.ProgressionJournal.Stages.PostPlantera", static () => NPC.downedPlantBoss),
		new (ProgressionStageId.PostGolem, "Mods.ProgressionJournal.Stages.PostGolem", static () => NPC.downedGolemBoss),
		new (ProgressionStageId.PostMoonLord, "Mods.ProgressionJournal.Stages.PostMoonLord", static () => NPC.downedMoonlord)
	];

	private static readonly Dictionary<ProgressionStageId, ProgressionStage> StagesById =
		OrderedStages.ToDictionary(stage => stage.Id);

	public static IReadOnlyList<ProgressionStage> All => OrderedStages;

	public static ProgressionStage Get(ProgressionStageId stageId) => StagesById[stageId];

	public static ProgressionStageId GetCurrentStageId()
	{
		var current = OrderedStages[0];

		foreach (var stage in OrderedStages) {
			if (stage.IsUnlocked()) {
				current = stage;
				continue;
			}

			break;
		}

		return current.Id;
	}
}
