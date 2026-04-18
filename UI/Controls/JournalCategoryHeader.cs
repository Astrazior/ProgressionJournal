using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public enum JournalCategoryHeaderStyle
{
    InlineRule,
    AccentTag,
    SideRail
}

public sealed class JournalCategoryHeader : UIElement
{
    private const float TextScale = 0.76f;
    private const float LeftPadding = 2f;
    private const float RightPadding = 12f;
    private readonly string _title;
    private readonly Color _accentColor;
    private readonly Color _textColor;
    private readonly JournalCategoryHeaderStyle _style;

    public JournalCategoryHeader(string title, Color accentColor, Color textColor, JournalCategoryHeaderStyle style)
    {
        _title = title;
        _accentColor = accentColor;
        _textColor = textColor;
        _style = style;
        IgnoresMouseInteraction = true;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        var dimensions = GetDimensions();
        var pixel = TextureAssets.MagicPixel.Value;
        var font = FontAssets.MouseText.Value;
        var titleSize = font.MeasureString(_title) * TextScale;

        switch (_style)
        {
            case JournalCategoryHeaderStyle.AccentTag:
                DrawAccentTag(spriteBatch, pixel, font, dimensions, titleSize);
                return;

            case JournalCategoryHeaderStyle.SideRail:
                DrawSideRail(spriteBatch, pixel, font, dimensions, titleSize);
                return;

            case JournalCategoryHeaderStyle.InlineRule:
            default:
                DrawInlineRule(spriteBatch, font, dimensions, titleSize);
                return;
        }
    }

    private void DrawInlineRule(SpriteBatch spriteBatch, DynamicSpriteFont font, CalculatedStyle dimensions, Vector2 titleSize)
    {
        var textX = dimensions.X + LeftPadding;
        var textY = dimensions.Y + (dimensions.Height - titleSize.Y) * 0.5f - 1f;
        DrawText(spriteBatch, font, textX, textY);
    }

    private void DrawAccentTag(SpriteBatch spriteBatch, Texture2D pixel, DynamicSpriteFont font, CalculatedStyle dimensions, Vector2 titleSize)
    {
        var centerY = dimensions.Y + dimensions.Height * 0.5f;
        var tagX = dimensions.X + LeftPadding;
        var tagY = centerY - titleSize.Y * 0.5f - 4f;
        var tagWidth = titleSize.X + 18f;
        var tagHeight = titleSize.Y + 8f;
        var textX = tagX + 9f;
        var textY = tagY + 4f - 1f;
        var lineStart = tagX + tagWidth + 12f;
        var lineEnd = dimensions.X + dimensions.Width - RightPadding;
        var lineY = (int)(centerY + 1f);

        DrawRectangle(spriteBatch, pixel, tagX, tagY, tagWidth, tagHeight, _accentColor * 0.16f);
        DrawRectangle(spriteBatch, pixel, tagX, tagY, 4f, tagHeight, _accentColor * 0.92f);
        DrawSegment(spriteBatch, pixel, tagX + 4f, tagX + tagWidth, (int)tagY, 1, _accentColor * 0.56f);
        DrawSegment(spriteBatch, pixel, tagX + 4f, tagX + tagWidth, (int)(tagY + tagHeight - 1f), 1, _accentColor * 0.32f);
        DrawSegment(spriteBatch, pixel, lineStart, lineEnd, lineY, 1, _accentColor * 0.34f);
        DrawText(spriteBatch, font, textX, textY);
    }

    private void DrawSideRail(SpriteBatch spriteBatch, Texture2D pixel, DynamicSpriteFont font, CalculatedStyle dimensions, Vector2 titleSize)
    {
        var centerY = dimensions.Y + dimensions.Height * 0.5f;
        var railX = dimensions.X + LeftPadding;
        var railY = dimensions.Y + 2f;
        var railHeight = dimensions.Height - 4f;
        var textX = railX + 12f;
        var textY = dimensions.Y + (dimensions.Height - titleSize.Y) * 0.5f - 1f;
        var underlineEnd = textX + titleSize.X + 2f;
        var underlineY = (int)(centerY + titleSize.Y * 0.5f) + 1;

        DrawRectangle(spriteBatch, pixel, railX, railY, 4f, railHeight, _accentColor);
        DrawRectangle(spriteBatch, pixel, railX, railY, 12f, 1f, _accentColor * 0.48f);
        DrawRectangle(spriteBatch, pixel, railX, railY + railHeight - 1f, 12f, 1f, _accentColor * 0.28f);
        DrawSegment(spriteBatch, pixel, textX, underlineEnd, underlineY, 1, _accentColor * 0.34f);
        DrawText(spriteBatch, font, textX, textY);
    }

    private void DrawText(SpriteBatch spriteBatch, DynamicSpriteFont font, float x, float y)
    {
        Utils.DrawBorderStringFourWay(
            spriteBatch,
            font,
            _title,
            x,
            y,
            _textColor,
            Color.Black * 0.65f,
            Vector2.Zero,
            TextScale);
    }

    private static void DrawSegment(SpriteBatch spriteBatch, Texture2D pixel, float startX, float endX, int y, int thickness, Color color)
    {
        var width = (int)(endX - startX);
        if (width <= 0 || thickness <= 0)
        {
            return;
        }

        spriteBatch.Draw(pixel, new Rectangle((int)startX, y, width, thickness), color);
    }

    private static void DrawRectangle(SpriteBatch spriteBatch, Texture2D pixel, float x, float y, float width, float height, Color color)
    {
        if (width <= 0f || height <= 0f)
        {
            return;
        }

        spriteBatch.Draw(pixel, new Rectangle((int)x, (int)y, (int)width, (int)height), color);
    }
}

