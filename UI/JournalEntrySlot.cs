using System.Linq;
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
	private const int AlternativeCycleTicks = 90;
	public static float WidthPixels => TextureAssets.InventoryBack9.Width();
	public static float SlotStep => WidthPixels + 4f;
	private static readonly Color AlternativeAccentColor = new(162, 214, 255);

	private readonly JournalStageEntry _entry;
	private readonly Item[][] _itemGroups;

	public JournalEntrySlot(JournalStageEntry entry)
	{
		_entry = entry;
		_itemGroups = entry.Entry.ItemGroups
			.Select(group => group.ItemIds.Select(CreateItem).ToArray())
			.ToArray();
		Width.Set(GetVisualWidth(_itemGroups.Length), 0f);
		Height.Set(TextureAssets.InventoryBack9.Height(), 0f);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		base.DrawSelf(spriteBatch);

		Rectangle inner = GetInnerDimensions().ToRectangle();
		float oldScale = Main.inventoryScale;
		var oldBack9 = TextureAssets.InventoryBack9;
		TextureAssets.InventoryBack9 = GetSlotTexture(_entry.Evaluation.Tier);

		Main.inventoryScale = 1f;

		for (int index = 0; index < _itemGroups.Length; index++) {
			var displayItem = GetDisplayedItem(index);
			Main.instance.LoadItem(displayItem.type);
			Vector2 slotPosition = inner.TopLeft() + new Vector2(index * SlotStep, 0f);
			ItemSlot.Draw(spriteBatch, ref displayItem, ItemSlot.Context.TrashItem, slotPosition);

			if (_entry.Entry.ItemGroups[index].HasAlternatives) {
				DrawAlternativeMarker(spriteBatch, slotPosition);
			}
		}

		TextureAssets.InventoryBack9 = oldBack9;
		Main.inventoryScale = oldScale;

		if (IsMouseHovering) {
			int hoveredIndex = GetHoveredItemIndex(inner);

			if (hoveredIndex >= 0) {
				var hoverItem = GetDisplayedItem(hoveredIndex).Clone();
				string hoverName = _entry.Entry.ItemGroups[hoveredIndex].GetDisplayName();
				hoverItem.SetNameOverride(hoverName);
				Main.HoverItem = hoverItem;
				Main.hoverItemName = hoverName;
			}
		}
	}

	public static float GetVisualWidth(int itemCount)
	{
		if (itemCount <= 0) {
			return 0f;
		}

		return WidthPixels + ((itemCount - 1) * SlotStep);
	}

	private int GetHoveredItemIndex(Rectangle inner)
	{
		for (int index = 0; index < _itemGroups.Length; index++) {
			var slotRectangle = new Rectangle(
				inner.X + (int)(index * SlotStep),
				inner.Y,
				(int)WidthPixels,
				inner.Height);

			if (slotRectangle.Contains(Main.MouseScreen.ToPoint())) {
				return index;
			}
		}

		return -1;
	}

	private static Asset<Texture2D> GetSlotTexture(RecommendationTier tier) => tier switch
	{
		RecommendationTier.Recommended => TextureAssets.InventoryBack3,
		RecommendationTier.Additional => TextureAssets.InventoryBack8,
		RecommendationTier.NotRecommended => TextureAssets.InventoryBack13,
		RecommendationTier.Useless => TextureAssets.InventoryBack11,
		_ => TextureAssets.InventoryBack9
	};

	private static Item CreateItem(int itemId)
	{
		var item = new Item();
		item.SetDefaults(itemId);
		return item;
	}

	private Item GetDisplayedItem(int groupIndex)
	{
		var groupItems = _itemGroups[groupIndex];
		if (groupItems.Length == 1) {
			return groupItems[0].Clone();
		}

		int cycleIndex = GetAlternativeCycleIndex() % groupItems.Length;
		return groupItems[cycleIndex].Clone();
	}

	private static int GetAlternativeCycleIndex()
	{
		return (int)(Main.GameUpdateCount / AlternativeCycleTicks);
	}

	private static void DrawAlternativeMarker(SpriteBatch spriteBatch, Vector2 slotPosition)
	{
		Utils.DrawBorderStringFourWay(
			spriteBatch,
			FontAssets.MouseText.Value,
			"/",
			slotPosition.X + WidthPixels - 11f,
			slotPosition.Y - 2f,
			AlternativeAccentColor,
			Color.Black,
			Vector2.Zero,
			0.64f);
	}
}

