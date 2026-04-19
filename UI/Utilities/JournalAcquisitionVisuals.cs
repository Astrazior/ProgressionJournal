using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace ProgressionJournal.UI.Utilities;

public readonly record struct JournalConditionVisuals(
    IReadOnlyList<JournalSourceTokenData> Tokens,
    IReadOnlyList<string> RemainingText);

public static class JournalAcquisitionVisuals
{
    public static bool TryCreateSourceToken(JournalDropSource drop, out JournalSourceTokenData token)
    {
        if (drop.SourceNpcType is { } npcType)
        {
            token = new JournalSourceTokenData(JournalSourceTokenKind.Npc, npcType, drop.SourceName);
            return true;
        }

        if (drop.SourceItemId is { } itemId)
        {
            token = new JournalSourceTokenData(JournalSourceTokenKind.Item, itemId, Lang.GetItemNameValue(itemId));
            return true;
        }

        token = default;
        return false;
    }

    public static JournalSourceTokenData CreateSourceToken(JournalShopSource shop)
    {
        return new JournalSourceTokenData(JournalSourceTokenKind.Npc, shop.NpcType, shop.NpcName);
    }

    public static JournalConditionVisuals SplitConditions(IEnumerable<string> conditions)
    {
        var tokens = new List<JournalSourceTokenData>();
        var remainingText = new List<string>();

        foreach (var condition in conditions.Where(static condition => !string.IsNullOrWhiteSpace(condition)))
        {
            if (TryCreateConditionTokens(condition, tokens))
            {
                continue;
            }

            remainingText.Add(condition);
        }

        return new JournalConditionVisuals(
            tokens
                .Distinct()
                .ToArray(),
            remainingText.ToArray());
    }

    private static bool TryCreateConditionTokens(string condition, ICollection<JournalSourceTokenData> tokens)
    {
        var normalized = condition.Trim().ToLowerInvariant();
        var countBefore = tokens.Count;

        AddCombinedBiomeTokens(normalized, condition, tokens);

        AddIfMatch(tokens, normalized, condition, 55, "old one");
        AddIfMatch(tokens, normalized, condition, 54, "frost legion");
        AddIfMatch(tokens, normalized, condition, 53, "martian");
        AddIfMatch(tokens, normalized, condition, 52, "frost moon");
        AddIfMatch(tokens, normalized, condition, 51, "pumpkin moon");
        AddIfMatch(tokens, normalized, condition, 50, "pirate");
        AddIfMatch(tokens, normalized, condition, 49, "goblin");
        AddIfMatch(tokens, normalized, condition, 48, "party");
        AddIfMatch(tokens, normalized, condition, 47, "slime rain");
        AddIfMatch(tokens, normalized, condition, 46, "christmas");
        AddIfMatch(tokens, normalized, condition, 45, "halloween");
        AddIfMatch(tokens, normalized, condition, 43, "sandstorm");
        AddIfMatch(tokens, normalized, condition, 42, "blizzard");
        AddIfMatch(tokens, normalized, condition, 41, "windy");
        AddIfMatch(tokens, normalized, condition, 40, "rain");
        AddIfMatch(tokens, normalized, condition, 39, "eclipse");
        AddIfMatch(tokens, normalized, condition, 38, "blood moon");

        if (ContainsAny(normalized, "daytime", "during daytime", "during the day", "day only"))
        {
            tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, 36, condition));
        }

        if (ContainsAny(normalized, "nighttime", "during nighttime", "during the night", "at night", "night only"))
        {
            tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, 37, condition));
        }

        AddBiomeToken(tokens, normalized, condition);
        return tokens.Count > countBefore;
    }

    private static void AddCombinedBiomeTokens(string normalized, string condition, ICollection<JournalSourceTokenData> tokens)
    {
        if (normalized.Contains("crimson") && normalized.Contains("corruption"))
        {
            var crimsonFrame = normalized.Contains("underground") ? 13 : 12;
            var corruptionFrame = normalized.Contains("underground") ? 8 : 7;
            tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, crimsonFrame, condition));
            tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, corruptionFrame, condition));
        }
    }

    private static void AddBiomeToken(ICollection<JournalSourceTokenData> tokens, string normalized, string condition)
    {
        if (ContainsAny(normalized, "ocean", "water", "beach"))
        {
            tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, 28, condition));
            return;
        }

        if (normalized.Contains("oasis"))
        {
            tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, 27, condition));
            return;
        }

        if (normalized.Contains("graveyard"))
        {
            tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, 35, condition));
            return;
        }

        if (ContainsAny(normalized, "underworld", "hell"))
        {
            tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, 33, condition));
            return;
        }

        if (normalized.Contains("dungeon"))
        {
            tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, 32, condition));
            return;
        }

        if (normalized.Contains("temple"))
        {
            tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, 31, condition));
            return;
        }

        if (ContainsAny(normalized, "sky", "space", "floating island"))
        {
            tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, 26, condition));
            return;
        }

        if (normalized.Contains("jungle"))
        {
            var biomeFrame = normalized.Contains("underground") ? 23 : 22;
            tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, biomeFrame, condition));
            return;
        }

        if (normalized.Contains("hallow"))
        {
            if (normalized.Contains("desert"))
            {
                tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, normalized.Contains("underground") ? 20 : 19, condition));
                return;
            }

            if (ContainsAny(normalized, "ice", "snow"))
            {
                tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, 21, condition));
                return;
            }

            var biomeFrame = normalized.Contains("underground") ? 18 : 17;
            tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, biomeFrame, condition));
            return;
        }

        if (normalized.Contains("crimson"))
        {
            if (normalized.Contains("desert"))
            {
                tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, normalized.Contains("underground") ? 15 : 14, condition));
                return;
            }

            if (ContainsAny(normalized, "ice", "snow"))
            {
                tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, 16, condition));
                return;
            }

            var biomeFrame = normalized.Contains("underground") ? 13 : 12;
            tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, biomeFrame, condition));
            return;
        }

        if (ContainsAny(normalized, "corruption", "corrupt"))
        {
            if (normalized.Contains("desert"))
            {
                tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, normalized.Contains("underground") ? 10 : 9, condition));
                return;
            }

            if (ContainsAny(normalized, "ice", "snow"))
            {
                tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, 11, condition));
                return;
            }

            var biomeFrame = normalized.Contains("underground") ? 8 : 7;
            tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, biomeFrame, condition));
            return;
        }

        if (ContainsAny(normalized, "ice", "snow"))
        {
            var biomeFrame = normalized.Contains("underground") ? 6 : 5;
            tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, biomeFrame, condition));
            return;
        }

        if (normalized.Contains("desert"))
        {
            var biomeFrame = normalized.Contains("underground") ? 4 : 3;
            tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, biomeFrame, condition));
            return;
        }

        if (normalized.Contains("cavern"))
        {
            tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, 2, condition));
            return;
        }

        if (normalized.Contains("underground"))
        {
            tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, 1, condition));
            return;
        }

        if (ContainsAny(normalized, "surface", "above ground"))
        {
            tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, 0, condition));
        }
    }

    private static void AddIfMatch(ICollection<JournalSourceTokenData> tokens, string normalized, string condition, int frame, string keyword)
    {
        if (!normalized.Contains(keyword))
        {
            return;
        }

        tokens.Add(new JournalSourceTokenData(JournalSourceTokenKind.Bestiary, frame, condition));
    }

    private static bool ContainsAny(string source, params string[] patterns)
    {
        return patterns.Any(source.Contains);
    }
}
