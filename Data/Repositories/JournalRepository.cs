using System;
using System.Collections.Generic;
using System.Linq;

namespace ProgressionJournal.Data.Repositories;

public static partial class JournalRepository
{
    private static readonly Lazy<IReadOnlyList<JournalEntry>> Entries = new(BuildEntries);
    private static readonly Lazy<IReadOnlyList<JournalPreset>> Presets = new(BuildPresets);

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
        return entries;
    }

    private static IReadOnlyList<JournalPreset> BuildPresets() => [];
}

