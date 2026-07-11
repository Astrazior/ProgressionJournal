using System.Reflection;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ModLoader;

namespace ProgressionJournal.Data.Resolvers;

internal sealed class JournalRuntimeProgressionScenarios : IDisposable
{
    private static JournalProfile? _profileOverride;

    private readonly IReadOnlyList<JournalProfileStageDocument> _stages;
    private readonly Dictionary<string, BooleanFlagAccessor> _accessors;

    public JournalRuntimeProgressionScenarios()
    {
        var profile = CurrentProfile;
        _stages = profile is not null
            ? profile.Stages
            : [];
        _accessors = BuildAccessors(_stages, profile);
        StageNames = _stages.Count == 0
            ? [string.Empty]
            : _stages.Select(stage => stage.Name.Resolve()).ToArray();
    }

    internal static JournalProfile? CurrentProfile => _profileOverride
        ?? (JournalProfileRegistry.IsLoaded ? JournalProfileRegistry.Active : null);

    internal static IDisposable UseProfile(JournalProfile profile)
    {
        var previous = _profileOverride;
        _profileOverride = profile;
        return new ProfileScope(previous);
    }

    public int Count => StageNames.Count;

    public IReadOnlyList<string> StageNames { get; }

    public bool ChangesVanillaProgression(int stageIndex)
    {
        return stageIndex >= 0
            && stageIndex < _stages.Count
            && EnumerateConditions(_stages[stageIndex].Unlock).Any(static condition =>
                string.Equals(
                    condition.Type.Trim(),
                    "vanilla-flag",
                    StringComparison.OrdinalIgnoreCase));
    }

    public void Reset()
    {
        foreach (var accessor in _accessors.Values)
        {
            accessor.Set(false);
        }
    }

    public void Apply(int stageIndex)
    {
        for (var index = 0; index <= stageIndex && index < _stages.Count; index++)
        {
            ApplyCondition(_stages[index].Unlock);
        }
    }

    public void Dispose()
    {
        foreach (var accessor in _accessors.Values)
        {
            accessor.Restore();
        }
    }

    private void ApplyCondition(JournalUnlockConditionDocument condition)
    {
        foreach (var child in condition.Conditions)
        {
            ApplyCondition(child);
        }

        var type = condition.Type.Trim().ToLowerInvariant();
        switch (type)
        {
            case "mod-flag":
            {
                foreach (var key in SplitKeys(condition.Key))
                {
                    if (_accessors.TryGetValue($"mod:{condition.Mod}/{key}", out var accessor))
                    {
                        accessor.Set(true);
                    }
                }

                break;
            }
            case "vanilla-flag"
                when _accessors.TryGetValue($"vanilla:{condition.Key}", out var accessor):
                accessor.Set(true);
                ApplyDerivedVanillaFlags(condition.Key);
                break;
        }
    }

    private void ApplyDerivedVanillaFlags(string key)
    {
        if (key is "downedMechBoss1" or "downedMechBoss2" or "downedMechBoss3"
            && _accessors.TryGetValue("vanilla:downedMechBossAny", out var accessor))
        {
            accessor.Set(true);
        }
    }

