using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

internal sealed class JournalArmorSetSlot : UIElement
{
    private readonly JournalArmorSetFamily _armorSetFamily;
    private readonly JournalArmorSetDefinition _armorSet;
    private readonly Item _icon;
    private readonly Color _blockAccent;
    private readonly Action<JournalArmorSetFamily>? _onArmorSetSelected;
    private readonly Dictionary<string, ArmorSetTooltip> _tooltipCache = new(StringComparer.Ordinal);

    internal JournalArmorSetSlot(
        JournalArmorSetFamily armorSetFamily,
        Color blockAccent,
        Action<JournalArmorSetFamily>? onArmorSetSelected)
    {
        _armorSetFamily = armorSetFamily;
        _armorSet = armorSetFamily.Variants[0];
        _blockAccent = blockAccent;
        _onArmorSetSelected = onArmorSetSelected;
        _icon = JournalItemUtilities.CreateItem(_armorSet.IconItemId);

        Width.Set(GetVisualWidth(), 0f);
        Height.Set(TextureAssets.InventoryBack9.Height(), 0f);
        OnLeftClick += HandleLeftClick;
    }

    private static float WidthPixels => TextureAssets.InventoryBack9.Width();

    public static float GetVisualWidth() => WidthPixels;

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        var rectangle = GetInnerDimensions().ToRectangle();
        var oldScale = Main.inventoryScale;

        try
        {
            Main.inventoryScale = 1f;
            var displayItem = _icon.Clone();
            if (!displayItem.IsAir && JournalItemUtilities.IsValidItemId(displayItem.type))
            {
                Main.instance.LoadItem(displayItem.type);
                JournalItemSlotRenderer.Draw(
                    spriteBatch,
                    displayItem,
                    rectangle,
                    _blockAccent,
                    rectangle.Contains(Main.MouseScreen.ToPoint()));
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

        Main.HoverItem = new Item();
        Main.hoverItemName = string.Empty;
        Main.mouseText = true;
        var tooltip = GetTooltip(_armorSet);
        JournalTooltip.RequestArmorSet(
            tooltip.Title,
            tooltip.DefenseLabel,
            tooltip.TotalDefense,
            tooltip.EffectsTitle,
            tooltip.Effects,
            tooltip.BonusTitle,
            tooltip.BonusText);
    }

    private ArmorSetTooltip GetTooltip(JournalArmorSetDefinition armorSet)
    {
        var culture = Language.ActiveCulture.Name;
        var cacheKey = $"{armorSet.Key}|{culture}";
        if (_tooltipCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var bonusText = armorSet.ResolveBonusText().Trim();
        var items = armorSet.ItemIds
            .Select(JournalItemUtilities.CreateItem)
            .Where(static item => !item.IsAir)
            .ToArray();
        var effects = JournalItemTooltipUtilities.GetAggregatedNumericEffectLines(items);

        var tooltip = new ArmorSetTooltip(
            armorSet.ResolveDisplayName(),
            Language.GetTextValue("Mods.ProgressionJournal.UI.ArmorSetDefenseLabel"),
            armorSet.TotalDefense,
            Language.GetTextValue("Mods.ProgressionJournal.UI.ArmorSetTotalStats"),
            effects.ToArray(),
            Language.GetTextValue("Mods.ProgressionJournal.UI.ArmorSetBonus"),
            bonusText);
        _tooltipCache[cacheKey] = tooltip;
        return tooltip;
    }

    private void HandleLeftClick(UIMouseEvent evt, UIElement listeningElement)
    {
        _onArmorSetSelected?.Invoke(_armorSetFamily);
    }

    private readonly record struct ArmorSetTooltip(
        string Title,
        string DefenseLabel,
        int TotalDefense,
        string EffectsTitle,
        string[] Effects,
        string BonusTitle,
        string BonusText);
}
