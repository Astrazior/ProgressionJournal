namespace ProgressionJournal.Data.Models;

public sealed class JournalNpcSpawnAvailability(
    int npcType,
    bool observed,
    int earliestStageIndex,
    string earliestStageName,
    IEnumerable<string> conditions)
{
    public int NpcType { get; } = npcType;

    public bool Observed { get; } = observed;

    public int EarliestStageIndex { get; } = earliestStageIndex;

    public string EarliestStageName { get; } = earliestStageName;

    public IReadOnlyList<string> Conditions { get; } = conditions
        .Where(static condition => !string.IsNullOrWhiteSpace(condition))
        .Distinct()
        .ToArray();
}
