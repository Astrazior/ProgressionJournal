using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using ProgressionJournal.Systems;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace ProgressionJournal.UI.Composition;

public static class JournalContentBuilder
{
    private const float SavedBuildPreviewWidth = 126f;
    private const float SavedBuildPreviewHeight = 178f;

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

    public static void PopulateBuildPlanner(
        UIList entryList,
        ProgressionStageId stageId,
        CombatClass combatClass,
        Func<string, int> getSelectedItemId,
        Action<string> onSlotClick)
    {
        entryList.Add(CreateBuildEquipmentPanel(stageId, combatClass, getSelectedItemId, onSlotClick, JournalSystem.ClearBuildItem));
        entryList.Add(CreateBuildConsumablesPanel(combatClass, getSelectedItemId, onSlotClick, JournalSystem.ClearBuildItem));
    }

    public static void PopulateSavedBuilds(
        UIList entryList,
        ProgressionStageId stageId,
        CombatClass combatClass,
        IReadOnlyList<JournalSavedBuild> builds)
    {
        foreach (var build in builds)
        {
            entryList.Add(CreateSavedBuildCard(build, stageId, combatClass));
        }
    }

    private static string GetTierTitle(RecommendationTier tier) => tier switch
    {
        RecommendationTier.Recommended => Language.GetTextValue("Mods.ProgressionJournal.UI.RecommendedBlock"),
        RecommendationTier.Additional => Language.GetTextValue("Mods.ProgressionJournal.UI.AdditionalBlock"),
        RecommendationTier.NotRecommended => Language.GetTextValue("Mods.ProgressionJournal.UI.NotRecommendedBlock"),
        RecommendationTier.Useless => Language.GetTextValue("Mods.ProgressionJournal.UI.UselessBlock"),
        _ => string.Empty
    };

    private static UIPanel CreateBuildEquipmentPanel(
        ProgressionStageId stageId,
        CombatClass combatClass,
        Func<string, int> getSelectedItemId,
        Action<string> onSlotClick,
        Action<string> onSlotRightClick)
    {
        var panel = JournalUiElementFactory.CreatePanel();
        panel.Width.Set(0f, 1f);
        panel.BackgroundColor = JournalUiTheme.PresetPanelBackground;
        panel.BorderColor = JournalUiTheme.PresetPanelBorder;

        var top = JournalUiMetrics.BlockVerticalPadding;

        var title = new UIText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildEquipmentTitle"), JournalUiMetrics.BuildPanelHeaderScale, true)
        {
            TextColor = JournalUiTheme.SectionHeaderText
        };
        title.Left.Set(JournalUiMetrics.BlockHorizontalPadding, 0f);
        title.Top.Set(top, 0f);
        panel.Append(title);
        top += 24f;

        top += 12f;
        top = AppendBuildHeader(panel, Language.GetTextValue("Mods.ProgressionJournal.UI.Weapons"), top);
        top = AppendEquipmentRow(
            panel,
            combatClass,
            [
                JournalBuildPlannerCatalog.PrimaryWeaponSlotKey,
                JournalBuildPlannerCatalog.SupportWeaponSlotKey,
                JournalBuildPlannerCatalog.ClassSpecificSlotKey
            ],
            top,
            getSelectedItemId,
            onSlotClick,
            onSlotRightClick);

        var armorHeaderTop = top + 8f;
        var armorHeader = CreateBuildSectionLabel(Language.GetTextValue("Mods.ProgressionJournal.UI.ArmorLabel"));
        armorHeader.Left.Set(JournalUiMetrics.BlockHorizontalPadding, 0f);
        armorHeader.Top.Set(armorHeaderTop, 0f);
        panel.Append(armorHeader);

        var accessoriesLabel = CreateBuildSectionLabel(Language.GetTextValue("Mods.ProgressionJournal.UI.Accessories"));
        accessoriesLabel.Left.Set(JournalUiMetrics.BlockHorizontalPadding + JournalUiMetrics.BuildSlotSize * 2.4f, 0f);
        accessoriesLabel.Top.Set(armorHeaderTop, 0f);
        panel.Append(accessoriesLabel);

        var armorBottom = AppendEquipmentColumn(
            panel,
            combatClass,
            [
                JournalBuildPlannerCatalog.ArmorHeadSlotKey,
                JournalBuildPlannerCatalog.ArmorBodySlotKey,
                JournalBuildPlannerCatalog.ArmorLegsSlotKey
            ],
            JournalUiMetrics.BlockHorizontalPadding,
            armorHeaderTop + 26f,
            getSelectedItemId,
            onSlotClick,
            onSlotRightClick);

        var accessoryKeys = Enumerable.Range(1, JournalBuildPlannerCatalog.GetAccessorySlotCount(stageId))
            .Select(JournalBuildPlannerCatalog.GetAccessorySlotKey)
            .ToArray();
        AppendEquipmentGrid(
            panel,
            combatClass,
            accessoryKeys,
            2,
            JournalUiMetrics.BlockHorizontalPadding + JournalUiMetrics.BuildSlotSize * 2.4f,
            armorHeaderTop + 26f,
            getSelectedItemId,
            onSlotClick,
            onSlotRightClick);

        top = MathF.Max(armorBottom, armorHeaderTop + 26f + GetGridHeight(accessoryKeys.Length, 2));

        panel.Height.Set(top + JournalUiMetrics.BlockVerticalPadding, 0f);
        return panel;
    }

