using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProgressionJournal.Data;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;

namespace ProgressionJournal.UI;

public sealed class JournalEntryPanel : UIPanel
{
	private readonly JournalStageEntry _entry;

	public JournalEntryPanel(JournalStageEntry entry)
	{
		_entry = entry;
		Width.Set(0f, 1f);
		Height.Set(74f, 0f);
		SetPadding(0f);
		BorderColor = GetBorderColor(entry.Evaluation.Tier);
		BackgroundColor = GetBackgroundColor(entry.Evaluation.Tier);

		string itemName = TrimForUi(entry.Entry.GetDisplayName(), 58);
		string categoryText = Language.GetTextValue($"Mods.ProgressionJournal.Categories.{entry.Entry.Category}");
		string detailText = entry.Entry.ItemIds.Count > 1
			? Language.GetTextValue(
				"Mods.ProgressionJournal.UI.EntryContains",
				TrimForUi(string.Join(", ", entry.Entry.ItemIds.Select(Lang.GetItemNameValue)), 92))
			: Language.GetTextValue("Mods.ProgressionJournal.UI.EntrySingleItem");
		string tierText = Language.GetTextValue($"Mods.ProgressionJournal.Tiers.{entry.Evaluation.Tier}");

		var title = new UIText(itemName, 0.62f, true);
		title.Left.Set(54f, 0f);
		title.Top.Set(10f, 0f);
		title.Width.Set(-190f, 1f);
		Append(title);

		var category = new UIText(categoryText, 0.48f);
		category.Left.Set(54f, 0f);
		category.Top.Set(34f, 0f);
		category.Width.Set(-190f, 1f);
		category.TextColor = new Color(216, 226, 236);
		Append(category);

		var details = new UIText(detailText, 0.42f);
		details.Left.Set(54f, 0f);
		details.Top.Set(50f, 0f);
		details.Width.Set(-76f, 1f);
		details.TextColor = new Color(174, 198, 218);
		Append(details);

		var tierLabel = new UIText(tierText, 0.5f, true);
		tierLabel.Left.Set(-122f, 1f);
		tierLabel.Top.Set(12f, 0f);
		tierLabel.Width.Set(108f, 0f);
		tierLabel.TextColor = GetBorderColor(entry.Evaluation.Tier);
		Append(tierLabel);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		base.DrawSelf(spriteBatch);

		Main.instance.LoadItem(_entry.Entry.RepresentativeItemId);
		var itemTexture = TextureAssets.Item[_entry.Entry.RepresentativeItemId].Value;
		var dimensions = GetDimensions().ToRectangle();
		var scale = MathF.Min(28f / itemTexture.Width, 28f / itemTexture.Height);
		var position = new Vector2(dimensions.X + 24f, dimensions.Y + dimensions.Height * 0.5f);

		spriteBatch.Draw(
			itemTexture,
			position,
			null,
			Color.White,
			0f,
			itemTexture.Size() * 0.5f,
			scale,
			SpriteEffects.None,
			0f);

		if (IsMouseHovering) {
			Main.hoverItemName = _entry.Entry.GetDisplayName();
		}
	}

	private static string TrimForUi(string text, int maxLength)
	{
		if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength) {
			return text;
		}

		return text[..(maxLength - 3)].TrimEnd() + "...";
	}

	private static Color GetBackgroundColor(RecommendationTier tier) => tier switch
	{
		RecommendationTier.Recommended => new Color(30, 58, 40),
		RecommendationTier.Situational => new Color(69, 60, 24),
		RecommendationTier.NotRecommended => new Color(73, 46, 24),
		RecommendationTier.Useless => new Color(68, 31, 31),
		_ => new Color(30, 38, 48)
	};

	private static Color GetBorderColor(RecommendationTier tier) => tier switch
	{
		RecommendationTier.Recommended => new Color(112, 194, 132),
		RecommendationTier.Situational => new Color(209, 186, 94),
		RecommendationTier.NotRecommended => new Color(214, 140, 88),
		RecommendationTier.Useless => new Color(212, 102, 102),
		_ => new Color(120, 138, 160)
	};
}
