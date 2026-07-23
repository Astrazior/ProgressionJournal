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
        JournalTooltip.Request(tooltip.Text, tooltip.FramedBonusText);
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
        var lines = new List<string>
        {
            armorSet.ResolveDisplayName(),
            Language.GetTextValue(
                "Mods.ProgressionJournal.UI.ArmorSetTotalDefense",
                armorSet.TotalDefense)
        };
        var items = armorSet.ItemIds
            .Select(JournalItemUtilities.CreateItem)
            .Where(static item => !item.IsAir)
            .ToArray();
        var effects = JournalItemTooltipUtilities.GetAggregatedNumericEffectLines(items);
        if (effects.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add(Language.GetTextValue("Mods.ProgressionJournal.UI.ArmorSetTotalStats"));
            lines.AddRange(effects.Select(static effect => $"• {effect}"));
        }

        var tooltip = new ArmorSetTooltip(
            string.Join(Environment.NewLine, lines),
            string.IsNullOrWhiteSpace(bonusText)
                ? string.Empty
                : string.Join(
                    Environment.NewLine,
                    Language.GetTextValue("Mods.ProgressionJournal.UI.ArmorSetBonus"),
                    bonusText));
        _tooltipCache[cacheKey] = tooltip;
        return tooltip;
    }

    private void HandleLeftClick(UIMouseEvent evt, UIElement listeningElement)
    {
        _onArmorSetSelected?.Invoke(_armorSetFamily);
    }

    private readonly record struct ArmorSetTooltip(string Text, string FramedBonusText);
}
