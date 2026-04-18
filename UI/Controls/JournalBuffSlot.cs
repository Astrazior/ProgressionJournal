using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalBuffSlot : UIElement
{
    private const int AlternativeCycleTicks = 90;
    private const string BestiarySupportIconTexturePath = "Images/UI/Bestiary/Icon_Rank_Light";
    private const int SupportIconSize = 12;
    private const int SupportIconPadding = 3;

    private readonly JournalCombatBuffEntry _entry;
    private readonly Item[][] _itemGroups;
    private readonly string? _classSpecificLabel;

    public JournalBuffSlot(JournalCombatBuffEntry entry)
    {
        _entry = entry;
        _itemGroups = entry.ItemGroups
            .Select(group => group.ItemIds.Select(JournalItemUtilities.CreateItem).ToArray())
            .ToArray();

        if (entry.IsClassSpecific)
        {
            _classSpecificLabel = Language.GetTextValue("Mods.ProgressionJournal.UI.CombatBuffClassSpecificTag");
        }

        Width.Set(GetVisualWidth(_itemGroups.Length), 0f);
        Height.Set(TextureAssets.InventoryBack9.Height(), 0f);
    }

    private static float WidthPixels => TextureAssets.InventoryBack9.Width();

    private static float SlotStep => WidthPixels + 4f;

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
        var hoveredIndex = GetHoveredItemIndex(inner);
        var oldScale = Main.inventoryScale;

        try
        {
            Main.inventoryScale = 1f;

            for (var index = 0; index < _itemGroups.Length; index++)
            {
                var displayItem = GetDisplayedItem(index);
                Main.instance.LoadItem(displayItem.type);

                var slotPosition = inner.TopLeft() + new Vector2(index * SlotStep, 0f);
                ItemSlot.Draw(spriteBatch, ref displayItem, ItemSlot.Context.TrashItem, slotPosition);
                DrawPriorityBadge(spriteBatch, slotPosition);

                if (_entry.ItemGroups[index].HasAlternatives)
                {
                    DrawAlternativeMarker(spriteBatch, slotPosition);
                }
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

        var hoverItem = GetDisplayedItem(hoveredIndex).Clone();
        var hoverName = _entry.ItemGroups[hoveredIndex].GetDisplayName();

        if (!string.IsNullOrWhiteSpace(_classSpecificLabel))
        {
            hoverName = $"{hoverName} [{_classSpecificLabel}]";
        }

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

    private Item GetDisplayedItem(int groupIndex)
    {
        var groupItems = _itemGroups[groupIndex];
        if (groupItems.Length == 1)
        {
            return groupItems[0].Clone();
        }

        var cycleIndex = (int)(Main.GameUpdateCount / AlternativeCycleTicks) % groupItems.Length;
        return groupItems[cycleIndex].Clone();
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

    private void DrawPriorityBadge(SpriteBatch spriteBatch, Vector2 slotPosition)
    {
        if (!_entry.IsClassSpecific)
        {
            return;
        }

        var texture = Main.Assets.Request<Texture2D>(BestiarySupportIconTexturePath).Value;
        var iconRectangle = new Rectangle(
            (int)slotPosition.X + SupportIconPadding,
            (int)slotPosition.Y + SupportIconPadding,
            SupportIconSize,
            SupportIconSize);

        spriteBatch.Draw(texture, iconRectangle, Color.White);
    }
}
