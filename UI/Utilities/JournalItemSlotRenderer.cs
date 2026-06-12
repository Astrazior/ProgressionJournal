using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace ProgressionJournal.UI.Utilities;

public enum JournalSlotMarkerPlacement
{
    TopRight,
    TopLeft,
    BottomRight,
    BottomLeft,
    LeftRail,
    BottomRail
}

public readonly record struct JournalSlotMarker(
    string Text,
    Color Color,
    JournalSlotMarkerPlacement Placement = JournalSlotMarkerPlacement.TopRight,
    bool DrawText = true);

public static class JournalItemSlotRenderer
{
    private const float ItemSizeLimit = 32f;

    public static void Draw(
        SpriteBatch spriteBatch,
        Item item,
        Rectangle rectangle,
        Color accent,
        bool hovered = false,
        bool disabled = false,
        float scale = 1f,
        bool emphasizeOuterAccent = false,
        JournalSlotMarker? marker = null)
    {
        DrawBackground(spriteBatch, rectangle, accent, hovered, disabled, emphasizeOuterAccent);

        if (!item.IsAir)
        {
            var itemColor = disabled ? Color.White * 0.42f : Color.White;

            ItemSlot.DrawItemIcon(
                item,
                ItemSlot.Context.TrashItem,
                spriteBatch,
                rectangle.Center.ToVector2(),
                scale,
                ItemSizeLimit,
                itemColor);

            if (item.stack > 1)
            {
                Utils.DrawBorderStringFourWay(
                    spriteBatch,
                    FontAssets.ItemStack.Value,
                    item.stack.ToString(),
                    rectangle.X + 10f,
                    rectangle.Y + 26f,
                    itemColor,
                    Color.Black * (disabled ? 0.42f : 1f),
                    Vector2.Zero,
                    0.8f);
            }
        }

        if (marker.HasValue)
        {
            DrawMarker(spriteBatch, rectangle, marker.Value, disabled);
        }
    }

    public static void DrawBackground(
        SpriteBatch spriteBatch,
        Rectangle rectangle,
        Color accent,
        bool hovered = false,
        bool disabled = false,
        bool emphasizeOuterAccent = false)
    {
        var outerShadow = JournalUiTheme.ItemSlotOuterShadow;

        var outerEdge = Color.Lerp(
            JournalUiTheme.ItemSlotOuterEdge,
            accent,
            emphasizeOuterAccent ? 0.30f : 0.08f);

        var well = JournalUiTheme.ItemSlotWell;

        if (hovered)
        {
            outerEdge = Lighten(outerEdge, 0.12f);
        }

        var topBevel = Lighten(outerEdge, hovered ? 0.25f : 0.15f);
        var bottomBevel = Color.Lerp(outerEdge, Color.Black, hovered ? 0.34f : 0.46f);

        if (disabled)
        {
            outerShadow *= 0.55f;
            outerEdge *= 0.55f;
            well *= 0.55f;
            topBevel *= 0.55f;
            bottomBevel *= 0.55f;
        }

        var shadowRect = rectangle;
        shadowRect.Offset(2, 3);
        DrawChamferedRectangle(spriteBatch, shadowRect, 5, outerShadow * 0.70f);

        DrawChamferedRectangle(spriteBatch, rectangle, 5, outerEdge);
        DrawBevel(spriteBatch, rectangle, 5, topBevel, bottomBevel);

        var wellRect = rectangle;
        wellRect.Inflate(-5, -5);
        DrawChamferedRectangle(spriteBatch, wellRect, 2, well);
        DrawSoftInnerHighlight(spriteBatch, wellRect, disabled);
    }

