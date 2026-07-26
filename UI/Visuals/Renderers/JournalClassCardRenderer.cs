using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace ProgressionJournal.UI.Visuals.Renderers;

internal static class JournalClassCardRenderer
{
    private const string FrameTexturePath =
        "ProgressionJournal/Assets/UI/Panels/VolumetricPanelFrame";
    private const string BackgroundTexturePath =
        "ProgressionJournal/Assets/UI/Panels/VolumetricPanelBackground";
    private const string TitleShadeTexturePath =
        "ProgressionJournal/Assets/UI/Classes/ClassTitleShade";
    private const int FrameSourceCornerSize = 10;
    private const int FrameDestinationCornerSize = 10;
    private const int FrameInset = 5;
    private const int TitleShadeSourceCornerSize = 12;
    private const int TitleShadeDestinationCornerSize = 9;

    public static void Draw(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        JournalClassPalette palette,
        bool selected,
        bool hovered)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        if (selected)
        {
            bounds.Offset(0, -1);
        }

        var frameTexture = ModContent.Request<Texture2D>(FrameTexturePath).Value;
        var backgroundTexture = ModContent.Request<Texture2D>(BackgroundTexturePath).Value;

        var shadow = bounds;
        shadow.Offset(selected ? 3 : 4, selected ? 4 : 6);
        JournalNineSliceRenderer.Draw(
            spriteBatch,
            frameTexture,
            shadow,
            FrameSourceCornerSize,
            FrameDestinationCornerSize,
            JournalUiTheme.ItemSlotOuterShadow * (selected ? 0.72f : 0.9f));

        var accent = selected
            ? Color.Lerp(palette.Accent, Color.White, 0.12f)
            : hovered
                ? Color.Lerp(palette.Border, palette.Accent, 0.62f)
                : palette.Border;
        var background = selected
            ? Color.Lerp(palette.Background, palette.Accent, 0.10f)
            : hovered
                ? Color.Lerp(palette.Background, palette.Accent, 0.05f)
                : palette.Background;

        var face = bounds;
        face.Inflate(-FrameInset, -FrameInset);
        if (face is { Width: > 0, Height: > 0 })
        {
            spriteBatch.Draw(
                backgroundTexture,
                face,
                Color.Lerp(background, Color.White, selected ? 0.10f : 0.04f));
        }

        JournalNineSliceRenderer.Draw(
            spriteBatch,
            frameTexture,
            bounds,
            FrameSourceCornerSize,
            FrameDestinationCornerSize,
            Color.Lerp(Color.White, accent, selected ? 0.22f : 0.38f));

        var plate = new Rectangle(face.X + 11, face.Y + 6, face.Width - 22, 28);
        if (plate is { Width: > 0, Height: > 0 })
        {
            var plateBackground = Color.Lerp(
                palette.Background,
                Color.Black,
                selected ? 0.18f : 0.26f);
            var titleShadeTexture = ModContent.Request<Texture2D>(TitleShadeTexturePath).Value;
            JournalNineSliceRenderer.Draw(
                spriteBatch,
                titleShadeTexture,
                plate,
                TitleShadeSourceCornerSize,
                TitleShadeDestinationCornerSize,
                plateBackground * 0.88f);
        }
    }
}
