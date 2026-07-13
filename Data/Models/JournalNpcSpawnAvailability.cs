namespace ProgressionJournal.Data.Models;

public sealed class JournalNpcSpawnAvailability(
    int npcType,
    bool observed,
    int earliestStageIndex,
    string earliestStageName,
    IEnumerable<string> conditions,
    IEnumerable<string>? eventCategories = null)
{
    public int NpcType { get; } = npcType;

    public bool Observed { get; } = observed;

    public int EarliestStageIndex { get; } = earliestStageIndex;

    public string EarliestStageName { get; } = earliestStageName;

    public IReadOnlyList<string> Conditions { get; } = conditions
        .Where(static condition => !string.IsNullOrWhiteSpace(condition))
        .Distinct()
        .ToArray();

    public IReadOnlyList<string> EventCategories { get; } = (eventCategories ?? [])
        .Where(static category => !string.IsNullOrWhiteSpace(category))
        .Distinct(StringComparer.Ordinal)
        .ToArray();
}
