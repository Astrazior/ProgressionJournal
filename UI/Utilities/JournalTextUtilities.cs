using Terraria.GameContent;

namespace ProgressionJournal.UI.Utilities;

public static class JournalTextUtilities
{
    public static string TrimToPixelWidth(string text, float maxWidth, float textScale)
    {
        if (string.IsNullOrWhiteSpace(text) || MeasureMouseTextWidth(text, textScale) <= maxWidth)
        {
            return text;
        }

        const string ellipsis = "...";

        for (var length = text.Length - 1; length > 0; length--)
        {
            var candidate = text[..length].TrimEnd() + ellipsis;
            if (MeasureMouseTextWidth(candidate, textScale) <= maxWidth)
            {
                return candidate;
            }
        }

        return ellipsis;
    }

    private static float MeasureMouseTextWidth(string text, float textScale)
    {
        return FontAssets.MouseText.Value.MeasureString(text).X * textScale;
    }

    public static IReadOnlyList<string> WrapToPixelWidth(string text, float maxWidth, float textScale)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var paragraphs = text.Replace("\r\n", "\n").Split('\n');
        if (paragraphs.Length == 1 && MeasureMouseTextWidth(text, textScale) <= maxWidth)
        {
            return [text];
        }

        var lines = new List<string>();
        foreach (var paragraph in paragraphs)
        {
            WrapParagraph(paragraph, maxWidth, textScale, lines);
        }

        return lines;
    }

    private static void WrapParagraph(
        string paragraph,
        float maxWidth,
        float textScale,
        List<string> lines)
    {
        var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            lines.Add(string.Empty);
            return;
        }

        var currentLine = string.Empty;

        foreach (var word in words)
        {
            var candidate = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
            if (MeasureMouseTextWidth(candidate, textScale) <= maxWidth)
            {
                currentLine = candidate;
                continue;
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
            }

            currentLine = MeasureMouseTextWidth(word, textScale) <= maxWidth
                ? word
                : TrimToPixelWidth(word, maxWidth, textScale);
        }

        if (!string.IsNullOrEmpty(currentLine))
        {
            lines.Add(currentLine);
        }
    }
}
