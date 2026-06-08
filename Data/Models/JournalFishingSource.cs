namespace ProgressionJournal.Data.Models;

public sealed class JournalFishingSource(IEnumerable<string> conditions)
{
    public IReadOnlyList<string> Conditions { get; } = conditions
        .Where(static condition => !string.IsNullOrWhiteSpace(condition))
        .Distinct()
        .ToArray();
}
