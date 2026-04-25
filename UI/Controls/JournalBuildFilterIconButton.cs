using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalBuildFilterIconButton : JournalHoverPanel
{
    private readonly string _iconKey;
    private bool _active;
    private string? _hoverText;

    public JournalBuildFilterIconButton(string iconKey, Action onClick)
    {
        _iconKey = iconKey;
        var onClick1 = onClick;
        SetPadding(0f);
        OnLeftClick += (_, _) => onClick1();
    }

    public void SetActive(bool active)
    {
        _active = active;
    }

    public void SetHoverText(string hoverText)
    {
        _hoverText = hoverText;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var background = _active ? new Color(42, 60, 48) : new Color(24, 34, 46);
        var border = _active ? JournalUiTheme.SectionHeaderText : new Color(78, 100, 122);

        BackgroundColor = IsMouseHovering
            ? Color.Lerp(background, Color.White, 0.12f)
            : background;
        BorderColor = IsMouseHovering
            ? Color.Lerp(border, Color.White, 0.2f)
            : border;

        base.DrawSelf(spriteBatch);

        var bounds = GetDimensions().ToRectangle();
        var iconColor = _active ? JournalUiTheme.SectionHeaderText : JournalUiTheme.RootTitleText;
        DrawIcon(spriteBatch, bounds, iconColor);

        if (IsMouseHovering && !string.IsNullOrWhiteSpace(_hoverText))
        {
            Main.hoverItemName = _hoverText;
        }
    }

    private void DrawIcon(SpriteBatch spriteBatch, Rectangle bounds, Color color)
    {
        if (string.Equals(_iconKey, "sort", StringComparison.Ordinal))
        {
            DrawSortIcon(spriteBatch, bounds, color);
            return;
        }

        DrawFilterIcon(spriteBatch, bounds, color);
    }

    private static void DrawFilterIcon(SpriteBatch spriteBatch, Rectangle bounds, Color color)
    {
        var x = bounds.X + bounds.Width / 2 - 9;
        var y = bounds.Y + bounds.Height / 2 - 8;
        Fill(spriteBatch, x, y, 18, 2, color);
        Fill(spriteBatch, x + 2, y + 4, 14, 2, color);
        Fill(spriteBatch, x + 5, y + 8, 8, 2, color);
        Fill(spriteBatch, x + 8, y + 10, 2, 6, color);
    }

    private static void DrawSortIcon(SpriteBatch spriteBatch, Rectangle bounds, Color color)
    {
        var x = bounds.X + bounds.Width / 2 - 9;
        var y = bounds.Y + bounds.Height / 2 - 9;
        Fill(spriteBatch, x + 1, y + 2, 2, 14, color);
        Fill(spriteBatch, x - 1, y + 12, 6, 2, color);
        Fill(spriteBatch, x + 8, y + 2, 8, 2, color);
        Fill(spriteBatch, x + 8, y + 8, 11, 2, color);
        Fill(spriteBatch, x + 8, y + 14, 14, 2, color);
    }

    private static void Fill(SpriteBatch spriteBatch, int x, int y, int width, int height, Color color)
    {
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(x, y, width, height), color);
    }
}
