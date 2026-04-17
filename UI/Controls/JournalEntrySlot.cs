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

    private readonly JournalStageEntry _entry;
    private readonly Item[][] _itemGroups;

    public JournalEntrySlot(JournalStageEntry entry)
    {
        _entry = entry;
        _itemGroups = entry.Entry.ItemGroups
            .Select(group => group.ItemIds.Select(JournalItemUtilities.CreateItem).ToArray())
            .ToArray();

        Width.Set(GetVisualWidth(_itemGroups.Length), 0f);
        Height.Set(TextureAssets.InventoryBack9.Height(), 0f);
    }

    public static float WidthPixels => TextureAssets.InventoryBack9.Width();

    public static float SlotStep => WidthPixels + 4f;

    public static float GetVisualWidth(int itemCount)
    {
        if (itemCount <= 0)
        {
            return 0f;
        }

        return WidthPixels + (itemCount - 1) * SlotStep;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        var inner = GetInnerDimensions().ToRectangle();
        for (var index = 0; index < _itemGroups.Length; index++)
        {
            var displayItem = GetDisplayedItem(index);
            Main.instance.LoadItem(displayItem.type);

            var slotPosition = inner.TopLeft() + new Vector2(index * SlotStep, 0f);
            var slotTexture = GetSlotTexture(_entry.Evaluation.Tier);
            DrawSlotBackground(spriteBatch, slotPosition, slotTexture);

            var itemCenter = slotPosition + new Vector2(slotTexture.Value.Width * 0.5f, slotTexture.Value.Height * 0.5f);
            ItemSlot.DrawItemIcon(displayItem, ItemSlot.Context.TrashItem, spriteBatch, itemCenter, 1f, WidthPixels, Color.White);

            if (_entry.Entry.ItemGroups[index].HasAlternatives)
            {
                DrawAlternativeMarker(spriteBatch, slotPosition);
            }
        }

        if (!IsMouseHovering)
        {
            return;
        }

        var hoveredIndex = GetHoveredItemIndex(inner);
        if (hoveredIndex < 0)
        {
            return;
        }

        var hoverItem = GetDisplayedItem(hoveredIndex).Clone();
        var hoverName = _entry.Entry.ItemGroups[hoveredIndex].GetDisplayName();
        hoverItem.SetNameOverride(hoverName);
        Main.HoverItem = hoverItem;
        Main.hoverItemName = hoverName;
    }

    private int GetHoveredItemIndex(Rectangle inner)
    {
        for (var index = 0; index < _itemGroups.Length; index++)
        {
            var slotRectangle = new Rectangle(
                inner.X + (int)(index * SlotStep),
                inner.Y,
                (int)WidthPixels,
                inner.Height);

            if (slotRectangle.Contains(Main.MouseScreen.ToPoint()))
            {
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

    private Item GetDisplayedItem(int groupIndex)
    {
        var groupItems = _itemGroups[groupIndex];
        if (groupItems.Length == 1)
        {
            return groupItems[0].Clone();
        }

        var cycleIndex = GetAlternativeCycleIndex() % groupItems.Length;
        return groupItems[cycleIndex].Clone();
    }

    private static int GetAlternativeCycleIndex()
    {
        return (int)(Main.GameUpdateCount / AlternativeCycleTicks);
    }

    private static void DrawSlotBackground(SpriteBatch spriteBatch, Vector2 position, Asset<Texture2D> slotTexture)
    {
        spriteBatch.Draw(slotTexture.Value, position, Color.White);
    }

    private static void DrawAlternativeMarker(SpriteBatch spriteBatch, Vector2 slotPosition)
    {
        Utils.DrawBorderStringFourWay(
            spriteBatch,
            FontAssets.MouseText.Value,
            "/",
            slotPosition.X + WidthPixels - 11f,
            slotPosition.Y - 2f,
            JournalUiTheme.EntryAlternativeMarker,
            Color.Black,
            Vector2.Zero,
            0.64f);
    }
}
