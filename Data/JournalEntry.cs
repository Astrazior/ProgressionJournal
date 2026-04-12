using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace ProgressionJournal.Data;

public sealed class JournalEntry
{
	private readonly Dictionary<ProgressionStageId, StageEvaluation> _evaluations;

	public JournalEntry(
		string key,
		JournalItemCategory category,
		CombatClass classes,
		IEnumerable<int> itemIds,
		IEnumerable<StageEvaluation> evaluations,
		OptionalBossRequirementId? optionalBossRequirement = null)
	{
		Key = key;
		Category = category;
		Classes = classes;
		ItemIds = itemIds.Distinct().ToArray();
		OptionalBossRequirement = optionalBossRequirement;

		if (ItemIds.Count == 0) {
			throw new ArgumentException("A journal entry must contain at least one item id.", nameof(itemIds));
		}

		RepresentativeItemId = ItemIds[0];
		_evaluations = evaluations.ToDictionary(evaluation => evaluation.StageId);
	}

	public string Key { get; }

	public JournalItemCategory Category { get; }

	public CombatClass Classes { get; }

	public IReadOnlyList<int> ItemIds { get; }

	public int RepresentativeItemId { get; }

	public OptionalBossRequirementId? OptionalBossRequirement { get; }

	public bool HasOptionalBossRequirement => OptionalBossRequirement.HasValue;

	public bool AppliesToClass(CombatClass combatClass) => (Classes & combatClass) != 0;

	public bool TryGetEvaluation(ProgressionStageId stageId, out StageEvaluation evaluation)
	{
		if (_evaluations.TryGetValue(stageId, out evaluation!)) {
			return true;
		}

		int targetIndex = ProgressionStageCatalog.GetStageOrderIndex(stageId);
		StageEvaluation? nearestPreviousEvaluation = null;
		int nearestPreviousIndex = -1;
		bool hasLaterEvaluation = false;

		foreach (var pair in _evaluations) {
			int evaluationIndex = ProgressionStageCatalog.GetStageOrderIndex(pair.Key);

			if (evaluationIndex < targetIndex && evaluationIndex > nearestPreviousIndex) {
				nearestPreviousEvaluation = pair.Value;
				nearestPreviousIndex = evaluationIndex;
				continue;
			}

			if (evaluationIndex > targetIndex) {
				hasLaterEvaluation = true;
			}
		}

		if (nearestPreviousEvaluation is not null && hasLaterEvaluation) {
			evaluation = nearestPreviousEvaluation;
			return true;
		}

		evaluation = null!;
		return false;
	}

	public StageEvaluation GetEvaluation(ProgressionStageId stageId)
	{
		if (TryGetEvaluation(stageId, out var evaluation)) {
			return evaluation;
		}

		throw new KeyNotFoundException($"No evaluation exists for stage '{stageId}' in entry '{Key}'.");
	}

	public string GetDisplayName() => string.Join(" / ", ItemIds.Select(Lang.GetItemNameValue));
}
