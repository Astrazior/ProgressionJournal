using System.Reflection;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;

namespace ProgressionJournal.Data.Resolvers;

internal sealed class JournalRuntimeProgressionScenarios : IDisposable
{
    private static JournalProfile? _profileOverride;

    private readonly JournalProfileStageDocument[] _stages;
    private readonly JournalUnlockConditionDocument[][][] _stageVariants;
    private readonly HashSet<string>[][] _stageVariantKeys;
    private readonly Dictionary<string, BooleanFlagAccessor> _accessors;
    private readonly Dictionary<string, NpcKillCountAccessor> _npcKillCounts;

    public JournalRuntimeProgressionScenarios()
    {
        var profile = CurrentProfile;
        _stages = profile is not null
            ? profile.Stages.ToArray()
            : [];
        _stageVariants = BuildStageVariants(_stages);
        _stageVariantKeys = _stageVariants
            .Select(stage => stage
                .Select(variant => variant
                    .Select(GetConditionKey)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase))
                .ToArray())
            .ToArray();
        _accessors = BuildAccessors(_stages, profile);
        _npcKillCounts = BuildNpcKillCountAccessors(_stages);
        StageNames = _stages.Length == 0
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
            && stageIndex < _stages.Length
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

        foreach (var accessor in _npcKillCounts.Values)
        {
            accessor.Set(false);
        }
    }

    public int GetVariantCount(int stageIndex)
    {
        return stageIndex < 0
            ? 1
            : _stageVariants[Math.Min(stageIndex, _stageVariants.Length - 1)].Length;
    }

    public HashSet<string> GetVariantConditionKeys(int stageIndex, int variantIndex)
    {
        if (stageIndex < 0)
        {
            return [];
        }

        var effectiveStageIndex = Math.Min(stageIndex, _stageVariantKeys.Length - 1);
        if (variantIndex < 0 || variantIndex >= _stageVariantKeys[effectiveStageIndex].Length)
        {
            throw new ArgumentOutOfRangeException(nameof(variantIndex));
        }

        return new HashSet<string>(
            _stageVariantKeys[effectiveStageIndex][variantIndex],
            StringComparer.OrdinalIgnoreCase);
    }

    public bool IsVariantContinuation(
        int stageIndex,
        int variantIndex,
        int earlierStageIndex,
        int earlierVariantIndex)
    {
        if (stageIndex < 0
            || stageIndex >= _stageVariantKeys.Length
            || variantIndex < 0
            || variantIndex >= _stageVariantKeys[stageIndex].Length
            || earlierStageIndex < 0
            || earlierStageIndex >= _stageVariantKeys.Length
            || earlierVariantIndex < 0
            || earlierVariantIndex >= _stageVariantKeys[earlierStageIndex].Length)
        {
            return false;
        }

        var current = _stageVariantKeys[stageIndex][variantIndex];
        return _stageVariantKeys[earlierStageIndex][earlierVariantIndex].All(current.Contains);
    }

    public void Apply(int stageIndex, int variantIndex)
    {
        if (stageIndex < 0)
        {
            return;
        }

        var variants = _stageVariants[Math.Min(stageIndex, _stageVariants.Length - 1)];
        if (variantIndex < 0 || variantIndex >= variants.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(variantIndex));
        }

        foreach (var condition in variants[variantIndex])
        {
            ApplyCondition(condition);
        }
    }

    public void Dispose()
    {
        foreach (var accessor in _accessors.Values)
        {
            accessor.Restore();
        }

        foreach (var accessor in _npcKillCounts.Values)
        {
            accessor.Restore();
        }
    }

    private void ApplyCondition(JournalUnlockConditionDocument condition)
    {
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
            case "npc"
                when _npcKillCounts.TryGetValue(
                    $"npc:{condition.Mod}/{condition.Npc}",
                    out var npcAccessor):
                npcAccessor.Set(true);
                break;
        }
    }

    private static JournalUnlockConditionDocument[][][] BuildStageVariants(
        JournalProfileStageDocument[] stages)
    {
        if (stages.Length == 0)
        {
            return [[[]]];
        }

        var result = new JournalUnlockConditionDocument[stages.Length][][];
        JournalUnlockConditionDocument[][] accumulated = [[]];
        for (var index = 0; index < stages.Length; index++)
        {
            accumulated = CombineVariants(
                accumulated,
                BuildConditionVariants(stages[index].Unlock));
            result[index] = accumulated;
        }

        return result;
    }

    private static JournalUnlockConditionDocument[][] BuildConditionVariants(
        JournalUnlockConditionDocument condition)
    {
        if (condition.Conditions.Count == 0)
        {
            return [[condition]];
        }

        var childVariants = condition.Conditions
            .Select(BuildConditionVariants)
            .ToArray();
        if (string.Equals(condition.Mode, "any", StringComparison.OrdinalIgnoreCase))
        {
            return DeduplicateVariants(childVariants.SelectMany(static variants => variants));
        }

        JournalUnlockConditionDocument[][] combined = [[]];
        foreach (var variants in childVariants)
        {
            combined = CombineVariants(combined, variants);
        }

        return combined;
    }

    private static JournalUnlockConditionDocument[][] CombineVariants(
        JournalUnlockConditionDocument[][] left,
        JournalUnlockConditionDocument[][] right)
    {
        return DeduplicateVariants(left.SelectMany(
            leftVariant => right.Select(
                rightVariant => leftVariant.Concat(rightVariant).ToArray())));
    }

    private static JournalUnlockConditionDocument[][] DeduplicateVariants(
        IEnumerable<JournalUnlockConditionDocument[]> variants)
    {
        var result = new Dictionary<string, JournalUnlockConditionDocument[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var variant in variants)
        {
            var normalized = variant
                .DistinctBy(GetConditionKey, StringComparer.OrdinalIgnoreCase)
                .OrderBy(GetConditionKey, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            result.TryAdd(string.Join('\n', normalized.Select(GetConditionKey)), normalized);
        }

        return result.Values.ToArray();
    }

    private static string GetConditionKey(JournalUnlockConditionDocument condition)
    {
        return $"{condition.Type}\u001f{condition.Mod}\u001f{condition.Key}\u001f{condition.Npc}";
    }

    private static Dictionary<string, NpcKillCountAccessor> BuildNpcKillCountAccessors(
        IEnumerable<JournalProfileStageDocument> stages)
    {
        var result = new Dictionary<string, NpcKillCountAccessor>(StringComparer.OrdinalIgnoreCase);
        foreach (var condition in stages.SelectMany(static stage => EnumerateConditions(stage.Unlock))
                     .Where(static condition => string.Equals(
                         condition.Type.Trim(),
                         "npc",
                         StringComparison.OrdinalIgnoreCase)))
        {
            var key = $"npc:{condition.Mod}/{condition.Npc}";
            if (!result.ContainsKey(key)
                && TryCreateNpcKillCountAccessor(condition, out var accessor))
            {
                result[key] = accessor;
            }
        }

        return result;
    }

    private static bool TryCreateNpcKillCountAccessor(
        JournalUnlockConditionDocument condition,
        out NpcKillCountAccessor accessor)
    {
        if (string.IsNullOrWhiteSpace(condition.Mod)
            || string.IsNullOrWhiteSpace(condition.Npc)
            || !ModContent.TryFind(condition.Mod, condition.Npc, out ModNPC modNpc)
            || !ContentSamples.NpcsByNetId.TryGetValue(modNpc.Type, out var sample)
            || !ContentSamples.NpcBestiaryCreditIdsByNpcNetIds.TryGetValue(
                modNpc.Type,
                out var creditId)
            || string.IsNullOrWhiteSpace(creditId))
        {
            accessor = null!;
            return false;
        }

        var kills = Main.BestiaryTracker.Kills;
        accessor = new NpcKillCountAccessor(
            kills.GetKillCount(sample),
            value => Main.BestiaryTracker.Kills.SetKillCountDirectly(creditId, value));
        return true;
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

    private sealed class NpcKillCountAccessor(int originalValue, Action<int> setValue)
    {
        public void Set(bool value) => setValue(value ? 1 : 0);

        public void Restore() => setValue(originalValue);
    }

    private sealed class ProfileScope(JournalProfile? previous) : IDisposable
    {
        public void Dispose()
        {
            _profileOverride = previous;
        }
    }
}
