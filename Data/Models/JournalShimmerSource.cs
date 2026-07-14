using Terraria;

namespace ProgressionJournal.Data.Models;

public sealed class JournalShimmerSource(IEnumerable<Item> inputItems)
{
    public IReadOnlyList<Item> InputItems { get; } = inputItems
        .Select(static item => item.Clone())
        .ToArray();
}
