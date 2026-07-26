using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace ProgressionJournal.UI.Visuals.Renderers;

internal static class JournalStageButtonRenderer
{
    private const string FrameTexturePath =
        "ProgressionJournal/Assets/UI/Stages/StageControlFrame";
    private const string BackgroundTexturePath =
        "ProgressionJournal/Assets/UI/Panels/VolumetricPanelBackground";
    private const int SourceCornerSize = 16;
    private const int DestinationCornerSize = 9;
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

        if (active)
        {
            bounds.Offset(0, -1);
        }

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

        var frameTexture = ModContent.Request<Texture2D>(FrameTexturePath).Value;
        var backgroundTexture = ModContent.Request<Texture2D>(BackgroundTexturePath).Value;

        var shadow = bounds;
        shadow.Offset(active ? 2 : 3, active ? 3 : 4);
        JournalNineSliceRenderer.Draw(
            spriteBatch,
            frameTexture,
            shadow,
            SourceCornerSize,
            DestinationCornerSize,
            JournalUiTheme.ItemSlotOuterShadow * (active ? 0.70f : 0.84f));

        var face = bounds;
        face.Inflate(-FaceInset, -FaceInset);
        if (face is { Width: > 0, Height: > 0 })
        {
            var faceTint = active
                ? Color.Lerp(background, Color.White, 0.22f)
                : background;
            spriteBatch.Draw(backgroundTexture, face, faceTint);
        }

        var frameTintStrength = active ? 0f : 0.16f;
        var frameTint = Color.Lerp(Color.White, accent, frameTintStrength);
        if (!interactable)
        {
            frameTint *= 0.58f;
        }

        JournalNineSliceRenderer.Draw(
            spriteBatch,
            frameTexture,
            bounds,
            SourceCornerSize,
            DestinationCornerSize,
            frameTint);

        if (active && face is { Width: > 0, Height: > 0 })
        {
            var selectedFaceTint = Color.Lerp(background, Color.White, 0.42f);
            spriteBatch.Draw(backgroundTexture, face, selectedFaceTint);
        }
    }
}
