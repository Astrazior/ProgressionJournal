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
        BackgroundColor = JournalUiTheme.PresetPanelBackground;
        BorderColor = JournalUiTheme.PresetPanelBorder;

        var title = new UIText(JournalTextUtilities.TrimToCharacterCount(preset.GetDisplayName(), 64), 0.52f, true);
        title.Left.Set(14f, 0f);
        title.Top.Set(10f, 0f);
        title.Width.Set(-28f, 1f);
        Append(title);

        Append(CreateLine(Language.GetTextValue("Mods.ProgressionJournal.UI.Weapons"), preset.GetWeaponsText(), 32f));
        Append(CreateLine(Language.GetTextValue("Mods.ProgressionJournal.UI.ArmorLabel"), preset.GetArmorText(), 50f));
        Append(CreateLine(Language.GetTextValue("Mods.ProgressionJournal.UI.Accessories"), preset.GetAccessoriesText(), 68f));
    }

    private static UIText CreateLine(string label, string value, float top)
    {
        var text = new UIText($"{label}: {JournalTextUtilities.TrimToCharacterCount(value, 88)}", 0.38f);
        text.Left.Set(14f, 0f);
        text.Top.Set(top, 0f);
        text.Width.Set(-28f, 1f);
        text.TextColor = JournalUiTheme.PresetPanelText;
        return text;
    }
}
