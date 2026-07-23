namespace ProgressionJournal.Data.Models;

public sealed class JournalStageEntry
{
	public JournalStageEntry(
		JournalEntry entry,
		StageEvaluation evaluation,
		JournalWikiRecommendation? wikiRecommendation = null)
		: this(entry, evaluation, wikiRecommendation, null)
	{
	}

	internal JournalStageEntry(
		JournalEntry entry,
		StageEvaluation evaluation,
		JournalWikiRecommendation? wikiRecommendation,
		JournalArmorSetFamily? armorSet)
	{
		Entry = entry;
		Evaluation = evaluation;
		WikiRecommendation = wikiRecommendation;
		ArmorSet = armorSet;
	}

	public JournalEntry Entry { get; }

	public StageEvaluation Evaluation { get; }

	public JournalWikiRecommendation? WikiRecommendation { get; }

	internal JournalArmorSetFamily? ArmorSet { get; }

	public bool IsWikiRecommendation => WikiRecommendation is not null;
}

