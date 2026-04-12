using Microsoft.Xna.Framework;
using ProgressionJournal.Common.Data;
using ProgressionJournal.Common.Progression;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;

namespace ProgressionJournal.Common.UI;

public sealed class JournalEntryPanel : UIPanel
{
	public JournalEntryPanel(JournalStageEntry entry)
	{
		Width.Set(0f, 1f);
		Height.Set(58f, 0f);
		SetPadding(8f);
		BorderColor = GetBorderColor(entry.Evaluation.Tier);
		BackgroundColor = GetBackgroundColor(entry.Evaluation.Tier);

		string itemName = entry.Entry.GetDisplayName();
		string categoryText = Language.GetTextValue($"Mods.ProgressionJournal.Categories.{entry.Entry.Category}");

		UIText title = new(itemName, 0.9f);
		title.Width.Set(-16f, 1f);
		Append(title);

		UIText meta = new(categoryText, 0.72f);
		meta.Top.Set(28f, 0f);
		meta.Width.Set(-16f, 1f);
		meta.TextColor = new Color(210, 220, 235);
		Append(meta);
	}

	private static Color GetBackgroundColor(RecommendationTier tier) => tier switch
	{
		RecommendationTier.Recommended => new Color(34, 65, 46),
		RecommendationTier.Situational => new Color(74, 68, 28),
		RecommendationTier.NotRecommended => new Color(68, 42, 22),
		RecommendationTier.Useless => new Color(62, 28, 28),
		_ => new Color(32, 38, 48)
	};

	private static Color GetBorderColor(RecommendationTier tier) => tier switch
	{
		RecommendationTier.Recommended => new Color(104, 178, 124),
		RecommendationTier.Situational => new Color(196, 176, 82),
		RecommendationTier.NotRecommended => new Color(196, 126, 74),
		RecommendationTier.Useless => new Color(198, 88, 88),
		_ => new Color(120, 138, 160)
	};
}
