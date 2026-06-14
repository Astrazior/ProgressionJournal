using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;

namespace ProgressionJournal.UI.Visuals.Renderers;

internal static class JournalVolumetricPanelRenderer
{
    private const int ShadowOffsetX = 4;
    private const int ShadowOffsetY = 5;
    private const int FrameThickness = 5;
    private const int OuterBevelSize = 4;
    private const int InnerDepthSize = 5;

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

        var cornerCut = Math.Clamp(Math.Min(bounds.Width, bounds.Height) / 8, 5, 10);

        var stoneBorder = Color.Lerp(border, new Color(92, 88, 82), 0.35f);
        var stoneBackground = Color.Lerp(background, new Color(54, 52, 49), 0.25f);

        DrawSoftShadow(spriteBatch, bounds, cornerCut);

        DrawChamferedGradient(
            spriteBatch,
            bounds,
            cornerCut,
            Color.Lerp(stoneBorder, Color.White, 0.16f),
            stoneBorder,
            Color.Lerp(stoneBorder, Color.Black, 0.30f));

        DrawOuterBevel(spriteBatch, bounds, cornerCut, stoneBorder);

        var inner = bounds;
        inner.Inflate(-FrameThickness, -FrameThickness);

        if (inner.Width <= 0 || inner.Height <= 0)
        {
            return;
        }

        var innerCornerCut = Math.Max(2, cornerCut - FrameThickness);

