using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace ProgressionJournal.UI.Visuals.Renderers;

internal static class JournalSourceCardRenderer
{
    private const string BestiaryPanelTexturePath = "Images/UI/Bestiary/Stat_Panel";

    public static void Draw(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        Color accent,
        bool highlighted = false)
    {
        var texture = Main.Assets.Request<Texture2D>(BestiaryPanelTexturePath).Value;
        var darkBackground = Color.Lerp(
            JournalUiTheme.RootBackground,
            JournalUiTheme.PanelBackground,
            0.34f);
        var tint = highlighted
            ? Color.Lerp(darkBackground, accent, 0.10f)
            : darkBackground;

        var fill = bounds;
        fill.Inflate(-4, -4);
        if (fill is { Width: > 0, Height: > 0 })
        {
            spriteBatch.Draw(
                TextureAssets.MagicPixel.Value,
                fill,
                Color.Lerp(darkBackground, accent, highlighted ? 0.08f : 0.025f) * 0.96f);
        }

        DrawNineSlice(spriteBatch, texture, bounds, tint);
        DrawInnerFrame(spriteBatch, bounds, accent, highlighted);
    }

    public static void DrawTooltip(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        Color accent)
    {
        var texture = Main.Assets.Request<Texture2D>(BestiaryPanelTexturePath).Value;
        var background = Color.Lerp(
            JournalUiTheme.RootBackground,
            JournalUiTheme.PanelBackground,
            0.34f);

        var fill = bounds;
        fill.Inflate(-4, -4);
        if (fill is { Width: > 0, Height: > 0 })
        {
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, fill, background * 0.96f);
        }

        DrawNineSlice(
            spriteBatch,
            texture,
            bounds,
            Color.Lerp(background, accent, 0.035f));
    }

    private static void DrawInnerFrame(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        Color accent,
        bool highlighted)
    {
        var frame = bounds;
        frame.Inflate(-4, -4);
        if (frame.Width <= 8 || frame.Height <= 8)
        {
            return;
        }

        var pixel = TextureAssets.MagicPixel.Value;
        var baseColor = Color.Lerp(
            JournalUiTheme.RootBackground,
            accent,
            highlighted ? 0.16f : 0.08f);
        var outer = Color.Lerp(baseColor, Color.Black, 0.20f) * 0.94f;
        var middle = Color.Lerp(baseColor, Color.Black, 0.40f) * 0.90f;
        var inner = Color.Lerp(baseColor, Color.Black, 0.58f) * 0.84f;

        DrawBorder(spriteBatch, pixel, frame, 2, outer);
        frame.Inflate(-2, -2);
        DrawBorder(spriteBatch, pixel, frame, 2, middle);
        frame.Inflate(-2, -2);
        DrawBorder(spriteBatch, pixel, frame, 1, inner);
    }

    private static void DrawBorder(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        Rectangle rectangle,
        int thickness,
        Color color)
    {
        if (rectangle.Width <= thickness * 2 || rectangle.Height <= thickness * 2)
        {
            return;
        }

        spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Y + thickness, thickness, rectangle.Height - thickness * 2), color);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.Right - thickness, rectangle.Y + thickness, thickness, rectangle.Height - thickness * 2), color);
    }

    private static void DrawNineSlice(
        SpriteBatch spriteBatch,
        Texture2D texture,
        Rectangle destination,
        Color color)
    {
        var sourceCornerWidth = Math.Max(1, texture.Width / 3);
        var sourceCornerHeight = Math.Max(1, texture.Height / 3);
        var destinationCornerWidth = Math.Min(sourceCornerWidth, destination.Width / 2);
        var destinationCornerHeight = Math.Min(sourceCornerHeight, destination.Height / 2);
        var sourceCenterWidth = Math.Max(1, texture.Width - sourceCornerWidth * 2);
        var sourceCenterHeight = Math.Max(1, texture.Height - sourceCornerHeight * 2);
        var destinationCenterWidth = Math.Max(0, destination.Width - destinationCornerWidth * 2);
        var destinationCenterHeight = Math.Max(0, destination.Height - destinationCornerHeight * 2);

        DrawSlice(spriteBatch, texture, color, destination.X, destination.Y, destinationCornerWidth, destinationCornerHeight, 0, 0, sourceCornerWidth, sourceCornerHeight);
        DrawSlice(spriteBatch, texture, color, destination.X + destinationCornerWidth, destination.Y, destinationCenterWidth, destinationCornerHeight, sourceCornerWidth, 0, sourceCenterWidth, sourceCornerHeight);
        DrawSlice(spriteBatch, texture, color, destination.Right - destinationCornerWidth, destination.Y, destinationCornerWidth, destinationCornerHeight, texture.Width - sourceCornerWidth, 0, sourceCornerWidth, sourceCornerHeight);
        DrawSlice(spriteBatch, texture, color, destination.X, destination.Y + destinationCornerHeight, destinationCornerWidth, destinationCenterHeight, 0, sourceCornerHeight, sourceCornerWidth, sourceCenterHeight);
        DrawSlice(spriteBatch, texture, color, destination.X + destinationCornerWidth, destination.Y + destinationCornerHeight, destinationCenterWidth, destinationCenterHeight, sourceCornerWidth, sourceCornerHeight, sourceCenterWidth, sourceCenterHeight);
        DrawSlice(spriteBatch, texture, color, destination.Right - destinationCornerWidth, destination.Y + destinationCornerHeight, destinationCornerWidth, destinationCenterHeight, texture.Width - sourceCornerWidth, sourceCornerHeight, sourceCornerWidth, sourceCenterHeight);
        DrawSlice(spriteBatch, texture, color, destination.X, destination.Bottom - destinationCornerHeight, destinationCornerWidth, destinationCornerHeight, 0, texture.Height - sourceCornerHeight, sourceCornerWidth, sourceCornerHeight);
        DrawSlice(spriteBatch, texture, color, destination.X + destinationCornerWidth, destination.Bottom - destinationCornerHeight, destinationCenterWidth, destinationCornerHeight, sourceCornerWidth, texture.Height - sourceCornerHeight, sourceCenterWidth, sourceCornerHeight);
        DrawSlice(spriteBatch, texture, color, destination.Right - destinationCornerWidth, destination.Bottom - destinationCornerHeight, destinationCornerWidth, destinationCornerHeight, texture.Width - sourceCornerWidth, texture.Height - sourceCornerHeight, sourceCornerWidth, sourceCornerHeight);
    }

    private static void DrawSlice(
        SpriteBatch spriteBatch,
        Texture2D texture,
        Color color,
        int destinationX,
        int destinationY,
        int destinationWidth,
        int destinationHeight,
        int sourceX,
        int sourceY,
        int sourceWidth,
        int sourceHeight)
    {
        if (destinationWidth <= 0 || destinationHeight <= 0)
        {
            return;
        }

        spriteBatch.Draw(
            texture,
            new Rectangle(destinationX, destinationY, destinationWidth, destinationHeight),
            new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
            color);
    }
}
