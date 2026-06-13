using System.Text.Json;
using System.Text.Json.Serialization;
using Terraria.Localization;

namespace ProgressionJournal.Data.Profiles;

[JsonConverter(typeof(JournalLocalizedTextConverter))]
public sealed class JournalLocalizedText
{
    public Dictionary<string, string> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string Resolve()
    {
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

    public bool IsEmpty => Values.Count == 0 || Values.Values.All(string.IsNullOrWhiteSpace);

    public bool HasLocale(string locale) =>
        Values.TryGetValue(locale, out var value) && !string.IsNullOrWhiteSpace(value);

    public override string ToString() => Resolve();

    public static implicit operator JournalLocalizedText(string value)
    {
        var text = new JournalLocalizedText();
        if (!string.IsNullOrWhiteSpace(value))
        {
            text.Values["en-US"] = value;
        }

        return text;
    }

    public static implicit operator string(JournalLocalizedText value) => value?.Resolve() ?? string.Empty;
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

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Localized text must be a string or locale object.");
        }

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var locale = reader.GetString() ?? string.Empty;
            reader.Read();
            result.Values[locale] = reader.TokenType == JsonTokenType.String
                ? reader.GetString() ?? string.Empty
                : string.Empty;
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, JournalLocalizedText value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var pair in value.Values.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            writer.WriteString(pair.Key, pair.Value);
        }

        writer.WriteEndObject();
    }
}
