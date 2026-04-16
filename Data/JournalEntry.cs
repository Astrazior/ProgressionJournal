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
		IEnumerable<StageEvaluation> evaluations)
		: this(
			key,
			category,
			classes,
			itemIds.Select(itemId => new JournalItemGroup([itemId])),
			evaluations)
	{
	}

	public JournalEntry(
		string key,
		JournalItemCategory category,
		CombatClass classes,
		IEnumerable<JournalItemGroup> itemGroups,
		IEnumerable<StageEvaluation> evaluations)
	{
		Key = key;
		Category = category;
		Classes = classes;
		ItemGroups = itemGroups.ToArray();

		if (ItemGroups.Count == 0) {
			throw new ArgumentException("A journal entry must contain at least one item group.", nameof(itemGroups));
		}

		ItemIds = ItemGroups.SelectMany(group => group.ItemIds).ToArray();
		RepresentativeItemId = ItemGroups[0].RepresentativeItemId;
		_evaluations = evaluations.ToDictionary(evaluation => evaluation.StageId);
	}

	public string Key { get; }

	public JournalItemCategory Category { get; }

	public CombatClass Classes { get; }

	public IReadOnlyList<JournalItemGroup> ItemGroups { get; }

	public IReadOnlyList<int> ItemIds { get; }

	public int RepresentativeItemId { get; }

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

	public string GetDisplayName() => string.Join(" + ", ItemGroups.Select(group => group.GetDisplayName()));
}
