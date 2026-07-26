using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace ProgressionJournal.UI.Visuals.Elements;

public sealed class JournalSourceSectionHeader : UIElement
{
    private const string DividerTexturePath =
        "ProgressionJournal/Assets/UI/Sources/SourceHeaderDivider";
    private const int DividerLeftCapWidth = 6;
    private const int DividerRightCapWidth = 22;
    private const float IconSize = 30f;
    private const float TextScale = 0.7f;

    private readonly string _title;
    private readonly int _iconItemId;
    private readonly Color _accent;

    public JournalSourceSectionHeader(string title, int iconItemId, Color accent)
    {
        _title = title;
        _iconItemId = iconItemId;
        _accent = accent;
        Height.Set(42f, 0f);
        IgnoresMouseInteraction = true;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        var bounds = GetDimensions();
        var font = FontAssets.MouseText.Value;
        var textSize = font.MeasureString(_title) * TextScale;
        var iconCenter = new Vector2(bounds.X + IconSize * 0.5f + 2f, bounds.Y + bounds.Height * 0.5f);

        if (JournalItemUtilities.TryCreateItem(_iconItemId, out var item))
        {
            Main.instance.LoadItem(item.type);
            var texture = TextureAssets.Item[item.type].Value;
            var source = Main.itemAnimations[item.type]?.GetFrame(texture) ?? texture.Bounds;
            var scale = MathF.Min(IconSize / source.Width, IconSize / source.Height);
            spriteBatch.Draw(texture, iconCenter, source, Color.White, 0f, source.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }

        var textX = bounds.X + IconSize + 12f;
        var textY = bounds.Y + (bounds.Height - textSize.Y) * 0.5f + 1f;
        Utils.DrawBorderStringFourWay(
            spriteBatch,
            font,
            _title,
            textX,
            textY,
            Color.Lerp(JournalUiTheme.RootTitleText, _accent, 0.24f),
            Color.Black * 0.7f,
            Vector2.Zero,
            TextScale);

        var lineX = textX + textSize.X + 10f;
        var lineWidth = Math.Max(0, (int)(bounds.X + bounds.Width - lineX - 5f));
        if (lineWidth > 0)
        {
            var dividerTexture = ModContent.Request<Texture2D>(DividerTexturePath).Value;
            DrawDivider(
                spriteBatch,
                dividerTexture,
                new Rectangle(
                    (int)lineX,
                    (int)(bounds.Y + bounds.Height * 0.5f - dividerTexture.Height * 0.5f),
                    lineWidth,
                    dividerTexture.Height));
        }
    }

    private static void DrawDivider(
        SpriteBatch spriteBatch,
        Texture2D texture,
        Rectangle destination)
    {
        var leftWidth = Math.Min(DividerLeftCapWidth, destination.Width);
        var remainingWidth = Math.Max(0, destination.Width - leftWidth);
        var rightWidth = Math.Min(DividerRightCapWidth, remainingWidth);
        var middleWidth = remainingWidth - rightWidth;
        var sourceMiddleWidth = texture.Width
                                - DividerLeftCapWidth
                                - DividerRightCapWidth;

        DrawDividerSlice(
            spriteBatch,
            texture,
            new Rectangle(0, 0, DividerLeftCapWidth, texture.Height),
            new Rectangle(
                destination.X,
                destination.Y,
                leftWidth,
                destination.Height));
        DrawDividerSlice(
            spriteBatch,
            texture,
            new Rectangle(
                DividerLeftCapWidth,
                0,
                sourceMiddleWidth,
                texture.Height),
            new Rectangle(
                destination.X + leftWidth,
                destination.Y,
                middleWidth,
                destination.Height));
        DrawDividerSlice(
            spriteBatch,
            texture,
            new Rectangle(
                texture.Width - DividerRightCapWidth,
                0,
                DividerRightCapWidth,
                texture.Height),
            new Rectangle(
                destination.Right - rightWidth,
                destination.Y,
                rightWidth,
                destination.Height));
    }

    private static void DrawDividerSlice(
        SpriteBatch spriteBatch,
        Texture2D texture,
        Rectangle source,
        Rectangle destination)
    {
        if (source is { Width: > 0, Height: > 0 }
            && destination is { Width: > 0, Height: > 0 })
        {
            spriteBatch.Draw(texture, destination, source, Color.White);
        }
    }
}
