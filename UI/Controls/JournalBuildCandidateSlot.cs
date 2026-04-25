using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalBuildCandidateSlot : UIElement
{
    private const int DisplaySlotIndex = 10;

    private readonly Item _item;
    private readonly bool _selected;
    private readonly bool _disabled;
    private readonly Item[] _displayItems = CreateDisplayItems();
    private float _visualScale = 1f;

    public JournalBuildCandidateSlot(Item item, bool selected, bool disabled, Action onClick)
    {
        _item = item.Clone();
        _selected = selected;
        _disabled = disabled;
        Width.Set(JournalUiMetrics.BuildSlotSize, 0f);
        Height.Set(JournalUiMetrics.BuildSlotSize, 0f);
        OnLeftClick += (_, _) =>
        {
            if (!_disabled)
            {
                onClick();
            }
        };
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        var targetScale = IsMouseHovering && !_disabled ? 1.08f : 1f;
        _visualScale = MathHelper.Lerp(_visualScale, targetScale, 0.35f);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        var dimensions = GetInnerDimensions().ToRectangle();
        var position = dimensions.TopLeft() + new Vector2(
            dimensions.Width * (1f - _visualScale) * 0.5f,
            dimensions.Height * (1f - _visualScale) * 0.5f);
        var oldScale = Main.inventoryScale;
        var displayItem = _item.Clone();
        var previousBackground = TextureAssets.InventoryBack;

        try
        {
            if (!JournalItemUtilities.IsValidItemId(displayItem.type))
            {
                return;
            }

            Main.instance.LoadItem(displayItem.type);
            Main.inventoryScale = _visualScale;
            if (_selected)
            {
                TextureAssets.InventoryBack = TextureAssets.InventoryBack14;
            }

            _displayItems[DisplaySlotIndex] = displayItem;
            ItemSlot.Draw(spriteBatch, _displayItems, ItemSlot.Context.InventoryItem, DisplaySlotIndex, position);
        }
        finally
        {
            _displayItems[DisplaySlotIndex].TurnToAir();
            TextureAssets.InventoryBack = previousBackground;
            Main.inventoryScale = oldScale;
        }

        if (!IsMouseHovering)
        {
            return;
        }

        var hoverItem = _item.Clone();
        if (_disabled)
        {
            hoverItem.SetNameOverride($"{hoverItem.HoverName} ({Language.GetTextValue("Mods.ProgressionJournal.UI.BuildPickerAlreadySelected")})");
        }

        ItemSlot.OverrideHover(ref hoverItem, ItemSlot.Context.InventoryItem);
    }

    private static Item[] CreateDisplayItems()
    {
        var items = new Item[DisplaySlotIndex + 1];
        for (var index = 0; index < items.Length; index++)
        {
            items[index] = new Item();
        }

        return items;
    }
}
