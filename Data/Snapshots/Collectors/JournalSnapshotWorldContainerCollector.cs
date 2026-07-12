using ProgressionJournal.Commands;
using Terraria.ID;
using Terraria.ModLoader;

namespace ProgressionJournal.Data.Snapshots.Collectors;

internal static class JournalSnapshotWorldContainerCollector
{
    public static List<SnapshotDrop> Collect(HashSet<int> includedItems)
    {
        return JournalContainerLootCatalog.GetAllDrops()
            .Where(drop => TryResolveItemReference(drop.TargetItem, out var targetItemId)
                && includedItems.Contains(targetItemId))
            .Select(drop => new SnapshotDrop(
                "container",
                drop.SourceItem,
                drop.TargetItem,
                drop.DropRate,
                drop.StackMin,
                drop.StackMax,
                []))
            .ToList();
    }

    private static bool TryResolveItemReference(string reference, out int itemId)
    {
        itemId = ItemID.None;
        var separator = reference.IndexOf('/');
        if (separator <= 0 || separator >= reference.Length - 1)
        {
            return false;
        }

        var modName = reference[..separator];
        var itemName = reference[(separator + 1)..];
        if (string.Equals(modName, "Terraria", StringComparison.OrdinalIgnoreCase))
        {
            return ItemID.Search.TryGetId(itemName, out itemId);
        }

        if (!ModContent.TryFind<ModItem>($"{modName}/{itemName}", out var modItem))
        {
            return false;
        }

        itemId = modItem.Type;
        return true;
    }
}
