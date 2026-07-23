using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace ProgressionJournal.UI.Visuals.Renderers;

internal static class JournalTooltipRenderer
{
    private const string FrameTexturePath =
        "ProgressionJournal/Assets/UI/Tooltips/TooltipFrame";
    private const string MarkerTexturePath =
        "ProgressionJournal/Assets/UI/Tooltips/TooltipMarker";
    private const int TextureSize = 64;
    private const int SourceCornerSize = 12;
    private const int DestinationCornerSize = 12;

    public static void DrawPanel(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        Color accent,
        bool drawShadow = true,
        float accentStrength = 0.10f)
    {
        if (bounds is not { Width: > 0, Height: > 0 })
        {
            return;
        }

        if (drawShadow)
        {
            var shadowBounds = bounds;
            shadowBounds.Offset(5, 6);
            DrawNineSlice(spriteBatch, shadowBounds, Color.Black * 0.48f);
        }

        DrawNineSlice(
            spriteBatch,
            bounds,
            Color.Lerp(Color.White, accent, accentStrength));
    }

    public static void DrawRule(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        Color color)
    {
        if (bounds is not { Width: > 0, Height: > 0 })
        {
            return;
        }

        var source = new Rectangle(
            SourceCornerSize,
            4,
            TextureSize - SourceCornerSize * 2,
            3);
        spriteBatch.Draw(GetFrameTexture(), bounds, source, color);
    }

    public static void DrawMarker(
        SpriteBatch spriteBatch,
        Rectangle bounds)
    {
        if (bounds is not { Width: > 0, Height: > 0 })
        {
            return;
        }

        spriteBatch.Draw(
            ModContent.Request<Texture2D>(MarkerTexturePath).Value,
            bounds,
            Color.White);
    }

    private static Texture2D GetFrameTexture()
    {
        return ModContent.Request<Texture2D>(FrameTexturePath).Value;
    }

    private static void DrawNineSlice(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        Color color)
    {
        var texture = GetFrameTexture();
        var destinationCornerWidth = Math.Min(DestinationCornerSize, bounds.Width / 2);
        var destinationCornerHeight = Math.Min(DestinationCornerSize, bounds.Height / 2);
        var destinationCenterWidth = bounds.Width - destinationCornerWidth * 2;
        var destinationCenterHeight = bounds.Height - destinationCornerHeight * 2;
        const int sourceCenterSize = TextureSize - SourceCornerSize * 2;

        DrawSlice(
            spriteBatch,
            texture,
            new Rectangle(0, 0, SourceCornerSize, SourceCornerSize),
            new Rectangle(bounds.X, bounds.Y, destinationCornerWidth, destinationCornerHeight),
            color);
        DrawSlice(
            spriteBatch,
            texture,
            new Rectangle(SourceCornerSize, 0, sourceCenterSize, SourceCornerSize),
            new Rectangle(
                bounds.X + destinationCornerWidth,
                bounds.Y,
                destinationCenterWidth,
                destinationCornerHeight),
            color);
        DrawSlice(
            spriteBatch,
            texture,
            new Rectangle(TextureSize - SourceCornerSize, 0, SourceCornerSize, SourceCornerSize),
            new Rectangle(
                bounds.Right - destinationCornerWidth,
                bounds.Y,
                destinationCornerWidth,
                destinationCornerHeight),
            color);
        DrawSlice(
            spriteBatch,
            texture,
            new Rectangle(0, SourceCornerSize, SourceCornerSize, sourceCenterSize),
            new Rectangle(
                bounds.X,
                bounds.Y + destinationCornerHeight,
                destinationCornerWidth,
                destinationCenterHeight),
            color);
        DrawSlice(
            spriteBatch,
            texture,
            new Rectangle(SourceCornerSize, SourceCornerSize, sourceCenterSize, sourceCenterSize),
            new Rectangle(
                bounds.X + destinationCornerWidth,
                bounds.Y + destinationCornerHeight,
                destinationCenterWidth,
                destinationCenterHeight),
            color);
        DrawSlice(
            spriteBatch,
            texture,
            new Rectangle(
                TextureSize - SourceCornerSize,
                SourceCornerSize,
                SourceCornerSize,
                sourceCenterSize),
            new Rectangle(
                bounds.Right - destinationCornerWidth,
                bounds.Y + destinationCornerHeight,
                destinationCornerWidth,
                destinationCenterHeight),
            color);
        DrawSlice(
            spriteBatch,
            texture,
            new Rectangle(0, TextureSize - SourceCornerSize, SourceCornerSize, SourceCornerSize),
            new Rectangle(
                bounds.X,
                bounds.Bottom - destinationCornerHeight,
                destinationCornerWidth,
                destinationCornerHeight),
            color);
        DrawSlice(
            spriteBatch,
            texture,
            new Rectangle(
                SourceCornerSize,
                TextureSize - SourceCornerSize,
                sourceCenterSize,
                SourceCornerSize),
            new Rectangle(
                bounds.X + destinationCornerWidth,
                bounds.Bottom - destinationCornerHeight,
                destinationCenterWidth,
                destinationCornerHeight),
            color);
        DrawSlice(
            spriteBatch,
            texture,
            new Rectangle(
                TextureSize - SourceCornerSize,
                TextureSize - SourceCornerSize,
                SourceCornerSize,
                SourceCornerSize),
            new Rectangle(
                bounds.Right - destinationCornerWidth,
                bounds.Bottom - destinationCornerHeight,
                destinationCornerWidth,
                destinationCornerHeight),
            color);
    }

    private static void DrawSlice(
        SpriteBatch spriteBatch,
        Texture2D texture,
        Rectangle source,
        Rectangle destination,
        Color color)
    {
        if (destination is { Width: > 0, Height: > 0 })
        {
            spriteBatch.Draw(texture, destination, source, color);
        }
    }
}
