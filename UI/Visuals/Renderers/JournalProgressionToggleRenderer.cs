using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ProgressionJournal.UI.Visuals.Renderers;

internal static class JournalProgressionToggleRenderer
{
    private const int CornerCut = 4;
    private static readonly Asset<Texture2D> CheckTexture =
        ModContent.Request<Texture2D>("ProgressionJournal/Assets/UI/ProgressionModeCheck");

    public static void Draw(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        JournalButtonStyle style,
        bool enabled,
        bool hovered)
    {
        bounds.X += 2;
        bounds.Width = Math.Max(1, bounds.Width - 2);

        var background = hovered
            ? Color.Lerp(style.Background, Color.White, 0.10f)
            : style.Background;
        var accent = hovered
            ? Color.Lerp(style.Border, Color.White, 0.22f)
            : style.Border;

        var shadow = bounds;
        shadow.Offset(2, 3);
        DrawChamferedRectangle(
            spriteBatch,
            shadow,
            CornerCut,
            JournalUiTheme.ItemSlotOuterShadow * 0.78f);

        DrawVerticalGradient(
            spriteBatch,
            bounds,
            CornerCut,
            Color.Lerp(accent, Color.White, 0.24f),
            accent,
            Color.Lerp(accent, Color.Black, 0.38f));

        var face = bounds;
        face.Inflate(-3, -3);
        DrawChamferedRectangle(
            spriteBatch,
            face,
            2,
            Color.Lerp(background, Color.Black, 0.08f));

        if (enabled)
        {
            DrawCheck(spriteBatch, bounds, hovered);
        }
    }

    private static void DrawCheck(SpriteBatch spriteBatch, Rectangle bounds, bool hovered)
    {
        var texture = CheckTexture.Value;
        var position = new Vector2(bounds.Center.X, bounds.Center.Y);
        spriteBatch.Draw(
            texture,
            position,
            null,
            hovered ? Color.White : Color.White * 0.92f,
            0f,
            new Vector2(texture.Width * 0.5f, texture.Height * 0.5f),
            1f,
            SpriteEffects.None,
            0f);
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

        if (width > 0)
        {
            spriteBatch.Draw(
                TextureAssets.MagicPixel.Value,
                new Rectangle(rectangle.X + inset, y, width, 1),
                color);
        }
    }
}
