using System;
using System.Collections.Generic;
using System.Linq;
using ProgressionJournal.Common.Data;
using Terraria;
using Terraria.Localization;

namespace ProgressionJournal.Common.Progression;

public sealed class JournalEntry
{
	private readonly Dictionary<ProgressionStageId, StageEvaluation> _evaluations;

	public JournalEntry(
		string key,
		JournalItemCategory category,
		CombatClass classes,
		IEnumerable<int> itemIds,
		IEnumerable<StageEvaluation> evaluations)
	{
		Key = key;
		Category = category;
		Classes = classes;
		ItemIds = itemIds.Distinct().ToArray();

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

	public bool AppliesToClass(CombatClass combatClass) => (Classes & combatClass) != 0;

	public bool TryGetEvaluation(ProgressionStageId stageId, out StageEvaluation evaluation) => _evaluations.TryGetValue(stageId, out evaluation!);

	public StageEvaluation GetEvaluation(ProgressionStageId stageId) => _evaluations[stageId];

	public string GetDisplayName() => string.Join(" / ", ItemIds.Select(Lang.GetItemNameValue));
}
