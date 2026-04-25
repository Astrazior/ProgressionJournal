using System.Collections.Generic;

namespace ProgressionJournal.Data.Models;

public sealed class JournalBuildCandidateGroup(
    string title,
    IReadOnlyList<JournalBuildCandidate> candidates,
    int iconItemId = 0)
{
    public string Title { get; } = title;

    public IReadOnlyList<JournalBuildCandidate> Candidates { get; } = candidates;

    public int IconItemId { get; } = iconItemId;
}