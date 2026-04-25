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
    private Texture2D? _iconTexture;
    private int _itemIconId;

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

    public void SetIconTexture(Texture2D? iconTexture)
    {
        _iconTexture = iconTexture;
    }

    public void SetItemIcon(int itemIconId)
    {
        _itemIconId = itemIconId;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var background = _active ? new Color(38, 54, 48) : new Color(22, 30, 38);
        BackgroundColor = IsMouseHovering
            ? Color.Lerp(background, Color.White, _active ? 0.16f : 0.1f)
            : background;
        BorderColor = Color.Transparent;

        base.DrawSelf(spriteBatch);

        var bounds = GetDimensions().ToRectangle();
        if (_active)
        {
            DrawSoftAccent(spriteBatch, bounds);
        }

        var iconColor = _active ? JournalUiTheme.SectionHeaderText : JournalUiTheme.RootTitleText * 0.86f;
        if (!DrawDynamicIcon(spriteBatch, bounds, iconColor))
        {
            DrawIcon(spriteBatch, bounds, iconColor);
        }

        if (IsMouseHovering && !string.IsNullOrWhiteSpace(_hoverText))
        {
            Main.hoverItemName = _hoverText;
        }
    }

    private bool DrawDynamicIcon(SpriteBatch spriteBatch, Rectangle bounds, Color color)
    {
        if (_iconTexture is not null)
        {
            DrawTextureCentered(spriteBatch, _iconTexture, bounds, color, 0.7f);
            return true;
        }

        if (_itemIconId <= 0 || _itemIconId >= TextureAssets.Item.Length) return false;
        Main.instance.LoadItem(_itemIconId);
        DrawTextureCentered(spriteBatch, TextureAssets.Item[_itemIconId].Value, bounds, Color.White, 0.7f);
        return true;

    }

    private static void DrawTextureCentered(SpriteBatch spriteBatch, Texture2D texture, Rectangle bounds, Color color, float maxBoundsScale)
    {
        var maxWidth = bounds.Width * maxBoundsScale;
        var maxHeight = bounds.Height * maxBoundsScale;
        var scale = MathF.Min(maxWidth / texture.Width, maxHeight / texture.Height);
        var position = new Vector2(bounds.X + bounds.Width * 0.5f, bounds.Y + bounds.Height * 0.5f);
        var origin = new Vector2(texture.Width * 0.5f, texture.Height * 0.5f);
        spriteBatch.Draw(texture, position, null, color, 0f, origin, scale, SpriteEffects.None, 0f);
    }

    private static void DrawSoftAccent(SpriteBatch spriteBatch, Rectangle bounds)
    {
        var accent = JournalUiTheme.SectionHeaderText * 0.72f;
        Fill(spriteBatch, bounds.X + 5, bounds.Bottom - 4, bounds.Width - 10, 2, accent);
        Fill(spriteBatch, bounds.X + 7, bounds.Y + 4, bounds.Width - 14, 1, accent * 0.22f);
    }

    private void DrawIcon(SpriteBatch spriteBatch, Rectangle bounds, Color color)
    {
        switch (_iconKey)
        {
            case "guide":
                DrawGuideIcon(spriteBatch, bounds, color);
                return;
            case "mods":
                DrawModsIcon(spriteBatch, bounds, color);
                return;
            case "sort_desc":
                DrawSortIcon(spriteBatch, bounds, color, descending: true);
                return;
            case "sort_asc":
                DrawSortIcon(spriteBatch, bounds, color, descending: false);
                return;
            case "reset":
                DrawResetIcon(spriteBatch, bounds, color);
                return;
            case "sort":
                DrawSortIcon(spriteBatch, bounds, color, descending: true);
                return;
            default:
                DrawFilterIcon(spriteBatch, bounds, color);
                return;
        }
    }

    private static void DrawGuideIcon(SpriteBatch spriteBatch, Rectangle bounds, Color color)
    {
        var x = bounds.X + bounds.Width / 2 - 10;
        var y = bounds.Y + bounds.Height / 2 - 8;
        Fill(spriteBatch, x, y + 1, 8, 14, color * 0.86f);
        Fill(spriteBatch, x + 12, y + 1, 8, 14, color * 0.86f);
        Fill(spriteBatch, x + 9, y, 2, 16, color);
        Fill(spriteBatch, x + 2, y + 4, 5, 1, color * 0.55f);
        Fill(spriteBatch, x + 2, y + 8, 5, 1, color * 0.55f);
        Fill(spriteBatch, x + 13, y + 4, 5, 1, color * 0.55f);
        Fill(spriteBatch, x + 13, y + 8, 5, 1, color * 0.55f);
    }

    private static void DrawModsIcon(SpriteBatch spriteBatch, Rectangle bounds, Color color)
    {
        var x = bounds.X + bounds.Width / 2 - 9;
        var y = bounds.Y + bounds.Height / 2 - 9;
        Fill(spriteBatch, x + 1, y + 1, 8, 8, color * 0.85f);
        Fill(spriteBatch, x + 10, y + 1, 7, 8, color * 0.6f);
        Fill(spriteBatch, x + 1, y + 10, 7, 7, color * 0.6f);
        Fill(spriteBatch, x + 9, y + 9, 8, 8, color * 0.9f);
        Fill(spriteBatch, x + 7, y + 4, 4, 2, color);
        Fill(spriteBatch, x + 12, y + 7, 2, 4, color);
        Fill(spriteBatch, x + 6, y + 12, 4, 2, color);
    }

    private static void DrawFilterIcon(SpriteBatch spriteBatch, Rectangle bounds, Color color)
    {
        var x = bounds.X + bounds.Width / 2 - 9;
        var y = bounds.Y + bounds.Height / 2 - 8;
        Fill(spriteBatch, x, y, 18, 2, color);
        Fill(spriteBatch, x + 2, y + 4, 14, 2, color * 0.9f);
        Fill(spriteBatch, x + 5, y + 8, 8, 2, color * 0.9f);
        Fill(spriteBatch, x + 8, y + 10, 2, 6, color);
    }

    private static void DrawSortIcon(SpriteBatch spriteBatch, Rectangle bounds, Color color, bool descending)
    {
        var x = bounds.X + bounds.Width / 2 - 10;
        var y = bounds.Y + bounds.Height / 2 - 9;
        var arrowTop = descending ? y + 2 : y + 12;
        var arrowBottom = descending ? y + 14 : y + 2;
        Fill(spriteBatch, x + 1, y + 3, 2, 12, color);
        Fill(spriteBatch, x - 1, arrowBottom, 6, 2, color);
        Fill(spriteBatch, x, arrowBottom + (descending ? -2 : 2), 4, 2, color);
        Fill(spriteBatch, x + 8, arrowTop, 11, 2, color * 0.72f);
        Fill(spriteBatch, x + 8, y + 8, 8, 2, color * 0.86f);
        Fill(spriteBatch, x + 8, arrowBottom, 5, 2, color);
    }

    private static void DrawResetIcon(SpriteBatch spriteBatch, Rectangle bounds, Color color)
    {
        var x = bounds.X + bounds.Width / 2 - 8;
        var y = bounds.Y + bounds.Height / 2 - 8;
        Fill(spriteBatch, x + 4, y, 9, 2, color);
        Fill(spriteBatch, x + 2, y + 2, 2, 3, color);
        Fill(spriteBatch, x, y + 4, 6, 2, color);
        Fill(spriteBatch, x + 13, y + 4, 2, 8, color);
        Fill(spriteBatch, x + 4, y + 13, 9, 2, color);
        Fill(spriteBatch, x + 1, y + 8, 2, 5, color);
        Fill(spriteBatch, x + 10, y + 5, 6, 2, color);
        Fill(spriteBatch, x + 12, y + 3, 2, 6, color);
    }

    private static void Fill(SpriteBatch spriteBatch, int x, int y, int width, int height, Color color)
    {
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(x, y, width, height), color);
    }
}
