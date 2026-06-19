namespace ProgressionJournal.Data.Models;

public sealed class JournalTownNpcAvailability(
    int npcType,
    bool observed,
    int earliestStageIndex,
    string earliestStageName,
    bool requiresSpecialUnlock,
    bool requiresInventory,
    bool requiresTownPopulation)
{
    public int NpcType { get; } = npcType;

    public bool Observed { get; } = observed;

    public int EarliestStageIndex { get; } = earliestStageIndex;

    public string EarliestStageName { get; } = earliestStageName;

    public bool RequiresSpecialUnlock { get; } = requiresSpecialUnlock;

    public bool RequiresInventory { get; } = requiresInventory;

    public bool RequiresTownPopulation { get; } = requiresTownPopulation;
}
