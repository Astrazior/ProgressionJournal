using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalItemStrip : UIElement
{
    private const float SlotSpacing = 4f;

    private readonly JournalSavedBuildItemReference[] _items;
    private readonly int[] _stacks;
    private readonly Action<int>? _onItemSelected;

    public JournalItemStrip(IEnumerable<Item> items, Action<int>? onItemSelected = null)
    {
        var itemArray = items.ToArray();
        _items = itemArray
            .Select(static item => new JournalSavedBuildItemReference(
                item.type,
                item.ModItem?.Mod.Name ?? string.Empty,
                item.ModItem?.Name ?? string.Empty,
                item.HoverName))
            .ToArray();
        _stacks = itemArray
            .Select(static item => item.stack)
            .ToArray();
        _onItemSelected = onItemSelected;
        SetSize();
        if (_onItemSelected is not null)
        {
            OnLeftClick += HandleLeftClick;
        }
    }

    public JournalItemStrip(IEnumerable<JournalSavedBuildItemReference> items)
    {
        _items = items.ToArray();
        _stacks = Enumerable.Repeat(1, _items.Length).ToArray();
        SetSize();
    }

    private void SetSize()
    {
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
                    item.stack = _stacks[index];
                    Main.instance.LoadItem(item.type);
                    var rectangle = new Rectangle(
                        (int)position.X,
                        (int)position.Y,
                        (int)SlotWidth,
                        TextureAssets.InventoryBack9.Height());
                    JournalItemSlotRenderer.Draw(
                        spriteBatch,
                        item,
                        rectangle,
                        JournalUiTheme.ItemSlotDefaultAccent,
                        rectangle.Contains(Main.MouseScreen.ToPoint()));
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
            hoverItem.stack = _stacks[hoveredIndex];
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

    private void HandleLeftClick(UIMouseEvent evt, UIElement listeningElement)
    {
        var hoveredIndex = GetHoveredItemIndex(GetInnerDimensions().ToRectangle());
        if (hoveredIndex < 0 || !_items[hoveredIndex].IsLoaded)
        {
            return;
        }

        _onItemSelected?.Invoke(_items[hoveredIndex].Type);
    }

    private static void DrawUnloadedSlot(SpriteBatch spriteBatch, Vector2 position)
    {
        var rectangle = new Rectangle((int)position.X, (int)position.Y, (int)SlotWidth, TextureAssets.InventoryBack9.Height());
        JournalItemSlotRenderer.DrawBackground(spriteBatch, rectangle, JournalUiTheme.ItemSlotDefaultAccent);

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

        return string.Equals(displayName, Language.GetTextValue("Mods.ProgressionJournal.UI.BuildUnloadedItem"), StringComparison.OrdinalIgnoreCase) ? displayName : Language.GetTextValue("Mods.ProgressionJournal.UI.BuildUnloadedItemTooltip", displayName);
    }
}
