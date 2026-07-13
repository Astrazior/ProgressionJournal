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
        Func<object?, SnapshotCondition> createCondition,
        Func<int, string> getStageId)
    {
        var shops = NPCShopDatabase.AllShops
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
                    getStageId(stageIndex),
                    stageName);
            })
            .ToList();

        shops.AddRange(JournalExactShopCatalog.GetAllSources()
            .Where(source => includedNpcs.Contains(source.NpcType)
                && includedItems.Contains(source.TargetItemId))
            .Select(source =>
            {
                var availability = JournalTownNpcAvailabilityResolver.GetAvailability(source.NpcType);
                return new SnapshotShop(
                    getNpcReference(source.NpcType),
                    "Shop",
                    getItemReference(source.TargetItemId),
                    source.Conditions
                        .Select(static condition => new SnapshotCondition(condition.Type, condition.Description))
                        .ToList(),
                    availability.Observed,
                    availability.EarliestStageIndex,
                    getStageId(availability.EarliestStageIndex),
                    availability.EarliestStageName);
            }));

        return shops;
    }

    private static IEnumerable<object?> EnumerateObjects<T>(IEnumerable<T>? values)
    {
        return values?.Cast<object?>() ?? [];
    }
}
