namespace ProgressionJournal.Data;

public sealed class StageEvaluation(ProgressionStageId stageId, RecommendationTier tier, string? noteKey = null)
{
	public ProgressionStageId StageId { get; } = stageId;

	public RecommendationTier Tier { get; } = tier;

	public string? NoteKey { get; } = noteKey;
}
