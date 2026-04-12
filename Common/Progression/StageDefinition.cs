using System;
using ProgressionJournal.Common.Data;

namespace ProgressionJournal.Common.Progression;

public sealed class StageDefinition(ProgressionStageId id, string localizationKey, Func<bool> unlockCondition)
{
	public ProgressionStageId Id { get; } = id;

	public string LocalizationKey { get; } = localizationKey;

	public Func<bool> UnlockCondition { get; } = unlockCondition;

	public bool IsUnlocked() => UnlockCondition();
}
