using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalRecommendationHeader(string title, string? hoverText = null) : UIElement
{
    private const float HelpScale = 0.82f;
    private const float HelpRightPadding = 7f;
    private const float TooltipMaxWidth = 360f;
    private const float TooltipTextScale = 1f;
    private const int TooltipPadding = 10;
    private const int TooltipOffset = 16;

    private static string? _pendingTooltip;

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        var dimensions = GetInnerDimensions();
        var font = FontAssets.MouseText.Value;
        const float textScale = 1.18f;

        var titleSize = font.MeasureString(title) * textScale;
        var centerX = dimensions.X + dimensions.Width * 0.5f;
        var textX = centerX - titleSize.X * 0.5f;
        var textY = dimensions.Y + (dimensions.Height - titleSize.Y) * 0.5f - 1f;

        Utils.DrawBorderStringFourWay(
            spriteBatch,
            font,
            title,
            textX,
            textY,
            JournalUiTheme.SectionHeaderText,
            Color.Black * 0.7f,
            Vector2.Zero,
            textScale);

        if (string.IsNullOrWhiteSpace(hoverText))
        {
            return;
        }

        const string helpText = "?";
        var helpSize = font.MeasureString(helpText) * HelpScale;
        var helpX = dimensions.X + dimensions.Width - helpSize.X - HelpRightPadding;
        var helpY = dimensions.Y + (dimensions.Height - helpSize.Y) * 0.5f;
        var helpBounds = new Rectangle(
            (int)helpX - 3,
            (int)helpY - 2,
            (int)helpSize.X + 6,
            (int)helpSize.Y + 4);
        var hovered = helpBounds.Contains(Main.MouseScreen.ToPoint());

        Utils.DrawBorderStringFourWay(
            spriteBatch,
            font,
            helpText,
            helpX,
            helpY,
            hovered ? Color.White : JournalUiTheme.SectionHeaderText * 0.8f,
            Color.Black * 0.7f,
            Vector2.Zero,
            HelpScale);

        if (hovered)
        {
            _pendingTooltip = hoverText;
        }
    }

    internal static void ClearPendingTooltip()
    {
        _pendingTooltip = null;
    }

    internal static bool DrawPendingTooltip(SpriteBatch spriteBatch)
    {
        if (string.IsNullOrWhiteSpace(_pendingTooltip))
        {
            return true;
        }

        DrawTooltip(spriteBatch, FontAssets.MouseText.Value, _pendingTooltip);
        _pendingTooltip = null;
        return true;
    }

    private static void DrawTooltip(SpriteBatch spriteBatch, DynamicSpriteFont font, string text)
    {
        var lines = JournalTextUtilities.WrapToPixelWidth(
            text,
            TooltipMaxWidth,
            TooltipTextScale);
        var lineHeight = font.LineSpacing * TooltipTextScale;
        var textWidth = lines.Max(line => font.MeasureString(line).X * TooltipTextScale);
        var width = (int)MathF.Ceiling(textWidth) + TooltipPadding * 2;
        var height = (int)MathF.Ceiling(lineHeight * lines.Count) + TooltipPadding * 2;
        var mouse = Main.MouseScreen;
        var viewportWidth = Main.screenWidth;
        var viewportHeight = Main.screenHeight;
        var x = mouse.X + TooltipOffset;
        var y = mouse.Y + TooltipOffset;

        if (x + width + TooltipOffset > viewportWidth)
        {
            x = mouse.X - width - TooltipOffset;
        }

        if (y + height + TooltipOffset > viewportHeight)
        {
            y = mouse.Y - height - TooltipOffset;
        }

        x = MathHelper.Clamp(x, TooltipOffset, Math.Max(TooltipOffset, viewportWidth - width - TooltipOffset));
        y = MathHelper.Clamp(y, TooltipOffset, Math.Max(TooltipOffset, viewportHeight - height - TooltipOffset));

        JournalSourceCard.DrawBackground(
            spriteBatch,
            new Rectangle((int)x, (int)y, width, height),
            JournalUiTheme.PresetPanelBorder);

        for (var index = 0; index < lines.Count; index++)
        {
            Utils.DrawBorderStringFourWay(
                spriteBatch,
                font,
                lines[index],
                x + TooltipPadding,
                y + TooltipPadding + index * lineHeight,
                JournalUiTheme.ContentDescriptionText,
                Color.Black * 0.75f,
                Vector2.Zero,
                TooltipTextScale);
        }
    }
}

