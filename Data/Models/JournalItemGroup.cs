using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace ProgressionJournal.Data;

public sealed class JournalItemGroup
{
	public JournalItemGroup(IEnumerable<int> itemIds)
	{
		ItemIds = itemIds.Distinct().ToArray();

		if (ItemIds.Count == 0) {
			throw new ArgumentException("A journal item group must contain at least one item id.", nameof(itemIds));
		}

		RepresentativeItemId = ItemIds[0];
	}

	public IReadOnlyList<int> ItemIds { get; }

	public int RepresentativeItemId { get; }

	public bool HasAlternatives => ItemIds.Count > 1;

	public string GetDisplayName() => string.Join(" / ", ItemIds.Select(Lang.GetItemNameValue));
}
