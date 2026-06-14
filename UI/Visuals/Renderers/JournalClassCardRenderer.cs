using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;

namespace ProgressionJournal.UI.Visuals.Renderers;

internal static class JournalClassCardRenderer
{
    private const int CornerCut = 9;
    private const int FrameInset = 5;

    public static void Draw(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        JournalClassPalette palette,
        bool selected,
        bool hovered)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        if (selected)
        {
            bounds.Offset(0, -1);
        }

        DrawShadow(spriteBatch, bounds, selected);
        DrawFrame(spriteBatch, bounds, palette, selected, hovered);

        var face = bounds;
        face.Inflate(-FrameInset, -FrameInset);
        DrawFace(spriteBatch, face, palette, selected, hovered);
        DrawTitlePlate(spriteBatch, face, palette, selected);
        DrawPreviewBay(spriteBatch, face, palette, selected);
    }

    private static void DrawShadow(SpriteBatch spriteBatch, Rectangle bounds, bool selected)
    {
        var shadow = bounds;
        shadow.Offset(selected ? 3 : 4, selected ? 4 : 6);
        DrawChamferedRectangle(
            spriteBatch,
            shadow,
            CornerCut,
            JournalUiTheme.ItemSlotOuterShadow * (selected ? 0.72f : 0.9f));

        var contact = new Rectangle(bounds.X + 12, bounds.Bottom + 2, bounds.Width - 24, 4);
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, contact, Color.Black * 0.22f);
    }

    private static void DrawFrame(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        JournalClassPalette palette,
        bool selected,
        bool hovered)
    {
        var accent = selected
            ? Color.Lerp(palette.Accent, Color.White, 0.12f)
            : hovered
                ? Color.Lerp(palette.Border, palette.Accent, 0.62f)
                : palette.Border;
        var top = Color.Lerp(accent, Color.White, selected ? 0.32f : 0.20f);
        var bottom = Color.Lerp(accent, Color.Black, selected ? 0.30f : 0.48f);

        DrawVerticalGradient(spriteBatch, bounds, CornerCut, top, accent, bottom);
    }

    private static void DrawFace(
        SpriteBatch spriteBatch,
        Rectangle face,
        JournalClassPalette palette,
        bool selected,
        bool hovered)
    {
        var background = selected
            ? Color.Lerp(palette.Background, palette.Accent, 0.10f)
            : hovered
                ? Color.Lerp(palette.Background, palette.Accent, 0.05f)
                : palette.Background;
        var top = Color.Lerp(background, Color.White, 0.10f);
        var bottom = Color.Lerp(background, Color.Black, 0.24f);
        DrawVerticalGradient(spriteBatch, face, Math.Max(3, CornerCut - FrameInset), top, background, bottom);
    }

    private static void DrawTitlePlate(
        SpriteBatch spriteBatch,
        Rectangle face,
        JournalClassPalette palette,
        bool selected)
    {
        var plate = new Rectangle(face.X + 11, face.Y + 6, face.Width - 22, 28);
        var plateBackground = Color.Lerp(palette.Background, Color.Black, 0.24f);
        var plateBorder = Color.Lerp(palette.Border, palette.Accent, selected ? 0.68f : 0.32f);

        DrawChamferedRectangle(spriteBatch, plate, 5, Color.Lerp(plateBorder, Color.Black, 0.25f));
        plate.Inflate(-2, -2);
        DrawVerticalGradient(
            spriteBatch,
            plate,
            3,
            Color.Lerp(plateBackground, Color.White, 0.10f),
            plateBackground,
            Color.Lerp(plateBackground, Color.Black, 0.18f));
    }

    private static void DrawPreviewBay(
        SpriteBatch spriteBatch,
        Rectangle face,
        JournalClassPalette palette,
        bool selected)
    {
        var bay = new Rectangle(face.X + 15, face.Y + 39, face.Width - 30, face.Height - 51);
        var center = bay.Center.X;
        var pixel = TextureAssets.MagicPixel.Value;
        var glow = Color.Lerp(palette.Accent, Color.White, 0.08f);

        for (var inset = 0; inset < bay.Width / 2; inset++)
        {
            var normalized = inset / (float)Math.Max(1, bay.Width / 2);
            var alpha = (1f - normalized) * (selected ? 0.026f : 0.015f);
            var x = center - inset;
            spriteBatch.Draw(pixel, new Rectangle(x, bay.Y + 4, 2, bay.Height - 8), glow * alpha);
            spriteBatch.Draw(pixel, new Rectangle(center + inset, bay.Y + 4, 2, bay.Height - 8), glow * alpha);
        }
    }

    private static void DrawVerticalGradient(
        SpriteBatch spriteBatch,
        Rectangle rectangle,
        int cornerCut,
        Color top,
        Color middle,
        Color bottom)
    {
        for (var row = 0; row < rectangle.Height; row++)
        {
            var t = rectangle.Height <= 1 ? 0f : row / (float)(rectangle.Height - 1);
            var color = t < 0.42f
                ? Color.Lerp(top, middle, t / 0.42f)
                : Color.Lerp(middle, bottom, (t - 0.42f) / 0.58f);
            DrawHorizontalLine(spriteBatch, rectangle, cornerCut, rectangle.Y + row, color);
        }
    }

    private static void DrawChamferedRectangle(
        SpriteBatch spriteBatch,
        Rectangle rectangle,
        int cornerCut,
        Color color)
    {
        for (var y = rectangle.Y; y < rectangle.Bottom; y++)
        {
            DrawHorizontalLine(spriteBatch, rectangle, cornerCut, y, color);
        }
    }

    private static void DrawHorizontalLine(
        SpriteBatch spriteBatch,
        Rectangle rectangle,
        int cornerCut,
        int y,
        Color color)
    {
        var row = y - rectangle.Y;
        var inset = row < cornerCut
            ? cornerCut - row
            : row >= rectangle.Height - cornerCut
                ? cornerCut - (rectangle.Height - 1 - row)
                : 0;
        var width = rectangle.Width - inset * 2;

        if (width > 0)
        {
            spriteBatch.Draw(
                TextureAssets.MagicPixel.Value,
                new Rectangle(rectangle.X + inset, y, width, 1),
                color);
        }
    }
}
