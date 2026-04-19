using System.Collections.Generic;
using System.Linq;

namespace ProgressionJournal.Data.Models;

public sealed class JournalShopSource(
    string npcName,
    string shopName,
    IEnumerable<string> conditions)
{
    public string NpcName { get; } = npcName;

    public string ShopName { get; } = shopName;

    public IReadOnlyList<string> Conditions { get; } = conditions
        .Where(static condition => !string.IsNullOrWhiteSpace(condition))
        .Distinct()
        .ToArray();
}
