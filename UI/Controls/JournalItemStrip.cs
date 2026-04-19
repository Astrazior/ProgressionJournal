using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalItemStrip : UIElement
{
    private const float SlotSpacing = 4f;

    private readonly Item[] _items;

    public JournalItemStrip(IEnumerable<Item> items)
    {
        _items = items.Select(static item => item.Clone()).ToArray();
        Width.Set(GetVisualWidth(_items.Length), 0f);
        Height.Set(TextureAssets.InventoryBack9.Height(), 0f);
    }

    private static float SlotWidth => TextureAssets.InventoryBack9.Width();

    public static float GetVisualWidth(int itemCount)
    {
        if (itemCount <= 0)
        {
            return 0f;
        }

        return itemCount * SlotWidth + (itemCount - 1) * SlotSpacing;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        if (_items.Length == 0)
        {
            return;
        }

        var hoveredIndex = GetHoveredItemIndex(GetInnerDimensions().ToRectangle());
        var oldScale = Main.inventoryScale;

        try
        {
            Main.inventoryScale = 1f;

            for (var index = 0; index < _items.Length; index++)
            {
                var item = _items[index].Clone();
                Main.instance.LoadItem(item.type);

                var position = GetInnerDimensions().ToRectangle().TopLeft() + new Vector2(index * (SlotWidth + SlotSpacing), 0f);
                ItemSlot.Draw(spriteBatch, ref item, ItemSlot.Context.TrashItem, position);
            }
        }
        finally
        {
            Main.inventoryScale = oldScale;
        }

        if (!IsMouseHovering || hoveredIndex < 0)
        {
            return;
        }

        var hoverItem = _items[hoveredIndex].Clone();
        Main.HoverItem = hoverItem;
        Main.hoverItemName = hoverItem.HoverName;
    }

    private int GetHoveredItemIndex(Rectangle inner)
    {
        for (var index = 0; index < _items.Length; index++)
        {
            var rectangle = new Rectangle(
                inner.X + (int)(index * (SlotWidth + SlotSpacing)),
                inner.Y,
                (int)SlotWidth,
                inner.Height);

            if (rectangle.Contains(Main.MouseScreen.ToPoint()))
            {
                return index;
            }
        }

        return -1;
    }
}
