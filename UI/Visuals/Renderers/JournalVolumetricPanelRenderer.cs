using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;

namespace ProgressionJournal.UI.Visuals.Renderers;

internal static class JournalVolumetricPanelRenderer
{
    private const int ShadowOffsetX = 3;
    private const int ShadowOffsetY = 4;
    private const int FrameThickness = 3;

    public static void Draw(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        Color background,
        Color border)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var cornerCut = Math.Clamp(Math.Min(bounds.Width, bounds.Height) / 8, 4, 8);
        var shadow = bounds;
        shadow.Offset(ShadowOffsetX, ShadowOffsetY);
        DrawChamferedRectangle(spriteBatch, shadow, cornerCut, JournalUiTheme.ItemSlotOuterShadow * 0.82f);

        DrawChamferedRectangle(spriteBatch, bounds, cornerCut, border);
        DrawBevel(
            spriteBatch,
            bounds,
            cornerCut,
            Color.Lerp(border, Color.White, 0.24f),
            Color.Lerp(border, Color.Black, 0.58f));

        var inner = bounds;
        inner.Inflate(-FrameThickness, -FrameThickness);
        var innerCornerCut = Math.Max(2, cornerCut - FrameThickness);
        DrawChamferedRectangle(spriteBatch, inner, innerCornerCut, background);
        DrawInnerDepth(spriteBatch, inner, innerCornerCut, background);
    }

    private static void DrawInnerDepth(
        SpriteBatch spriteBatch,
        Rectangle inner,
        int cornerCut,
        Color background)
    {
        if (inner.Width <= 4 || inner.Height <= 4)
        {
            return;
        }

        var highlight = new Rectangle(
            inner.X + cornerCut,
            inner.Y + 1,
            Math.Max(1, inner.Width - cornerCut * 2),
            2);
        spriteBatch.Draw(
            TextureAssets.MagicPixel.Value,
            highlight,
            Color.Lerp(background, Color.White, 0.18f));

        var rightShadow = new Rectangle(
            inner.Right - 2,
            inner.Y + cornerCut,
            2,
            Math.Max(1, inner.Height - cornerCut * 2));
        var bottomShadow = new Rectangle(
            inner.X + cornerCut,
            inner.Bottom - 2,
            Math.Max(1, inner.Width - cornerCut * 2),
            2);
        var insetShadow = Color.Lerp(background, Color.Black, 0.52f);
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, rightShadow, insetShadow);
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, bottomShadow, insetShadow);

        var topGlow = new Rectangle(
            inner.X + cornerCut + 2,
            inner.Y + 4,
            Math.Max(1, inner.Width - cornerCut * 2 - 4),
            Math.Max(1, Math.Min(8, inner.Height / 8)));
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, topGlow, Color.White * 0.025f);
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

        spriteBatch.Draw(pixel, new Rectangle(rectangle.X + cornerCut, rectangle.Y, horizontalWidth, 2), topLeft);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Y + cornerCut, 2, verticalHeight), topLeft);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X + cornerCut, rectangle.Bottom - 2, horizontalWidth, 2), bottomRight);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.Right - 2, rectangle.Y + cornerCut, 2, verticalHeight), bottomRight);
    }

    private static void DrawChamferedRectangle(
        SpriteBatch spriteBatch,
        Rectangle rectangle,
        int cornerCut,
        Color color)
    {
        var pixel = TextureAssets.MagicPixel.Value;
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
}
