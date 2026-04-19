using System.Collections.Generic;
using System.Linq;

namespace ProgressionJournal.Data.Models;

public sealed class JournalItemAcquisitionInfo(
    int itemId,
    IEnumerable<JournalRecipeSource> recipes,
    IEnumerable<JournalDropSource> drops,
    IEnumerable<JournalShopSource> shops)
{
    public int ItemId { get; } = itemId;

    public IReadOnlyList<JournalRecipeSource> Recipes { get; } = recipes.ToArray();

    public IReadOnlyList<JournalDropSource> Drops { get; } = drops.ToArray();

    public IReadOnlyList<JournalShopSource> Shops { get; } = shops.ToArray();

    public bool HasAnySources => Recipes.Count > 0 || Drops.Count > 0 || Shops.Count > 0;
}
