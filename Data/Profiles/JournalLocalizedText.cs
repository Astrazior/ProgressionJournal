using System.Text.Json;
using System.Text.Json.Serialization;
using Terraria.Localization;

namespace ProgressionJournal.Data.Profiles;

[JsonConverter(typeof(JournalLocalizedTextConverter))]
public sealed class JournalLocalizedText
{
    public Dictionary<string, string> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string Key { get; internal set; } = string.Empty;

    public string Literal { get; internal set; } = string.Empty;

    public List<JournalLocalizedText> Arguments { get; } = [];

    public List<JournalLocalizedText> JoinedValues { get; } = [];

    public string Resolve()
    {
        if (!string.IsNullOrWhiteSpace(Key))
        {
            return Language.GetTextValue(
                Key,
                Arguments
                    .Select(static argument => (object)argument.Resolve())
                    .ToArray());
        }

        if (JoinedValues.Count > 0)
        {
            return string.Join(
                ", ",
                JoinedValues
                    .Select(static value => value.Resolve())
                    .Where(static value => !string.IsNullOrWhiteSpace(value)));
        }

        if (!string.IsNullOrWhiteSpace(Literal))
        {
            return Literal;
        }

        var culture = Language.ActiveCulture.Name;
        if (Values.TryGetValue(culture, out var localized) && !string.IsNullOrWhiteSpace(localized))
        {
            return localized;
        }

        if (Values.TryGetValue("en-US", out var english) && !string.IsNullOrWhiteSpace(english))
        {
            return english;
        }

        return Values.Values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Key)
        && string.IsNullOrWhiteSpace(Literal)
        && (JoinedValues.Count == 0 || JoinedValues.All(static value => value.IsEmpty))
        && (Values.Count == 0 || Values.Values.All(string.IsNullOrWhiteSpace));

    public bool HasLocale(string locale) =>
        Values.TryGetValue(locale, out var value) && !string.IsNullOrWhiteSpace(value);

    public override string ToString() => Resolve();

    public static JournalLocalizedText FromKey(
        string key,
        params JournalLocalizedText[] arguments)
    {
        var text = new JournalLocalizedText
        {
            Key = key
        };
        text.Arguments.AddRange(arguments);
        return text;
    }

    public static JournalLocalizedText FromLiteral(string value)
    {
        return new JournalLocalizedText
        {
            Literal = value
        };
    }

    public static JournalLocalizedText Join(IEnumerable<JournalLocalizedText> values)
    {
        var text = new JournalLocalizedText();
        text.JoinedValues.AddRange(values);
        return text;
    }

    public static implicit operator JournalLocalizedText(string value)
    {
        var text = new JournalLocalizedText();
        if (!string.IsNullOrWhiteSpace(value))
        {
            text.Values["en-US"] = value;
        }

        return text;
    }

    public static implicit operator string(JournalLocalizedText? value) => value?.Resolve() ?? string.Empty;
}

public sealed class JournalLocalizedTextConverter : JsonConverter<JournalLocalizedText>
{
    public override JournalLocalizedText Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = new JournalLocalizedText();
        if (reader.TokenType == JsonTokenType.String)
        {
            result.Values["en-US"] = reader.GetString() ?? string.Empty;
            return result;
        }

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Localized text must be a string or locale object.");
        }

        if (root.TryGetProperty("key", out var keyElement))
        {
            result.Key = keyElement.GetString() ?? string.Empty;
            if (!root.TryGetProperty("args", out var argumentsElement)
                || argumentsElement.ValueKind != JsonValueKind.Array) return result;
            foreach (var deserialized in argumentsElement.EnumerateArray().Select(argument => JsonSerializer.Deserialize<JournalLocalizedText>(
                         argument.GetRawText(),
                         options)).OfType<JournalLocalizedText>())
            {
                result.Arguments.Add(deserialized);
            }

            return result;
        }

        if (root.TryGetProperty("join", out var joinedValuesElement)
            && joinedValuesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var deserialized in joinedValuesElement.EnumerateArray().Select(joinedValue => JsonSerializer.Deserialize<JournalLocalizedText>(
                         joinedValue.GetRawText(),
                         options)).OfType<JournalLocalizedText>())
            {
                result.JoinedValues.Add(deserialized);
            }

            return result;
        }

        if (root.TryGetProperty("literal", out var literalElement))
        {
            result.Literal = literalElement.GetString() ?? string.Empty;
            return result;
        }

        foreach (var property in root.EnumerateObject())
        {
            result.Values[property.Name] = property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString() ?? string.Empty
                : string.Empty;
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, JournalLocalizedText value, JsonSerializerOptions options)
    {
        if (!string.IsNullOrWhiteSpace(value.Key))
        {
            writer.WriteStartObject();
            writer.WriteString("key", value.Key);
            if (value.Arguments.Count > 0)
            {
                writer.WritePropertyName("args");
                JsonSerializer.Serialize(writer, value.Arguments, options);
            }

            writer.WriteEndObject();
            return;
        }

        if (value.JoinedValues.Count > 0)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("join");
            JsonSerializer.Serialize(writer, value.JoinedValues, options);
            writer.WriteEndObject();
            return;
        }

        if (!string.IsNullOrWhiteSpace(value.Literal))
        {
            writer.WriteStartObject();
            writer.WriteString("literal", value.Literal);
            writer.WriteEndObject();
            return;
        }

        writer.WriteStartObject();
        foreach (var pair in value.Values.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            writer.WriteString(pair.Key, pair.Value);
        }

        writer.WriteEndObject();
    }
}
