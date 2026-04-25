using System.Collections.Generic;

namespace ProgressionJournal.Data.Models;

public sealed class JournalBuildCandidateGroup(string title, IReadOnlyList<JournalBuildCandidate> candidates)
{
    public string Title { get; } = title;

    public IReadOnlyList<JournalBuildCandidate> Candidates { get; } = candidates;
}
