using System.Collections.Generic;
using System;
using System.Linq;
using Terraria.ID;

namespace ProgressionJournal.Data.Models;

public sealed class JournalSavedBuild(
    string name,
    CombatClass combatClass,
    ProgressionStageId stageId,
    IReadOnlyDictionary<string, JournalSavedBuildItemReference> selectedItems,
    bool isFavorite,
    long favoriteSortKey,
    string sourcePath)
{
    public string Name { get; } = name;

    public CombatClass CombatClass { get; } = combatClass;

    public ProgressionStageId StageId { get; } = stageId;

    public string SourcePath { get; } = sourcePath;

    public bool IsFavorite { get; } = isFavorite;

    public long FavoriteSortKey { get; } = favoriteSortKey;

    public IReadOnlyDictionary<string, JournalSavedBuildItemReference> ItemReferences { get; } = new Dictionary<string, JournalSavedBuildItemReference>(selectedItems, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, int> SelectedItems { get; } = selectedItems
        .Where(static pair => pair.Value.IsLoaded)
        .ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value.Type,
            StringComparer.OrdinalIgnoreCase);

    public int GetSelectedItemId(string slotKey)
    {
        return SelectedItems.GetValueOrDefault(slotKey, ItemID.None);
    }

    public JournalSavedBuildItemReference? GetSelectedItemReference(string slotKey)
    {
        return ItemReferences.GetValueOrDefault(slotKey);
    }
}
