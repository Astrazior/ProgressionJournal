using Terraria.ID;

namespace ProgressionJournal.Data.Models;

public sealed class JournalSavedBuildItemReference(
    int type,
    string modName,
    string itemName,
    string displayName,
    string itemData = "")
{
    public int Type { get; } = type;
    public string ModName { get; } = modName;
    public string ItemName { get; } = itemName;
    public string DisplayName { get; } = displayName;
    public string ItemData { get; } = itemData;

    public bool IsLoaded => Type > ItemID.None;
}