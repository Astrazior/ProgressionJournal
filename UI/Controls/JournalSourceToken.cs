using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ObjectData;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public enum JournalSourceTokenKind
{
    Item,
    Npc,
    Bestiary,
    Texture,
    Tile
}

public readonly record struct JournalSourceTokenData(
    JournalSourceTokenKind Kind,
    int Value,
    string HoverText,
    string? TexturePath = null);

public sealed class JournalSourceToken : UIElement
{
    private const string AchievementTexturePrefix = "achievement:";
    private const string AchievementsTexturePath = "Images/UI/Achievements";
    private const int AchievementIconSize = 64;
    private const string BestiaryFilterIconTexturePath = "Images/UI/Bestiary/Icon_Tags_Shadow";
    private const int BestiaryFilterIconColumns = 16;
    private const int BestiaryFilterIconRows = 5;
    private readonly JournalSourceTokenData _data;

    public JournalSourceToken(JournalSourceTokenData data)
    {
        _data = data;
        var tokenSize = GetTokenSize(data);
        Width.Set(tokenSize, 0f);
        Height.Set(tokenSize, 0f);
    }

    public static float TokenSize => 44f;

    public static float NpcTokenSize => 56f;

    public static float TileTokenSize => 70f;

    public static float GetTokenSize(JournalSourceTokenData data)
    {
        return data.Kind switch
        {
            JournalSourceTokenKind.Npc => NpcTokenSize,
            JournalSourceTokenKind.Tile => TileTokenSize,
            _ => TokenSize
        };
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        var bounds = GetDimensions().ToRectangle();
        DrawTokenFrame(spriteBatch, bounds);

        var inner = bounds;
        var innerPadding = _data.Kind switch
        {
            JournalSourceTokenKind.Npc => 8,
            JournalSourceTokenKind.Tile => 7,
            _ => 7
        };
        inner.Inflate(-innerPadding, -innerPadding);
        if (inner.Width <= 0 || inner.Height <= 0)
        {
            return;
        }

        switch (_data.Kind)
        {
            case JournalSourceTokenKind.Item:
                DrawItem(spriteBatch, inner);
                break;
            case JournalSourceTokenKind.Npc:
                DrawNpc(spriteBatch, inner);
                break;
            case JournalSourceTokenKind.Bestiary:
                DrawBestiaryIcon(spriteBatch, inner);
                break;
            case JournalSourceTokenKind.Texture:
                DrawTextureIcon(spriteBatch, inner);
                break;
            case JournalSourceTokenKind.Tile:
                DrawTile(spriteBatch, inner);
                break;
            default:
                throw new InvalidOperationException($"Unsupported {nameof(JournalSourceTokenKind)} value: {_data.Kind}");
        }

        ShowHoverText();
    }

    private void ShowHoverText()
    {
        if (!IsMouseHovering || string.IsNullOrWhiteSpace(_data.HoverText))
        {
            return;
        }

        if (_data.Kind == JournalSourceTokenKind.Item)
        {
            if (!JournalItemUtilities.TryCreateItem(_data.Value, out var hoverItem))
            {
                Main.HoverItem = new Item();
                Main.hoverItemName = _data.HoverText;
                Main.mouseText = true;
                return;
            }

            Main.HoverItem = hoverItem;
            Main.hoverItemName = hoverItem.HoverName;
            return;
        }

        Main.hoverItemName = _data.HoverText;
    }

    private void DrawItem(SpriteBatch spriteBatch, Rectangle inner)
    {
        if (!JournalItemUtilities.TryCreateItem(_data.Value, out var item))
        {
            return;
        }

        Main.instance.LoadItem(item.type);

        var itemTexture = TextureAssets.Item[item.type].Value;
        var sourceRectangle = Main.itemAnimations[item.type]?.GetFrame(itemTexture) ?? itemTexture.Bounds;
        DrawTexture(spriteBatch, itemTexture, sourceRectangle, inner, anchorBottom: false);
    }

