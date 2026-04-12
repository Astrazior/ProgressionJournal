using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProgressionJournal.Data;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace ProgressionJournal.UI;

public sealed class JournalEntrySlot : UIElement
{
	public static float WidthPixels => TextureAssets.InventoryBack9.Width();

	private readonly JournalStageEntry _entry;
	private Item _item;

	public JournalEntrySlot(JournalStageEntry entry)
	{
		_entry = entry;
		_item = new Item(entry.Entry.RepresentativeItemId);
		Width.Set(WidthPixels, 0f);
		Height.Set(TextureAssets.InventoryBack9.Height(), 0f);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		base.DrawSelf(spriteBatch);

		Main.instance.LoadItem(_entry.Entry.RepresentativeItemId);

		Rectangle inner = GetInnerDimensions().ToRectangle();
		float oldScale = Main.inventoryScale;
		var oldBack9 = TextureAssets.InventoryBack9;
		TextureAssets.InventoryBack9 = GetSlotTexture(_entry.Evaluation.Tier);

		Main.inventoryScale = 1f;
		ItemSlot.Draw(spriteBatch, ref _item, ItemSlot.Context.TrashItem, inner.TopLeft());

		TextureAssets.InventoryBack9 = oldBack9;
		Main.inventoryScale = oldScale;

		if (IsMouseHovering) {
			Main.hoverItemName = _entry.Entry.GetDisplayName();
		}
	}

	private static Asset<Texture2D> GetSlotTexture(RecommendationTier tier) => tier switch
	{
		RecommendationTier.Recommended => TextureAssets.InventoryBack3,
		RecommendationTier.Situational => TextureAssets.InventoryBack8,
		RecommendationTier.NotRecommended => TextureAssets.InventoryBack13,
		RecommendationTier.Useless => TextureAssets.InventoryBack11,
		_ => TextureAssets.InventoryBack9
	};
}
