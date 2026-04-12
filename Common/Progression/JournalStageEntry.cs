namespace ProgressionJournal.Common.Progression;

public sealed class JournalStageEntry
{
	public JournalStageEntry(JournalEntry entry, StageEvaluation evaluation)
	{
		Entry = entry;
		Evaluation = evaluation;
	}

	public JournalEntry Entry { get; }

	public StageEvaluation Evaluation { get; }
}
