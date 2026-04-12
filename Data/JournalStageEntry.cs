namespace ProgressionJournal.Data;

public sealed class JournalStageEntry(JournalEntry entry, StageEvaluation evaluation)
{
	public JournalEntry Entry { get; } = entry;

	public StageEvaluation Evaluation { get; } = evaluation;
}