    private void DrawNpc(SpriteBatch spriteBatch, Rectangle inner)
    {
        if (_data.Value <= NPCID.None || _data.Value >= TextureAssets.Npc.Length)
        {
            return;
        }

        Main.instance.LoadNPC(_data.Value);
        var npcTexture = TextureAssets.Npc[_data.Value].Value;
        var frameCount = Math.Max(1, Main.npcFrameCount[_data.Value]);
        var frameHeight = npcTexture.Height / frameCount;
        var frameIndex = frameCount == 1 ? 0 : (int)(Main.GameUpdateCount / 10 % frameCount);
        var sourceRectangle = new Rectangle(0, frameIndex * frameHeight, npcTexture.Width, frameHeight);
        DrawTexture(spriteBatch, npcTexture, sourceRectangle, inner, anchorBottom: true);
    }

    private void DrawBestiaryIcon(SpriteBatch spriteBatch, Rectangle inner)
    {
        var iconTexture = Main.Assets.Request<Texture2D>(BestiaryFilterIconTexturePath).Value;
        var sourceRectangle = GetBestiaryFilterSourceRectangle(iconTexture, _data.Value);
        DrawTexture(spriteBatch, iconTexture, sourceRectangle, inner, anchorBottom: false);
    }

    private void DrawTextureIcon(SpriteBatch spriteBatch, Rectangle inner)
    {
        if (string.IsNullOrWhiteSpace(_data.TexturePath))
        {
            return;
        }

        if (_data.TexturePath.StartsWith(AchievementTexturePrefix, StringComparison.Ordinal))
        {
            DrawAchievementIcon(spriteBatch, inner, _data.TexturePath[AchievementTexturePrefix.Length..]);
            return;
        }

        var iconTexture = Main.Assets.Request<Texture2D>(_data.TexturePath).Value;
        DrawTexture(spriteBatch, iconTexture, iconTexture.Bounds, inner, anchorBottom: false);
    }

    private void DrawTile(SpriteBatch spriteBatch, Rectangle inner)
    {
        if (_data.Value < 0 || _data.Value >= TextureAssets.Tile.Length)
        {
            return;
        }

        Main.instance.LoadTiles(_data.Value);
        var tileTexture = TextureAssets.Tile[_data.Value].Value;
        var tileData = TileObjectData.GetTileData(_data.Value, 0);
        if (tileData is null || tileData.Width <= 0 || tileData.Height <= 0)
        {
            DrawTexture(
                spriteBatch,
                tileTexture,
                new Rectangle(0, 0, Math.Min(tileTexture.Width, 16), Math.Min(tileTexture.Height, 16)),
                inner,
                anchorBottom: true);
            return;
        }

        DrawTileObject(spriteBatch, tileTexture, tileData, inner);
    }

