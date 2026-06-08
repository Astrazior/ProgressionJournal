using Terraria.ID;

namespace ProgressionJournal.Data.Models;

public sealed class JournalSavedBuild(
    string name,
    string profileId,
    string classId,
    string stageId,
    IReadOnlyDictionary<string, JournalSavedBuildItemReference> selectedItems,
    bool isFavorite,
    long favoriteSortKey,
    string sourcePath)
{
    public string Name { get; } = name;

    public string ProfileId { get; } = profileId;

    public string ClassId { get; } = classId;

    public string StageId { get; } = stageId;

    public CombatClass CombatClass => JournalClassIds.ToLegacy(ClassId);

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
