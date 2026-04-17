using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Localization;

namespace ProgressionJournal.Data;

public abstract class JournalPreset(
	string key,
	CombatClass combatClass,
	ProgressionStageId stageId,
	IEnumerable<int> weaponIds,
	IEnumerable<int> armorIds,
	IEnumerable<int> accessoryIds,
	string? notesKey = null)
{
	public string Key { get; } = key;

	public CombatClass CombatClass { get; } = combatClass;

	public ProgressionStageId StageId { get; } = stageId;

	public IReadOnlyList<int> WeaponIds { get; } = weaponIds.Distinct().ToArray();

	public IReadOnlyList<int> ArmorIds { get; } = armorIds.Distinct().ToArray();

	public IReadOnlyList<int> AccessoryIds { get; } = accessoryIds.Distinct().ToArray();

	public string? NotesKey { get; } = notesKey;

	public string GetDisplayName() => Language.GetTextValue($"Mods.ProgressionJournal.Presets.{Key}.Name");

	public string GetWeaponsText() => FormatItems(WeaponIds);

	public string GetArmorText() => FormatItems(ArmorIds);

	public string GetAccessoriesText() => FormatItems(AccessoryIds);

	private static string FormatItems(IReadOnlyList<int> itemIds)
	{
		return itemIds.Count == 0
			? Language.GetTextValue("Mods.ProgressionJournal.UI.None")
			: string.Join(", ", itemIds.Select(Lang.GetItemNameValue));
	}
}
