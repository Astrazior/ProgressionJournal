using System.Collections.Generic;
using System.Linq;
using Terraria.GameContent;

namespace ProgressionJournal.UI.Utilities;

public static class JournalTextUtilities
{
    public static string TrimToCharacterCount(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength)
        {
            return text;
        }

        return text[..(maxLength - 3)].TrimEnd() + "...";
    }

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

    public static float MeasureMouseTextWidth(string text, float textScale)
    {
        return FontAssets.MouseText.Value.MeasureString(text).X * textScale;
    }

    public static IReadOnlyList<string> WrapToPixelWidth(string text, float maxWidth, float textScale)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        if (MeasureMouseTextWidth(text, textScale) <= maxWidth)
        {
            return [text];
        }

        var words = text.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return [TrimToPixelWidth(text, maxWidth, textScale)];
        }

        var lines = new List<string>();
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

        return lines.Where(static line => !string.IsNullOrWhiteSpace(line)).ToArray();
    }
}