    private static void DrawMarker(
        SpriteBatch spriteBatch,
        Rectangle slot,
        JournalSlotMarker marker,
        bool disabled)
    {
        var color = disabled ? marker.Color * 0.45f : marker.Color;

        if (marker.Placement == JournalSlotMarkerPlacement.LeftRail)
        {
            var rail = new Rectangle(slot.X + 3, slot.Y + 8, 3, slot.Height - 16);
            DrawChamferedRectangle(spriteBatch, rail, 1, Color.Lerp(color, Color.Black, 0.25f));
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rail.X, rail.Y + 1, 1, rail.Height - 2), Lighten(color, 0.25f));
            return;
        }

        if (marker.Placement == JournalSlotMarkerPlacement.BottomRail)
        {
            var rail = new Rectangle(slot.X + 8, slot.Bottom - 5, slot.Width - 16, 3);
            DrawChamferedRectangle(spriteBatch, rail, 1, Color.Lerp(color, Color.Black, 0.25f));
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rail.X + 1, rail.Y, rail.Width - 2, 1), Lighten(color, 0.25f));
            return;
        }

        var text = marker.Text;
        var drawText = marker.DrawText && !string.IsNullOrWhiteSpace(text);

        var font = FontAssets.ItemStack.Value;
        var textScale = text.Length <= 1 ? 0.66f : 0.54f;
        var textSize = drawText ? font.MeasureString(text) * textScale : Vector2.Zero;

        var badgeWidth = drawText
            ? Math.Max(13, (int)Math.Ceiling(textSize.X) + 8)
            : 10;

        badgeWidth = Math.Min(badgeWidth, Math.Max(10, slot.Width - 8));

        const int badgeHeight = 13;

        var badge = marker.Placement switch
        {
            JournalSlotMarkerPlacement.TopLeft => new Rectangle(slot.X + 3, slot.Y + 3, badgeWidth, badgeHeight),
            JournalSlotMarkerPlacement.BottomRight => new Rectangle(slot.Right - badgeWidth - 3, slot.Bottom - badgeHeight - 3, badgeWidth, badgeHeight),
            JournalSlotMarkerPlacement.BottomLeft => new Rectangle(slot.X + 3, slot.Bottom - badgeHeight - 3, badgeWidth, badgeHeight),
            _ => new Rectangle(slot.Right - badgeWidth - 3, slot.Y + 3, badgeWidth, badgeHeight)
        };

        var shadow = badge;
        shadow.Offset(1, 1);

        DrawChamferedRectangle(spriteBatch, shadow, 2, Color.Black * (disabled ? 0.30f : 0.50f));
        DrawChamferedRectangle(spriteBatch, badge, 2, Color.Lerp(color, Color.Black, 0.22f));
        DrawBevel(spriteBatch, badge, 2, Lighten(color, 0.24f), Color.Lerp(color, Color.Black, 0.50f));

        var inner = badge;
        inner.Inflate(-2, -2);

        if (inner is { Width: > 0, Height: > 0 })
        {
            DrawChamferedRectangle(spriteBatch, inner, 1, color * (disabled ? 0.28f : 0.48f));
        }

        if (!drawText)
        {
            return;
        }

        var textPosition = new Vector2(
            badge.Center.X - textSize.X / 2f,
            badge.Center.Y - textSize.Y / 2f - 1f);

        Utils.DrawBorderStringFourWay(
            spriteBatch,
            font,
            text,
            textPosition.X,
            textPosition.Y,
            Color.White * (disabled ? 0.60f : 1f),
            Color.Black * (disabled ? 0.48f : 0.95f),
            Vector2.Zero,
            textScale);
    }

    private static void DrawSoftInnerHighlight(SpriteBatch spriteBatch, Rectangle wellRect, bool disabled)
    {
        var gloss = new Rectangle(
            wellRect.X + 2,
            wellRect.Y + 2,
            Math.Max(1, wellRect.Width - 4),
            Math.Max(1, wellRect.Height / 3));

        DrawChamferedRectangle(
            spriteBatch,
            gloss,
            2,
            Color.White * (disabled ? 0.018f : 0.045f));
    }

    private static void DrawBevel(
        SpriteBatch spriteBatch,
        Rectangle rectangle,
        int cornerCut,
        Color topLeft,
        Color bottomRight)
    {
        var pixel = TextureAssets.MagicPixel.Value;

        if (rectangle.Width <= 0 || rectangle.Height <= 0)
        {
            return;
        }

        cornerCut = Math.Min(cornerCut, Math.Min(rectangle.Width, rectangle.Height) / 2);

        if (rectangle.Width <= cornerCut * 2 || rectangle.Height <= cornerCut * 2)
        {
            return;
        }

        spriteBatch.Draw(pixel, new Rectangle(rectangle.X + cornerCut, rectangle.Y, rectangle.Width - cornerCut * 2, 1), topLeft);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Y + cornerCut, 1, rectangle.Height - cornerCut * 2), topLeft);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X + cornerCut, rectangle.Bottom - 1, rectangle.Width - cornerCut * 2, 1), bottomRight);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.Right - 1, rectangle.Y + cornerCut, 1, rectangle.Height - cornerCut * 2), bottomRight);

        for (var i = 0; i < cornerCut; i++)
        {
            spriteBatch.Draw(pixel, new Rectangle(rectangle.X + cornerCut - i - 1, rectangle.Y + i, 1, 1), topLeft);
            spriteBatch.Draw(pixel, new Rectangle(rectangle.X + i, rectangle.Y + cornerCut - i - 1, 1, 1), topLeft);

            spriteBatch.Draw(pixel, new Rectangle(rectangle.Right - cornerCut + i, rectangle.Y + i, 1, 1), topLeft * 0.70f);
            spriteBatch.Draw(pixel, new Rectangle(rectangle.Right - i - 1, rectangle.Y + cornerCut - i - 1, 1, 1), bottomRight);

            spriteBatch.Draw(pixel, new Rectangle(rectangle.X + i, rectangle.Bottom - cornerCut + i, 1, 1), bottomRight);
            spriteBatch.Draw(pixel, new Rectangle(rectangle.X + cornerCut - i - 1, rectangle.Bottom - i - 1, 1, 1), bottomRight * 0.70f);

            spriteBatch.Draw(pixel, new Rectangle(rectangle.Right - cornerCut + i, rectangle.Bottom - i - 1, 1, 1), bottomRight);
            spriteBatch.Draw(pixel, new Rectangle(rectangle.Right - i - 1, rectangle.Bottom - cornerCut + i, 1, 1), bottomRight);
        }
    }

    private static void DrawChamferedRectangle(
        SpriteBatch spriteBatch,
        Rectangle rectangle,
        int cornerCut,
        Color color)
    {
        var pixel = TextureAssets.MagicPixel.Value;

        if (rectangle.Width <= 0 || rectangle.Height <= 0)
        {
            return;
        }

        cornerCut = Math.Min(cornerCut, Math.Min(rectangle.Width, rectangle.Height) / 2);

        if (cornerCut <= 0)
        {
            spriteBatch.Draw(pixel, rectangle, color);
            return;
        }

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

    private static Color Lighten(Color color, float amount)
    {
        return Color.Lerp(color, Color.White, amount);
    }
}
