namespace ProgressionJournal.Data.Models;

public sealed class JournalStageEntry(
    JournalEntry entry,
    StageEvaluation evaluation,
    JournalWikiRecommendation? wikiRecommendation = null)
{
	public JournalEntry Entry { get; } = entry;

	public StageEvaluation Evaluation { get; } = evaluation;

	public JournalWikiRecommendation? WikiRecommendation { get; } = wikiRecommendation;

	public bool IsWikiRecommendation => WikiRecommendation is not null;
}

