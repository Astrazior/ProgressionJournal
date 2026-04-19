using System.Collections.Generic;
using System.Linq;

namespace ProgressionJournal.Data.Models;

public sealed class JournalDropSource(
    string sourceName,
    int? sourceNpcType,
    int? sourceItemId,
    float dropRate,
    int stackMin,
    int stackMax,
    IEnumerable<string> conditions)
{
    public string SourceName { get; } = sourceName;

    public int? SourceNpcType { get; } = sourceNpcType;

    public int? SourceItemId { get; } = sourceItemId;

    public float DropRate { get; } = dropRate;

    public int StackMin { get; } = stackMin;

    public int StackMax { get; } = stackMax;

    public IReadOnlyList<string> Conditions { get; } = conditions
        .Where(static condition => !string.IsNullOrWhiteSpace(condition))
        .Distinct()
        .ToArray();
}
