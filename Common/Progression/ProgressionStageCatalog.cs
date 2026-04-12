using System.Collections.Generic;
using System.Linq;
using ProgressionJournal.Common.Data;
using Terraria;

namespace ProgressionJournal.Common.Progression;

public static class ProgressionStageCatalog
{
	private static readonly IReadOnlyList<StageDefinition> OrderedStages = new[]
	{
		new StageDefinition(ProgressionStageId.PreBoss, "Mods.ProgressionJournal.Stages.PreBoss", static () => true),
		new StageDefinition(ProgressionStageId.PostEyeOfCthulhu, "Mods.ProgressionJournal.Stages.PostEyeOfCthulhu", static () => NPC.downedBoss1),
		new StageDefinition(ProgressionStageId.PostWorldEvil, "Mods.ProgressionJournal.Stages.PostWorldEvil", static () => NPC.downedBoss2),
		new StageDefinition(ProgressionStageId.PostSkeletron, "Mods.ProgressionJournal.Stages.PostSkeletron", static () => NPC.downedBoss3),
		new StageDefinition(ProgressionStageId.HardmodeEntry, "Mods.ProgressionJournal.Stages.HardmodeEntry", static () => Main.hardMode),
		new StageDefinition(ProgressionStageId.PostMechBosses, "Mods.ProgressionJournal.Stages.PostMechBosses", static () => NPC.downedMechBossAny),
		new StageDefinition(ProgressionStageId.PostPlantera, "Mods.ProgressionJournal.Stages.PostPlantera", static () => NPC.downedPlantBoss),
		new StageDefinition(ProgressionStageId.PostGolem, "Mods.ProgressionJournal.Stages.PostGolem", static () => NPC.downedGolemBoss),
		new StageDefinition(ProgressionStageId.PostMoonLord, "Mods.ProgressionJournal.Stages.PostMoonLord", static () => NPC.downedMoonlord)
	};

	private static readonly Dictionary<ProgressionStageId, StageDefinition> StagesById =
		OrderedStages.ToDictionary(stage => stage.Id);

	public static IReadOnlyList<StageDefinition> All => OrderedStages;

	public static StageDefinition Get(ProgressionStageId stageId) => StagesById[stageId];

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
