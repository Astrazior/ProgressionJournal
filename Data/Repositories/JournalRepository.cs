using System;
using System.Collections.Generic;
using System.Linq;

namespace ProgressionJournal.Data.Repositories;

public static partial class JournalRepository
{
    private static readonly Lazy<IReadOnlyList<JournalEntry>> Entries = new(BuildEntries);
    private static readonly Lazy<IReadOnlyList<JournalPreset>> Presets = new(BuildPresets);
    private static readonly Lazy<IReadOnlyList<JournalCombatBuffEntry>> CombatBuffEntries = new(BuildCombatBuffEntries);
    private static readonly List<JournalEntry> ExternalEntries = [];

    public static IReadOnlyList<JournalStageEntry> GetEntries(ProgressionStageId stageId, CombatClass combatClass)
    {
        return Entries.Value
            .Where(entry => entry.AppliesToClass(combatClass) && entry.TryGetEvaluation(stageId, out _))
            .Select(entry => new JournalStageEntry(entry, entry.GetEvaluation(stageId)))
            .OrderBy(entry => JournalOrdering.GetTierOrder(entry.Evaluation.Tier))
            .ThenBy(entry => JournalOrdering.GetCategoryOrder(entry.Entry.Category))
            .ThenBy(entry => JournalOrdering.GetStageEntryDisplayOrderOverride(stageId, entry.Entry.Key))
            .ThenBy(entry => entry.Entry.GetDisplayName(), StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyList<JournalPreset> GetPresets(ProgressionStageId stageId, CombatClass combatClass)
    {
        return Presets.Value
            .Where(preset => preset.StageId == stageId && preset.CombatClass == combatClass)
            .ToArray();
    }

    private static List<JournalEntry> BuildEntries()
    {
        List<JournalEntry> entries = [];
        AddWeaponEntries(entries);
        AddClassSpecificEntries(entries);
        AddArmorEntries(entries);
        AddAccessoryEntries(entries);
        entries.AddRange(ExternalEntries);
        return entries;
    }

    private static IReadOnlyList<JournalPreset> BuildPresets() => [];

    public static void RegisterExternalEntry(JournalEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (Entries.IsValueCreated)
        {
            throw new InvalidOperationException("External journal entries must be registered before the repository is initialized. Register them in Mod.PostSetupContent or ModSystem.PostSetupContent.");
        }

        if (ExternalEntries.Any(existing => string.Equals(existing.Key, entry.Key, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"An external journal entry with key '{entry.Key}' is already registered.");
        }

        ExternalEntries.Add(entry);
    }

    internal static void ClearExternalContent()
    {
        ExternalEntries.Clear();
    }
}

