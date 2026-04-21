using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.Localization;

namespace ProgressionJournal.UI.Utilities;

public readonly record struct JournalConditionVisuals(
    IReadOnlyList<JournalSourceTokenData> Tokens,
    IReadOnlyList<string> RemainingText);

public static class JournalAcquisitionVisuals
{
    private const string MoonTexturePathPrefix = "Images/Moon_";
    private const string HardmodeTexturePath = "achievement:ITS_HARD";

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

    private static readonly (int TextureIndex, string[] Keywords)[] MoonPhaseTokens =
    [
        (0, ["full moon", "полнолуние", "полная луна"]),
        (1, ["waning gibbous", "убывающая луна", "убывающий месяц"]),
        (2, ["third quarter", "last quarter", "последняя четверть", "третья четверть"]),
        (3, ["waning crescent", "старый месяц", "убывающий серп"]),
        (4, ["new moon", "новолуние", "новая луна"]),
        (5, ["waxing crescent", "растущий серп", "молодой месяц"]),
        (6, ["first quarter", "первая четверть"]),
        (7, ["waxing gibbous", "растущая луна"])
    ];
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

    public static IReadOnlyList<JournalSourceTokenData> GetNpcBestiaryTokens(int npcType)
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

        AddHardmodeToken(normalized, condition, tokens);
        AddMoonPhaseToken(normalized, condition, tokens);
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

    private static void AddHardmodeToken(string normalized, string condition, ICollection<JournalSourceTokenData> tokens)
    {
        if (!ContainsAny(normalized, "hardmode", "hard mode", "хардмод"))
        {
            return;
        }

        tokens.Add(new JournalSourceTokenData(
            JournalSourceTokenKind.Texture,
            0,
            condition,
            HardmodeTexturePath));
    }

    private static void AddMoonPhaseToken(string normalized, string condition, ICollection<JournalSourceTokenData> tokens)
    {
        foreach (var (textureIndex, keywords) in MoonPhaseTokens)
        {
            if (!keywords.Any(normalized.Contains))
            {
                continue;
            }

            tokens.Add(new JournalSourceTokenData(
                JournalSourceTokenKind.Texture,
                textureIndex,
                condition,
                $"{MoonTexturePathPrefix}{textureIndex}"));
            return;
        }
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
