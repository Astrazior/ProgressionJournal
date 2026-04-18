using System;
using Terraria.GameContent.UI.Elements;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalTextButton : UIPanel
{
    private readonly float _textScale;
    private readonly UIText _label;

    public JournalTextButton(string text, float textScale, Action onClick)
    {
        _textScale = textScale;
        SetPadding(0f);

        _label = CreateLabel(text);
        Append(_label);
        SetStyle(JournalUiTheme.GetDefaultTextButtonStyle());

        OnLeftClick += (_, _) => onClick();
    }

    public void SetText(string text) => _label.SetText(text);

    public void SetStyle(JournalButtonStyle style)
    {
        BackgroundColor = style.Background;
        BorderColor = style.Border;
        _label.TextColor = style.Text;
    }

    private UIText CreateLabel(string text)
    {
        return new UIText(text, _textScale)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        };
    }
}

