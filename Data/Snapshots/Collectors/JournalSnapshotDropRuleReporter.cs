using ProgressionJournal.Commands;
using Terraria.GameContent.ItemDropRules;

namespace ProgressionJournal.Data.Snapshots.Collectors;

internal static class JournalSnapshotDropRuleReporter
{
    public static List<SnapshotDrop> Collect(
        List<IItemDropRule>? rules,
        string sourceType,
        string source,
        HashSet<int> includedItems,
        Func<int, string> getItemReference,
        Func<object?, SnapshotCondition> createCondition,
        Action<string, Exception> logDebug)
    {
        if (rules is null)
        {
            return [];
        }

        List<DropRateInfo> reported = [];
        foreach (var rule in rules)
        {
            try
            {
                rule.ReportDroprates(reported, new DropRateInfoChainFeed(1f));
            }
            catch (Exception exception)
            {
                logDebug($"Failed to inspect drop rates for snapshot source '{source}'.", exception);
            }
        }

        return reported
            .Where(drop => includedItems.Contains(drop.itemId))
            .Select(drop => new SnapshotDrop(
                sourceType,
                source,
                getItemReference(drop.itemId),
                drop.dropRate,
                drop.stackMin,
                drop.stackMax,
                EnumerateObjects(drop.conditions).Select(createCondition).ToList()))
            .ToList();
    }

    private static IEnumerable<object?> EnumerateObjects<T>(IEnumerable<T>? values)
    {
        return values?.Cast<object?>() ?? [];
    }
}