        DrawChamferedGradient(
            spriteBatch,
            inner,
            innerCornerCut,
            Color.Lerp(stoneBackground, Color.White, 0.055f),
            stoneBackground,
            Color.Lerp(stoneBackground, Color.Black, 0.13f));
        DrawInnerDepth(spriteBatch, inner, innerCornerCut, stoneBackground);
    }

    private static void DrawSoftShadow(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        int cornerCut)
    {
        for (var i = 4; i >= 0; i--)
        {
            var shadow = bounds;
            shadow.Offset(ShadowOffsetX + i, ShadowOffsetY + i);
            shadow.Inflate(i, i);

            DrawChamferedRectangle(
                spriteBatch,
                shadow,
                cornerCut + i,
                JournalUiTheme.ItemSlotOuterShadow * (0.04f + i * 0.018f));
        }

        var contact = bounds;
        contact.Offset(2, 2);

        DrawChamferedRectangle(
            spriteBatch,
            contact,
            cornerCut,
            Color.Black * 0.16f);
    }

    private static void DrawOuterBevel(
        SpriteBatch spriteBatch,
        Rectangle rectangle,
        int cornerCut,
        Color baseColor)
    {
        var pixel = TextureAssets.MagicPixel.Value;

        var light = Color.Lerp(baseColor, Color.White, 0.38f);
        var softLight = Color.Lerp(baseColor, Color.White, 0.18f);
        var dark = Color.Lerp(baseColor, Color.Black, 0.60f);
        var softDark = Color.Lerp(baseColor, Color.Black, 0.34f);

        var horizontalWidth = rectangle.Width - cornerCut * 2;
        var verticalHeight = rectangle.Height - cornerCut * 2;

        if (horizontalWidth <= 0 || verticalHeight <= 0)
        {
            return;
        }

        for (var i = 0; i < OuterBevelSize; i++)
        {
            var t = 1f - i / (float)OuterBevelSize;

            spriteBatch.Draw(
                pixel,
                new Rectangle(rectangle.X + cornerCut, rectangle.Y + i, horizontalWidth, 1),
                light * (0.50f * t));

            spriteBatch.Draw(
                pixel,
                new Rectangle(rectangle.X + i, rectangle.Y + cornerCut, 1, verticalHeight),
                softLight * (0.34f * t));

            spriteBatch.Draw(
                pixel,
                new Rectangle(rectangle.X + cornerCut, rectangle.Bottom - 1 - i, horizontalWidth, 1),
                dark * (0.62f * t));

            spriteBatch.Draw(
                pixel,
                new Rectangle(rectangle.Right - 1 - i, rectangle.Y + cornerCut, 1, verticalHeight),
                softDark * (0.56f * t));
        }

        DrawChamferCornerBevel(spriteBatch, rectangle, cornerCut, light, dark);
    }

    private static void DrawChamferCornerBevel(
        SpriteBatch spriteBatch,
        Rectangle rectangle,
        int cornerCut,
        Color light,
        Color dark)
    {
        var pixel = TextureAssets.MagicPixel.Value;

        for (var row = 0; row < cornerCut; row++)
        {
            var topY = rectangle.Y + row;
            var bottomY = rectangle.Bottom - row - 1;

            var leftX = rectangle.X + cornerCut - row;
            var rightX = rectangle.Right - cornerCut + row - 1;

            var t = 1f - row / (float)Math.Max(1, cornerCut);

            spriteBatch.Draw(pixel, new Rectangle(leftX, topY, 2, 1), light * (0.30f * t));
            spriteBatch.Draw(pixel, new Rectangle(rightX - 1, bottomY, 2, 1), dark * (0.44f * t));
        }
    }

    private static void DrawInnerDepth(
        SpriteBatch spriteBatch,
        Rectangle inner,
        int cornerCut,
        Color background)
    {
        var pixel = TextureAssets.MagicPixel.Value;

        var topShadow = Color.Lerp(background, Color.Black, 0.78f);
        var leftShadow = Color.Lerp(background, Color.Black, 0.58f);
        var bottomShadow = Color.Lerp(background, Color.Black, 0.72f);
        var bottomLight = Color.Lerp(background, Color.White, 0.16f);
        var rightLight = Color.Lerp(background, Color.White, 0.10f);

        for (var i = 0; i < InnerDepthSize; i++)
        {
            var t = 1f - i / (float)InnerDepthSize;

            DrawChamferedHorizontalLine(
                spriteBatch,
                inner,
                cornerCut,
                inner.Y + i,
                topShadow * (0.30f * t));

            DrawChamferedHorizontalLine(
                spriteBatch,
                inner,
                cornerCut,
                inner.Bottom - 1 - i,
                bottomShadow * (0.24f * t));

            DrawChamferedHorizontalLine(
                spriteBatch,
                inner,
                cornerCut,
                inner.Bottom - 1 - i,
                bottomLight * (0.055f * t));

            spriteBatch.Draw(
                pixel,
                new Rectangle(inner.X + i, inner.Y + cornerCut, 1, Math.Max(1, inner.Height - cornerCut * 2)),
                leftShadow * (0.18f * t));

            spriteBatch.Draw(
                pixel,
                new Rectangle(inner.Right - 1 - i, inner.Y + cornerCut, 1, Math.Max(1, inner.Height - cornerCut * 2)),
                rightLight * (0.10f * t));
        }

        DrawChamferedHorizontalLine(spriteBatch, inner, cornerCut, inner.Y, Color.Black * 0.28f);
        DrawChamferedHorizontalLine(spriteBatch, inner, cornerCut, inner.Bottom - 1, Color.Black * 0.24f);
    }

    private static void DrawChamferedGradient(
        SpriteBatch spriteBatch,
        Rectangle rectangle,
        int cornerCut,
        Color top,
        Color middle,
        Color bottom)
    {
        if (rectangle.Width <= 0 || rectangle.Height <= 0)
        {
            return;
        }

        for (var row = 0; row < rectangle.Height; row++)
        {
            var t = rectangle.Height <= 1
                ? 0f
                : row / (float)(rectangle.Height - 1);

            var color = t < 0.5f
                ? Color.Lerp(top, middle, t / 0.5f)
                : Color.Lerp(middle, bottom, (t - 0.5f) / 0.5f);

            DrawChamferedHorizontalLine(
                spriteBatch,
                rectangle,
                cornerCut,
                rectangle.Y + row,
                color);
        }
    }

    private static void DrawChamferedHorizontalLine(
        SpriteBatch spriteBatch,
        Rectangle rectangle,
        int cornerCut,
        int y,
        Color color)
    {
        if (y < rectangle.Y || y >= rectangle.Bottom)
        {
            return;
        }

        var row = y - rectangle.Y;
        var inset = 0;

        if (cornerCut > 0)
        {
            if (row < cornerCut)
            {
                inset = cornerCut - row;
            }
            else if (row >= rectangle.Height - cornerCut)
            {
                inset = cornerCut - (rectangle.Height - 1 - row);
            }
        }

        var width = rectangle.Width - inset * 2;

        if (width <= 0)
        {
            return;
        }

        spriteBatch.Draw(
            TextureAssets.MagicPixel.Value,
            new Rectangle(rectangle.X + inset, y, width, 1),
            color);
    }

    private static void DrawChamferedRectangle(
        SpriteBatch spriteBatch,
        Rectangle rectangle,
        int cornerCut,
        Color color)
    {
        DrawChamferedGradient(spriteBatch, rectangle, cornerCut, color, color, color);
    }

}
