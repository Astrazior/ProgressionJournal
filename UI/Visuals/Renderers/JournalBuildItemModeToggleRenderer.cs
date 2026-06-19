using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace ProgressionJournal.UI.Visuals.Renderers;

internal static class JournalBuildItemModeToggleRenderer
{
    private const int OuterInset = 1;
    private const int TrackInset = 5;
    private const int ThumbGap = 3;

    public static void Draw(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        bool allItemsEnabled,
        bool hovered)
    {
        var outerBackground = hovered
            ? Color.Lerp(JournalUiTheme.PanelBackground, Color.White, 0.08f)
            : JournalUiTheme.PanelBackground;
        var outerBorder = hovered
            ? Color.Lerp(JournalUiTheme.PanelBorder, Color.White, 0.18f)
            : JournalUiTheme.PanelBorder;

        var outer = bounds;
        outer.Inflate(-OuterInset, -OuterInset);
        JournalVolumetricPanelRenderer.Draw(spriteBatch, outer, outerBackground, outerBorder);

        var track = outer;
        track.Inflate(-TrackInset, -TrackInset);
        DrawTrack(spriteBatch, track);

        var thumbWidth = Math.Max(18, (track.Width - ThumbGap) / 2);
        var leftWell = new Rectangle(track.X, track.Y, thumbWidth, track.Height);
        var rightWell = new Rectangle(track.Right - thumbWidth, track.Y, thumbWidth, track.Height);
        DrawItemIcon(spriteBatch, leftWell, ItemID.Book, JournalUiTheme.RootTitleText * 0.24f, 8f);
        DrawItemIcon(spriteBatch, rightWell, ItemID.Chest, JournalUiTheme.RootTitleText * 0.24f, 8f);

        var thumb = new Rectangle(
            allItemsEnabled ? track.Right - thumbWidth : track.X,
            track.Y,
            thumbWidth,
            track.Height);

        var thumbBackground = allItemsEnabled
            ? new Color(43, 84, 59)
            : new Color(45, 65, 84);
        var thumbBorder = allItemsEnabled
            ? new Color(112, 184, 126)
            : new Color(112, 148, 182);

        if (hovered)
        {
            thumbBackground = Color.Lerp(thumbBackground, Color.White, 0.10f);
            thumbBorder = Color.Lerp(thumbBorder, Color.White, 0.18f);
        }

        DrawRaisedThumb(spriteBatch, thumb, thumbBackground, thumbBorder);
        DrawItemIcon(spriteBatch, thumb, allItemsEnabled ? ItemID.Chest : ItemID.Book, Color.White, 5f);
    }

    private static void DrawTrack(SpriteBatch spriteBatch, Rectangle track)
    {
        var pixel = TextureAssets.MagicPixel.Value;
        spriteBatch.Draw(pixel, track, new Color(8, 14, 20) * 0.96f);
        spriteBatch.Draw(pixel, new Rectangle(track.X, track.Y, track.Width, 2), Color.Black * 0.52f);
        spriteBatch.Draw(pixel, new Rectangle(track.X, track.Y, 2, track.Height), Color.Black * 0.38f);
        spriteBatch.Draw(pixel, new Rectangle(track.X, track.Bottom - 1, track.Width, 1), JournalUiTheme.PanelBorder * 0.32f);

        var dividerX = track.Center.X;
        spriteBatch.Draw(pixel, new Rectangle(dividerX, track.Y + 3, 1, Math.Max(1, track.Height - 6)), Color.Black * 0.42f);
    }

    private static void DrawRaisedThumb(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        Color background,
        Color border)
    {
        var pixel = TextureAssets.MagicPixel.Value;
        var shadow = bounds;
        shadow.Offset(2, 2);
        spriteBatch.Draw(pixel, shadow, Color.Black * 0.44f);

        spriteBatch.Draw(pixel, bounds, Color.Lerp(border, Color.Black, 0.20f));

        var face = bounds;
        face.Inflate(-2, -2);
        spriteBatch.Draw(pixel, face, background);
        spriteBatch.Draw(pixel, new Rectangle(face.X, face.Y, face.Width, 2), Color.Lerp(border, Color.White, 0.34f));
        spriteBatch.Draw(pixel, new Rectangle(face.X, face.Y, 2, face.Height), Color.Lerp(border, Color.White, 0.18f));
        spriteBatch.Draw(pixel, new Rectangle(face.X, face.Bottom - 2, face.Width, 2), Color.Lerp(border, Color.Black, 0.52f));
        spriteBatch.Draw(pixel, new Rectangle(face.Right - 2, face.Y, 2, face.Height), Color.Lerp(border, Color.Black, 0.34f));
    }

    private static void DrawItemIcon(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        int itemId,
        Color color,
        float padding)
    {
        Main.instance.LoadItem(itemId);
        var texture = TextureAssets.Item[itemId].Value;
        var frame = Main.itemAnimations[itemId]?.GetFrame(texture) ?? texture.Frame();
        var maxSize = Math.Max(1f, Math.Min(bounds.Width, bounds.Height) - padding);
        var scale = Math.Min(maxSize / frame.Width, maxSize / frame.Height);
        var position = new Vector2(bounds.Center.X, bounds.Center.Y);

        spriteBatch.Draw(
            texture,
            position,
            frame,
            color,
            0f,
            frame.Size() * 0.5f,
            scale,
            SpriteEffects.None,
            0f);
    }
}
