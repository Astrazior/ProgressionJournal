using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProgressionJournal.Data;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace ProgressionJournal.UI;

public sealed class JournalEntrySlot : UIElement
{
	private readonly JournalStageEntry _entry;

	public JournalEntrySlot(JournalStageEntry entry)
	{
		_entry = entry;
		Width.Set(44f, 0f);
		Height.Set(44f, 0f);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		base.DrawSelf(spriteBatch);

		var dimensions = GetDimensions().ToRectangle();
		var slotTexture = TextureAssets.InventoryBack.Value;
		var slotColor = GetSlotColor(_entry.Evaluation.Tier);
		spriteBatch.Draw(slotTexture, new Vector2(dimensions.X, dimensions.Y), slotColor);

		Main.instance.LoadItem(_entry.Entry.RepresentativeItemId);
		var itemTexture = TextureAssets.Item[_entry.Entry.RepresentativeItemId].Value;
		var scale = MathF.Min(28f / itemTexture.Width, 28f / itemTexture.Height);
		var position = new Vector2(dimensions.X + dimensions.Width * 0.5f, dimensions.Y + dimensions.Height * 0.5f);

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

	private static Color GetSlotColor(RecommendationTier tier) => tier switch
	{
		RecommendationTier.Recommended => new Color(130, 210, 150),
		RecommendationTier.Situational => new Color(214, 191, 103),
		RecommendationTier.NotRecommended => new Color(222, 148, 98),
		RecommendationTier.Useless => new Color(214, 110, 110),
		_ => Color.White
	};
}
