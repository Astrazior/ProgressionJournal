namespace ProgressionJournal.Data.Models;

public sealed class JournalDropSource(
    string sourceName,
    int? sourceNpcType,
    int? sourceItemId,
    float dropRate,
    int stackMin,
    int stackMax,
    IEnumerable<string> conditions,
    bool showDropRate = true,
    string? sourceReference = null)
{
    public JournalDropSource(
        string sourceName,
        int? sourceNpcType,
        int? sourceItemId,
        float dropRate,
        int stackMin,
        int stackMax,
        IEnumerable<string> conditions)
        : this(
            sourceName,
            sourceNpcType,
            sourceItemId,
            dropRate,
            stackMin,
            stackMax,
            conditions,
            showDropRate: true)
    {
    }

    public string SourceName { get; } = sourceName;

    public int? SourceNpcType { get; } = sourceNpcType;

    public int? SourceItemId { get; } = sourceItemId;

    public string SourceReference { get; } = sourceReference ?? string.Empty;

    public float DropRate { get; } = dropRate;

    public bool ShowDropRate { get; } = showDropRate;

    public int StackMin { get; } = stackMin;

    public int StackMax { get; } = stackMax;

    public IReadOnlyList<string> Conditions { get; } = conditions
        .Where(static condition => !string.IsNullOrWhiteSpace(condition))
        .Distinct()
        .ToArray();
}
