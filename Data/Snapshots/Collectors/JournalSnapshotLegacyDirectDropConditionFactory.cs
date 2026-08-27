using ProgressionJournal.Commands;

namespace ProgressionJournal.Data.Snapshots.Collectors;

internal static class JournalSnapshotLegacyDirectDropConditionFactory
{
    public static List<SnapshotCondition> Create(
        int[] requiredItemIds,
        Func<int, string> getItemReference)
    {
        if (requiredItemIds.Length == 0)
        {
            return [];
        }

        return
        [
            new SnapshotCondition(
                "ProgressionJournal.ScriptedItemGrant",
                "Granted by a scripted interaction after consuming the required item.",
                Facts: requiredItemIds
                    .Select(itemId => new SnapshotConditionFact(
                        "item-owned",
                        getItemReference(itemId)))
                    .ToList())
        ];
    }
}
