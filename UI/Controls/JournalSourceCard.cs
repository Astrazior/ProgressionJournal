using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalSourceCard : UIPanel
{
    private const string BestiaryPanelTexturePath = "Images/UI/Bestiary/Stat_Panel";

    private readonly Color _accent;

    public JournalSourceCard(Color accent)
    {
        _accent = accent;
        SetPadding(0f);
        BackgroundColor = Color.Transparent;
        BorderColor = Color.Transparent;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        var bounds = GetDimensions().ToRectangle();
        var texture = Main.Assets.Request<Texture2D>(BestiaryPanelTexturePath).Value;
        var darkBackground = Color.Lerp(
            JournalUiTheme.RootBackground,
            JournalUiTheme.PanelBackground,
            0.34f);
        var tint = IsMouseHovering
            ? Color.Lerp(darkBackground, _accent, 0.10f)
            : darkBackground;
        DrawNineSlice(spriteBatch, texture, bounds, tint);
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
