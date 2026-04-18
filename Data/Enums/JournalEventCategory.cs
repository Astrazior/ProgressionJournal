using Terraria.Localization;

namespace ProgressionJournal.Data.Enums;

public enum JournalEventCategory
{
	BloodMoon,
	GoblinArmy,
	PirateInvasion,
	OldOnesArmy,
	SolarEclipse,
	PumpkinMoon,
	FrostMoon,
	MartianMadness
}

public static class JournalEventCategoryExtensions
{
	public static int GetBestiaryFilterFrame(this JournalEventCategory category) => category switch
	{
		JournalEventCategory.BloodMoon => 38,
		JournalEventCategory.SolarEclipse => 39,
		JournalEventCategory.GoblinArmy => 49,
		JournalEventCategory.PirateInvasion => 50,
		JournalEventCategory.PumpkinMoon => 51,
		JournalEventCategory.FrostMoon => 52,
		JournalEventCategory.MartianMadness => 53,
		JournalEventCategory.OldOnesArmy => 55,
		_ => 0
	};

	public static string GetDisplayName(this JournalEventCategory category) => Language.GetTextValue(category switch
	{
		JournalEventCategory.BloodMoon => "Bestiary_Events.BloodMoon",
		JournalEventCategory.GoblinArmy => "Bestiary_Invasions.Goblins",
		JournalEventCategory.PirateInvasion => "Bestiary_Invasions.Pirates",
		JournalEventCategory.OldOnesArmy => "Bestiary_Invasions.OldOnesArmy",
		JournalEventCategory.SolarEclipse => "Bestiary_Events.Eclipse",
		JournalEventCategory.PumpkinMoon => "Bestiary_Invasions.PumpkinMoon",
		JournalEventCategory.FrostMoon => "Bestiary_Invasions.FrostMoon",
		JournalEventCategory.MartianMadness => "Bestiary_Invasions.Martian",
		_ => string.Empty
	});
}
