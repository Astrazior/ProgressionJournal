using ProgressionJournal.Commands;
using ProgressionJournal.Data.Resolvers;
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
        if (rules is not { Count: > 0 })
        {
            return [];
        }

        using var worldState = new JournalWorldStateIsolation();
        List<SnapshotDrop> result = [];
        foreach (var scenario in JournalWorldStateIsolation.ComprehensiveScenarios)
        {
            JournalWorldStateIsolation.ApplyScenario(scenario);
            List<DropRateInfo> reported = [];
            foreach (var rule in rules)
            {
                try
                {
                    rule.ReportDroprates(reported, new DropRateInfoChainFeed(1f));
                }
                catch (Exception exception)
                {
                    logDebug(
                        $"Failed to inspect drop rates for snapshot source '{source}' "
                        + $"in world scenario '{scenario.Name}'.",
                        exception);
                }
            }

            result.AddRange(reported
                .Where(drop => includedItems.Contains(drop.itemId))
                .Select(drop => new SnapshotDrop(
                    sourceType,
                    source,
                    getItemReference(drop.itemId),
                    drop.dropRate,
                    drop.stackMin,
                    drop.stackMax,
                    EnumerateObjects(drop.conditions).Select(createCondition).ToList())));
        }

        return result
            .DistinctBy(BuildIdentity)
            .ToList();
    }

    private static string BuildIdentity(SnapshotDrop drop)
    {
        return $"{drop.SourceType}\u001f{drop.Source}\u001f{drop.Item}\u001f"
               + $"{drop.Rate:R}\u001f{drop.StackMin}\u001f{drop.StackMax}\u001f"
               + string.Join(
                   '\u001e',
                   drop.Conditions.Select(condition =>
                       $"{condition.Type}\u001d{condition.Key}\u001d{condition.Description}\u001d"
                       + string.Join(
                           '\u001c',
                           condition.Facts?.Select(fact => $"{fact.Kind}:{fact.Item}") ?? [])));
    }

    private static IEnumerable<object?> EnumerateObjects<T>(IEnumerable<T>? values)
    {
        return values?.Cast<object?>() ?? [];
    }
}