    private static void DrawTileObject(
        SpriteBatch spriteBatch,
        Texture2D texture,
        TileObjectData tileData,
        Rectangle inner)
    {
        var coordinateWidth = Math.Max(1, tileData.CoordinateWidth);
        var coordinatePadding = Math.Max(0, tileData.CoordinatePadding);
        var rowHeights = tileData.CoordinateHeights;
        if (rowHeights is null || rowHeights.Length < tileData.Height)
        {
            return;
        }

        var contentWidth = coordinateWidth * tileData.Width;
        var contentHeight = rowHeights.Take(tileData.Height).Sum();
        if (contentWidth <= 0 || contentHeight <= 0)
        {
            return;
        }

        var availableScale = MathF.Min(inner.Width / (float)contentWidth, inner.Height / (float)contentHeight);
        var scale = availableScale >= 1f
            ? MathF.Max(1f, MathF.Floor(availableScale))
            : availableScale;
        var drawWidth = contentWidth * scale;
        var drawHeight = contentHeight * scale;
        var drawLeft = inner.Center.X - drawWidth * 0.5f;
        var drawTop = inner.Bottom - drawHeight;
        var contentY = 0;
        var sourceY = 0;

        for (var row = 0; row < tileData.Height; row++)
        {
            var rowHeight = rowHeights[row];
            for (var column = 0; column < tileData.Width; column++)
            {
                var sourceX = column * (coordinateWidth + coordinatePadding);
                var source = new Rectangle(sourceX, sourceY, coordinateWidth, rowHeight);
                if (source.Right > texture.Width || source.Bottom > texture.Height)
                {
                    continue;
                }

                var left = (int)MathF.Floor(drawLeft + column * coordinateWidth * scale);
                var right = (int)MathF.Ceiling(drawLeft + (column + 1) * coordinateWidth * scale);
                var top = (int)MathF.Floor(drawTop + contentY * scale);
                var bottom = (int)MathF.Ceiling(drawTop + (contentY + rowHeight) * scale);
                if (scale < 1f)
                {
                    right += column + 1 < tileData.Width ? 1 : 0;
                    bottom += row + 1 < tileData.Height ? 1 : 0;
                }
                spriteBatch.Draw(
                    texture,
                    new Rectangle(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top)),
                    source,
                    Color.White);
            }

            contentY += rowHeight;
            sourceY += rowHeight + coordinatePadding;
        }
    }

    private static void DrawAchievementIcon(SpriteBatch spriteBatch, Rectangle inner, string achievementId)
    {
        if (string.IsNullOrWhiteSpace(achievementId))
        {
            return;
        }

        var iconTexture = Main.Assets.Request<Texture2D>(AchievementsTexturePath).Value;
        var sourceRectangle = GetAchievementSourceRectangle(iconTexture, achievementId);
        if (sourceRectangle.Width <= 0 || sourceRectangle.Height <= 0)
        {
            return;
        }

        DrawTexture(spriteBatch, iconTexture, sourceRectangle, inner, anchorBottom: false);
    }

    private static Rectangle GetAchievementSourceRectangle(Texture2D texture, string achievementId)
    {
        var iconIndex = Main.Achievements.GetIconIndex(achievementId);
        if (iconIndex < 0 || texture.Width < AchievementIconSize || texture.Height < AchievementIconSize)
        {
            return Rectangle.Empty;
        }

        var columns = texture.Width / AchievementIconSize;
        if (columns <= 0)
        {
            return Rectangle.Empty;
        }

        return new Rectangle(
            (iconIndex % columns) * AchievementIconSize,
            (iconIndex / columns) * AchievementIconSize,
            AchievementIconSize,
            AchievementIconSize);
    }

    private static void DrawTexture(SpriteBatch spriteBatch, Texture2D texture, Rectangle sourceRectangle, Rectangle inner, bool anchorBottom)
    {
        if (sourceRectangle.Width <= 0 || sourceRectangle.Height <= 0)
        {
            return;
        }
        var scale = MathF.Min(inner.Width / (float)sourceRectangle.Width, inner.Height / (float)sourceRectangle.Height);
        var position = anchorBottom
            ? new Vector2(inner.Center.X, inner.Bottom)
            : inner.Center.ToVector2();
        var origin = anchorBottom
            ? new Vector2(sourceRectangle.Width * 0.5f, sourceRectangle.Height)
            : sourceRectangle.Size() * 0.5f;

        spriteBatch.Draw(
            texture,
            position,
            sourceRectangle,
            Color.White,
            0f,
            origin,
            scale,
            SpriteEffects.None,
            0f);
    }

    private static Rectangle GetBestiaryFilterSourceRectangle(Texture2D texture, int frame)
    {
        var frameWidth = texture.Width / BestiaryFilterIconColumns;
        var frameHeight = texture.Height / BestiaryFilterIconRows;
        var frameX = frame % BestiaryFilterIconColumns;
        var frameY = frame / BestiaryFilterIconColumns;

        return new Rectangle(frameX * frameWidth, frameY * frameHeight, frameWidth, frameHeight);
    }

    private void DrawTokenFrame(SpriteBatch spriteBatch, Rectangle bounds)
    {
        var (background, border) = GetFrameColors(_data.Kind);
        if (IsMouseHovering)
        {
            background = Color.Lerp(background, Color.White, 0.10f);
            border = Color.Lerp(border, Color.White, 0.24f);
        }

        var cornerCut = _data.Kind switch
        {
            JournalSourceTokenKind.Npc => 7,
            JournalSourceTokenKind.Bestiary => 11,
            JournalSourceTokenKind.Texture => 8,
            JournalSourceTokenKind.Tile => 5,
            _ => 6
        };

        var shadow = bounds;
        shadow.Offset(2, 3);
        DrawChamferedRectangle(spriteBatch, shadow, cornerCut, Color.Black * 0.36f);
        DrawChamferedRectangle(spriteBatch, bounds, cornerCut, border);
        DrawBevel(spriteBatch, bounds, cornerCut, Color.Lerp(border, Color.White, 0.28f), Color.Lerp(border, Color.Black, 0.52f));

        var inner = bounds;
        inner.Inflate(-4, -4);
        DrawChamferedRectangle(spriteBatch, inner, Math.Max(2, cornerCut - 3), background);

        var highlight = inner;
        highlight.Inflate(-2, -2);
        highlight.Height = Math.Max(1, highlight.Height / 3);
        DrawChamferedRectangle(spriteBatch, highlight, Math.Max(1, cornerCut - 5), Color.White * 0.035f);

    }

    private static (Color Background, Color Border) GetFrameColors(JournalSourceTokenKind kind)
    {
        return kind switch
        {
            JournalSourceTokenKind.Item => (new Color(18, 31, 43), new Color(91, 145, 184)),
            JournalSourceTokenKind.Npc => (new Color(43, 25, 28), new Color(190, 100, 91)),
            JournalSourceTokenKind.Bestiary => (new Color(17, 39, 39), new Color(82, 166, 153)),
            JournalSourceTokenKind.Texture => (new Color(42, 34, 20), new Color(210, 165, 73)),
            JournalSourceTokenKind.Tile => (new Color(43, 36, 24), new Color(190, 149, 76)),
            _ => (JournalUiTheme.PanelBackground, JournalUiTheme.PanelBorder)
        };
    }

    private static void DrawBevel(
        SpriteBatch spriteBatch,
        Rectangle rectangle,
        int cornerCut,
        Color topLeft,
        Color bottomRight)
    {
        var pixel = TextureAssets.MagicPixel.Value;
        var horizontalWidth = rectangle.Width - cornerCut * 2;
        var verticalHeight = rectangle.Height - cornerCut * 2;
        if (horizontalWidth <= 0 || verticalHeight <= 0)
        {
            return;
        }

        spriteBatch.Draw(pixel, new Rectangle(rectangle.X + cornerCut, rectangle.Y, horizontalWidth, 2), topLeft * 0.72f);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Y + cornerCut, 2, verticalHeight), topLeft * 0.52f);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X + cornerCut, rectangle.Bottom - 2, horizontalWidth, 2), bottomRight * 0.80f);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.Right - 2, rectangle.Y + cornerCut, 2, verticalHeight), bottomRight * 0.72f);
    }

    private static void DrawChamferedRectangle(
        SpriteBatch spriteBatch,
        Rectangle rectangle,
        int cornerCut,
        Color color)
    {
        if (rectangle.Width <= 0 || rectangle.Height <= 0)
        {
            return;
        }

        var pixel = TextureAssets.MagicPixel.Value;
        cornerCut = Math.Min(cornerCut, Math.Min(rectangle.Width, rectangle.Height) / 2);
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X, rectangle.Y + cornerCut, rectangle.Width, rectangle.Height - cornerCut * 2),
            color);

        for (var row = 0; row < cornerCut; row++)
        {
            var inset = cornerCut - row;
            var width = rectangle.Width - inset * 2;
            if (width <= 0)
            {
                continue;
            }

            spriteBatch.Draw(pixel, new Rectangle(rectangle.X + inset, rectangle.Y + row, width, 1), color);
            spriteBatch.Draw(pixel, new Rectangle(rectangle.X + inset, rectangle.Bottom - row - 1, width, 1), color);
        }
    }

}
