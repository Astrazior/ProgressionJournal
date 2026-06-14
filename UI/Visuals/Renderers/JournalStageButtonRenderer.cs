using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;

namespace ProgressionJournal.UI.Visuals.Renderers;

internal static class JournalStageButtonRenderer
{
    private const int CornerCut = 5;
    private const int FaceInset = 4;

    public static void Draw(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        JournalButtonStyle style,
        bool active,
        bool hovered,
        bool interactable)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var lift = active ? -1 : 0;
        bounds.Offset(0, lift);

        var background = interactable
            ? style.Background
            : Color.Lerp(style.Background, JournalUiTheme.RootBackground, 0.48f);
        var accent = interactable
            ? style.Border
            : Color.Lerp(style.Border, JournalUiTheme.RootBackground, 0.62f);

        if (hovered && interactable)
        {
            background = Color.Lerp(background, Color.White, 0.10f);
            accent = Color.Lerp(accent, Color.White, 0.22f);
        }

        DrawShadow(spriteBatch, bounds, active);
        DrawBottomStep(spriteBatch, bounds, accent, active);
        DrawBody(spriteBatch, bounds, background, accent, active);
        DrawTopSignal(spriteBatch, bounds, accent, active, hovered && interactable);
    }

    private static void DrawShadow(SpriteBatch spriteBatch, Rectangle bounds, bool active)
    {
        var shadow = bounds;
        shadow.Offset(active ? 2 : 3, active ? 4 : 5);
        DrawChamferedRectangle(
            spriteBatch,
            shadow,
            CornerCut,
            JournalUiTheme.ItemSlotOuterShadow * (active ? 0.72f : 0.86f));
    }

    private static void DrawBottomStep(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        Color accent,
        bool active)
    {
        var ledge = new Rectangle(
            bounds.X + CornerCut + 5,
            bounds.Bottom - 2,
            Math.Max(1, bounds.Width - (CornerCut + 5) * 2),
            2);
        spriteBatch.Draw(
            TextureAssets.MagicPixel.Value,
            ledge,
            Color.Lerp(accent, Color.Black, active ? 0.18f : 0.28f));

        var highlight = new Rectangle(ledge.X + 3, ledge.Y, Math.Max(1, ledge.Width - 6), 1);
        spriteBatch.Draw(
            TextureAssets.MagicPixel.Value,
            highlight,
            Color.Lerp(accent, Color.White, active ? 0.38f : 0.24f));
    }

    private static void DrawBody(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        Color background,
        Color accent,
        bool active)
    {
        var frameTop = Color.Lerp(accent, Color.White, active ? 0.30f : 0.16f);
        var frameBottom = Color.Lerp(accent, Color.Black, active ? 0.26f : 0.38f);
        DrawVerticalGradient(spriteBatch, bounds, CornerCut, frameTop, accent, frameBottom);

        var face = bounds;
        face.Inflate(-FaceInset, -FaceInset);
        face.Height -= 1;

        var faceTop = Color.Lerp(background, Color.White, active ? 0.13f : 0.07f);
        var faceBottom = Color.Lerp(background, Color.Black, active ? 0.12f : 0.20f);
        DrawVerticalGradient(
            spriteBatch,
            face,
            Math.Max(2, CornerCut - FaceInset),
            faceTop,
            background,
            faceBottom);

        DrawInsetEdges(spriteBatch, face, background);
    }

    private static void DrawInsetEdges(
        SpriteBatch spriteBatch,
        Rectangle face,
        Color background)
    {
        var pixel = TextureAssets.MagicPixel.Value;
        var topShadow = Color.Lerp(background, Color.Black, 0.62f);
        var bottomLight = Color.Lerp(background, Color.White, 0.14f);

        spriteBatch.Draw(
            pixel,
            new Rectangle(face.X + 3, face.Y, Math.Max(1, face.Width - 6), 2),
            topShadow * 0.58f);
        spriteBatch.Draw(
            pixel,
            new Rectangle(face.X + 3, face.Bottom - 1, Math.Max(1, face.Width - 6), 1),
            bottomLight * 0.54f);
    }

    private static void DrawTopSignal(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        Color accent,
        bool active,
        bool hovered)
    {
        var width = active
            ? Math.Max(12, bounds.Width - 26)
            : Math.Max(10, bounds.Width / 3);
        var signal = new Rectangle(
            bounds.Center.X - width / 2,
            bounds.Y + 2,
            width,
            active ? 2 : 1);
        var strength = active ? 0.82f : hovered ? 0.54f : 0.22f;
        spriteBatch.Draw(
            TextureAssets.MagicPixel.Value,
            signal,
            Color.Lerp(accent, Color.White, 0.32f) * strength);
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
            var t = rectangle.Height <= 1
                ? 0f
                : row / (float)(rectangle.Height - 1);
            var color = t < 0.45f
                ? Color.Lerp(top, middle, t / 0.45f)
                : Color.Lerp(middle, bottom, (t - 0.45f) / 0.55f);
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

        if (width <= 0)
        {
            return;
        }

        spriteBatch.Draw(
            TextureAssets.MagicPixel.Value,
            new Rectangle(rectangle.X + inset, y, width, 1),
            color);
    }
}
