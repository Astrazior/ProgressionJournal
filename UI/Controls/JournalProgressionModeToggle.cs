using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalProgressionModeToggle : JournalHoverPanel
{
    private JournalButtonStyle _style;
    private string? _hoverText;
    private bool _enabled;

    public JournalProgressionModeToggle(Action onClick)
    {
        var onClick1 = onClick;
        SetPadding(0f);
        SetStyle(JournalUiTheme.GetDefaultTextButtonStyle());
        OnLeftClick += (_, _) => onClick1();
    }

    public void SetEnabled(bool enabled) => _enabled = enabled;

    public void SetHoverText(string hoverText) => _hoverText = hoverText;

    public void SetStyle(JournalButtonStyle style)
    {
        _style = style;
        BackgroundColor = style.Background;
        BorderColor = style.Border;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        JournalProgressionToggleRenderer.Draw(
            spriteBatch,
            GetDimensions().ToRectangle(),
            _style,
            _enabled,
            IsMouseHovering);

        if (IsMouseHovering && !string.IsNullOrWhiteSpace(_hoverText))
        {
            Main.hoverItemName = _hoverText;
        }
    }
}
