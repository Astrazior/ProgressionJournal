using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProgressionJournal.Data;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.UI;

namespace ProgressionJournal.UI;

public sealed class JournalEntrySlot : UIElement
{
	public static float WidthPixels => TextureAssets.InventoryBack9.Width();
	public static float SlotStep => WidthPixels + 4f;
	private static readonly Color OptionalBossBorderColor = new(224, 196, 112);
	private static readonly Color OptionalBossAccentColor = new(255, 232, 156);

	private readonly JournalStageEntry _entry;
	private readonly Item[] _items;

	public JournalEntrySlot(JournalStageEntry entry)
	{
		_entry = entry;
		_items = entry.Entry.ItemIds.Select(CreateItem).ToArray();
		Width.Set(GetVisualWidth(_items.Length), 0f);
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

		for (int index = 0; index < _items.Length; index++) {
			Main.instance.LoadItem(_items[index].type);
			Vector2 slotPosition = inner.TopLeft() + new Vector2(index * SlotStep, 0f);
			ItemSlot.Draw(spriteBatch, ref _items[index], ItemSlot.Context.TrashItem, slotPosition);
		}

		if (_entry.Entry.HasOptionalBossRequirement) {
			DrawOptionalBossFrame(spriteBatch, inner);
			DrawOptionalBossMarker(spriteBatch, inner);
		}

		TextureAssets.InventoryBack9 = oldBack9;
		Main.inventoryScale = oldScale;

		if (IsMouseHovering) {
			int hoveredIndex = GetHoveredItemIndex(inner);

			if (hoveredIndex >= 0) {
				var hoverItem = _items[hoveredIndex].Clone();
				string hoverName = hoverItem.Name;

				if (_entry.Entry.OptionalBossRequirement is { } requirementId) {
					hoverName = $"{hoverName} * {GetOptionalBossRequirementText(requirementId)}";
					hoverItem.SetNameOverride(hoverName);
				}

				Main.HoverItem = hoverItem;
				Main.hoverItemName = hoverName;
			}
			else {
				Main.hoverItemName = GetEntryHoverName();
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
		for (int index = 0; index < _items.Length; index++) {
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
		RecommendationTier.Situational => TextureAssets.InventoryBack8,
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

	private string GetEntryHoverName()
	{
		string displayName = _entry.Entry.GetDisplayName();
		return _entry.Entry.OptionalBossRequirement is { } requirementId
			? $"{displayName} * {GetOptionalBossRequirementText(requirementId)}"
			: displayName;
	}

	private static string GetOptionalBossRequirementText(OptionalBossRequirementId requirementId)
	{
		return Language.GetTextValue($"Mods.ProgressionJournal.OptionalBosses.{requirementId}");
	}

	private static void DrawOptionalBossFrame(SpriteBatch spriteBatch, Rectangle inner)
	{
		var pixel = TextureAssets.MagicPixel.Value;
		const int thickness = 2;

		spriteBatch.Draw(pixel, new Rectangle(inner.Left - 2, inner.Top - 2, inner.Width + 4, thickness), OptionalBossBorderColor);
		spriteBatch.Draw(pixel, new Rectangle(inner.Left - 2, inner.Bottom, inner.Width + 4, thickness), OptionalBossBorderColor);
		spriteBatch.Draw(pixel, new Rectangle(inner.Left - 2, inner.Top - 2, thickness, inner.Height + 4), OptionalBossBorderColor);
		spriteBatch.Draw(pixel, new Rectangle(inner.Right, inner.Top - 2, thickness, inner.Height + 4), OptionalBossBorderColor);
	}

	private static void DrawOptionalBossMarker(SpriteBatch spriteBatch, Rectangle inner)
	{
		Utils.DrawBorderStringFourWay(
			spriteBatch,
			FontAssets.MouseText.Value,
			"*",
			inner.X + 3f,
			inner.Y - 2f,
			OptionalBossAccentColor,
			Color.Black,
			Vector2.Zero,
			0.72f);
	}
}
