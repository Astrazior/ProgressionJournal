using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace ProgressionJournal.UI.Composition;

public static class JournalContentBuilder
{
    private static readonly JournalBuffCategory[] CombatBuffSectionOrder =
    [
        JournalBuffCategory.Station,
        JournalBuffCategory.Passive,
        JournalBuffCategory.Basic,
        JournalBuffCategory.Potion,
        JournalBuffCategory.Eternal,
        JournalBuffCategory.Food,
        JournalBuffCategory.Flask
    ];

    private static readonly RecommendationTier[] TierOrder =
    [
        RecommendationTier.Recommended,
        RecommendationTier.Additional,
        RecommendationTier.NotRecommended,
        RecommendationTier.Useless
    ];

    public static void PopulateEntries(UIList entryList, ProgressionStageId stageId, IReadOnlyList<JournalStageEntry> entries, Action<int>? onItemSelected = null)
    {
        if (entries.Count == 0)
        {
            entryList.Add(JournalUiElementFactory.CreateSectionHeader(Language.GetTextValue("Mods.ProgressionJournal.UI.EmptyState")));
            return;
        }

        foreach (var tier in TierOrder)
        {
            var tierEntries = GetEntriesForTier(entries, stageId, tier);
            if (tierEntries.Length == 0)
            {
                continue;
            }

            var palette = JournalUiTheme.GetRecommendationBlockStyle(tier);
            entryList.Add(CreateRecommendationBlock(GetTierTitle(tier), tierEntries, palette, onItemSelected));
        }
    }

    public static void PopulateCombatBuffs(UIList entryList, IReadOnlyList<JournalCombatBuffEntry> combatBuffEntries, Action<int>? onItemSelected = null)
    {
        if (combatBuffEntries.Count == 0)
        {
            return;
        }

        var buffPanel = new JournalCombatBuffPanel(
            CombatBuffSectionOrder,
            "Mods.ProgressionJournal.UI.CombatBuffsTitle",
            showTitle: false,
            autoHeight: true,
            onItemSelected: onItemSelected);
        buffPanel.Width.Set(0f, 1f);
        buffPanel.SetEntries(combatBuffEntries);
        entryList.Add(buffPanel);
    }

    public static void PopulatePresets(UIList entryList, IReadOnlyList<JournalPreset> presets)
    {
        if (presets.Count == 0)
        {
            entryList.Add(JournalUiElementFactory.CreateSectionHeader(Language.GetTextValue("Mods.ProgressionJournal.UI.PresetsEmptyState")));
            return;
        }

        foreach (var preset in presets)
        {
            entryList.Add(new JournalPresetPanel(preset));
        }
    }

    public static void PopulateDevelopmentNotice(UIList entryList, string text)
    {
        var container = new UIElement();
        container.Width.Set(0f, 1f);
        container.Height.Set(320f, 0f);

        var notice = new UIText(text, 0.9f, true)
        {
            HAlign = 0.5f,
            VAlign = 0.5f,
            TextColor = JournalUiTheme.RootTitleText
        };
        container.Append(notice);

        entryList.Add(container);
    }

    private static string GetTierTitle(RecommendationTier tier) => tier switch
    {
        RecommendationTier.Recommended => Language.GetTextValue("Mods.ProgressionJournal.UI.RecommendedBlock"),
        RecommendationTier.Additional => Language.GetTextValue("Mods.ProgressionJournal.UI.AdditionalBlock"),
        RecommendationTier.NotRecommended => Language.GetTextValue("Mods.ProgressionJournal.UI.NotRecommendedBlock"),
        RecommendationTier.Useless => Language.GetTextValue("Mods.ProgressionJournal.UI.UselessBlock"),
        _ => string.Empty
    };

