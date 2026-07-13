using ProgressionJournal.Commands;
using Terraria.ID;

namespace ProgressionJournal.Data.Snapshots.Collectors;

internal static class JournalSnapshotNpcAvailabilityCollector
{
    public static List<SnapshotNpcAvailability> Collect(
        HashSet<int> includedNpcs,
        Func<int, string> getNpcReference,
        Func<int, string> getStageId)
    {
        return includedNpcs.Select(npcId =>
        {
            var npc = ContentSamples.NpcsByNetId[npcId];
            if (npc.townNPC)
            {
                var availability = JournalTownNpcAvailabilityResolver.GetAvailability(npcId);
                return new SnapshotNpcAvailability(
                    getNpcReference(npcId),
                    "town",
                    availability.Observed,
                    availability.EarliestStageIndex,
                    getStageId(availability.EarliestStageIndex),
                    availability.EarliestStageName,
                    [],
                    []);
            }

            var spawnAvailability = JournalNpcSpawnAvailabilityResolver.GetAvailability(npcId);
            return new SnapshotNpcAvailability(
                getNpcReference(npcId),
                "spawn",
                spawnAvailability.Observed,
                spawnAvailability.EarliestStageIndex,
                getStageId(spawnAvailability.EarliestStageIndex),
                spawnAvailability.EarliestStageName,
                spawnAvailability.Conditions.ToList(),
                spawnAvailability.EventCategories.ToList());
        }).ToList();
    }
}
