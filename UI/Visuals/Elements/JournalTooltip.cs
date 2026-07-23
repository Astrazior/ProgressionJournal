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
    private const float ArmorContentWidth = 420f;
    private const float ArmorTitleScale = 1.08f;
    private const float ArmorBodyScale = 0.94f;
    private const float ArmorSectionTitleScale = 0.88f;
    private const float ArmorDefenseValueScale = 1.18f;
    private const float ArmorDefenseValueWidth = 84f;
    private const int ArmorOuterPadding = 14;
    private const int ArmorSectionGap = 12;
    private const int ArmorInnerPadding = 10;
    private const int ArmorEffectIndent = 14;
    private const int ArmorRowGap = 6;

    private static string? _pendingText;
    private static ArmorSetTooltipContent? _pendingArmorSet;

    public static void Request(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        _pendingText = text;
        _pendingArmorSet = null;
    }

    public static void RequestArmorSet(
        string title,
        string defenseLabel,
        int totalDefense,
        string effectsTitle,
        IReadOnlyList<string> effects,
        string bonusTitle,
        string? bonusText)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        _pendingArmorSet = new ArmorSetTooltipContent(
            title.Trim(),
            defenseLabel.Trim(),
            totalDefense,
            effectsTitle.Trim(),
            effects
                .Where(static effect => !string.IsNullOrWhiteSpace(effect))
                .Select(static effect => effect.Trim())
                .ToArray(),
            bonusTitle.Trim(),
            SplitDisplayLines(bonusText));
        _pendingText = null;
    }

    public static void Clear()
    {
        _pendingText = null;
        _pendingArmorSet = null;
    }

    public static bool DrawPending(SpriteBatch spriteBatch)
    {
        if (_pendingArmorSet is { } armorSet)
        {
            DrawArmorSet(spriteBatch, armorSet);
            Clear();
            return true;
        }

        if (string.IsNullOrWhiteSpace(_pendingText))
        {
            return true;
        }

        Draw(spriteBatch, _pendingText);
        _pendingText = null;
        return true;
    }

    private static void DrawArmorSet(SpriteBatch spriteBatch, ArmorSetTooltipContent content)
    {
        var armorAccent = JournalUiTheme.GetCategoryStyle(JournalItemCategory.Armor).Border;
        var bonusAccent = JournalUiTheme.EventEntryOutline;
        var titleBlock = CreateTextBlock(
            content.Title,
            ArmorContentWidth,
            JournalUiTheme.RootTitleText,
            ArmorTitleScale);
        var defenseLabelBlock = CreateTextBlock(
            content.DefenseLabel,
            ArmorContentWidth
            - ArmorInnerPadding * 2
            - ArmorDefenseValueWidth
            - ArmorRowGap,
            JournalUiTheme.ContentDescriptionText,
            ArmorBodyScale);
        var defenseValueBlock = CreateTextBlock(
            content.TotalDefense.ToString(),
            ArmorDefenseValueWidth,
            Color.White,
            ArmorDefenseValueScale);
        var effectsTitleBlock = content.Effects.Length == 0
            ? TextBlock.Empty
            : CreateTextBlock(
                content.EffectsTitle,
                ArmorContentWidth,
                armorAccent,
                ArmorSectionTitleScale);
        var effectBlocks = content.Effects
            .Select(effect => CreateTextBlock(
                effect,
                ArmorContentWidth - ArmorEffectIndent,
                JournalUiTheme.ContentDescriptionText,
                ArmorBodyScale))
            .ToArray();
        var bonusTitleBlock = content.BonusLines.Length == 0
            ? TextBlock.Empty
            : CreateTextBlock(
                content.BonusTitle,
                ArmorContentWidth - ArmorInnerPadding * 2,
                bonusAccent,
                ArmorSectionTitleScale);
        var bonusBlocks = content.BonusLines
            .Select(line => CreateTextBlock(
                line,
                ArmorContentWidth - ArmorInnerPadding * 2 - ArmorEffectIndent,
                JournalUiTheme.PresetPanelText,
                ArmorBodyScale))
            .ToArray();

        var defenseRowHeight = Math.Max(
            38f,
            Math.Max(defenseLabelBlock.Size.Y, defenseValueBlock.Size.Y) + ArmorInnerPadding);
        var effectsHeight = effectsTitleBlock.IsEmpty
            ? 0f
            : ArmorSectionGap
              + effectsTitleBlock.Size.Y
              + ArmorRowGap
              + GetRowsHeight(effectBlocks);
        var bonusContentHeight = bonusTitleBlock.IsEmpty
            ? 0f
            : ArmorInnerPadding
              + bonusTitleBlock.Size.Y
              + ArmorRowGap
              + 1f
              + ArmorRowGap
              + GetRowsHeight(bonusBlocks)
              + ArmorInnerPadding;
        var bonusHeight = bonusTitleBlock.IsEmpty
            ? 0f
            : ArmorSectionGap + bonusContentHeight;
        var contentHeight = titleBlock.Size.Y
                            + ArmorRowGap
                            + 2f
                            + ArmorRowGap
                            + defenseRowHeight
                            + effectsHeight
                            + bonusHeight;
        var width = (int)MathF.Ceiling(ArmorContentWidth) + ArmorOuterPadding * 2;
        var height = (int)MathF.Ceiling(contentHeight) + ArmorOuterPadding * 2;
        var bounds = ResolveBounds(width, height);

        DrawTooltipShadow(spriteBatch, bounds);
        JournalSourceCardRenderer.Draw(spriteBatch, bounds, armorAccent);

        var contentX = bounds.X + ArmorOuterPadding;
        var cursorY = (float)(bounds.Y + ArmorOuterPadding);
        DrawTextBlock(
            spriteBatch,
            titleBlock,
            new Vector2(
                contentX + (ArmorContentWidth - titleBlock.Size.X) * 0.5f,
                cursorY),
            ArmorContentWidth);
        cursorY += titleBlock.Size.Y + ArmorRowGap;

        DrawHorizontalRule(
            spriteBatch,
            new Rectangle(contentX, (int)MathF.Ceiling(cursorY), (int)ArmorContentWidth, 2),
            armorAccent);
        cursorY += 2f + ArmorRowGap;

        var defenseBounds = new Rectangle(
            contentX,
            (int)MathF.Ceiling(cursorY),
            (int)ArmorContentWidth,
            (int)MathF.Ceiling(defenseRowHeight));
        DrawInsetPanel(spriteBatch, defenseBounds, armorAccent, 0.10f);
        DrawTextBlock(
            spriteBatch,
            defenseLabelBlock,
            new Vector2(
                defenseBounds.X + ArmorInnerPadding,
                defenseBounds.Center.Y - defenseLabelBlock.Size.Y * 0.5f),
            ArmorContentWidth - ArmorInnerPadding * 2);
        DrawTextBlock(
            spriteBatch,
            defenseValueBlock,
            new Vector2(
                defenseBounds.Right - ArmorInnerPadding - defenseValueBlock.Size.X,
                defenseBounds.Center.Y - defenseValueBlock.Size.Y * 0.5f),
            ArmorDefenseValueWidth);
        cursorY += defenseRowHeight;

        if (!effectsTitleBlock.IsEmpty)
        {
            cursorY += ArmorSectionGap;
            DrawSectionTitle(
                spriteBatch,
                effectsTitleBlock,
                new Vector2(contentX, cursorY),
                ArmorContentWidth,
                armorAccent);
            cursorY += effectsTitleBlock.Size.Y + ArmorRowGap;

            foreach (var effectBlock in effectBlocks)
            {
                DrawEffectRow(
                    spriteBatch,
                    effectBlock,
                    new Vector2(contentX, cursorY),
                    ArmorContentWidth,
                    armorAccent);
                cursorY += effectBlock.Size.Y + ArmorRowGap;
            }

            cursorY -= ArmorRowGap;
        }

        if (bonusTitleBlock.IsEmpty)
        {
            return;
        }

        cursorY += ArmorSectionGap;
        var bonusBounds = new Rectangle(
            contentX,
            (int)MathF.Ceiling(cursorY),
            (int)ArmorContentWidth,
            (int)MathF.Ceiling(bonusContentHeight));
        DrawInsetPanel(spriteBatch, bonusBounds, bonusAccent, 0.12f);
        var bonusX = bonusBounds.X + ArmorInnerPadding;
        var bonusY = (float)(bonusBounds.Y + ArmorInnerPadding);
        DrawTextBlock(
            spriteBatch,
            bonusTitleBlock,
            new Vector2(bonusX, bonusY),
            ArmorContentWidth - ArmorInnerPadding * 2);
        bonusY += bonusTitleBlock.Size.Y + ArmorRowGap;
        DrawHorizontalRule(
            spriteBatch,
            new Rectangle(
                bonusX,
                (int)MathF.Ceiling(bonusY),
                bonusBounds.Width - ArmorInnerPadding * 2,
                1),
            bonusAccent * 0.72f);
        bonusY += 1f + ArmorRowGap;

        foreach (var bonusBlock in bonusBlocks)
        {
            DrawEffectRow(
                spriteBatch,
                bonusBlock,
                new Vector2(bonusX, bonusY),
                bonusBounds.Width - ArmorInnerPadding * 2,
                bonusAccent);
            bonusY += bonusBlock.Size.Y + ArmorRowGap;
        }
    }

    private static void Draw(SpriteBatch spriteBatch, string text)
    {
        var mainText = CreateTextBlock(text, MaxWidth);
        if (mainText.Snippets.Length == 0)
        {
            return;
        }

        var width = (int)MathF.Ceiling(mainText.Size.X) + Padding * 2;
        var height = (int)MathF.Ceiling(mainText.Size.Y) + Padding * 2;
        var bounds = ResolveBounds(width, height);

        JournalSourceCardRenderer.DrawTooltip(
            spriteBatch,
            bounds,
            JournalUiTheme.PresetPanelBorder);

        DrawTextBlock(
            spriteBatch,
            mainText,
            new Vector2(bounds.X + Padding, bounds.Y + Padding),
            MaxWidth);
    }

    private static Rectangle ResolveBounds(int width, int height)
    {
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
        return new Rectangle((int)x, (int)y, width, height);
    }

    private static float GetRowsHeight(IReadOnlyList<TextBlock> rows)
    {
        if (rows.Count == 0)
        {
            return 0f;
        }

        return rows.Sum(static row => row.Size.Y) + ArmorRowGap * (rows.Count - 1);
    }

    private static string[] SplitDisplayLines(string? text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? []
            : text
                .Replace("\r", string.Empty)
                .Split(
                    '\n',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static void DrawTooltipShadow(SpriteBatch spriteBatch, Rectangle bounds)
    {
        var shadow = bounds;
        shadow.Offset(5, 6);
        shadow.Inflate(2, 2);
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, shadow, Color.Black * 0.48f);
    }

    private static void DrawInsetPanel(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        Color accent,
        float accentStrength)
    {
        var pixel = TextureAssets.MagicPixel.Value;
        var background = Color.Lerp(JournalUiTheme.RootBackground, accent, accentStrength);
        spriteBatch.Draw(pixel, bounds, background * 0.96f);
        DrawBorder(spriteBatch, bounds, 1, Color.Lerp(accent, Color.Black, 0.20f) * 0.90f);

        var highlight = new Rectangle(bounds.X + 1, bounds.Y + 1, bounds.Width - 2, 1);
        if (highlight.Width > 0)
        {
            spriteBatch.Draw(pixel, highlight, Color.Lerp(accent, Color.White, 0.16f) * 0.58f);
        }
    }

    private static void DrawSectionTitle(
        SpriteBatch spriteBatch,
        TextBlock title,
        Vector2 position,
        float availableWidth,
        Color accent)
    {
        DrawTextBlock(spriteBatch, title, position, availableWidth);

        const float ruleGap = 10f;
        var ruleX = position.X + title.Size.X + ruleGap;
        var ruleWidth = availableWidth - title.Size.X - ruleGap;
        if (ruleWidth <= 0f)
        {
            return;
        }

        DrawHorizontalRule(
            spriteBatch,
            new Rectangle(
                (int)MathF.Ceiling(ruleX),
                (int)MathF.Ceiling(position.Y + title.Size.Y * 0.56f),
                (int)MathF.Floor(ruleWidth),
                1),
            accent * 0.50f);
    }

    private static void DrawEffectRow(
        SpriteBatch spriteBatch,
        TextBlock text,
        Vector2 position,
        float availableWidth,
        Color accent)
    {
        var markerY = (int)MathF.Ceiling(position.Y + Math.Min(9f, text.Size.Y * 0.5f));
        spriteBatch.Draw(
            TextureAssets.MagicPixel.Value,
            new Rectangle((int)position.X + 2, markerY, 4, 4),
            accent * 0.86f);
        DrawTextBlock(
            spriteBatch,
            text,
            new Vector2(position.X + ArmorEffectIndent, position.Y),
            availableWidth - ArmorEffectIndent);
    }

    private static void DrawHorizontalRule(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        Color color)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        spriteBatch.Draw(TextureAssets.MagicPixel.Value, bounds, color);
    }

    private static void DrawBorder(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        int thickness,
        Color color)
    {
        if (bounds.Width <= thickness * 2 || bounds.Height <= thickness * 2)
        {
            return;
        }

        var pixel = TextureAssets.MagicPixel.Value;
        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, thickness), color);
        spriteBatch.Draw(
            pixel,
            new Rectangle(bounds.X, bounds.Bottom - thickness, bounds.Width, thickness),
            color);
        spriteBatch.Draw(
            pixel,
            new Rectangle(bounds.X, bounds.Y + thickness, thickness, bounds.Height - thickness * 2),
            color);
        spriteBatch.Draw(
            pixel,
            new Rectangle(bounds.Right - thickness, bounds.Y + thickness, thickness, bounds.Height - thickness * 2),
            color);
    }

    private static TextBlock CreateTextBlock(string text, float maxWidth)
    {
        return CreateTextBlock(
            text,
            maxWidth,
            JournalUiTheme.ContentDescriptionText,
            TextScale);
    }

    private static TextBlock CreateTextBlock(
        string text,
        float maxWidth,
        Color color,
        float scale)
    {
        var snippets = ChatManager
            .ParseMessage(text, color)
            .ToArray();
        ChatManager.ConvertNormalSnippets(snippets);
        var size = ChatManager.GetStringSize(
            FontAssets.MouseText.Value,
            snippets,
            new Vector2(scale),
            maxWidth);
        return new TextBlock(snippets, size, scale);
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
            new Vector2(textBlock.Scale),
            out _,
            maxWidth,
            2f);
    }

    private sealed record ArmorSetTooltipContent(
        string Title,
        string DefenseLabel,
        int TotalDefense,
        string EffectsTitle,
        string[] Effects,
        string BonusTitle,
        string[] BonusLines);

    private readonly record struct TextBlock(
        TextSnippet[] Snippets,
        Vector2 Size,
        float Scale)
    {
        public static TextBlock Empty { get; } = new([], Vector2.Zero, TextScale);

        public bool IsEmpty => Snippets.Length == 0;
    }
}
