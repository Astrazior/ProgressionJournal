using ProgressionJournal.Commands;
using Terraria.ModLoader;

namespace ProgressionJournal.Data.Snapshots.Collectors;

internal static class JournalSnapshotShopCollector
{
    public static List<SnapshotShop> Collect(
        HashSet<int> includedItems,
        HashSet<int> includedNpcs,
        Func<int, string> getItemReference,
        Func<int, string> getNpcReference,
        Func<object?, SnapshotCondition> createCondition)
    {
        return NPCShopDatabase.AllShops
            .SelectMany(static shop => shop.ActiveEntries.Select(entry => new { shop, entry }))
            .Where(pair => pair.entry.Item is not null
                && !pair.entry.Item.IsAir
                && includedNpcs.Contains(pair.shop.NpcType)
                && includedItems.Contains(pair.entry.Item.type))
            .Select(pair =>
            {
                var observed = JournalTownNpcAvailabilityResolver.TryGetShopStage(
                    pair.shop.NpcType,
                    pair.shop.Name,
                    pair.entry.Item.type,
                    out var stageIndex,
                    out var stageName);
                return new SnapshotShop(
                    getNpcReference(pair.shop.NpcType),
                    pair.shop.Name,
                    getItemReference(pair.entry.Item.type),
                    EnumerateObjects(pair.entry.Conditions).Select(createCondition).ToList(),
                    observed,
                    stageIndex,
                    stageName);
            })
            .ToList();
    }

    private static IEnumerable<object?> EnumerateObjects<T>(IEnumerable<T>? values)
    {
        return values?.Cast<object?>() ?? [];
    }
}
