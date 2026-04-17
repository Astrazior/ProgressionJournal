using System;
using Terraria.GameContent.UI.Elements;

namespace ProgressionJournal.UI;

public static class JournalUiElementFactory
{
    public static UIPanel CreatePanel()
    {
        var panel = new UIPanel();
        panel.SetPadding(0f);
        panel.BackgroundColor = JournalUiTheme.PanelBackground;
        panel.BorderColor = JournalUiTheme.PanelBorder;
        return panel;
    }

    public static JournalTextButton CreateTextButton(string text, float width, float height, Action onClick, float textScale = 0.48f)
    {
        var button = new JournalTextButton(text, textScale, onClick);
        button.Width.Set(width, 0f);
        button.Height.Set(height, 0f);
        return button;
    }

    public static JournalStageButton CreateStageButton(Action onClick)
    {
        var button = new JournalStageButton(onClick);
        button.Height.Set(JournalUiMetrics.StageButtonDefaultHeight, 0f);
        return button;
    }

    public static UIText CreateSectionHeader(string text)
    {
        var header = new UIText(text, 0.56f, true);
        header.Height.Set(22f, 0f);
        header.VAlign = 0.5f;
        header.TextColor = JournalUiTheme.SectionHeaderText;
        return header;
    }
}
