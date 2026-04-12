using System;
using System.Collections.Generic;
using System.Linq;
using ProgressionJournal.Common.Data;
using ProgressionJournal.Content.Sources;

namespace ProgressionJournal.Common.Progression;

public static class JournalDatabase
{
	private static readonly Lazy<IReadOnlyList<JournalEntry>> Entries = new(BuildEntries);

	public static IReadOnlyList<JournalEntry> AllEntries => Entries.Value;

	public static IReadOnlyList<JournalStageEntry> GetEntries(ProgressionStageId stageId, CombatClass combatClass)
	{
		return AllEntries
			.Where(entry => entry.AppliesToClass(combatClass) && entry.TryGetEvaluation(stageId, out _))
			.Select(entry => new JournalStageEntry(entry, entry.GetEvaluation(stageId)))
			.OrderBy(entry => GetTierOrder(entry.Evaluation.Tier))
			.ThenBy(entry => entry.Entry.Category)
			.ThenBy(entry => entry.Entry.GetDisplayName(), StringComparer.CurrentCultureIgnoreCase)
			.ToArray();
	}

	private static IReadOnlyList<JournalEntry> BuildEntries()
	{
		IJournalContentSource[] sources =
		{
			new VanillaJournalContentSource()
		};

		return sources
			.SelectMany(source => source.GetEntries())
			.ToArray();
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
