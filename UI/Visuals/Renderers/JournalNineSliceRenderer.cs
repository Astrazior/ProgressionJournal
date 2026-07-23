using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ProgressionJournal.UI.Visuals.Renderers;

internal static class JournalNineSliceRenderer
{
    public static void Draw(
        SpriteBatch spriteBatch,
        Texture2D texture,
        Rectangle bounds,
        int sourceCornerSize,
        int destinationCornerSize,
        Color color)
    {
        if (bounds is not { Width: > 0, Height: > 0 })
        {
            return;
        }

        var sourceCornerWidth = Math.Clamp(
            sourceCornerSize,
            1,
            Math.Max(1, texture.Width / 2));
        var sourceCornerHeight = Math.Clamp(
            sourceCornerSize,
            1,
            Math.Max(1, texture.Height / 2));
        var destinationCornerWidth = Math.Min(
            destinationCornerSize,
            bounds.Width / 2);
        var destinationCornerHeight = Math.Min(
            destinationCornerSize,
            bounds.Height / 2);
        var sourceCenterWidth = Math.Max(
            0,
            texture.Width - sourceCornerWidth * 2);
        var sourceCenterHeight = Math.Max(
            0,
            texture.Height - sourceCornerHeight * 2);
        var destinationCenterWidth = Math.Max(
            0,
            bounds.Width - destinationCornerWidth * 2);
        var destinationCenterHeight = Math.Max(
            0,
            bounds.Height - destinationCornerHeight * 2);

        DrawSlice(
            spriteBatch,
            texture,
            new Rectangle(0, 0, sourceCornerWidth, sourceCornerHeight),
            new Rectangle(
                bounds.X,
                bounds.Y,
                destinationCornerWidth,
                destinationCornerHeight),
            color);
        DrawSlice(
            spriteBatch,
            texture,
            new Rectangle(
                sourceCornerWidth,
                0,
                sourceCenterWidth,
                sourceCornerHeight),
            new Rectangle(
                bounds.X + destinationCornerWidth,
                bounds.Y,
                destinationCenterWidth,
                destinationCornerHeight),
            color);
        DrawSlice(
            spriteBatch,
            texture,
            new Rectangle(
                texture.Width - sourceCornerWidth,
                0,
                sourceCornerWidth,
                sourceCornerHeight),
            new Rectangle(
                bounds.Right - destinationCornerWidth,
                bounds.Y,
                destinationCornerWidth,
                destinationCornerHeight),
            color);
        DrawSlice(
            spriteBatch,
            texture,
            new Rectangle(
                0,
                sourceCornerHeight,
                sourceCornerWidth,
                sourceCenterHeight),
            new Rectangle(
                bounds.X,
                bounds.Y + destinationCornerHeight,
                destinationCornerWidth,
                destinationCenterHeight),
            color);
        DrawSlice(
            spriteBatch,
            texture,
            new Rectangle(
                sourceCornerWidth,
                sourceCornerHeight,
                sourceCenterWidth,
                sourceCenterHeight),
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
                texture.Width - sourceCornerWidth,
                sourceCornerHeight,
                sourceCornerWidth,
                sourceCenterHeight),
            new Rectangle(
                bounds.Right - destinationCornerWidth,
                bounds.Y + destinationCornerHeight,
                destinationCornerWidth,
                destinationCenterHeight),
            color);
        DrawSlice(
            spriteBatch,
            texture,
            new Rectangle(
                0,
                texture.Height - sourceCornerHeight,
                sourceCornerWidth,
                sourceCornerHeight),
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
                sourceCornerWidth,
                texture.Height - sourceCornerHeight,
                sourceCenterWidth,
                sourceCornerHeight),
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
                texture.Width - sourceCornerWidth,
                texture.Height - sourceCornerHeight,
                sourceCornerWidth,
                sourceCornerHeight),
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
        if (source is { Width: > 0, Height: > 0 }
            && destination is { Width: > 0, Height: > 0 })
        {
            spriteBatch.Draw(texture, destination, source, color);
        }
    }
}
