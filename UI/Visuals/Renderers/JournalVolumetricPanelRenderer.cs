using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace ProgressionJournal.UI.Visuals.Renderers;

internal static class JournalVolumetricPanelRenderer
{
    private const string FrameTexturePath =
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
        bool drawShadow = true)
    {
        if (bounds is not { Width: > 0, Height: > 0 })
        {
            return;
        }

        var frameTexture = ModContent.Request<Texture2D>(FrameTexturePath).Value;
        var backgroundTexture = ModContent.Request<Texture2D>(BackgroundTexturePath).Value;
        var stoneBorder = Color.Lerp(border, new Color(92, 88, 82), 0.35f);
        var stoneBackground = Color.Lerp(background, new Color(54, 52, 49), 0.25f);
        var frameTint = Color.Lerp(Color.White, stoneBorder, 0.08f);
        var backgroundTint = Color.Lerp(stoneBackground, Color.White, 0.055f);

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
            spriteBatch.Draw(
                backgroundTexture,
                inner,
                backgroundTint);
        }

        JournalNineSliceRenderer.Draw(
            spriteBatch,
            frameTexture,
            bounds,
            SourceCornerSize,
            DestinationCornerSize,
            frameTint);
    }
}
