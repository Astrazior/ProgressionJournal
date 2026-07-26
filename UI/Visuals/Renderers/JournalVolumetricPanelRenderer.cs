using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ProgressionJournal.UI.Visuals.Renderers;

internal static class JournalVolumetricPanelRenderer
{
    private const string DefaultFrameTexturePath =
        "ProgressionJournal/Assets/UI/Panels/VolumetricPanelFrame";
    private const string BackgroundTexturePath =
        "ProgressionJournal/Assets/UI/Panels/VolumetricPanelBackground";
    private const int SourceCornerSize = 10;
    private const int DestinationCornerSize = 10;
    private const int FrameInset = 4;
    private const int ShadowOffsetX = 4;
    private const int ShadowOffsetY = 5;

    public static void Draw(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        Color background,
        Color border,
        bool drawShadow = true,
        bool preservePalette = false,
        string? frameTextureOverridePath = null,
        string? backgroundTextureOverridePath = null)
    {
        if (bounds is not { Width: > 0, Height: > 0 })
        {
            return;
        }

        var frameTexture = ModContent.Request<Texture2D>(
            frameTextureOverridePath ?? DefaultFrameTexturePath).Value;
        var backgroundTexture = backgroundTextureOverridePath is null
            ? ModContent.Request<Texture2D>(BackgroundTexturePath).Value
            : Main.Assets.Request<Texture2D>(backgroundTextureOverridePath).Value;
        var stoneBorder = Color.Lerp(border, new Color(92, 88, 82), 0.35f);
        var stoneBackground = Color.Lerp(background, new Color(54, 52, 49), 0.25f);
        var opaqueBorder = new Color(border.R, border.G, border.B);
        var frameTint = preservePalette
            ? opaqueBorder
            : Color.Lerp(Color.White, stoneBorder, 0.08f);
        var backgroundTint = preservePalette
            ? background
            : Color.Lerp(stoneBackground, Color.White, 0.055f);

        if (drawShadow)
        {
            var shadowBounds = bounds;
            shadowBounds.Offset(ShadowOffsetX, ShadowOffsetY);
            JournalNineSliceRenderer.Draw(
                spriteBatch,
                frameTexture,
                shadowBounds,
                SourceCornerSize,
                DestinationCornerSize,
                JournalUiTheme.ItemSlotOuterShadow * 0.78f);
        }

        var inner = bounds;
        inner.Inflate(-FrameInset, -FrameInset);
        if (inner is { Width: > 0, Height: > 0 })
        {
            if (backgroundTextureOverridePath is null)
            {
                spriteBatch.Draw(
                    backgroundTexture,
                    inner,
                    backgroundTint);
            }
            else
            {
                DrawCroppedBackground(spriteBatch, backgroundTexture, inner);
            }
        }

        JournalNineSliceRenderer.Draw(
            spriteBatch,
            frameTexture,
            bounds,
            SourceCornerSize,
            DestinationCornerSize,
            frameTint);
    }

    private static void DrawCroppedBackground(SpriteBatch spriteBatch, Texture2D texture, Rectangle destination)
    {
        var source = new Rectangle(0, 0, texture.Width, texture.Height);
        var sourceAspectRatio = (float)source.Width / source.Height;
        var destinationAspectRatio = (float)destination.Width / destination.Height;

        if (sourceAspectRatio > destinationAspectRatio)
        {
            source.Width = Math.Max(1, (int)MathF.Round(source.Height * destinationAspectRatio));
            source.X = (texture.Width - source.Width) / 2;
        }
        else if (sourceAspectRatio < destinationAspectRatio)
        {
            source.Height = Math.Max(1, (int)MathF.Round(source.Width / destinationAspectRatio));
            source.Y = (texture.Height - source.Height) / 2;
        }

        spriteBatch.Draw(texture, destination, source, Color.White);
    }
}
