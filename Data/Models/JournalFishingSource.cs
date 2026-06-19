namespace ProgressionJournal.Data.Models;

public sealed class JournalFishingSource(IEnumerable<string> conditions)
{
    public IReadOnlyList<string> Conditions { get; } = conditions
        .Where(static condition => !string.IsNullOrWhiteSpace(condition))
        .Distinct()
        .ToArray();
}

public sealed class JournalFishingAvailability(
    bool observed,
    int earliestStageIndex,
    string earliestStageName,
    IEnumerable<string> conditions)
{
    public bool Observed { get; } = observed;

    public int EarliestStageIndex { get; } = earliestStageIndex;

    public string EarliestStageName { get; } = earliestStageName;

    public IReadOnlyList<string> Conditions { get; } = conditions
        .Where(static condition => !string.IsNullOrWhiteSpace(condition))
        .Distinct()
        .ToArray();
}
