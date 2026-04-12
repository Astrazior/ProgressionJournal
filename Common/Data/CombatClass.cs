using System;

namespace ProgressionJournal.Common.Data;

[Flags]
public enum CombatClass
{
	None = 0,
	Melee = 1,
	Ranged = 1 << 1,
	Magic = 1 << 2,
	Summoner = 1 << 3,
	All = Melee | Ranged | Magic | Summoner
}
