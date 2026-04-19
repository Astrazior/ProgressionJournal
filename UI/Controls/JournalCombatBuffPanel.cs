using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalCombatBuffPanel : UIPanel
{
    private readonly IReadOnlyList<JournalBuffCategory> _sectionOrder;
    private readonly string _titleLocalizationKey;
    private readonly bool _showTitle;
    private readonly bool _autoHeight;
    private readonly bool _useConsumableOverlayLayout;
    private readonly Action<int>? _onItemSelected;

    public JournalCombatBuffPanel(
        IReadOnlyList<JournalBuffCategory> sectionOrder,
        string titleLocalizationKey,
        bool showTitle = true,
        bool autoHeight = false,
        bool useConsumableOverlayLayout = false,
        Action<int>? onItemSelected = null)
    {
        _sectionOrder = sectionOrder;
        _titleLocalizationKey = titleLocalizationKey;
        _showTitle = showTitle;
        _autoHeight = autoHeight;
        _useConsumableOverlayLayout = useConsumableOverlayLayout;
        _onItemSelected = onItemSelected;
        SetPadding(0f);
        BackgroundColor = JournalUiTheme.PresetPanelBackground;
        BorderColor = JournalUiTheme.PresetPanelBorder;
    }

    public bool HasEntries { get; private set; }

    public float ContentHeight { get; private set; }

    public void SetEntries(IReadOnlyList<JournalCombatBuffEntry> entries)
    {
        RemoveAllChildren();
        HasEntries = entries.Count > 0;
        ContentHeight = 0f;

        if (!HasEntries)
        {
            if (_autoHeight)
            {
                Height.Set(0f, 0f);
            }

            return;
        }

        if (_useConsumableOverlayLayout)
        {
            SetConsumableOverlayEntries(entries);
            return;
        }

        var top = JournalUiMetrics.BlockVerticalPadding;
        const float contentLeft = JournalUiMetrics.BlockHorizontalPadding + JournalUiMetrics.CategoryContentIndent;

        if (_showTitle)
        {
            var title = new UIText(Language.GetTextValue(_titleLocalizationKey), 0.48f, true)
            {
                HAlign = 0.5f
            };
            title.Top.Set(top, 0f);
            title.TextColor = JournalUiTheme.SectionHeaderText;
            Append(title);
            top += JournalUiMetrics.RecommendationHeaderHeight + JournalUiMetrics.RecommendationHeaderBottomSpacing;
        }

        var hasAnyCategory = false;
        foreach (var category in _sectionOrder)
        {
            var categoryEntries = entries.Where(entry => entry.Category == category).ToArray();
            if (categoryEntries.Length == 0)
            {
                continue;
            }

            if (hasAnyCategory)
            {
                top += JournalUiMetrics.BuffSectionSpacing;
            }

            var header = CreateCategoryHeader(category);
            header.Left.Set(JournalUiMetrics.BlockHorizontalPadding, 0f);
            header.Top.Set(top, 0f);
            Append(header);
            top += GetCategoryHeaderHeight() + GetCategoryHeaderBottomSpacing();

            foreach (var rowEntries in ChunkEntries(categoryEntries, JournalUiMetrics.BuffSlotsPerRow))
            {
                var row = CreateSlotRow(rowEntries);
                row.Left.Set(contentLeft, 0f);
                row.Top.Set(top, 0f);
                Append(row);
                top += JournalUiMetrics.RowHeight + JournalUiMetrics.RowSpacing;
            }

            top -= JournalUiMetrics.RowSpacing;
            hasAnyCategory = true;
        }

        if (_autoHeight)
        {
            Height.Set(top + 4f, 0f);
        }

        ContentHeight = top + 4f;
    }

    private void SetConsumableOverlayEntries(IReadOnlyList<JournalCombatBuffEntry> entries)
    {
        const float totalWidth = JournalUiMetrics.CombatBuffOverlayWidth - JournalUiMetrics.CombatBuffOverlayInset * 2f;
        const float columnWidth = (totalWidth - JournalUiMetrics.ConsumableOverlayColumnGap) * 0.5f;
        var leftTop = 0f;
        var rightTop = 0f;

        leftTop = AppendConsumableColumnSection(
            entries,
            JournalBuffCategory.Basic,
            0f,
            leftTop,
            columnWidth,
            JournalUiMetrics.ConsumableOverlayColumnSlots);

        rightTop = AppendConsumableColumnSection(
            entries,
            JournalBuffCategory.Potion,
            columnWidth + JournalUiMetrics.ConsumableOverlayColumnGap,
            rightTop,
            columnWidth,
            JournalUiMetrics.ConsumableOverlayColumnSlots);

        leftTop = AppendConsumableColumnSection(
            entries,
            JournalBuffCategory.Food,
            0f,
            leftTop,
            columnWidth,
            JournalUiMetrics.ConsumableOverlayColumnSlots);

        rightTop = AppendConsumableColumnSection(
            entries,
            JournalBuffCategory.Eternal,
            columnWidth + JournalUiMetrics.ConsumableOverlayColumnGap,
            rightTop,
            columnWidth,
            JournalUiMetrics.ConsumableOverlayColumnSlots);

        leftTop = AppendConsumableColumnSection(
            entries,
            JournalBuffCategory.Flask,
            0f,
            leftTop,
            columnWidth,
            JournalUiMetrics.ConsumableOverlayColumnSlots);

        var top = System.MathF.Max(leftTop, rightTop);

        if (_autoHeight)
        {
            Height.Set(top + 4f, 0f);
        }

        ContentHeight = top + 4f;
    }

    private float AppendConsumableColumnSection(
        IReadOnlyList<JournalCombatBuffEntry> allEntries,
        JournalBuffCategory category,
        float left,
        float top,
        float width,
        int maxSlotsPerRow)
    {
        var sectionHeight = AppendCategorySection(allEntries, category, left, top, width, maxSlotsPerRow);
        if (sectionHeight <= 0f)
        {
            return top;
        }

        return top + sectionHeight + JournalUiMetrics.BuffSectionSpacing;
    }

    private float AppendCategorySection(
        IReadOnlyList<JournalCombatBuffEntry> allEntries,
        JournalBuffCategory category,
        float left,
        float top,
        float width,
        int maxSlotsPerRow)
    {
        var categoryEntries = allEntries.Where(entry => entry.Category == category).ToArray();
        if (categoryEntries.Length == 0)
        {
            return 0f;
        }

        var startTop = top;
        var header = CreateCategoryHeader(category, width);
        header.Left.Set(left, 0f);
        header.Top.Set(top, 0f);
        Append(header);
        top += GetCategoryHeaderHeight() + GetCategoryHeaderBottomSpacing();

        foreach (var rowEntries in ChunkEntries(categoryEntries, maxSlotsPerRow))
        {
            var row = CreateSlotRow(rowEntries);
            row.Left.Set(left + JournalUiMetrics.CategoryContentIndent, 0f);
            row.Top.Set(top, 0f);
            Append(row);
            top += JournalUiMetrics.RowHeight + JournalUiMetrics.RowSpacing;
        }

        top -= JournalUiMetrics.RowSpacing;
        return top - startTop;
    }

    private static string GetCategoryTitle(JournalBuffCategory category) => category switch
    {
        JournalBuffCategory.Station => Language.GetTextValue("Mods.ProgressionJournal.UI.CombatBuffStations"),
        JournalBuffCategory.Passive => Language.GetTextValue("Mods.ProgressionJournal.UI.CombatBuffPassive"),
        JournalBuffCategory.Basic => Language.GetTextValue("Mods.ProgressionJournal.UI.CombatBuffBasic"),
        JournalBuffCategory.Potion => Language.GetTextValue("Mods.ProgressionJournal.UI.CombatBuffPotions"),
        JournalBuffCategory.Eternal => Language.GetTextValue("Mods.ProgressionJournal.UI.CombatBuffEternal"),
        JournalBuffCategory.Food => Language.GetTextValue("Mods.ProgressionJournal.UI.CombatBuffFood"),
        JournalBuffCategory.Flask => Language.GetTextValue("Mods.ProgressionJournal.UI.CombatBuffFlasks"),
        _ => string.Empty
    };

    private static JournalCategoryHeader CreateCategoryHeader(JournalBuffCategory category, float? width = null)
    {
        var header = new JournalCategoryHeader(
            GetCategoryTitle(category),
            JournalUiTheme.PanelBorder,
            JournalUiTheme.RootTitleText,
            JournalUiTheme.CategoryHeaderStyle);

        if (width is { } fixedWidth)
        {
            header.Width.Set(fixedWidth, 0f);
        }
        else
        {
            header.Width.Set(-(JournalUiMetrics.BlockHorizontalPadding * 2f), 1f);
        }

        header.Height.Set(GetCategoryHeaderHeight(), 0f);
        return header;
    }

    private static IEnumerable<JournalCombatBuffEntry[]> ChunkEntries(IReadOnlyList<JournalCombatBuffEntry> entries, int maxSlotsPerRow)
    {
        var row = new List<JournalCombatBuffEntry>();
        var occupiedSlots = 0;

        foreach (var entry in entries)
        {
            var entrySlots = entry.ItemGroups.Count;

            if (row.Count > 0 && occupiedSlots + entrySlots > maxSlotsPerRow)
            {
                yield return row.ToArray();
                row.Clear();
                occupiedSlots = 0;
            }

            row.Add(entry);
            occupiedSlots += entrySlots;
        }

        if (row.Count > 0)
        {
            yield return row.ToArray();
        }
    }

    private UIElement CreateSlotRow(IReadOnlyList<JournalCombatBuffEntry> entries)
    {
        var row = new UIElement();
        row.Width.Set(GetRowWidth(entries), 0f);
        row.Height.Set(JournalUiMetrics.RowHeight, 0f);

        var left = 0f;
        foreach (var entry in entries)
        {
            var slot = new JournalBuffSlot(entry, _onItemSelected);
            slot.Left.Set(left, 0f);
            row.Append(slot);
            left += JournalBuffSlot.GetVisualWidth(entry) + JournalUiMetrics.EntrySpacing;
        }

        return row;
    }

    private static float GetRowWidth(IReadOnlyList<JournalCombatBuffEntry> entries)
    {
        if (entries.Count == 0)
        {
            return 0f;
        }

        return entries.Sum(JournalBuffSlot.GetVisualWidth)
            + JournalUiMetrics.EntrySpacing * (entries.Count - 1);
    }

    private static float GetCategoryHeaderHeight() => JournalUiTheme.CategoryHeaderStyle switch
    {
        JournalCategoryHeaderStyle.AccentTag => 24f,
        JournalCategoryHeaderStyle.SideRail => 22f,
        _ => 20f
    };

    private static float GetCategoryHeaderBottomSpacing() => JournalUiTheme.CategoryHeaderStyle switch
    {
        JournalCategoryHeaderStyle.AccentTag => 7f,
        _ => 6f
    };
}
