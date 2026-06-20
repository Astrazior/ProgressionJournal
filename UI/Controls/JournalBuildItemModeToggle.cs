using Microsoft.Xna.Framework.Graphics;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalBuildItemModeToggle : JournalHoverPanel
{
    private string? _hoverText;
    private bool _allItemsEnabled;

    public JournalBuildItemModeToggle(Action onClick)
    {
        SetPadding(0f);
        OnLeftClick += (_, _) => onClick();
    }

    public void SetAllItemsEnabled(bool enabled)
    {
        _allItemsEnabled = enabled;
    }

    public void SetHoverText(string hoverText)
    {
        _hoverText = hoverText;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        JournalBuildItemModeToggleRenderer.Draw(
            spriteBatch,
            GetDimensions().ToRectangle(),
            _allItemsEnabled,
            IsMouseHovering);

        if (IsMouseHovering && !string.IsNullOrWhiteSpace(_hoverText))
        {
            JournalTooltip.Request(_hoverText);
        }
    }
}
