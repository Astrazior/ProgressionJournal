using System;

namespace ProgressionJournal.Data;

public sealed class ProgressionStage(ProgressionStageId id, string localizationKey, Func<bool> unlockCondition)
{
	public ProgressionStageId Id { get; } = id;

	public string LocalizationKey { get; } = localizationKey;

	public Func<bool> UnlockCondition { get; } = unlockCondition;

	public bool IsUnlocked() => UnlockCondition();
}
