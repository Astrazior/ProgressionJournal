using System.Collections.Generic;
using System;
using Terraria.ID;

namespace ProgressionJournal.Data.Models;

public sealed class JournalSavedBuild(
    string name,
    CombatClass combatClass,
    ProgressionStageId stageId,
    IReadOnlyDictionary<string, int> selectedItems,
    string sourcePath)
{
    public string Name { get; } = name;

    public CombatClass CombatClass { get; } = combatClass;

    public ProgressionStageId StageId { get; } = stageId;

    public string SourcePath { get; } = sourcePath;

    public IReadOnlyDictionary<string, int> SelectedItems { get; } = new Dictionary<string, int>(selectedItems, StringComparer.OrdinalIgnoreCase);

    public int GetSelectedItemId(string slotKey)
    {
        return SelectedItems.GetValueOrDefault(slotKey, ItemID.None);
    }
}
