using Terraria;
using Terraria.ID;
using Terraria.Localization;

namespace ProgressionJournal.Data.Models;

internal sealed record JournalArmorSetDefinition(int HeadItemId, int BodyItemId, int LegItemId)
{
    public string Key => $"{HeadItemId}:{BodyItemId}:{LegItemId}";

    public IReadOnlyList<int> ItemIds => new[]
        {
            HeadItemId,
            BodyItemId,
            LegItemId
        }
        .Where(static itemId => itemId > ItemID.None)
        .ToArray();

    public int IconItemId => HeadItemId > ItemID.None
        ? HeadItemId
        : BodyItemId > ItemID.None
            ? BodyItemId
            : LegItemId;

    public int TotalDefense =>
        ItemIds.Sum(GetDefense) + JournalArmorSetBonusResolver.Resolve(this).DefenseBonus;

    public string ResolveDisplayName()
    {
        var itemNames = ItemIds
            .Select(Lang.GetItemNameValue)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .ToArray();
        if (itemNames.Length == 0)
        {
            return string.Empty;
        }

        var commonPrefix = FindCommonWords(itemNames, fromEnd: false);
        if (IsUsefulNamePart(commonPrefix))
        {
            return FormatArmorName(commonPrefix, armorFirst: false);
        }

        var commonSuffix = FindCommonWords(itemNames, fromEnd: true);
        if (IsUsefulNamePart(commonSuffix))
        {
            return FormatArmorName(commonSuffix, armorFirst: true);
        }

        var fallback = RemoveHeadPieceName(itemNames[0]);
        return FormatArmorName(fallback.Name, fallback.ArmorFirst);
    }

    public string ResolveBonusText()
    {
        var result = JournalArmorSetBonusResolver.Resolve(this);
        return result.Failed
            ? Language.GetTextValue("Mods.ProgressionJournal.UI.ArmorSetBonusUnavailable")
            : result.Text;
    }

    internal void PrimeBonus() => JournalArmorSetBonusResolver.Resolve(this);

    private static int GetDefense(int itemId)
    {
        return itemId > ItemID.None
               && ContentSamples.ItemsByType.TryGetValue(itemId, out var item)
               && item is not null
            ? item.defense
            : 0;
    }

    private static string FindCommonWords(IReadOnlyList<string> names, bool fromEnd)
    {
        var words = names
            .Select(static name => name.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToArray();
        var commonCount = 0;
        var limit = words.Min(static values => values.Length);

        while (commonCount < limit)
        {
            var referenceIndex = fromEnd ? words[0].Length - commonCount - 1 : commonCount;
            var reference = words[0][referenceIndex];
            if (words.Skip(1).Any(values =>
                    !string.Equals(
                        values[fromEnd ? values.Length - commonCount - 1 : commonCount],
                        reference,
                        StringComparison.OrdinalIgnoreCase)))
            {
                break;
            }

            commonCount++;
        }

        if (commonCount == 0)
        {
            return string.Empty;
        }

        return fromEnd
            ? string.Join(' ', words[0].Skip(words[0].Length - commonCount))
            : string.Join(' ', words[0].Take(commonCount));
    }

    private static bool IsUsefulNamePart(string value)
    {
        var armorPieceWords = GetLocalizedRuleValues("ArmorSetPieceWords")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return !string.IsNullOrWhiteSpace(value)
               && value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                   .Any(word => !armorPieceWords.Contains(word));
    }

    private static ArmorNameCandidate RemoveHeadPieceName(string headName)
    {
        foreach (var phrase in GetLocalizedRuleValues("ArmorSetPiecePhrases")
                     .OrderByDescending(static value => value.Length))
        {
            if (TryRemoveNamePart(headName, phrase, fromStart: true, out var remainder))
            {
                return new ArmorNameCandidate(remainder, true);
            }

            if (TryRemoveNamePart(headName, phrase, fromStart: false, out remainder))
            {
                return new ArmorNameCandidate(remainder, false);
            }
        }

        var words = headName.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var armorPieceWords = GetLocalizedRuleValues("ArmorSetPieceWords")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (words.Length > 1 && armorPieceWords.Contains(words[0]))
        {
            return new ArmorNameCandidate(string.Join(' ', words.Skip(1)), true);
        }

        if (words.Length > 1 && armorPieceWords.Contains(words[^1]))
        {
            return new ArmorNameCandidate(string.Join(' ', words.Take(words.Length - 1)), false);
        }

        return new ArmorNameCandidate(headName, false);
    }

    private static string FormatArmorName(string namePart, bool armorFirst)
    {
        var trimmed = namePart.Trim();
        return armorFirst
            ? Language.GetTextValue("Mods.ProgressionJournal.UI.ArmorSetFallbackNameAfter", trimmed)
            : Language.GetTextValue(
                "Mods.ProgressionJournal.UI.ArmorSetFallbackName",
                ApplyLocalizedInflections(trimmed));
    }

    private static bool TryRemoveNamePart(
        string name,
        string part,
        bool fromStart,
        out string remainder)
    {
        var comparison = StringComparison.OrdinalIgnoreCase;
        if (fromStart
            && name.StartsWith($"{part} ", comparison))
        {
            remainder = name[(part.Length + 1)..].Trim();
            return remainder.Length > 0;
        }

        if (!fromStart
            && name.EndsWith($" {part}", comparison))
        {
            remainder = name[..^(part.Length + 1)].Trim();
            return remainder.Length > 0;
        }

        remainder = string.Empty;
        return false;
    }

    private static string ApplyLocalizedInflections(string value)
    {
        var replacements = GetLocalizedRuleValues("ArmorSetNameInflections")
            .Select(static rule => rule.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(static parts => parts.Length == 2 && parts[0].Length > 0)
            .ToArray();
        if (replacements.Length == 0)
        {
            return value;
        }

        return string.Join(' ', value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(word => ApplyLocalizedInflection(word, replacements)));
    }

    private static string ApplyLocalizedInflection(string word, IReadOnlyList<string[]> replacements)
    {
        foreach (var replacement in replacements)
        {
            if (word.EndsWith(replacement[0], StringComparison.OrdinalIgnoreCase))
            {
                return $"{word[..^replacement[0].Length]}{replacement[1]}";
            }
        }

        return word;
    }

    private static string[] GetLocalizedRuleValues(string key)
    {
        var localizationKey = $"Mods.ProgressionJournal.UI.{key}";
        if (!Language.Exists(localizationKey))
        {
            return [];
        }

        return Language.GetTextValue(localizationKey)
            .Split(
                '|',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private readonly record struct ArmorNameCandidate(string Name, bool ArmorFirst);
}
