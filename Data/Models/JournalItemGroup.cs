using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Localization;

namespace ProgressionJournal.Data.Models;

public sealed class JournalItemGroup
{
	public JournalItemGroup(IEnumerable<int> itemIds, string? displayNameLocalizationKey = null, int? displayBuffId = null)
	{
		ItemIds = itemIds.Distinct().ToArray();

		if (ItemIds.Count == 0) {
			throw new ArgumentException("A journal item group must contain at least one item id.", nameof(itemIds));
		}

		DisplayNameLocalizationKey = displayNameLocalizationKey;
		DisplayBuffId = displayBuffId;
		RepresentativeItemId = ItemIds[0];
	}

	public IReadOnlyList<int> ItemIds { get; }

	public string? DisplayNameLocalizationKey { get; }

	public int? DisplayBuffId { get; }

	public int RepresentativeItemId { get; }

	public bool HasAlternatives => ItemIds.Count > 1;

	public string GetDisplayName()
	{
		return !string.IsNullOrWhiteSpace(DisplayNameLocalizationKey) ? Language.GetTextValue(DisplayNameLocalizationKey) : string.Join(" / ", ItemIds.Select(Lang.GetItemNameValue));
	}
}

