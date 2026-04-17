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
}