    private static Dictionary<string, BooleanFlagAccessor> BuildAccessors(
        IEnumerable<JournalProfileStageDocument> stages,
        JournalProfile? profile)
    {
        var result = new Dictionary<string, BooleanFlagAccessor>(StringComparer.OrdinalIgnoreCase);
        foreach (var condition in stages.SelectMany(static stage => EnumerateConditions(stage.Unlock)))
        {
            var type = condition.Type.Trim().ToLowerInvariant();
            switch (type)
            {
                case "mod-flag"
                    when !string.IsNullOrWhiteSpace(condition.Mod)
                         && ModLoader.TryGetMod(condition.Mod, out var mod):
                {
                    foreach (var key in SplitKeys(condition.Key))
                    {
                        var accessorKey = $"mod:{condition.Mod}/{key}";
                        if (!result.ContainsKey(accessorKey)
                            && TryCreateModFlagAccessor(mod, key, accessorKey, out var accessor))
                        {
                            result[accessorKey] = accessor;
                        }
                    }

                    break;
                }
                case "vanilla-flag":
                {
                    var accessorKey = $"vanilla:{condition.Key}";
                    if (!result.ContainsKey(accessorKey)
                        && TryCreateVanillaFlagAccessor(condition.Key, accessorKey, out var accessor))
                    {
                        result[accessorKey] = accessor;
                    }

                    break;
                }
            }
        }

        if (result.Keys.Any(static key =>
                key is "vanilla:downedMechBoss1"
                    or "vanilla:downedMechBoss2"
                    or "vanilla:downedMechBoss3")
            && !result.ContainsKey("vanilla:downedMechBossAny")
            && TryCreateVanillaFlagAccessor(
                "downedMechBossAny",
                "vanilla:downedMechBossAny",
                out var mechBossAccessor))
        {
            result["vanilla:downedMechBossAny"] = mechBossAccessor;
        }

        AddIsolationAccessors(result, profile);
        return result;
    }

