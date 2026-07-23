using System.Text.RegularExpressions;
using Terraria.GameContent.Bestiary;
using Terraria.Localization;

namespace ProgressionJournal.UI.Utilities;

public readonly record struct JournalConditionVisuals(
    IReadOnlyList<JournalSourceTokenData> Tokens,
    IReadOnlyList<string> RemainingText);

public static class JournalAcquisitionVisuals
{
#pragma warning disable SYSLIB1045 // tModLoader's in-game compiler does not run the GeneratedRegex source generator.
    private static readonly Regex LeadingItemTagRegex = new(
        @"^\s*\[i(?:/[^\]:]+)*:[^\]]+\]\s*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
#pragma warning restore SYSLIB1045

    private static readonly HashSet<int> BestiaryLocationFrames =
    [
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21,
        22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 44, 56, 57, 58, 59
    ];

    private static readonly Dictionary<string, int> BestiaryDisplayKeyFrames = new()
    {
        ["Bestiary_Biomes.Surface"] = 0,
        ["Bestiary_Biomes.Underground"] = 1,
        ["Bestiary_Biomes.Caverns"] = 2,
        ["Bestiary_Biomes.Desert"] = 3,
        ["Bestiary_Biomes.UndergroundDesert"] = 4,
        ["Bestiary_Biomes.Snow"] = 5,
        ["Bestiary_Biomes.UndergroundSnow"] = 6,
        ["Bestiary_Biomes.TheCorruption"] = 7,
        ["Bestiary_Biomes.UndergroundCorruption"] = 8,
        ["Bestiary_Biomes.CorruptDesert"] = 9,
        ["Bestiary_Biomes.CorruptUndergroundDesert"] = 10,
        ["Bestiary_Biomes.CorruptIce"] = 11,
        ["Bestiary_Biomes.Crimson"] = 12,
        ["Bestiary_Biomes.UndergroundCrimson"] = 13,
        ["Bestiary_Biomes.CrimsonDesert"] = 14,
        ["Bestiary_Biomes.CrimsonUndergroundDesert"] = 15,
        ["Bestiary_Biomes.CrimsonIce"] = 16,
        ["Bestiary_Biomes.TheHallow"] = 17,
        ["Bestiary_Biomes.UndergroundHallow"] = 18,
        ["Bestiary_Biomes.HallowDesert"] = 19,
        ["Bestiary_Biomes.HallowUndergroundDesert"] = 20,
        ["Bestiary_Biomes.HallowIce"] = 21,
        ["Bestiary_Biomes.Jungle"] = 22,
        ["Bestiary_Biomes.UndergroundJungle"] = 23,
        ["Bestiary_Biomes.SurfaceMushroom"] = 24,
        ["Bestiary_Biomes.UndergroundMushroom"] = 25,
        ["Bestiary_Biomes.Sky"] = 26,
        ["Bestiary_Biomes.Oasis"] = 27,
        ["Bestiary_Biomes.Ocean"] = 28,
        ["Bestiary_Biomes.Marble"] = 29,
        ["Bestiary_Biomes.Granite"] = 30,
        ["Bestiary_Biomes.TheTemple"] = 31,
        ["Bestiary_Biomes.TheDungeon"] = 32,
        ["Bestiary_Biomes.TheUnderworld"] = 33,
        ["Bestiary_Biomes.SpiderNest"] = 34,
        ["Bestiary_Biomes.Graveyard"] = 35,
        ["Bestiary_Times.DayTime"] = 36,
        ["Bestiary_Times.NightTime"] = 37,
        ["Bestiary_Events.BloodMoon"] = 38,
        ["Bestiary_Events.Eclipse"] = 39,
        ["Bestiary_Events.Rain"] = 40,
        ["Bestiary_Events.WindyDay"] = 41,
        ["Bestiary_Events.Blizzard"] = 42,
        ["Bestiary_Events.Sandstorm"] = 43,
        ["Bestiary_Biomes.Meteor"] = 44,
        ["Bestiary_Events.Halloween"] = 45,
        ["Bestiary_Events.Christmas"] = 46,
        ["Bestiary_Events.SlimeRain"] = 47,
        ["Bestiary_Events.Party"] = 48,
        ["Bestiary_Invasions.Goblins"] = 49,
        ["Bestiary_Invasions.Pirates"] = 50,
        ["Bestiary_Invasions.PumpkinMoon"] = 51,
        ["Bestiary_Invasions.FrostMoon"] = 52,
        ["Bestiary_Invasions.Martian"] = 53,
        ["Bestiary_Invasions.FrostLegion"] = 54,
        ["Bestiary_Invasions.OldOnesArmy"] = 55,
        ["Bestiary_Biomes.SolarPillar"] = 56,
        ["Bestiary_Biomes.VortexPillar"] = 57,
        ["Bestiary_Biomes.NebulaPillar"] = 58,
        ["Bestiary_Biomes.StardustPillar"] = 59
    };
    private static readonly Dictionary<int, JournalSourceTokenData[]> NpcBestiaryTokenCache = new();
    private static readonly Dictionary<int, JournalSourceTokenData[]> NpcLocationTokenCache = new();

    public static bool TryCreateSourceToken(JournalDropSource drop, out JournalSourceTokenData token)
    {
        if (drop.SourceNpcType is { } npcType)
        {
            token = new JournalSourceTokenData(JournalSourceTokenKind.Npc, npcType, drop.SourceName);
            return true;
        }

        if (drop.SourceItemId is { } itemId)
        {
            token = new JournalSourceTokenData(JournalSourceTokenKind.Item, itemId, drop.SourceName);
            return true;
        }

        token = default;
        return false;
    }

    public static JournalSourceTokenData CreateSourceToken(JournalShopSource shop)
    {
        return new JournalSourceTokenData(JournalSourceTokenKind.Npc, shop.NpcType, shop.NpcName);
    }

    private static JournalSourceTokenData[] GetNpcBestiaryTokens(int npcType)
    {
        if (NpcBestiaryTokenCache.TryGetValue(npcType, out var cachedTokens))
        {
            return cachedTokens;
        }

        var bestiaryEntry = BestiaryDatabaseNPCsPopulator.FindEntryByNPCID(npcType);
        if (bestiaryEntry is null)
        {
            return [];
        }

        var tokens = bestiaryEntry.Info
            .OfType<FilterProviderInfoElement>()
            .Select(static element => element.GetDisplayNameKey())
            .Distinct()
            .Select(TryCreateBestiaryToken)
            .Where(static token => token.HasValue)
            .Select(static token => token!.Value)
            .ToArray();
        NpcBestiaryTokenCache[npcType] = tokens;
        return tokens;
    }

    public static IReadOnlyList<JournalSourceTokenData> GetNpcLocationTokens(int npcType)
    {
        if (NpcLocationTokenCache.TryGetValue(npcType, out var cachedTokens))
        {
            return cachedTokens;
        }

        var tokens = GetNpcBestiaryTokens(npcType)
            .Where(static token => IsLocationFrame(token.Value))
            .OrderBy(static token => token.Value)
            .ToArray();
        NpcLocationTokenCache[npcType] = tokens;
        return tokens;
    }

    public static IReadOnlyList<JournalSourceTokenData> GetCommonNpcLocationTokens(IEnumerable<int> npcTypes)
    {
        JournalSourceTokenData[]? intersection = null;

        foreach (var npcType in npcTypes.Distinct())
        {
            var currentTokens = GetNpcLocationTokens(npcType);
            intersection = intersection is null
                ? currentTokens.ToArray()
                : intersection
                    .Where(token => currentTokens.Any(current => current.Kind == token.Kind && current.Value == token.Value))
                    .ToArray();

            if (intersection.Length == 0)
            {
                break;
            }
        }

        return intersection ?? [];
    }

    public static JournalConditionVisuals SplitConditions(IEnumerable<string> conditions)
    {
        var normalizedConditions = conditions
            .Where(static condition => !string.IsNullOrWhiteSpace(condition))
            .Select(static condition => RemoveLeadingItemTag(RemoveRedundantItemPrefix(condition)))
            .Where(static condition => !string.IsNullOrWhiteSpace(condition))
            .Select(static condition => IsHardmodeOnlyCondition(condition)
                ? Language.GetTextValue("Mods.ProgressionJournal.UI.FishingWorldHardmode")
                : condition)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        return new JournalConditionVisuals([], normalizedConditions);
    }

    private static string RemoveLeadingItemTag(string condition) =>
        LeadingItemTagRegex.Replace(condition, string.Empty);

    private static string RemoveRedundantItemPrefix(string condition)
    {
        var separatorIndex = condition.IndexOf(':');
        if (separatorIndex < 0)
        {
            return condition;
        }

        var label = condition[..separatorIndex].Trim();
        if (!label.Equals("Предмет", StringComparison.OrdinalIgnoreCase)
            && !label.Equals("Предметы", StringComparison.OrdinalIgnoreCase)
            && !label.Equals("Item", StringComparison.OrdinalIgnoreCase)
            && !label.Equals("Items", StringComparison.OrdinalIgnoreCase))
        {
            return condition;
        }

        return condition[(separatorIndex + 1)..].TrimStart();
    }

    public static bool IsHardmodeCondition(string condition)
    {
        var normalized = condition.Trim().ToLowerInvariant();
        return ContainsAny(normalized, "hardmode", "hard mode", "хардмод", "сложн");
    }

    private static bool IsHardmodeOnlyCondition(string condition) =>
        condition.Trim().ToLowerInvariant() is
            "hardmode"
            or "in hardmode"
            or "in hard mode"
            or "world: hardmode"
            or "available after: hardmode"
            or "drops in hardmode"
            or "drops in hard mode"
            or "хардмод"
            or "в хардмоде"
            or "мир: хардмод"
            or "доступно после этапа: hardmode"
            or "выпадает в сложном режиме";

    private static bool ContainsAny(string source, params string[] patterns)
    {
        return patterns.Any(source.Contains);
    }

    private static JournalSourceTokenData? TryCreateBestiaryToken(string displayNameKey)
    {
        if (!BestiaryDisplayKeyFrames.TryGetValue(displayNameKey, out var frame))
        {
            return null;
        }

        return new JournalSourceTokenData(
            JournalSourceTokenKind.Bestiary,
            frame,
            Language.GetTextValue(displayNameKey));
    }

    private static bool IsLocationFrame(int frame)
    {
        return BestiaryLocationFrames.Contains(frame);
    }
}
