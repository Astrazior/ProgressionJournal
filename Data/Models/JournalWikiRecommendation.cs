namespace ProgressionJournal.Data.Models;

public sealed record JournalWikiRecommendation(
    string StageId,
    IReadOnlySet<string> ClassIds,
    string SourceName,
    string SourceUrl,
    JournalLocalizedText Target);
