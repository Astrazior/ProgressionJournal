using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ProgressionJournal.Api;

internal static class ProgressionJournalApi
{
    public const int Version = 1;

    public static object HandleCall(object[] args)
    {
        if (args.Length == 0 || args[0] is not string command || string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("ProgressionJournal.Call requires a command string as the first argument.", nameof(args));
        }

        return command switch
        {
            "GetApiVersion" => Version,
            "RegisterEntry" => RegisterEntry(args),
            _ => throw new ArgumentException($"Unknown ProgressionJournal.Call command '{command}'.", nameof(args))
        };
    }

    private static bool RegisterEntry(object[] args)
    {
        if (args.Length is < 6 or > 8)
        {
            throw new ArgumentException("RegisterEntry expects 5 to 7 arguments after the command: key, category, classes, itemGroups, evaluations, [eventCategory], [isSupportWeapon].", nameof(args));
        }

        var key = RequireString(args[1], "key");
        var category = ParseEnum<JournalItemCategory>(args[2], "category");
        var classes = ParseFlagsEnum<CombatClass>(args[3], "classes");
        var itemGroups = ParseItemGroups(args[4], "itemGroups");
        var evaluations = ParseEvaluations(args[5], "evaluations");
        var eventCategory = args.Length >= 7 ? ParseNullableEnum<JournalEventCategory>(args[6], "eventCategory") : null;
        var isSupportWeapon = args.Length >= 8 && ParseBool(args[7], "isSupportWeapon");

        JournalRepository.RegisterExternalEntry(
            new JournalEntry(key, category, classes, itemGroups, evaluations, eventCategory, isSupportWeapon));

        return true;
    }

    private static string RequireString(object value, string argumentName)
    {
        if (value is string text && !string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        throw new ArgumentException($"Argument '{argumentName}' must be a non-empty string.");
    }

    private static bool ParseBool(object value, string argumentName)
    {
        return value switch
        {
            bool result => result,
            string text when bool.TryParse(text, out var result) => result,
            _ => throw new ArgumentException($"Argument '{argumentName}' must be a boolean.")
        };
    }

    private static TEnum ParseEnum<TEnum>(object value, string argumentName) where TEnum : struct, Enum
    {
        switch (value)
        {
            case TEnum typed:
                return typed;
            case string text when Enum.TryParse(text, true, out TEnum parsedFromString):
                return parsedFromString;
        }

        if (TryConvertToInt32(value, out var numericValue))
        {
            return (TEnum)Enum.ToObject(typeof(TEnum), numericValue);
        }

        throw new ArgumentException($"Argument '{argumentName}' must be a {typeof(TEnum).Name} value or name.");
    }

    private static TEnum ParseFlagsEnum<TEnum>(object value, string argumentName) where TEnum : struct, Enum
    {
        switch (value)
        {
            case TEnum typed:
                return typed;
            case string text:
            {
                var normalized = text.Replace("|", ",", StringComparison.Ordinal);
                if (Enum.TryParse(normalized, true, out TEnum parsedFromString))
                {
                    return parsedFromString;
                }

                break;
            }
        }

        if (TryConvertToInt32(value, out var numericValue))
        {
            return (TEnum)Enum.ToObject(typeof(TEnum), numericValue);
        }

        throw new ArgumentException($"Argument '{argumentName}' must be a {typeof(TEnum).Name} flag value or name.");
    }

    private static TEnum? ParseNullableEnum<TEnum>(object? value, string argumentName) where TEnum : struct, Enum
    {
        switch (value)
        {
            case null:
            case string text when string.IsNullOrWhiteSpace(text):
                return null;
        }

        if (TryConvertToInt32(value, out var numericValue) && numericValue < 0)
        {
            return null;
        }

        return ParseEnum<TEnum>(value, argumentName);
    }

    private static List<JournalItemGroup> ParseItemGroups(object value, string argumentName)
    {
        if (TryParseIntSequence(value, out var singleGroup))
        {
            return [new JournalItemGroup(singleGroup)];
        }

        List<JournalItemGroup> groups = [];
        foreach (var element in EnumerateObjects(value, argumentName))
        {
            if (!TryParseIntSequence(element, out var itemIds))
            {
                throw new ArgumentException($"Argument '{argumentName}' must be an int[] or int[][].");
            }

            groups.Add(new JournalItemGroup(itemIds));
        }

        return groups.Count == 0
            ? throw new ArgumentException($"Argument '{argumentName}' must contain at least one item group.")
            : groups;
    }

    private static List<StageEvaluation> ParseEvaluations(object value, string argumentName)
    {
        List<StageEvaluation> evaluations = [];

        foreach (var pairValue in EnumerateObjects(value, argumentName))
        {
            var pair = EnumerateObjects(pairValue, argumentName).ToArray();
            if (pair.Length != 2)
            {
                throw new ArgumentException($"Each evaluation in '{argumentName}' must contain exactly two values: stageId and tier.");
            }

            var stageId = ParseEnum<ProgressionStageId>(pair[0], "stageId");
            var tier = ParseEnum<RecommendationTier>(pair[1], "tier");
            evaluations.Add(new StageEvaluation(stageId, tier));
        }

        return evaluations.Count == 0
            ? throw new ArgumentException($"Argument '{argumentName}' must contain at least one evaluation.")
            : evaluations;
    }

    private static IEnumerable<object> EnumerateObjects(object value, string argumentName)
    {
        if (value is string || value is not IEnumerable enumerable)
        {
            throw new ArgumentException($"Argument '{argumentName}' must be an enumerable value.");
        }

        foreach (var element in enumerable)
        {
            if (element is null)
            {
                throw new ArgumentException($"Argument '{argumentName}' cannot contain null values.");
            }

            yield return element;
        }
    }

    private static bool TryParseIntSequence(object value, out int[] values)
    {
        values = [];

        if (value is string || value is not IEnumerable enumerable)
        {
            return false;
        }

        List<int> items = [];
        foreach (var element in enumerable)
        {
            if (element is null || !TryConvertToInt32(element, out var parsed))
            {
                values = [];
                return false;
            }

            items.Add(parsed);
        }

        if (items.Count == 0)
        {
            values = [];
            return false;
        }

        values = items.ToArray();
        return true;
    }

    private static bool TryConvertToInt32(object? value, out int result)
    {
        try
        {
            switch (value)
            {
                case null:
                    result = 0;
                    return false;
                case int intValue:
                    result = intValue;
                    return true;
                case long longValue and >= int.MinValue and <= int.MaxValue:
                    result = (int)longValue;
                    return true;
                case short shortValue:
                    result = shortValue;
                    return true;
                case byte byteValue:
                    result = byteValue;
                    return true;
                case sbyte sbyteValue:
                    result = sbyteValue;
                    return true;
                case uint uintValue and <= int.MaxValue:
                    result = (int)uintValue;
                    return true;
                case ushort ushortValue:
                    result = ushortValue;
                    return true;
                case string text when int.TryParse(text, out var parsedText):
                    result = parsedText;
                    return true;
                default:
                    result = Convert.ToInt32(value);
                    return true;
            }
        }
        catch
        {
            result = 0;
            return false;
        }
    }
}
