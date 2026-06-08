using Terraria.ModLoader;

namespace ProgressionJournal.UI.Utilities;

public static class JournalDamageClassCatalog
{
    private static IReadOnlyList<JournalDamageClassCandidate>? _candidates;

    public static IReadOnlyList<JournalDamageClassCandidate> GetCandidates()
    {
        if (_candidates is not null)
        {
            return _candidates;
        }

        Dictionary<string, JournalDamageClassCandidate> candidates =
            new(StringComparer.OrdinalIgnoreCase);

        AddCandidate(candidates, DamageClass.Melee, JournalClassIds.Melee, "Melee");
        AddCandidate(candidates, DamageClass.Ranged, JournalClassIds.Ranged, "Ranged");
        AddCandidate(candidates, DamageClass.Magic, JournalClassIds.Magic, "Magic");
        AddCandidate(candidates, DamageClass.Summon, JournalClassIds.Summoner, "Summoner");

        var emptyRun = 0;
        for (var type = 0; type < 4096 && emptyRun < 32; type++)
        {
            var damageClass = DamageClassLoader.GetDamageClass(type);
            if (damageClass is null)
            {
                emptyRun++;
                continue;
            }

            emptyRun = 0;
            AddCandidate(candidates, damageClass);
        }

        _candidates = candidates.Values
            .OrderBy(static value => value.IsVanilla ? 0 : 1)
            .ThenBy(static value => value.SourceName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(static value => value.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        return _candidates;
    }

    private static void AddCandidate(
        IDictionary<string, JournalDamageClassCandidate> candidates,
        DamageClass damageClass,
        string? forcedId = null,
        string? forcedName = null)
    {
        var type = damageClass.GetType();
        var typeName = type.Name;
        var fullTypeName = type.FullName ?? typeName;
        var isVanilla = type.Assembly == typeof(DamageClass).Assembly;
        if (isVanilla && forcedId is null)
        {
            return;
        }

        var sourceName = isVanilla ? "Terraria" : GetSourceName(type);
        var displayName = forcedName ?? CleanDisplayName(damageClass.DisplayName.Value, typeName);
        var id = forcedId ?? CreateId($"{sourceName}-{typeName}");
        var candidate = new JournalDamageClassCandidate(
            id,
            displayName,
            sourceName,
            [fullTypeName, typeName],
            isVanilla);

        candidates.TryAdd(fullTypeName, candidate);
    }

    private static string CleanDisplayName(string value, string fallback)
    {
        var name = value.Trim();
        if (name.EndsWith(" damage", StringComparison.CurrentCultureIgnoreCase))
        {
            name = name[..^" damage".Length].Trim();
        }

        return string.IsNullOrWhiteSpace(name) ? fallback.Replace("DamageClass", string.Empty) : name;
    }

    private static string GetSourceName(Type type)
    {
        var namespaceName = type.Namespace ?? string.Empty;
        var separator = namespaceName.IndexOf('.');
        return separator > 0 ? namespaceName[..separator] : type.Assembly.GetName().Name ?? "Mod";
    }

    private static string CreateId(string value)
    {
        return new string(value
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray())
            .Trim('-');
    }
}

public sealed record JournalDamageClassCandidate(
    string Id,
    string DisplayName,
    string SourceName,
    IReadOnlyList<string> DamageClassNames,
    bool IsVanilla);
