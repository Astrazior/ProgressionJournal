using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalEntrySlot : UIElement
{
    private const int AlternativeCycleTicks = 90;
    private const string BestiaryFilterIconTexturePath = "Images/UI/Bestiary/Icon_Tags_Shadow";
    private const string BestiarySupportIconTexturePath = "Images/UI/Bestiary/Icon_Rank_Light";
    private const int BestiaryFilterIconColumns = 16;
    private const int BestiaryFilterIconRows = 5;
    private const int EventBadgeSize = 18;
    private const int EventBadgePadding = 2;
    private const int EventBadgeInnerPadding = 1;
    private const int SupportIconSize = 12;
    private const int SupportIconPadding = 3;

    private readonly JournalStageEntry _entry;
    private readonly Item[][] _itemGroups;
    private readonly int? _eventBadgeFrame;
    private readonly string? _eventLabel;
    private readonly string? _supportLabel;
    private readonly Action<int>? _onItemSelected;

    public JournalEntrySlot(JournalStageEntry entry, Action<int>? onItemSelected = null)
    {
        _entry = entry;
        _onItemSelected = onItemSelected;
        _itemGroups = entry.Entry.ItemGroups
            .Select(group => group.ItemIds.Select(JournalItemUtilities.CreateItem).ToArray())
            .ToArray();
        if (entry.Entry.EventCategory is { } eventCategory)
        {
            _eventBadgeFrame = eventCategory.GetBestiaryFilterFrame();
            _eventLabel = eventCategory.GetDisplayName();
        }

        if (entry.Entry.IsSupportWeapon)
        {
            _supportLabel = Language.GetTextValue("Mods.ProgressionJournal.UI.SupportWeaponTag");
        }

        Width.Set(GetVisualWidth(_itemGroups.Length), 0f);
        Height.Set(TextureAssets.InventoryBack9.Height(), 0f);
        OnLeftClick += HandleLeftClick;
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
                DrawVanillaSlot(spriteBatch, ref displayItem, slotPosition);
                DrawSpecialOutline(spriteBatch, slotPosition, hoveredIndex == index);
                DrawSupportBadge(spriteBatch, slotPosition);
                DrawEventBadge(spriteBatch, slotPosition);

                if (_entry.Entry.ItemGroups[index].HasAlternatives)
                {
                    DrawAlternativeMarker(spriteBatch, slotPosition);
                }
            }
        }
        finally
        {
            Main.inventoryScale = oldScale;
        }

        if (!IsMouseHovering)
        {
            return;
        }

        if (hoveredIndex < 0)
        {
            return;
        }

        var hoverItem = GetDisplayedItem(hoveredIndex).Clone();
        var hoverName = _entry.Entry.ItemGroups[hoveredIndex].GetDisplayName();
        var tagLabels = new List<string>(2);

        if (!string.IsNullOrWhiteSpace(_eventLabel))
        {
            tagLabels.Add(_eventLabel);
        }

        if (!string.IsNullOrWhiteSpace(_supportLabel))
        {
            tagLabels.Add(_supportLabel);
        }

        if (tagLabels.Count > 0)
        {
            hoverName = $"{hoverName} [{string.Join(", ", tagLabels)}]";
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

    private void DrawVanillaSlot(SpriteBatch spriteBatch, ref Item displayItem, Vector2 slotPosition)
    {
        var previousTexture = TextureAssets.InventoryBack9;

        try
        {
            TextureAssets.InventoryBack9 = GetSlotTexture(_entry.Evaluation.Tier);
            ItemSlot.Draw(spriteBatch, ref displayItem, ItemSlot.Context.TrashItem, slotPosition);
        }
        finally
        {
            TextureAssets.InventoryBack9 = previousTexture;
        }
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

    private void DrawSpecialOutline(SpriteBatch spriteBatch, Vector2 slotPosition, bool isHovered)
    {
        if (_entry.Entry.EventCategory is not null)
        {
            DrawOutline(
                spriteBatch,
                slotPosition,
                isHovered ? JournalUiTheme.EventEntryOutlineBright : JournalUiTheme.EventEntryOutline,
                JournalUiTheme.EventEntryOutlineShadow);
        }
    }

    private static void DrawOutline(SpriteBatch spriteBatch, Vector2 slotPosition, Color outerColor, Color shadowColor)
    {
        var rectangle = new Rectangle((int)slotPosition.X, (int)slotPosition.Y, (int)WidthPixels, TextureAssets.InventoryBack9.Height());
        var texture = TextureAssets.MagicPixel.Value;
        var innerColor = Color.Lerp(outerColor, Color.White, 0.35f);

        DrawRectangleOutline(spriteBatch, texture, rectangle, shadowColor);
        rectangle.Inflate(-1, -1);
        DrawRectangleOutline(spriteBatch, texture, rectangle, outerColor);
        rectangle.Inflate(-1, -1);
        DrawRectangleOutline(spriteBatch, texture, rectangle, innerColor);
    }

    private void DrawSupportBadge(SpriteBatch spriteBatch, Vector2 slotPosition)
    {
        if (!_entry.Entry.IsSupportWeapon)
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

    private void DrawEventBadge(SpriteBatch spriteBatch, Vector2 slotPosition)
    {
        if (_eventBadgeFrame is null)
        {
            return;
        }

        var texture = TextureAssets.MagicPixel.Value;
        var badgeRectangle = new Rectangle(
            (int)slotPosition.X + (int)WidthPixels - EventBadgeSize - EventBadgePadding,
            (int)slotPosition.Y + TextureAssets.InventoryBack9.Height() - EventBadgeSize - EventBadgePadding,
            EventBadgeSize,
            EventBadgeSize);

        spriteBatch.Draw(texture, badgeRectangle, JournalUiTheme.EventBadgeBackground);
        DrawRectangleOutline(spriteBatch, texture, badgeRectangle, JournalUiTheme.EventBadgeBorder);

        var itemTexture = Main.Assets.Request<Texture2D>(BestiaryFilterIconTexturePath).Value;
        var sourceRectangle = GetBestiaryFilterSourceRectangle(itemTexture, _eventBadgeFrame.Value);
        const int maxIconSize = EventBadgeSize - EventBadgeInnerPadding * 2;
        var scale = MathF.Min(maxIconSize / (float)sourceRectangle.Width, maxIconSize / (float)sourceRectangle.Height);
        var drawPosition = new Vector2(badgeRectangle.Center.X, badgeRectangle.Center.Y);

        spriteBatch.Draw(
            itemTexture,
            drawPosition,
            sourceRectangle,
            Color.White,
            0f,
            sourceRectangle.Size() * 0.5f,
            scale,
            SpriteEffects.None,
            0f);
    }

    private static Rectangle GetBestiaryFilterSourceRectangle(Texture2D texture, int frame)
    {
        var frameWidth = texture.Width / BestiaryFilterIconColumns;
        var frameHeight = texture.Height / BestiaryFilterIconRows;
        var frameX = frame % BestiaryFilterIconColumns;
        var frameY = frame / BestiaryFilterIconColumns;

        return new Rectangle(frameX * frameWidth, frameY * frameHeight, frameWidth, frameHeight);
    }

    private static void DrawRectangleOutline(SpriteBatch spriteBatch, Texture2D texture, Rectangle rectangle, Color color)
    {
        if (rectangle.Width <= 0 || rectangle.Height <= 0)
        {
            return;
        }

        spriteBatch.Draw(texture, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, 1), color);
        spriteBatch.Draw(texture, new Rectangle(rectangle.X, rectangle.Bottom - 1, rectangle.Width, 1), color);
        spriteBatch.Draw(texture, new Rectangle(rectangle.X, rectangle.Y, 1, rectangle.Height), color);
        spriteBatch.Draw(texture, new Rectangle(rectangle.Right - 1, rectangle.Y, 1, rectangle.Height), color);
    }
}