    private static UIPanel CreateBuildConsumablesPanel(
        CombatClass combatClass,
        Func<string, int> getSelectedItemId,
        Action<string> onSlotClick,
        Action<string> onSlotRightClick)
    {
        var panel = JournalUiElementFactory.CreatePanel();
        panel.Width.Set(0f, 1f);
        panel.BackgroundColor = JournalUiTheme.PresetPanelBackground;
        panel.BorderColor = JournalUiTheme.PresetPanelBorder;

        var top = JournalUiMetrics.BlockVerticalPadding;
        var title = new UIText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildConsumablesTitle"), JournalUiMetrics.BuildPanelHeaderScale, true)
        {
            TextColor = JournalUiTheme.SectionHeaderText
        };
        title.Left.Set(JournalUiMetrics.BlockHorizontalPadding, 0f);
        title.Top.Set(top, 0f);
        panel.Append(title);
        top += 24f;

        top = AppendBuildHeader(panel, Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSlotPotion"), top);
        top += AppendExpandableEquipmentGrid(
            panel,
            combatClass,
            JournalBuildPlannerCatalog.GetPotionSlotKey,
            JournalBuildPlannerCatalog.PotionSlotCount,
            minVisibleSlots: 1,
            4,
            JournalUiMetrics.BlockHorizontalPadding,
            top,
            getSelectedItemId,
            onSlotClick,
            onSlotRightClick,
            Language.GetTextValue("Mods.ProgressionJournal.UI.BuildAddPotionSlotTooltip")) + 10f;

        top = AppendBuildHeader(panel, Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSlotFood"), top);
        top += AppendExpandableEquipmentGrid(
            panel,
            combatClass,
            JournalBuildPlannerCatalog.GetFoodSlotKey,
            JournalBuildPlannerCatalog.FoodSlotCount,
            minVisibleSlots: 1,
            4,
            JournalUiMetrics.BlockHorizontalPadding,
            top,
            getSelectedItemId,
            onSlotClick,
            onSlotRightClick,
            Language.GetTextValue("Mods.ProgressionJournal.UI.BuildAddFoodAlternativeTooltip"));

        panel.Height.Set(top + JournalUiMetrics.BlockVerticalPadding, 0f);
        return panel;
    }

    private static float AppendBuildHeader(UIElement panel, string title, float top)
    {
        var header = CreateBuildSectionLabel(title);
        header.Left.Set(JournalUiMetrics.BlockHorizontalPadding, 0f);
        header.Top.Set(top, 0f);
        header.Width.Set(-JournalUiMetrics.BlockHorizontalPadding * 2f, 1f);
        panel.Append(header);
        return top + 24f;
    }

    private static float AppendEquipmentRow(
        UIElement panel,
        CombatClass combatClass,
        IReadOnlyList<string> slotKeys,
        float top,
        Func<string, int> getSelectedItemId,
        Action<string> onSlotClick,
        Action<string> onSlotRightClick)
    {
        var left = JournalUiMetrics.BlockHorizontalPadding;
        foreach (var slotKey in slotKeys)
        {
            var slot = CreateBuildSlot(slotKey, combatClass, getSelectedItemId, onSlotClick, onSlotRightClick);
            slot.Left.Set(left, 0f);
            slot.Top.Set(top, 0f);
            panel.Append(slot);
            left += JournalUiMetrics.BuildSlotSize + JournalUiMetrics.BuildSlotGap;
        }

        return top + JournalUiMetrics.BuildSlotSize + JournalUiMetrics.BuildSlotGap;
    }

    private static float AppendEquipmentColumn(
        UIElement panel,
        CombatClass combatClass,
        IReadOnlyList<string> slotKeys,
        float left,
        float top,
        Func<string, int> getSelectedItemId,
        Action<string> onSlotClick,
        Action<string> onSlotRightClick)
    {
        foreach (var slotKey in slotKeys)
        {
            var slot = CreateBuildSlot(slotKey, combatClass, getSelectedItemId, onSlotClick, onSlotRightClick);
            slot.Left.Set(left, 0f);
            slot.Top.Set(top, 0f);
            panel.Append(slot);
            top += JournalUiMetrics.BuildSlotSize + JournalUiMetrics.BuildSlotGap;
        }

        return top;
    }

    private static void AppendEquipmentGrid(
        UIElement panel,
        CombatClass combatClass,
        IReadOnlyList<string> slotKeys,
        int columns,
        float left,
        float top,
        Func<string, int> getSelectedItemId,
        Action<string> onSlotClick,
        Action<string> onSlotRightClick)
    {
        for (var index = 0; index < slotKeys.Count; index++)
        {
            var column = index % columns;
            var row = index / columns;
            var slot = CreateBuildSlot(slotKeys[index], combatClass, getSelectedItemId, onSlotClick, onSlotRightClick);
            slot.Left.Set(left + column * (JournalUiMetrics.BuildSlotSize + JournalUiMetrics.BuildSlotGap), 0f);
            slot.Top.Set(top + row * (JournalUiMetrics.BuildSlotSize + JournalUiMetrics.BuildSlotGap), 0f);
            panel.Append(slot);
        }
    }

    private static float AppendExpandableEquipmentGrid(
        UIElement panel,
        CombatClass combatClass,
        Func<int, string> getSlotKey,
        int maxSlotCount,
        int minVisibleSlots,
        int columns,
        float left,
        float top,
        Func<string, int> getSelectedItemId,
        Action<string> onSlotClick,
        Action<string> onSlotRightClick,
        string addSlotHoverText)
    {
        var visibleSlotCount = GetVisibleExpandableSlotCount(getSlotKey, maxSlotCount, minVisibleSlots, getSelectedItemId);
        var visualSlotCount = visibleSlotCount + (visibleSlotCount < maxSlotCount ? 1 : 0);

        for (var index = 0; index < visibleSlotCount; index++)
        {
            var slotKey = getSlotKey(index + 1);
            AppendGridElement(
                panel,
                CreateBuildSlot(slotKey, combatClass, getSelectedItemId, onSlotClick, onSlotRightClick),
                index,
                columns,
                left,
                top);
        }

        if (visibleSlotCount < maxSlotCount)
        {
            var addSlotKey = getSlotKey(visibleSlotCount + 1);
            AppendGridElement(
                panel,
                CreateBuildAddSlot(addSlotHoverText, () => onSlotClick(addSlotKey)),
                visibleSlotCount,
                columns,
                left,
                top);
        }

        return GetGridHeight(visualSlotCount, columns);
    }

    private static void AppendGridElement(UIElement panel, UIElement element, int index, int columns, float left, float top)
    {
        var column = index % columns;
        var row = index / columns;
        element.Left.Set(left + column * (JournalUiMetrics.BuildSlotSize + JournalUiMetrics.BuildSlotGap), 0f);
        element.Top.Set(top + row * (JournalUiMetrics.BuildSlotSize + JournalUiMetrics.BuildSlotGap), 0f);
        panel.Append(element);
    }

    private static int GetVisibleExpandableSlotCount(
        Func<int, string> getSlotKey,
        int maxSlotCount,
        int minVisibleSlots,
        Func<string, int> getSelectedItemId)
    {
        var visibleSlotCount = Math.Clamp(minVisibleSlots, 0, maxSlotCount);
        for (var slotIndex = maxSlotCount; slotIndex > visibleSlotCount; slotIndex--)
        {
            if (getSelectedItemId(getSlotKey(slotIndex)) > ItemID.None)
            {
                return slotIndex;
            }
        }

        return visibleSlotCount;
    }

    private static JournalBuildEquipmentSlot CreateBuildSlot(
        string slotKey,
        CombatClass combatClass,
        Func<string, int> getSelectedItemId,
        Action<string> onSlotClick,
        Action<string> onSlotRightClick)
    {
        return new JournalBuildEquipmentSlot(
            JournalBuildPlannerCatalog.GetSlotShortLabel(slotKey, combatClass),
            JournalBuildPlannerCatalog.GetSlotDisplayName(slotKey, combatClass),
            () => getSelectedItemId(slotKey),
            () => onSlotClick(slotKey),
            () => onSlotRightClick(slotKey));
    }

    private static JournalBuildEquipmentSlot CreateBuildAddSlot(string hoverText, Action onClick)
    {
        return new JournalBuildEquipmentSlot(
            "+",
            hoverText,
            static () => ItemID.None,
            onClick,
            static () => { });
    }

    private static UIText CreateBuildSectionLabel(string text)
    {
        return new UIText(text, JournalUiMetrics.BuildSectionTitleScale, true)
        {
            TextColor = JournalUiTheme.RootTitleText
        };
    }

    private static float GetGridHeight(int itemCount, int columns)
    {
        if (itemCount <= 0)
        {
            return 0f;
        }

        var rowCount = (int)Math.Ceiling(itemCount / (float)columns);
        return rowCount * JournalUiMetrics.BuildSlotSize + (rowCount - 1) * JournalUiMetrics.BuildSlotGap;
    }

    private static UIPanel CreateSavedBuildCard(
        JournalSavedBuild build,
        ProgressionStageId stageId,
        CombatClass combatClass)
    {
        var card = JournalUiElementFactory.CreatePanel();
        card.SetPadding(0f);
        card.Width.Set(0f, 1f);
        var palette = JournalUiTheme.GetClassPalette(combatClass);
        card.BackgroundColor = Color.Lerp(JournalUiTheme.PanelBackground, palette.Background, 0.48f);
        card.BorderColor = Color.Lerp(JournalUiTheme.PanelBorder, palette.Border, 0.72f);

        var top = JournalUiMetrics.BlockVerticalPadding;
        const float titleScale = JournalUiMetrics.BuildPanelHeaderScale;
        var title = new UIText(JournalTextUtilities.TrimToPixelWidth(build.Name, SavedBuildPreviewWidth, titleScale), titleScale, true)
        {
            TextColor = Color.Lerp(palette.Text, Color.White, 0.22f)
        };
        title.Left.Set(JournalUiMetrics.BlockHorizontalPadding, 0f);
        title.Top.Set(top, 0f);
        title.Width.Set(SavedBuildPreviewWidth, 0f);
        card.Append(title);

        AppendSavedBuildActions(card, build);
        top += 34f;

        var previewBottom = AppendSavedBuildCharacterPreview(card, build, stageId, top, palette);
        var equipmentBottom = AppendSavedBuildEquipmentSummary(card, build, stageId, top);
        var consumablesBottom = AppendSavedBuildConsumablesSummary(card, build, top);
        top = MathF.Max(previewBottom, MathF.Max(equipmentBottom, consumablesBottom));

        card.Height.Set(top + JournalUiMetrics.BlockVerticalPadding, 0f);
        return card;
    }

    private static void AppendSavedBuildActions(UIElement card, JournalSavedBuild build)
    {
        var editButton = JournalBuildActionButton.CreateEdit(() => JournalSystem.EditSavedBuild(build));
        editButton.Left.Set(-116f, 1f);
        editButton.Top.Set(7f, 0f);
        editButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildEditTooltip"));
        card.Append(editButton);

        var favoriteButton = JournalBuildActionButton.CreateFavorite(
            build.IsFavorite,
            () => JournalSystem.ToggleSavedBuildFavorite(build));
        favoriteButton.Left.Set(-78f, 1f);
        favoriteButton.Top.Set(7f, 0f);
        favoriteButton.SetHoverText(build.IsFavorite
            ? Language.GetTextValue("Mods.ProgressionJournal.UI.BuildFavoriteActiveTooltip")
            : Language.GetTextValue("Mods.ProgressionJournal.UI.BuildFavoriteTooltip"));
        card.Append(favoriteButton);

        var deleteButton = JournalBuildActionButton.CreateTrash(() => JournalSystem.DeleteSavedBuild(build));
        deleteButton.Left.Set(-40f, 1f);
        deleteButton.Top.Set(7f, 0f);
        deleteButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildDeleteTooltip"));
        card.Append(deleteButton);
    }

    private static float AppendSavedBuildCharacterPreview(
        UIElement card,
        JournalSavedBuild build,
        ProgressionStageId stageId,
        float top,
        JournalClassPalette palette)
    {
        var previewPanel = JournalUiElementFactory.CreatePanel();
        previewPanel.Left.Set(JournalUiMetrics.BlockHorizontalPadding, 0f);
        previewPanel.Top.Set(top, 0f);
        previewPanel.Width.Set(SavedBuildPreviewWidth, 0f);
        previewPanel.Height.Set(SavedBuildPreviewHeight, 0f);
        previewPanel.BackgroundColor = Color.Lerp(JournalUiTheme.RootBackground, palette.Background, 0.56f);
        previewPanel.BorderColor = Color.Lerp(palette.Border, palette.Accent, 0.42f);
        card.Append(previewPanel);

        var characterPreview = new JournalSavedBuildCharacterPreview(
            JournalPreviewPlayerFactory.CreateSavedBuildPreview(build, stageId),
            () => card.IsMouseHovering,
            1.18f,
            1f,
            0f);
        characterPreview.Width.Set(104f, 0f);
        characterPreview.Height.Set(146f, 0f);
        characterPreview.HAlign = 0.5f;
        characterPreview.Top.Set(18f, 0f);
        characterPreview.IgnoresMouseInteraction = true;
        previewPanel.Append(characterPreview);

        return top + SavedBuildPreviewHeight;
    }

    private static float AppendSavedBuildEquipmentSummary(
        UIElement card,
        JournalSavedBuild build,
        ProgressionStageId stageId,
        float top)
    {
        const float columnLeft = 164f;
        const int maxSlotsPerRow = 5;

        top = AppendSavedBuildSection(
            card,
            Language.GetTextValue("Mods.ProgressionJournal.UI.Weapons"),
            GetSelectedItems(
                build,
                JournalBuildPlannerCatalog.PrimaryWeaponSlotKey,
                JournalBuildPlannerCatalog.SupportWeaponSlotKey,
                JournalBuildPlannerCatalog.ClassSpecificSlotKey),
            columnLeft,
            top,
            maxSlotsPerRow);

        top = AppendSavedBuildSection(
            card,
            Language.GetTextValue("Mods.ProgressionJournal.UI.ArmorLabel"),
            GetSelectedItems(
                build,
                JournalBuildPlannerCatalog.ArmorHeadSlotKey,
                JournalBuildPlannerCatalog.ArmorBodySlotKey,
                JournalBuildPlannerCatalog.ArmorLegsSlotKey),
            columnLeft,
            top,
            maxSlotsPerRow);

        var accessoryItems = Enumerable.Range(1, JournalBuildPlannerCatalog.GetAccessorySlotCount(stageId))
            .Select(slotIndex => build.GetSelectedItemId(JournalBuildPlannerCatalog.GetAccessorySlotKey(slotIndex)))
            .Where(static itemId => itemId > ItemID.None)
            .ToArray();

        return AppendSavedBuildSection(
            card,
            Language.GetTextValue("Mods.ProgressionJournal.UI.Accessories"),
            accessoryItems,
            columnLeft,
            top,
            maxSlotsPerRow);
    }

    private static float AppendSavedBuildConsumablesSummary(UIElement card, JournalSavedBuild build, float top)
    {
        const float columnLeft = 482f;
        const int maxSlotsPerRow = 4;

        var potionItems = Enumerable.Range(1, JournalBuildPlannerCatalog.PotionSlotCount)
            .Select(slotIndex => build.GetSelectedItemId(JournalBuildPlannerCatalog.GetPotionSlotKey(slotIndex)))
            .Where(static itemId => itemId > ItemID.None)
            .ToArray();

        var foodItems = Enumerable.Range(1, JournalBuildPlannerCatalog.FoodSlotCount)
            .Select(slotIndex => build.GetSelectedItemId(JournalBuildPlannerCatalog.GetFoodSlotKey(slotIndex)))
            .Where(static itemId => itemId > ItemID.None)
            .ToArray();

        top = AppendSavedBuildSection(
            card,
            Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSlotPotion"),
            potionItems,
            columnLeft,
            top,
            maxSlotsPerRow);

        return AppendSavedBuildSection(
            card,
            Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSlotFood"),
            foodItems,
            columnLeft,
            top,
            maxSlotsPerRow);
    }

    private static float AppendSavedBuildSection(
        UIElement card,
        string title,
        IReadOnlyList<int> itemIds,
        float left,
        float top,
        int maxSlotsPerRow)
    {
        if (itemIds.Count == 0)
        {
            return top;
        }

        var titleElement = new UIText(title, JournalUiMetrics.BuildSectionTitleScale, true)
        {
            TextColor = JournalUiTheme.SectionHeaderText
        };
        titleElement.Left.Set(left, 0f);
        titleElement.Top.Set(top, 0f);
        card.Append(titleElement);
        top += 22f;

        for (var index = 0; index < itemIds.Count; index += maxSlotsPerRow)
        {
            var rowItems = itemIds
                .Skip(index)
                .Take(maxSlotsPerRow)
                .Select(JournalItemUtilities.CreateItem)
                .ToArray();

            var strip = new JournalItemStrip(rowItems);
            strip.Left.Set(left, 0f);
            strip.Top.Set(top, 0f);
            card.Append(strip);
            top += JournalUiMetrics.BuildSlotSize + 5f;
        }

        return top + 5f;
    }

    private static int[] GetSelectedItems(JournalSavedBuild build, params string[] slotKeys)
    {
        return slotKeys
            .Select(build.GetSelectedItemId)
            .Where(static itemId => itemId > ItemID.None)
            .ToArray();
    }

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

    private static JournalSystem JournalSystem => ModContent.GetInstance<JournalSystem>();
}

