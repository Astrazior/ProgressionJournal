namespace ProgressionJournal.Data.Models;

public sealed class StageEvaluation
{
	public StageEvaluation(ProgressionStageId stageId, RecommendationTier tier, string? noteKey = null)
		: this(JournalStageIds.FromLegacy(stageId), tier, noteKey)
	{
	}

	public StageEvaluation(string stageId, RecommendationTier tier, string? noteKey = null)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(stageId);
		StageId = stageId;
		Tier = tier;
		NoteKey = noteKey;
	}

	public string StageId { get; }

	public RecommendationTier Tier { get; }

	public string? NoteKey { get; }
}