    private static JournalStageEntry[] GetEntriesForTier(
        IReadOnlyList<JournalStageEntry> entries,
        ProgressionStageId stageId,
        RecommendationTier tier)
    {
        return entries
            .Where(entry => entry.Evaluation.Tier == tier)
            .OrderBy(entry => JournalOrdering.GetCategoryOrder(entry.Entry.Category))
            .ThenByDescending(GetCategoryStrength)
            .ThenBy(entry => JournalOrdering.GetStageEntryDisplayOrderOverride(stageId, entry.Entry.Key))
            .ThenBy(entry => entry.Entry.GetDisplayName(), StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static int GetCategoryStrength(JournalStageEntry entry) => entry.Entry.Category switch
    {
        JournalItemCategory.Weapon or JournalItemCategory.Armor => entry.Entry.CategoryStrength,
        _ => 0
    };

    private static UIPanel CreateRecommendationBlock(
        string title,
        IReadOnlyList<JournalStageEntry> entries,
        JournalPanelStyle palette,
        Action<int>? onItemSelected)
    {
        var block = JournalUiElementFactory.CreatePanel();
        block.Width.Set(0f, 1f);
        block.SetPadding(0f);
        block.BackgroundColor = palette.Background;
        block.BorderColor = palette.Border;

        var top = JournalUiMetrics.BlockVerticalPadding;

        var header = CreateRecommendationHeader(title);
        header.Left.Set(JournalUiMetrics.BlockHorizontalPadding, 0f);
        header.Top.Set(top, 0f);
        block.Append(header);
        top += JournalUiMetrics.RecommendationHeaderHeight + JournalUiMetrics.RecommendationHeaderBottomSpacing;

        var hasAnyCategory = false;
        foreach (var category in JournalOrdering.EntryCategories)
        {
            var categoryEntries = entries.Where(entry => entry.Entry.Category == category).ToArray();
            if (categoryEntries.Length == 0)
            {
                continue;
            }

            if (hasAnyCategory)
            {
                top += JournalUiMetrics.CategorySpacing;
            }

            var categoryHeader = CreateCategoryHeader(category);
            categoryHeader.Left.Set(JournalUiMetrics.BlockHorizontalPadding, 0f);
            categoryHeader.Top.Set(top, 0f);
            block.Append(categoryHeader);
            top += GetCategoryHeaderHeight() + GetCategoryHeaderBottomSpacing();

            foreach (var rowEntries in ChunkEntries(categoryEntries, JournalUiMetrics.EntrySlotsPerRow))
            {
                var row = CreateSlotRow(rowEntries, onItemSelected);
                row.Left.Set(JournalUiMetrics.BlockHorizontalPadding + JournalUiMetrics.CategoryContentIndent, 0f);
                row.Top.Set(top, 0f);
                block.Append(row);
                top += JournalUiMetrics.RowHeight + JournalUiMetrics.RowSpacing;
            }

            hasAnyCategory = true;
        }

        if (hasAnyCategory && top >= JournalUiMetrics.RowSpacing)
        {
            top -= JournalUiMetrics.RowSpacing;
        }

        block.Height.Set(top + 4f, 0f);
        return block;
    }

    private static IEnumerable<JournalStageEntry[]> ChunkEntries(IReadOnlyList<JournalStageEntry> entries, int maxSlotsPerRow)
    {
        var row = new List<JournalStageEntry>();
        var occupiedSlots = 0;

        foreach (var entry in entries)
        {
            var entrySlots = Math.Max(1, entry.Entry.ItemGroups.Count);

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

    private static JournalCategoryHeader CreateCategoryHeader(JournalItemCategory category)
    {
        var palette = JournalUiTheme.GetCategoryStyle(category);
        var header = new JournalCategoryHeader(
            Language.GetTextValue($"Mods.ProgressionJournal.Categories.{category}"),
            palette.Border,
            palette.Text,
            JournalUiTheme.CategoryHeaderStyle);

        header.Width.Set(-(JournalUiMetrics.BlockHorizontalPadding * 2f), 1f);
        header.Height.Set(GetCategoryHeaderHeight(), 0f);
        return header;
    }

    private static JournalRecommendationHeader CreateRecommendationHeader(string title)
    {
        var header = new JournalRecommendationHeader(title);
        header.Width.Set(-(JournalUiMetrics.BlockHorizontalPadding * 2f), 1f);
        header.Height.Set(JournalUiMetrics.RecommendationHeaderHeight, 0f);
        return header;
    }

    private static UIElement CreateSlotRow(JournalStageEntry[] entries, Action<int>? onItemSelected)
    {
        var row = new UIElement();
        row.Width.Set(GetRowWidth(entries), 0f);
        row.Height.Set(JournalUiMetrics.RowHeight, 0f);

        var left = 0f;
        foreach (var entry in entries)
        {
            var slot = new JournalEntrySlot(entry, onItemSelected);
            slot.Left.Set(left, 0f);
            row.Append(slot);
            left += JournalEntrySlot.GetVisualWidth(entry.Entry.ItemGroups.Count) + JournalUiMetrics.EntrySpacing;
        }

        return row;
    }

    private static float GetRowWidth(IReadOnlyList<JournalStageEntry> entries)
    {
        if (entries.Count == 0)
        {
            return 0f;
        }

        return entries.Sum(entry => JournalEntrySlot.GetVisualWidth(entry.Entry.ItemGroups.Count))
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

