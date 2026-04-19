using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public enum JournalSourceTokenKind
{
    Item,
    Npc,
    Bestiary
}

public readonly record struct JournalSourceTokenData(JournalSourceTokenKind Kind, int Value, string HoverText);

public sealed class JournalSourceToken : UIElement
{
    private const string BestiaryFilterIconTexturePath = "Images/UI/Bestiary/Icon_Tags_Shadow";
    private const int BestiaryFilterIconColumns = 16;
    private const int BestiaryFilterIconRows = 5;
    private const float InnerPadding = 4f;

    private readonly JournalSourceTokenData _data;

    public JournalSourceToken(JournalSourceTokenData data)
    {
        _data = data;
        var tokenSize = GetTokenSize(data);
        Width.Set(tokenSize, 0f);
        Height.Set(tokenSize, 0f);
    }

    public static float TokenSize => 34f;

    public static float NpcTokenSize => 46f;

    public static float GetTokenSize(JournalSourceTokenData data)
    {
        return data.Kind == JournalSourceTokenKind.Npc ? NpcTokenSize : TokenSize;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        var bounds = GetDimensions().ToRectangle();
        var texture = TextureAssets.MagicPixel.Value;
        var background = IsMouseHovering
            ? Color.Lerp(JournalUiTheme.PanelBackground, Color.White, 0.16f)
            : JournalUiTheme.PanelBackground;
        var border = IsMouseHovering
            ? Color.Lerp(JournalUiTheme.PanelBorder, Color.White, 0.28f)
            : JournalUiTheme.PanelBorder;

        spriteBatch.Draw(texture, bounds, background);
        DrawOutline(spriteBatch, texture, bounds, border);

        var inner = bounds;
        inner.Inflate((int)-InnerPadding, (int)-InnerPadding);
        if (inner.Width <= 0 || inner.Height <= 0)
        {
            return;
        }

        switch (_data.Kind)
        {
            case JournalSourceTokenKind.Item:
                DrawItem(spriteBatch, inner);
                break;
            case JournalSourceTokenKind.Npc:
                DrawNpc(spriteBatch, inner);
                break;
            case JournalSourceTokenKind.Bestiary:
                DrawBestiaryIcon(spriteBatch, inner);
                break;
        }

        if (!IsMouseHovering || string.IsNullOrWhiteSpace(_data.HoverText))
        {
            return;
        }

        if (_data.Kind == JournalSourceTokenKind.Item)
        {
            var hoverItem = JournalItemUtilities.CreateItem(_data.Value);
            Main.HoverItem = hoverItem;
            Main.hoverItemName = hoverItem.HoverName;
            return;
        }

        Main.hoverItemName = _data.HoverText;
    }

    private void DrawItem(SpriteBatch spriteBatch, Rectangle inner)
    {
        var item = JournalItemUtilities.CreateItem(_data.Value);
        Main.instance.LoadItem(item.type);

        var itemTexture = TextureAssets.Item[item.type].Value;
        var sourceRectangle = Main.itemAnimations[item.type]?.GetFrame(itemTexture) ?? itemTexture.Bounds;
        DrawTexture(spriteBatch, itemTexture, sourceRectangle, inner, anchorBottom: false);
    }

    private void DrawNpc(SpriteBatch spriteBatch, Rectangle inner)
    {
        if (_data.Value <= NPCID.None || _data.Value >= TextureAssets.Npc.Length)
        {
            return;
        }

        Main.instance.LoadNPC(_data.Value);
        var npcTexture = TextureAssets.Npc[_data.Value].Value;
        var frameCount = Math.Max(1, Main.npcFrameCount[_data.Value]);
        var frameHeight = npcTexture.Height / frameCount;
        var frameIndex = frameCount == 1 ? 0 : (int)(Main.GameUpdateCount / 10 % frameCount);
        var sourceRectangle = new Rectangle(0, frameIndex * frameHeight, npcTexture.Width, frameHeight);
        DrawTexture(spriteBatch, npcTexture, sourceRectangle, inner, anchorBottom: true);
    }

    private void DrawBestiaryIcon(SpriteBatch spriteBatch, Rectangle inner)
    {
        var iconTexture = Main.Assets.Request<Texture2D>(BestiaryFilterIconTexturePath).Value;
        var sourceRectangle = GetBestiaryFilterSourceRectangle(iconTexture, _data.Value);
        DrawTexture(spriteBatch, iconTexture, sourceRectangle, inner, anchorBottom: false);
    }

    private static void DrawTexture(SpriteBatch spriteBatch, Texture2D texture, Rectangle sourceRectangle, Rectangle inner, bool anchorBottom)
    {
        var scale = MathF.Min(inner.Width / (float)sourceRectangle.Width, inner.Height / (float)sourceRectangle.Height);
        var position = anchorBottom
            ? new Vector2(inner.Center.X, inner.Bottom)
            : inner.Center.ToVector2();
        var origin = anchorBottom
            ? new Vector2(sourceRectangle.Width * 0.5f, sourceRectangle.Height)
            : sourceRectangle.Size() * 0.5f;

        spriteBatch.Draw(
            texture,
            position,
            sourceRectangle,
            Color.White,
            0f,
            origin,
            scale,
            SpriteEffects.None,
            0f);
    }

    private static Rectangle GetBestiaryFilterSourceRectangle(Texture2D texture, int frame)
    {
        var frameWidth = texture.Width / BestiaryFilterIconColumns;
        var frameHeight = texture.Height / BestiaryFilterIconRows;
        var frameX = frame % BestiaryFilterIconColumns;
        var frameY = frame / BestiaryFilterIconColumns;

        return new Rectangle(frameX * frameWidth, frameY * frameHeight, frameWidth, frameHeight);
    }

    private static void DrawOutline(SpriteBatch spriteBatch, Texture2D texture, Rectangle rectangle, Color color)
    {
        spriteBatch.Draw(texture, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, 1), color);
        spriteBatch.Draw(texture, new Rectangle(rectangle.X, rectangle.Bottom - 1, rectangle.Width, 1), color);
        spriteBatch.Draw(texture, new Rectangle(rectangle.X, rectangle.Y, 1, rectangle.Height), color);
        spriteBatch.Draw(texture, new Rectangle(rectangle.Right - 1, rectangle.Y, 1, rectangle.Height), color);
    }
}
