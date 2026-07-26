using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace ProgressionJournal.UI.Visuals.Renderers;

internal static class JournalProgressionToggleRenderer
{
    private const string FrameTexturePath =
        "ProgressionJournal/Assets/UI/Stages/StageControlFrame";
    private const string BackgroundTexturePath =
        "ProgressionJournal/Assets/UI/Panels/VolumetricPanelBackground";
    private const int SourceCornerSize = 16;
    private const int DestinationCornerSize = 6;
    private static readonly Asset<Texture2D> CheckTexture =
        ModContent.Request<Texture2D>("ProgressionJournal/Assets/UI/ProgressionModeCheck");

    public static void Draw(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        JournalButtonStyle style,
        bool enabled,
        bool hovered)
    {
        var background = hovered
            ? Color.Lerp(style.Background, Color.White, 0.10f)
            : style.Background;
        var accent = hovered
            ? Color.Lerp(style.Border, Color.White, 0.22f)
            : style.Border;
        var frameTexture = ModContent.Request<Texture2D>(FrameTexturePath).Value;
        var backgroundTexture = ModContent.Request<Texture2D>(BackgroundTexturePath).Value;

        var shadow = bounds;
        shadow.Offset(2, 2);
        JournalNineSliceRenderer.Draw(
            spriteBatch,
            frameTexture,
            shadow,
            SourceCornerSize,
            DestinationCornerSize,
            JournalUiTheme.ItemSlotOuterShadow * 0.76f);

        var face = bounds;
        face.Inflate(-3, -3);
        if (face is { Width: > 0, Height: > 0 })
        {
            var faceTint = enabled
                ? Color.Lerp(background, accent, 0.20f)
                : background;
            spriteBatch.Draw(backgroundTexture, face, faceTint);
        }

        JournalNineSliceRenderer.Draw(
            spriteBatch,
            frameTexture,
            bounds,
            SourceCornerSize,
            DestinationCornerSize,
            Color.Lerp(Color.White, accent, enabled ? 0.44f : 0.18f));

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
}
