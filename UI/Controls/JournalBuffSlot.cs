using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
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
    private static readonly Asset<Texture2D> BestiarySupportIconTexture =
        Main.Assets.Request<Texture2D>(BestiarySupportIconTexturePath);

    private readonly JournalCombatBuffEntry _entry;
    private readonly int[][] _itemGroupIds;
    private readonly Item?[][] _itemGroups;
    private readonly string? _classSpecificLabel;
    private readonly Action<int>? _onItemSelected;
    private readonly Color _blockAccent;

    public JournalBuffSlot(
        JournalCombatBuffEntry entry,
        Color blockAccent,
        Action<int>? onItemSelected = null)
    {
        _entry = entry;
        _onItemSelected = onItemSelected;
        _blockAccent = blockAccent;
        _itemGroupIds = entry.ItemGroups
            .Select(static group => group.ItemIds.ToArray())
            .ToArray();
        _itemGroups = _itemGroupIds
            .Select(static group => new Item?[group.Length])
            .ToArray();

        if (entry.IsClassSpecific)
        {
            _classSpecificLabel = Language.GetTextValue("Mods.ProgressionJournal.UI.CombatBuffClassSpecificTag");
        }

        Width.Set(GetVisualWidth(entry), 0f);
        Height.Set(TextureAssets.InventoryBack9.Height(), 0f);
        OnLeftClick += HandleLeftClick;
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
        var scissor = spriteBatch.GraphicsDevice.ScissorRectangle;
        if (scissor is { Width: > 0, Height: > 0 } && !inner.Intersects(scissor))
        {
            return;
        }

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
            return GetItem(groupIndex, itemIndex: 0);
        }

        var cycleIndex = (int)(Main.GameUpdateCount / AlternativeCycleTicks) % groupItems.Length;
        return GetItem(groupIndex, cycleIndex);
    }

    private Item GetItem(int groupIndex, int itemIndex)
    {
        var cached = _itemGroups[groupIndex][itemIndex];
        if (cached is not null)
        {
            return cached;
        }

        var item = JournalItemUtilities.CreateItem(_itemGroupIds[groupIndex][itemIndex]);
        _itemGroups[groupIndex][itemIndex] = item;
        return item;
    }

    private void HandleLeftClick(UIMouseEvent evt, UIElement listeningElement)
    {
        if (_onItemSelected is null)
        {
            return;
        }

        var hoveredIndex = GetHoveredItemIndex(GetInnerDimensions().ToRectangle());
        if (hoveredIndex < 0)
        {
            return;
        }

        _onItemSelected(GetDisplayedItem(hoveredIndex).type);
    }

    private void DrawSlot(SpriteBatch spriteBatch, int groupIndex, Vector2 slotPosition)
    {
        var group = _entry.ItemGroups[groupIndex];
        if (group.DisplayBuffId is not { } buffId)
        {
            var displayItem = GetDisplayedItem(groupIndex);
            JournalItemUtilities.EnsureTextureLoaded(displayItem.type);
            var rectangle = new Rectangle(
                (int)slotPosition.X,
                (int)slotPosition.Y,
                (int)WidthPixels,
                TextureAssets.InventoryBack9.Height());
            JournalItemSlotRenderer.Draw(
                spriteBatch,
                displayItem,
                rectangle,
                _blockAccent,
                rectangle.Contains(Main.MouseScreen.ToPoint()),
                accentStrength: 0.30f);
            return;
        }

        DrawBuffSlot(spriteBatch, slotPosition, buffId);
    }

    private static void DrawBuffSlot(SpriteBatch spriteBatch, Vector2 slotPosition, int buffId)
    {
        var texture = TextureAssets.Buff[buffId].Value;
        var scale = MathF.Min(BuffIconSize / texture.Width, BuffIconSize / texture.Height);
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

        var texture = BestiarySupportIconTexture.Value;
        var iconRectangle = new Rectangle(
            (int)slotPosition.X + SupportIconPadding,
            (int)slotPosition.Y + SupportIconPadding,
            SupportIconSize,
            SupportIconSize);

        spriteBatch.Draw(texture, iconRectangle, Color.White);
    }
}
