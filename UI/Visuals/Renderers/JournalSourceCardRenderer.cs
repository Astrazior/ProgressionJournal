using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace ProgressionJournal.UI.Visuals.Renderers;

internal static class JournalSourceCardRenderer
{
    private const string FrameTexturePath =
        "ProgressionJournal/Assets/UI/Sources/SourceCardFrame";
    private const string BackgroundTexturePath =
        "ProgressionJournal/Assets/UI/Panels/VolumetricPanelBackground";
    private const int SourceCornerSize = 32;
    private const int DestinationCornerSize = 22;
    private const int BackgroundInset = 6;

    public static void Draw(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        Color accent,
        bool highlighted = false)
    {
        var background = Color.Lerp(
            JournalUiTheme.RootBackground,
            JournalUiTheme.PanelBackground,
            0.34f);
        if (highlighted)
        {
            background = Color.Lerp(background, accent, 0.08f);
        }

        var inner = bounds;
        inner.Inflate(-BackgroundInset, -BackgroundInset);
        if (inner is { Width: > 0, Height: > 0 })
        {
            var backgroundTexture = ModContent.Request<Texture2D>(
                BackgroundTexturePath).Value;
            spriteBatch.Draw(backgroundTexture, inner, background);
        }

        var frameTexture = ModContent.Request<Texture2D>(
            FrameTexturePath).Value;
        var frameTint = highlighted
            ? Color.Lerp(Color.White, accent, 0.06f)
            : Color.White;
        JournalNineSliceRenderer.Draw(
            spriteBatch,
            frameTexture,
            bounds,
            SourceCornerSize,
            DestinationCornerSize,
            frameTint);
    }
}
