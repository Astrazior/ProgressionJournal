using Microsoft.Xna.Framework;
using ProgressionJournal.Data;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;

namespace ProgressionJournal.UI;

public sealed class JournalPresetPanel : UIPanel
{
	public JournalPresetPanel(JournalPreset preset)
	{
		Width.Set(0f, 1f);
		Height.Set(116f, 0f);
		SetPadding(10f);
		BackgroundColor = new Color(28, 44, 58);
		BorderColor = new Color(102, 136, 166);

		var title = new UIText(preset.GetDisplayName(), 0.92f, true);
		Append(title);

		var weapons = CreateLine(
			Language.GetTextValue("Mods.ProgressionJournal.UI.Weapons"),
			preset.GetWeaponsText(),
			28f);
		Append(weapons);

		var armor = CreateLine(
			Language.GetTextValue("Mods.ProgressionJournal.UI.ArmorLabel"),
			preset.GetArmorText(),
			52f);
		Append(armor);

		var accessories = CreateLine(
			Language.GetTextValue("Mods.ProgressionJournal.UI.Accessories"),
			preset.GetAccessoriesText(),
			76f);
		Append(accessories);
	}

	private static UIText CreateLine(string label, string value, float top)
	{
		var text = new UIText($"{label}: {value}", 0.72f);
		text.Top.Set(top, 0f);
		text.Width.Set(0f, 1f);
		text.TextColor = new Color(216, 226, 236);
		return text;
	}
}
