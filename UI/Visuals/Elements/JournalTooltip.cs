using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace ProgressionJournal.UI.Visuals.Elements;

internal static class JournalTooltip
{
    private const float MaxWidth = 360f;
    private const float TextScale = 1f;
    private const int Padding = 10;
    private const int Offset = 16;
    private const int FramedTextPadding = 12;
    private const int FramedTextGap = 8;

    private static string? _pendingText;
    private static string? _pendingFramedText;

    public static void Request(string? text, string? framedText = null)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        _pendingText = text;
        _pendingFramedText = framedText;
    }

    public static void Clear()
    {
        _pendingText = null;
        _pendingFramedText = null;
    }

    public static bool DrawPending(SpriteBatch spriteBatch)
    {
        if (string.IsNullOrWhiteSpace(_pendingText))
        {
            return true;
        }

        Draw(spriteBatch, _pendingText, _pendingFramedText);
        _pendingText = null;
        _pendingFramedText = null;
        return true;
    }

    private static void Draw(SpriteBatch spriteBatch, string text, string? framedText)
    {
        var mainText = CreateTextBlock(text, MaxWidth);
        if (mainText.Snippets.Length == 0)
        {
            return;
        }

        var framedTextMaxWidth = MaxWidth - FramedTextPadding * 2;
        var framedTextBlock = string.IsNullOrWhiteSpace(framedText)
            ? TextBlock.Empty
            : CreateTextBlock(framedText, framedTextMaxWidth);
        var framedTextWidth = framedTextBlock.Snippets.Length == 0
            ? 0f
            : framedTextBlock.Size.X + FramedTextPadding * 2;
        var contentWidth = MathF.Max(mainText.Size.X, framedTextWidth);
        var width = (int)MathF.Ceiling(contentWidth) + Padding * 2;
        var mainTextHeight = mainText.Size.Y;
        var framedTextHeight = framedTextBlock.Snippets.Length == 0
            ? 0f
            : framedTextBlock.Size.Y + FramedTextPadding * 2;
        var framedSectionHeight = framedTextBlock.Snippets.Length == 0
            ? 0f
            : FramedTextGap + framedTextHeight;
        var height = (int)MathF.Ceiling(mainTextHeight + framedSectionHeight) + Padding * 2;
        var mouse = Main.MouseScreen;
        var x = mouse.X + Offset;
        var y = mouse.Y + Offset;

        if (x + width + Offset > Main.screenWidth)
        {
            x = mouse.X - width - Offset;
        }

        if (y + height + Offset > Main.screenHeight)
        {
            y = mouse.Y - height - Offset;
        }

        x = MathHelper.Clamp(x, Offset, Math.Max(Offset, Main.screenWidth - width - Offset));
        y = MathHelper.Clamp(y, Offset, Math.Max(Offset, Main.screenHeight - height - Offset));

        JournalSourceCardRenderer.DrawTooltip(
            spriteBatch,
            new Rectangle((int)x, (int)y, width, height),
            JournalUiTheme.PresetPanelBorder);

        DrawTextBlock(
            spriteBatch,
            mainText,
            new Vector2(x + Padding, y + Padding),
            MaxWidth);

        if (framedTextBlock.Snippets.Length == 0)
        {
            return;
        }

        var framedBounds = new Rectangle(
            (int)x + Padding,
            (int)MathF.Ceiling(y + Padding + mainTextHeight + FramedTextGap),
            width - Padding * 2,
            (int)MathF.Ceiling(framedTextHeight));
        JournalSourceCardRenderer.Draw(
            spriteBatch,
            framedBounds,
            JournalUiTheme.GetCategoryStyle(JournalItemCategory.Armor).Border,
            highlighted: true);

        DrawTextBlock(
            spriteBatch,
            framedTextBlock,
            new Vector2(
                framedBounds.X + FramedTextPadding,
                framedBounds.Y + FramedTextPadding),
            framedTextMaxWidth);
    }

    private static TextBlock CreateTextBlock(string text, float maxWidth)
    {
        var snippets = ChatManager
            .ParseMessage(text, JournalUiTheme.ContentDescriptionText)
            .ToArray();
        ChatManager.ConvertNormalSnippets(snippets);
        var size = ChatManager.GetStringSize(
            FontAssets.MouseText.Value,
            snippets,
            new Vector2(TextScale),
            maxWidth);
        return new TextBlock(snippets, size);
    }

    private static void DrawTextBlock(
        SpriteBatch spriteBatch,
        TextBlock textBlock,
        Vector2 position,
        float maxWidth)
    {
        ChatManager.DrawColorCodedStringWithShadow(
            spriteBatch,
            FontAssets.MouseText.Value,
            textBlock.Snippets,
            position,
            0f,
            Vector2.Zero,
            new Vector2(TextScale),
            out _,
            maxWidth,
            2f);
    }

    private readonly record struct TextBlock(TextSnippet[] Snippets, Vector2 Size)
    {
        public static TextBlock Empty { get; } = new([], Vector2.Zero);
    }
}
