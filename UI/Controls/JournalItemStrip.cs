using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalItemStrip : UIElement
{
    private const float SlotSpacing = 4f;

    private readonly JournalSavedBuildItemReference[] _items;

    public JournalItemStrip(IEnumerable<Item> items)
        : this(items.Select(static item => new JournalSavedBuildItemReference(
            item.type,
            item.ModItem?.Mod.Name ?? string.Empty,
            item.ModItem?.Name ?? string.Empty,
            item.HoverName)))
    {
    }

    public JournalItemStrip(IEnumerable<JournalSavedBuildItemReference> items)
    {
        _items = items.ToArray();
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
                var position = GetInnerDimensions().ToRectangle().TopLeft() + new Vector2(index * (SlotWidth + SlotSpacing), 0f);
                if (_items[index].IsLoaded && JournalItemUtilities.TryCreateItem(_items[index].Type, out var item))
                {
                    Main.instance.LoadItem(item.type);
                    ItemSlot.Draw(spriteBatch, ref item, ItemSlot.Context.TrashItem, position);
                    continue;
                }

                DrawUnloadedSlot(spriteBatch, position);
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

        if (_items[hoveredIndex].IsLoaded && JournalItemUtilities.TryCreateItem(_items[hoveredIndex].Type, out var hoverItem))
        {
            Main.HoverItem = hoverItem;
            Main.hoverItemName = hoverItem.HoverName;
            return;
        }

        Main.HoverItem = new Item();
        Main.hoverItemName = GetUnloadedHoverText(_items[hoveredIndex]);
        Main.mouseText = true;
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

    private static void DrawUnloadedSlot(SpriteBatch spriteBatch, Vector2 position)
    {
        var rectangle = new Rectangle((int)position.X, (int)position.Y, (int)SlotWidth, TextureAssets.InventoryBack9.Height());
        spriteBatch.Draw(TextureAssets.InventoryBack9.Value, rectangle, Color.White * 0.72f);

        Utils.DrawBorderStringFourWay(
            spriteBatch,
            FontAssets.MouseText.Value,
            "?",
            position.X + SlotWidth * 0.5f - 5f,
            position.Y + rectangle.Height * 0.5f - 11f,
            JournalUiTheme.SectionHeaderText,
            Color.Black,
            Vector2.Zero,
            0.9f);
    }

    private static string GetUnloadedHoverText(JournalSavedBuildItemReference itemReference)
    {
        var displayName = string.IsNullOrWhiteSpace(itemReference.DisplayName)
            ? Language.GetTextValue("Mods.ProgressionJournal.UI.BuildUnloadedItem")
            : itemReference.DisplayName;

        return string.Equals(displayName, Language.GetTextValue("Mods.ProgressionJournal.UI.BuildUnloadedItem"), System.StringComparison.OrdinalIgnoreCase) ? displayName : Language.GetTextValue("Mods.ProgressionJournal.UI.BuildUnloadedItemTooltip", displayName);
    }
}
