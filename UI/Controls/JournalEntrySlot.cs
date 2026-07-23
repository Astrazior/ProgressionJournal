using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
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

    private readonly JournalItemGroup[] _sourceGroups;
    private readonly Item[][] _itemGroups;
    private readonly int? _eventBadgeFrame;
    private readonly Asset<Texture2D>? _eventIcon;
    private readonly string? _eventLabel;
    private readonly bool _hasCustomEvent;
    private readonly string? _supportLabel;
    private readonly Action<int>? _onItemSelected;
    private readonly Color _blockAccent;
    private readonly bool _hasEvent;
    private readonly bool _isSupportWeapon;

    public JournalEntrySlot(
        JournalStageEntry entry,
        Color blockAccent,
        Action<int>? onItemSelected = null) : this(
        entry.Entry.ItemGroups,
        blockAccent,
        onItemSelected)
    {
        if (entry.Entry.EventCategory is { } eventCategory)
        {
            _hasEvent = true;
            _eventBadgeFrame = eventCategory.GetBestiaryFilterFrame();
            _eventLabel = eventCategory.GetDisplayName();
        }
        else if (!string.IsNullOrWhiteSpace(entry.Entry.CustomEventName))
        {
            _hasEvent = true;
            _eventLabel = entry.Entry.CustomEventName;
            _hasCustomEvent = true;
            _eventIcon = TryResolveEventIcon(entry.Entry.EventIcon);
        }

        _isSupportWeapon = entry.Entry.IsSupportWeapon;
        if (_isSupportWeapon)
        {
            _supportLabel = Language.GetTextValue("Mods.ProgressionJournal.UI.SupportWeaponTag");
        }
    }

    internal JournalEntrySlot(
        IReadOnlyList<JournalItemGroup> itemGroups,
        Color blockAccent,
        Action<int>? onItemSelected = null)
    {
        _sourceGroups = itemGroups.ToArray();
        _onItemSelected = onItemSelected;
        _blockAccent = blockAccent;
        _itemGroups = _sourceGroups
            .Select(group => group.ItemIds.Select(JournalItemUtilities.CreateItem).ToArray())
            .ToArray();
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
                if (displayItem.IsAir || !JournalItemUtilities.IsValidItemId(displayItem.type))
                {
                    continue;
                }

                Main.instance.LoadItem(displayItem.type);
                var slotPosition = inner.TopLeft() + new Vector2(index * SlotStep, 0f);
                DrawVanillaSlot(spriteBatch, ref displayItem, slotPosition);
                DrawSupportBadge(spriteBatch, slotPosition);
                DrawEventBadge(spriteBatch, slotPosition);

                if (_sourceGroups[index].HasAlternatives)
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
        var hoverName = _sourceGroups[hoveredIndex].GetDisplayName();
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
        var accent = _hasEvent
            ? JournalUiTheme.EventBadgeBorder
            : _blockAccent;
        var rectangle = new Rectangle(
            (int)slotPosition.X,
            (int)slotPosition.Y,
            (int)WidthPixels,
            TextureAssets.InventoryBack9.Height());
        JournalItemSlotRenderer.Draw(
            spriteBatch,
            displayItem,
            rectangle,
            accent,
            rectangle.Contains(Main.MouseScreen.ToPoint()),
            emphasizeOuterAccent: _hasEvent,
            accentStrength: 0.30f);
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

    private void DrawSupportBadge(SpriteBatch spriteBatch, Vector2 slotPosition)
    {
        if (!_isSupportWeapon)
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
        if (_eventBadgeFrame is null && !_hasCustomEvent)
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

        if (_eventBadgeFrame is { } frame)
        {
            var itemTexture = Main.Assets.Request<Texture2D>(BestiaryFilterIconTexturePath).Value;
            var sourceRectangle = GetBestiaryFilterSourceRectangle(itemTexture, frame);
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
            return;
        }

        if (_eventIcon is not null)
        {
            var itemTexture = _eventIcon.Value;
            const int maxIconSize = EventBadgeSize - EventBadgeInnerPadding * 2;
            var scale = MathF.Min(maxIconSize / (float)itemTexture.Width, maxIconSize / (float)itemTexture.Height);

            spriteBatch.Draw(
                itemTexture,
                badgeRectangle.Center.ToVector2(),
                null,
                Color.White,
                0f,
                itemTexture.Size() * 0.5f,
                scale,
                SpriteEffects.None,
                0f);
            return;
        }

        Utils.DrawBorderStringFourWay(
            spriteBatch,
            FontAssets.MouseText.Value,
            "E",
            badgeRectangle.X + 5f,
            badgeRectangle.Y + 1f,
            JournalUiTheme.EventBadgeBorder,
            Color.Black,
            Vector2.Zero,
            0.52f);
    }

    private static Asset<Texture2D>? TryResolveEventIcon(string assetPath)
    {
        var separatorIndex = assetPath.IndexOf('/');
        if (separatorIndex <= 0 || separatorIndex >= assetPath.Length - 1)
        {
            return null;
        }

        var modName = assetPath[..separatorIndex];
        var relativePath = assetPath[(separatorIndex + 1)..];
        if (!string.Equals(modName, "Terraria", StringComparison.OrdinalIgnoreCase)
            || !NPCID.Search.TryGetId(relativePath, out var npcType))
            return ModLoader.TryGetMod(modName, out var mod)
                   && mod.RequestAssetIfExists<Texture2D>(relativePath, out var asset)
                ? asset
                : null;
        var headSlot = JournalStageIconCatalog.GetBossHeadSlot(npcType);
        return headSlot >= 0 && headSlot < TextureAssets.NpcHeadBoss.Length
            ? TextureAssets.NpcHeadBoss[headSlot]
            : null;

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
