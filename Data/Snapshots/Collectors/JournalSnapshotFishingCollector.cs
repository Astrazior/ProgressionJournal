using ProgressionJournal.Commands;

namespace ProgressionJournal.Data.Snapshots.Collectors;

internal static class JournalSnapshotFishingCollector
{
    public static List<SnapshotFishingCatch> Collect(
        HashSet<int> includedItems,
        HashSet<int> includedNpcs,
        Func<int, string> getItemReference,
        Func<int, string> getNpcReference,
        Func<int, string> getStageId)
    {
        var result = includedItems
            .Select(itemId => new
            {
                Id = itemId,
                Availability = JournalFishingSourceResolver.GetItemAvailability(itemId)
            })
            .Where(static value => value.Availability.Observed)
            .Select(value => new SnapshotFishingCatch(
                "item",
                getItemReference(value.Id),
                value.Availability.EarliestStageIndex,
                getStageId(value.Availability.EarliestStageIndex),
                value.Availability.EarliestStageName,
                value.Availability.Conditions.ToList()))
            .ToList();
        result.AddRange(includedNpcs
            .Select(npcId => new
            {
                Id = npcId,
                Availability = JournalFishingSourceResolver.GetNpcAvailability(npcId)
            })
            .Where(static value => value.Availability.Observed)
            .Select(value => new SnapshotFishingCatch(
                "npc",
                getNpcReference(value.Id),
                value.Availability.EarliestStageIndex,
                getStageId(value.Availability.EarliestStageIndex),
                value.Availability.EarliestStageName,
                value.Availability.Conditions.ToList())));
        return result;
    }
}
