using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Elements;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalTextButton : JournalHoverPanel
{
    private readonly float _textScale;
    private readonly UIText _label;
    private JournalButtonStyle _style;

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
        _style = style;
        BackgroundColor = style.Background;
        BorderColor = style.Border;
        _label.TextColor = style.Text;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        BackgroundColor = IsMouseHovering
            ? Color.Lerp(_style.Background, Color.White, 0.14f)
            : _style.Background;
        BorderColor = IsMouseHovering
            ? Color.Lerp(_style.Border, Color.White, 0.28f)
            : _style.Border;
        _label.TextColor = IsMouseHovering
            ? Color.Lerp(_style.Text, Color.White, 0.18f)
            : _style.Text;

        base.DrawSelf(spriteBatch);
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

