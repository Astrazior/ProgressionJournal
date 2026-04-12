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
		Height.Set(96f, 0f);
		SetPadding(0f);
		BackgroundColor = new Color(24, 43, 58);
		BorderColor = new Color(104, 138, 168);

		var title = new UIText(TrimForUi(preset.GetDisplayName(), 64), 0.52f, true);
		title.Left.Set(14f, 0f);
		title.Top.Set(10f, 0f);
		title.Width.Set(-28f, 1f);
		Append(title);

		var weapons = CreateLine(
			Language.GetTextValue("Mods.ProgressionJournal.UI.Weapons"),
			preset.GetWeaponsText(),
			32f);
		Append(weapons);

		var armor = CreateLine(
			Language.GetTextValue("Mods.ProgressionJournal.UI.ArmorLabel"),
			preset.GetArmorText(),
			50f);
		Append(armor);

		var accessories = CreateLine(
			Language.GetTextValue("Mods.ProgressionJournal.UI.Accessories"),
			preset.GetAccessoriesText(),
			68f);
		Append(accessories);
	}

	private static UIText CreateLine(string label, string value, float top)
	{
		var text = new UIText($"{label}: {TrimForUi(value, 88)}", 0.38f);
		text.Left.Set(14f, 0f);
		text.Top.Set(top, 0f);
		text.Width.Set(-28f, 1f);
		text.TextColor = new Color(220, 228, 236);
		return text;
	}

	private static string TrimForUi(string text, int maxLength)
	{
		if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength) {
			return text;
		}

		return text[..(maxLength - 3)].TrimEnd() + "...";
	}
}
