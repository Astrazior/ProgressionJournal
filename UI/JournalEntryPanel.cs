using System;
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
		Height.Set(64f, 0f);
		SetPadding(8f);
		BorderColor = GetBorderColor(entry.Evaluation.Tier);
		BackgroundColor = GetBackgroundColor(entry.Evaluation.Tier);

		var itemName = entry.Entry.GetDisplayName();
		var categoryText = Language.GetTextValue($"Mods.ProgressionJournal.Categories.{entry.Entry.Category}");

		var title = new UIText(itemName, 0.9f);
		title.Left.Set(44f, 0f);
		title.Width.Set(-52f, 1f);
		Append(title);

		var meta = new UIText(categoryText, 0.72f);
		meta.Left.Set(44f, 0f);
		meta.Top.Set(28f, 0f);
		meta.Width.Set(-52f, 1f);
		meta.TextColor = new Color(210, 220, 235);
		Append(meta);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		base.DrawSelf(spriteBatch);

		Main.instance.LoadItem(_entry.Entry.RepresentativeItemId);
		var itemTexture = TextureAssets.Item[_entry.Entry.RepresentativeItemId].Value;
		var dimensions = GetInnerDimensions();
		var scale = MathF.Min(28f / itemTexture.Width, 28f / itemTexture.Height);
		var position = new Vector2(dimensions.X + 16f, dimensions.Y + dimensions.Height * 0.5f);

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
