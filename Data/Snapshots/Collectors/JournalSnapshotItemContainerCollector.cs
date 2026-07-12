using ProgressionJournal.Commands;
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

        return result;
    }
}
