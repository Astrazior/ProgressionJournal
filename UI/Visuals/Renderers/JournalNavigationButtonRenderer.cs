using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace ProgressionJournal.UI.Visuals.Renderers;

internal static class JournalNavigationButtonRenderer
{
    private const string TabFrameTexturePath =
        "ProgressionJournal/Assets/UI/Stages/StageControlFrame";
    private const string ProfileFrameTexturePath =
        "ProgressionJournal/Assets/UI/Profiles/ProfileButtonFrame";
    private const string BackgroundTexturePath =
        "ProgressionJournal/Assets/UI/Panels/VolumetricPanelBackground";

    public static void DrawTab(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        JournalButtonStyle style,
        bool hovered)
    {
        Draw(
            spriteBatch,
            bounds,
            style,
            hovered,
            TabFrameTexturePath,
            16,
            8,
            4,
            4,
            0.78f,
            faceAfterFrame: true);
    }

    public static void DrawProfile(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        JournalButtonStyle style,
        bool hovered)
    {
        if (style == JournalUiTheme.GetTabButtonStyle(active: true))
        {
            style = style with
            {
                Background = new Color(74, 138, 86)
            };
        }
        else if (style == JournalUiTheme.GetDefaultTextButtonStyle())
        {
            style = style with
            {
                Background = new Color(28, 40, 62)
            };
        }

        Draw(
            spriteBatch,
            bounds,
            style,
            hovered,
            ProfileFrameTexturePath,
            24,
            12,
            0,
            5,
            0.92f,
            faceAfterFrame: false);
    }

    private static void Draw(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        JournalButtonStyle style,
        bool hovered,
        string frameTexturePath,
        int sourceCornerSize,
        int destinationCornerSize,
        int faceHorizontalInset,
        int faceVerticalInset,
        float frameOpacity,
        bool faceAfterFrame)
    {
        if (bounds is not { Width: > 0, Height: > 0 })
        {
            return;
        }

        var faceColor = hovered
            ? Color.Lerp(style.Background, Color.White, 0.14f)
            : style.Background;
        var frameColor = hovered
            ? Color.White
            : Color.White * frameOpacity;

        if (!faceAfterFrame)
        {
            DrawFace(
                spriteBatch,
                bounds,
                faceHorizontalInset,
                faceVerticalInset,
                faceColor);
        }

        var frameTexture = ModContent.Request<Texture2D>(frameTexturePath).Value;
        JournalNineSliceRenderer.Draw(
            spriteBatch,
            frameTexture,
            bounds,
            sourceCornerSize,
            destinationCornerSize,
            frameColor);

        if (faceAfterFrame)
        {
            DrawFace(
                spriteBatch,
                bounds,
                faceHorizontalInset,
                faceVerticalInset,
                faceColor);
        }
    }

    private static void DrawFace(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        int horizontalInset,
        int verticalInset,
        Color faceColor)
    {
        bounds.Inflate(-horizontalInset, -verticalInset);
        if (bounds is not { Width: > 0, Height: > 0 })
        {
            return;
        }

        var backgroundTexture = ModContent.Request<Texture2D>(BackgroundTexturePath).Value;
        spriteBatch.Draw(backgroundTexture, bounds, faceColor);
    }
}