    private static void AddIsolationAccessors(
        IDictionary<string, BooleanFlagAccessor> accessors,
        JournalProfile? profile)
    {
        foreach (var type in new[]
                 {
                     typeof(Main),
                     typeof(NPC),
                     typeof(WorldGen),
                     typeof(DD2Event)
                 })
        {
            AddIsolationAccessors(type, accessors);
        }

        var relevantMods = profile is not null
            ? profile.Document.RequiredMods
                .Select(static requirement => requirement.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : [];
        foreach (var mod in ModLoader.Mods.Where(mod =>
                     mod.Code is not null
                     && (relevantMods.Count == 0 || relevantMods.Contains(mod.Name))))
        {
            foreach (var type in GetLoadableTypes(mod.Code))
            {
                AddIsolationAccessors(type, accessors);
            }
        }
    }

    private static void AddIsolationAccessors(
        Type type,
        IDictionary<string, BooleanFlagAccessor> accessors)
    {
        const BindingFlags flags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        foreach (var field in type.GetFields(flags).Where(static field =>
                     field.FieldType == typeof(bool)
                     && !field.IsInitOnly
                     && IsProgressionFlagName(field.Name)))
        {
            var key = $"isolation:{type.FullName}.{field.Name}";
            accessors.TryAdd(
                key,
                new BooleanFlagAccessor(
                    getValue: () => (bool)(field.GetValue(null) ?? false),
                    setValue: value => field.SetValue(null, value)));
        }
    }

    private static bool IsProgressionFlagName(string name)
    {
        var normalized = name.Trim('<', '>', '_').ToLowerInvariant();
        return normalized == "hardmode"
            || normalized.Contains("downed", StringComparison.Ordinal)
            || normalized.Contains("defeated", StringComparison.Ordinal)
            || normalized.Contains("killed", StringComparison.Ordinal)
            || normalized.Contains("unlocked", StringComparison.Ordinal)
            || normalized.Contains("saved", StringComparison.Ordinal)
            || normalized.Contains("rescued", StringComparison.Ordinal)
            || normalized.Contains("completed", StringComparison.Ordinal)
            || normalized.Contains("bossrush", StringComparison.Ordinal);
    }

    private static IEnumerable<JournalUnlockConditionDocument> EnumerateConditions(
        JournalUnlockConditionDocument condition)
    {
        yield return condition;
        foreach (var nested in condition.Conditions.SelectMany(EnumerateConditions))
        {
            yield return nested;
        }
    }

    private static string[] SplitKeys(string keys)
    {
        return keys.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static bool TryCreateModFlagAccessor(
        Mod mod,
        string key,
        string accessorKey,
        out BooleanFlagAccessor accessor)
    {
        foreach (var type in GetLoadableTypes(mod.Code))
        {
            if (TryCreateFlagAccessor(type, key, accessorKey, out accessor))
            {
                return true;
            }
        }

        accessor = null!;
        return false;
    }

    private static bool TryCreateVanillaFlagAccessor(
        string key,
        string accessorKey,
        out BooleanFlagAccessor accessor)
    {
        var separator = key.LastIndexOf('.');
        var typeName = separator > 0 ? key[..separator] : key == "hardMode" ? nameof(Main) : nameof(NPC);
        var memberName = separator > 0 ? key[(separator + 1)..] : key;
        var type = typeof(Main).Assembly.GetTypes().FirstOrDefault(candidate =>
            string.Equals(candidate.Name, typeName, StringComparison.Ordinal)
            || string.Equals(candidate.FullName, typeName, StringComparison.Ordinal));
        if (type is not null
            && TryCreateFlagAccessor(type, memberName, accessorKey, out accessor))
        {
            return true;
        }

        accessor = null!;
        return false;
    }

    private static bool TryCreateFlagAccessor(
        Type type,
        string key,
        string accessorKey,
        out BooleanFlagAccessor accessor)
    {
        const BindingFlags flags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        var bracketIndex = key.IndexOf('[');
        if (bracketIndex > 0
            && key.EndsWith(']')
            && int.TryParse(key[(bracketIndex + 1)..^1], out var arrayIndex))
        {
            var memberName = key[..bracketIndex];
            var arrayField = type.GetField(memberName, flags);
            if (arrayField?.FieldType == typeof(bool[]) && arrayField.GetValue(null) is bool[] values
                && arrayIndex >= 0 && arrayIndex < values.Length)
            {
                accessor = new BooleanFlagAccessor(
                    getValue: () => values[arrayIndex],
                    setValue: value => values[arrayIndex] = value);
                return true;
            }

            var arrayProperty = type.GetProperty(memberName, flags);
            if (arrayProperty?.PropertyType == typeof(bool[])
                && arrayProperty.GetValue(null) is bool[] propertyValues
                && arrayIndex >= 0 && arrayIndex < propertyValues.Length)
            {
                accessor = new BooleanFlagAccessor(
                    getValue: () => propertyValues[arrayIndex],
                    setValue: value => propertyValues[arrayIndex] = value);
                return true;
            }
        }

        foreach (var fieldName in new[] { key, $"_{key}", $"<{key}>k__BackingField" })
        {
            var field = type.GetField(fieldName, flags);
            if (field?.FieldType != typeof(bool) || field.IsInitOnly) continue;
            accessor = new BooleanFlagAccessor(
                getValue: () => (bool)(field.GetValue(null) ?? false),
                setValue: value => field.SetValue(null, value));
            return true;
        }

        var property = type.GetProperty(key, flags);
        if (property?.PropertyType == typeof(bool)
            && property is { GetMethod: { IsStatic: true }, SetMethod.IsStatic: true })
        {
            accessor = new BooleanFlagAccessor(
                getValue: () => (bool)(property.GetValue(null) ?? false),
                setValue: value => property.SetValue(null, value));
            return true;
        }

        accessor = null!;
        return false;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            ProgressionJournal.Instance?.Logger.Debug(
                $"Some types could not be loaded while preparing progression scenarios."
                + $"{Environment.NewLine}{exception}");
            return exception.Types.OfType<Type>();
        }
    }

    private sealed class BooleanFlagAccessor(Func<bool> getValue, Action<bool> setValue)
    {
        private readonly bool _originalValue = getValue();

        public void Set(bool value) => setValue(value);

        public void Restore() => setValue(_originalValue);
    }

    private sealed class ProfileScope(JournalProfile? previous) : IDisposable
    {
        public void Dispose()
        {
            _profileOverride = previous;
        }
    }
}
