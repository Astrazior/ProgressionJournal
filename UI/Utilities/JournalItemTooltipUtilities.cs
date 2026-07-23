using System.Globalization;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ProgressionJournal.UI.Utilities;

internal static class JournalItemTooltipUtilities
{
    private const int VanillaTooltipLineCapacity = 30;

#pragma warning disable SYSLIB1045 // tModLoader's in-game compiler does not run the GeneratedRegex source generator.
    private static readonly Regex ColorTagRegex = new(
        @"\[c/[0-9a-fA-F]{6}:(.*?)\]",
        RegexOptions.Compiled);
    private static readonly Regex NumericValueRegex = new(
        @"(?<![\p{L}\d])(?<sign>[+-]?)(?<number>\d+(?:[.,]\d+)?)(?<percent>\s*%)?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
#pragma warning restore SYSLIB1045

    public static IReadOnlyList<string> GetAggregatedNumericEffectLines(
        IEnumerable<Item> sourceItems)
    {
        return AggregateNumericLines(sourceItems.SelectMany(GetEffectLines));
    }

    public static string NormalizeText(string text) => ColorTagRegex.Replace(text, "$1");

    private static IReadOnlyList<string> GetEffectLines(Item sourceItem)
    {
        if (sourceItem.IsAir)
        {
            return [];
        }

        try
        {
            var item = sourceItem.Clone();
            var capacity = VanillaTooltipLineCapacity + (item.ToolTip?.Lines ?? 0);
            var names = new string[capacity];
            var text = new string[capacity];
            var modifiers = new bool[capacity];
            var badModifiers = new bool[capacity];
            var oneDropLogo = -1;
            var yoyoLogo = -1;
            var researchLine = -1;
            var lineCount = 1;

            Main.MouseText_DrawItemTooltip_GetLinesInfo(
                item,
                ref yoyoLogo,
                ref researchLine,
                item.knockBack,
                ref lineCount,
                text,
                modifiers,
                badModifiers,
                names,
                out var prefixLineIndex);

            var tooltipLines = ItemLoader.ModifyTooltips(
                item,
                ref lineCount,
                names,
                ref text,
                ref modifiers,
                ref badModifiers,
                ref oneDropLogo,
                out _,
                prefixLineIndex);

            return tooltipLines
                .Where(IsEffectLine)
                .Select(static line => NormalizeText(line.Text.Trim()))
                .Where(static line => !string.IsNullOrWhiteSpace(line))
                .ToArray();
        }
        catch (Exception exception)
        {
            var itemName = sourceItem.ModItem?.FullName
                           ?? ItemID.Search.GetName(sourceItem.type)
                           ?? sourceItem.type.ToString();
            ProgressionJournal.Instance?.Logger.Debug(
                $"Failed to resolve tooltip effects for armor item '{itemName}'." +
                $"{Environment.NewLine}{exception}");
            return [];
        }
    }

    private static bool IsEffectLine(TooltipLine line)
    {
        if (string.IsNullOrWhiteSpace(line.Text)
            || string.Equals(line.Name, "SetBonus", StringComparison.Ordinal))
        {
            return false;
        }

        return !string.Equals(line.Mod, "Terraria", StringComparison.Ordinal)
               || line.Name.StartsWith("Tooltip", StringComparison.Ordinal);
    }

    private static string[] AggregateNumericLines(IEnumerable<string> sourceLines)
    {
        var aggregations = new Dictionary<string, NumericLineAggregation>(StringComparer.Ordinal);
        var orderedKeys = new List<string>();

        foreach (var sourceLine in sourceLines)
        {
            var line = NormalizeText(sourceLine).Trim();
            var matches = NumericValueRegex.Matches(line).ToArray();
            if (matches.Length == 0
                || matches.Any(static match => !TryParseValue(match, out _)))
            {
                continue;
            }

            var signature = NumericValueRegex.Replace(
                line,
                static match =>
                    $"{match.Groups["sign"].Value}#{(match.Groups["percent"].Success ? "%" : string.Empty)}");
            if (!aggregations.TryGetValue(signature, out var aggregation))
            {
                aggregation = new NumericLineAggregation(line, new decimal[matches.Length]);
                aggregations.Add(signature, aggregation);
                orderedKeys.Add(signature);
            }

            for (var index = 0; index < matches.Length; index++)
            {
                TryParseValue(matches[index], out var value);
                aggregation.Totals[index] += value;
            }
        }

        return orderedKeys
            .Select(key => RenderAggregation(aggregations[key]))
            .ToArray();

        static bool TryParseValue(Match match, out decimal value)
        {
            var rawValue = $"{match.Groups["sign"].Value}{match.Groups["number"].Value}"
                .Replace(',', '.');
            return decimal.TryParse(
                rawValue,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out value);
        }
    }

    private static string RenderAggregation(NumericLineAggregation aggregation)
    {
        var valueIndex = 0;
        return NumericValueRegex.Replace(
            aggregation.Template,
            match =>
            {
                var value = aggregation.Totals[valueIndex++];
                var formatted = value.ToString("0.##", CultureInfo.InvariantCulture);
                if (value >= 0m
                    && string.Equals(match.Groups["sign"].Value, "+", StringComparison.Ordinal))
                {
                    formatted = $"+{formatted}";
                }

                return match.Groups["percent"].Success ? $"{formatted}%" : formatted;
            });
    }

    private sealed record NumericLineAggregation(string Template, decimal[] Totals);
}
