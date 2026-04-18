using System.Collections.Generic;
using System.Linq;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalCombatBuffPanel : UIPanel
{
    private static readonly JournalBuffCategory[] SectionOrder =
    [
        JournalBuffCategory.Station,
        JournalBuffCategory.Potion,
        JournalBuffCategory.Food
    ];

    public JournalCombatBuffPanel()
    {
        SetPadding(0f);
        BackgroundColor = JournalUiTheme.PresetPanelBackground;
        BorderColor = JournalUiTheme.PresetPanelBorder;
    }

    public bool HasEntries { get; private set; }

    public void SetEntries(IReadOnlyList<JournalCombatBuffEntry> entries)
    {
        RemoveAllChildren();
        HasEntries = entries.Count > 0;

        if (!HasEntries)
        {
            return;
        }

        var top = JournalUiMetrics.BlockVerticalPadding;
        var contentLeft = JournalUiMetrics.BlockHorizontalPadding + JournalUiMetrics.CategoryContentIndent;

        var title = new UIText(Language.GetTextValue("Mods.ProgressionJournal.UI.CombatBuffsTitle"), 0.48f, true)
        {
            HAlign = 0.5f
        };
        title.Top.Set(top, 0f);
        title.TextColor = JournalUiTheme.SectionHeaderText;
        Append(title);
        top += JournalUiMetrics.RecommendationHeaderHeight + JournalUiMetrics.RecommendationHeaderBottomSpacing;

        var hasAnyCategory = false;
        foreach (var category in SectionOrder)
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
    }

    private static string GetCategoryTitle(JournalBuffCategory category) => category switch
    {
        JournalBuffCategory.Station => Language.GetTextValue("Mods.ProgressionJournal.UI.CombatBuffStations"),
        JournalBuffCategory.Potion => Language.GetTextValue("Mods.ProgressionJournal.UI.CombatBuffPotions"),
        JournalBuffCategory.Food => Language.GetTextValue("Mods.ProgressionJournal.UI.CombatBuffFood"),
        _ => string.Empty
    };

    private static JournalCategoryHeader CreateCategoryHeader(JournalBuffCategory category)
    {
        var header = new JournalCategoryHeader(
            GetCategoryTitle(category),
            JournalUiTheme.PanelBorder,
            JournalUiTheme.RootTitleText,
            JournalUiTheme.CategoryHeaderStyle);

        header.Width.Set(-(JournalUiMetrics.BlockHorizontalPadding * 2f), 1f);
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

    private static UIElement CreateSlotRow(IReadOnlyList<JournalCombatBuffEntry> entries)
    {
        var row = new UIElement();
        row.Width.Set(GetRowWidth(entries), 0f);
        row.Height.Set(JournalUiMetrics.RowHeight, 0f);

        var left = 0f;
        foreach (var entry in entries)
        {
            var slot = new JournalBuffSlot(entry);
            slot.Left.Set(left, 0f);
            row.Append(slot);
            left += JournalBuffSlot.GetVisualWidth(entry.ItemGroups.Count) + JournalUiMetrics.EntrySpacing;
        }

        return row;
    }

    private static float GetRowWidth(IReadOnlyList<JournalCombatBuffEntry> entries)
    {
        if (entries.Count == 0)
        {
            return 0f;
        }

        return entries.Sum(entry => JournalBuffSlot.GetVisualWidth(entry.ItemGroups.Count))
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
