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
    private const float BuffIconSize = 32f;
    private const float GroupSpacing = 4f;

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

        Width.Set(GetVisualWidth(entry), 0f);
        Height.Set(TextureAssets.InventoryBack9.Height(), 0f);
    }

    private static float WidthPixels => TextureAssets.InventoryBack9.Width();

    public static float GetVisualWidth(JournalCombatBuffEntry entry)
    {
        if (entry.ItemGroups.Count <= 0)
        {
            return 0f;
        }

        return entry.ItemGroups.Sum(GetGroupWidth) + (entry.ItemGroups.Count - 1) * GroupSpacing;
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

            var left = 0f;
            for (var index = 0; index < _itemGroups.Length; index++)
            {
                var group = _entry.ItemGroups[index];
                var topOffset = group.DisplayBuffId is null ? 0f : (Height.Pixels - BuffIconSize) * 0.5f;
                var slotPosition = inner.TopLeft() + new Vector2(left, topOffset);
                DrawSlot(spriteBatch, index, slotPosition);
                DrawPriorityBadge(spriteBatch, slotPosition);

                if (_entry.ItemGroups[index].HasAlternatives)
                {
                    DrawAlternativeMarker(spriteBatch, slotPosition);
                }

                left += GetGroupWidth(group) + GroupSpacing;
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

        var hoveredGroup = _entry.ItemGroups[hoveredIndex];
        var hoverName = hoveredGroup.GetDisplayName();

        if (!string.IsNullOrWhiteSpace(_classSpecificLabel))
        {
            hoverName = $"{hoverName} [{_classSpecificLabel}]";
        }

        if (hoveredGroup.DisplayBuffId is null)
        {
            var hoverItem = GetDisplayedItem(hoveredIndex).Clone();
            hoverItem.SetNameOverride(hoverName);
            Main.HoverItem = hoverItem;
        }
        else
        {
            Main.HoverItem = new Item();
        }

        Main.hoverItemName = hoverName;
        Main.mouseText = true;
    }

    private int GetHoveredItemIndex(Rectangle inner)
    {
        var left = 0f;
        for (var index = 0; index < _itemGroups.Length; index++)
        {
            var group = _entry.ItemGroups[index];
            var width = GetGroupWidth(group);
            var topOffset = group.DisplayBuffId is null ? 0f : (Height.Pixels - BuffIconSize) * 0.5f;
            var slotRectangle = new Rectangle(
                inner.X + (int)left,
                inner.Y + (int)topOffset,
                (int)width,
                (int)(group.DisplayBuffId is null ? inner.Height : BuffIconSize));

            if (slotRectangle.Contains(Main.MouseScreen.ToPoint()))
            {
                return index;
            }

            left += width + GroupSpacing;
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

    private void DrawSlot(SpriteBatch spriteBatch, int groupIndex, Vector2 slotPosition)
    {
        var group = _entry.ItemGroups[groupIndex];
        if (group.DisplayBuffId is not { } buffId)
        {
            var displayItem = GetDisplayedItem(groupIndex);
            Main.instance.LoadItem(displayItem.type);
            ItemSlot.Draw(spriteBatch, ref displayItem, ItemSlot.Context.TrashItem, slotPosition);
            return;
        }

        DrawBuffSlot(spriteBatch, slotPosition, buffId);
    }

    private static void DrawBuffSlot(SpriteBatch spriteBatch, Vector2 slotPosition, int buffId)
    {
        var texture = TextureAssets.Buff[buffId].Value;
        var scale = System.MathF.Min(BuffIconSize / texture.Width, BuffIconSize / texture.Height);
        var position = slotPosition + new Vector2(BuffIconSize * 0.5f, BuffIconSize * 0.5f);

        spriteBatch.Draw(
            texture,
            position,
            null,
            Color.White,
            0f,
            new Vector2(texture.Width * 0.5f, texture.Height * 0.5f),
            scale,
            SpriteEffects.None,
            0f);
    }

    private static float GetGroupWidth(JournalItemGroup group) => group.DisplayBuffId is null ? WidthPixels : BuffIconSize;

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
