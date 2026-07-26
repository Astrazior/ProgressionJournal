using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ProgressionJournal.UI.Visuals.Renderers;

internal static class JournalBuildItemModeToggleRenderer
{
    private const string ProfileBookTexturePath =
        "ProgressionJournal/Assets/UI/JournalButtonIcon";
    private const int OuterInset = 1;
    private const int ContentInset = 6;
    private const int IconGap = 4;

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

        var content = outer;
        content.Inflate(-ContentInset, -ContentInset);
        var iconWidth = Math.Max(1, (content.Width - IconGap) / 2);
        var leftIconBounds = new Rectangle(content.X, content.Y, iconWidth, content.Height);
        var rightIconBounds = new Rectangle(
            content.Right - iconWidth,
            content.Y,
            iconWidth,
            content.Height);

        var inactiveColor = JournalUiTheme.RootTitleText * 0.26f;
        var activeColor = hovered
            ? Color.Lerp(Color.White, new Color(180, 214, 238), 0.18f)
            : Color.White;
        var bookTexture = ModContent.Request<Texture2D>(ProfileBookTexturePath).Value;

        DrawTextureIcon(
            spriteBatch,
            leftIconBounds,
            bookTexture,
            bookTexture.Frame(),
            allItemsEnabled ? inactiveColor : activeColor,
            1f);
        DrawItemIcon(
            spriteBatch,
            rightIconBounds,
            ItemID.Chest,
            allItemsEnabled ? activeColor : inactiveColor,
            1f);
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
        DrawTextureIcon(spriteBatch, bounds, texture, frame, color, padding);
    }

    private static void DrawTextureIcon(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        Texture2D texture,
        Rectangle frame,
        Color color,
        float padding)
    {
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
