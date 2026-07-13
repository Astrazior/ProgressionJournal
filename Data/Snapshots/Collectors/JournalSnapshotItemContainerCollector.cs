using ProgressionJournal.Commands;
using ProgressionJournal.Data.Resolvers;
using Terraria;

namespace ProgressionJournal.Data.Snapshots.Collectors;

internal static class JournalSnapshotItemContainerCollector
{
    public static List<SnapshotDrop> Collect(
        HashSet<int> includedItems,
        Func<int, string> getItemReference,
        Func<object?, SnapshotCondition> createCondition,
        Action<string, Exception> logDebug)
    {
        List<SnapshotDrop> result = [];
        foreach (var itemId in includedItems)
        {
            result.AddRange(JournalSnapshotDropRuleReporter.Collect(
                Main.ItemDropsDB.GetRulesForItemID(itemId),
                "container",
                getItemReference(itemId),
                includedItems,
                getItemReference,
                createCondition,
                logDebug));
        }

        result.AddRange(JournalLegacyDirectDropAnalyzer.GetAllItemDrops()
            .Where(drop => includedItems.Contains(drop.SourceItemId)
                && includedItems.Contains(drop.TargetItemId))
            .Select(drop => new SnapshotDrop(
                "container",
                getItemReference(drop.SourceItemId),
                getItemReference(drop.TargetItemId),
                drop.DropRate,
                drop.StackMin,
                drop.StackMax,
                [])));
        result.AddRange(JournalExactDropCatalog.GetAllItemDrops()
            .Where(drop => drop.SourceItemId is { } sourceItemId
                && includedItems.Contains(sourceItemId)
                && includedItems.Contains(drop.TargetItemId))
            .Select(drop => new SnapshotDrop(
                "container",
                getItemReference(drop.SourceItemId!.Value),
                getItemReference(drop.TargetItemId),
                drop.DropRate,
                drop.StackMin,
                drop.StackMax,
                drop.Conditions
                    .Select(static condition => new SnapshotCondition(
                        condition.Type,
                        condition.Description))
                    .ToList())));
        return result;
    }
}
