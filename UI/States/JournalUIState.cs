using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using ProgressionJournal.Systems;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace ProgressionJournal.UI.States;

public sealed class JournalUiState : UIState
{
    private const string BestiarySearchCancelTexturePath = "Images/UI/SearchCancel";
    private const string BestiaryBackButtonTexturePath = "Images/UI/Bestiary/Button_Back";
    private const string BestiaryForwardButtonTexturePath = "Images/UI/Bestiary/Button_Forward";
    private const string CraftingWindowToggleTexturePath = "Images/UI/Craft_Toggle_0";

    private readonly Dictionary<ProgressionStageId, JournalStageButton> _stageButtons = new();
    private readonly Dictionary<int, CachedAcquisitionView> _acquisitionViewCache = new();
    private JournalDraggablePanel _root = null!;
    private UIPanel _stagePanel = null!;
    private UIPanel _mainPanel = null!;
    private UIElement _contentTabsPanel = null!;
    private UIElement _contentPanel = null!;
    private UIText _stagePanelTitle = null!;
    private JournalTextButton _progressionModeToggleButton = null!;
    private JournalIconButton _closeButton = null!;
    private JournalTextButton _classButton = null!;
    private JournalTextButton _overviewTabButton = null!;
    private JournalTextButton _presetsTabButton = null!;
    private UIText _contentTitle = null!;
    private UIText _contentDescription = null!;
    private JournalIconButton _buildBuilderButton = null!;
    private JournalTextButton _buildSaveButton = null!;
    private UIElement _stageListContainer = null!;
    private UIElement _classSelectionContainer = null!;
    private UIList _entryList = null!;
    private UIScrollbar _scrollbar = null!;
    private UIPanel _sourcePanel = null!;
    private JournalIconButton _sourceClearButton = null!;
    private UIElement _sourcePreviewContainer = null!;
    private UIText _sourceItemName = null!;
    private UIList _sourceList = null!;
    private UIScrollbar _sourceScrollbar = null!;
    private JournalDimOverlay _buildPickerOverlay = null!;
    private UIPanel _buildPickerPanel = null!;
    private UIText _buildPickerTitle = null!;
    private JournalIconButton _buildPickerCloseButton = null!;
    private UIList _buildPickerList = null!;
    private UIScrollbar _buildPickerScrollbar = null!;
    private JournalDimOverlay _buildSaveOverlay = null!;
    private UIPanel _buildSavePanel = null!;
    private UIText _buildSaveTitle = null!;
    private JournalTextInput _buildSaveNameInput = null!;
    private UIText _buildSaveMessage = null!;
    private JournalTextButton _buildSaveConfirmButton = null!;
    private JournalTextButton _buildSaveCancelButton = null!;
    private bool _layoutInitialized;
    private int _layoutScreenWidth;
    private int _layoutScreenHeight;
    private bool _windowPositionInitialized;

    private sealed record CachedAcquisitionView(UIElement PreviewElement, IReadOnlyList<UIElement> Entries);

    public override void OnInitialize()
    {
        _root = new JournalDraggablePanel();
        _root.SetPadding(0f);
        _root.BackgroundColor = JournalUiTheme.RootBackground * JournalUiTheme.RootBackgroundOpacity;
        _root.BorderColor = JournalUiTheme.RootBorder;
        Append(_root);

        InitializeHeader();
        InitializeStagePanel();
        InitializeMainPanel();
        InitializeBuildPickerOverlay();
        InitializeBuildSaveOverlay();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (_root.ContainsPoint(Main.MouseScreen))
        {
            Main.LocalPlayer.mouseInterface = true;
            Main.blockMouse = true;
        }

        if (!Main.keyState.IsKeyDown(Keys.Escape) || !Main.oldKeyState.IsKeyUp(Keys.Escape))
        {
            return;
        }

        if (JournalSystem.ShowingBuildSaveDialog)
        {
            JournalSystem.CloseBuildSaveDialog();
            return;
        }

        if (JournalSystem.ActiveBuildSlotKey is not null)
        {
            JournalSystem.CloseBuildSlotPicker();
            return;
        }

        JournalSystem.HideView();
    }

    public void Refresh(
        CombatClass combatClass,
        ProgressionStageId stageId,
        bool selectingClass,
        bool showingPresets,
        bool showingBuildBuilder,
        bool showingCombatBuffsPage,
        bool progressionModeEnabled,
        bool hasSelectedClass,
        int selectedItemId)
    {
        ApplyNavigationLayout(hasSelectedClass);
        EnsureLayout();
        ApplyContentLayout(selectingClass, showingPresets);
        RefreshBuildActionButtons(selectingClass, showingPresets, showingBuildBuilder);
        UpdateStaticText(progressionModeEnabled);
        UpdateNavigationStyles(selectingClass, showingPresets);
        JournalStageButtonPresenter.Refresh(_stageButtons, stageId, progressionModeEnabled);
        RefreshContent(combatClass, stageId, selectingClass, showingPresets, showingBuildBuilder, showingCombatBuffsPage, selectedItemId);
        RefreshBuildPickerOverlay(combatClass, stageId, showingPresets, showingBuildBuilder);
        RefreshBuildSaveOverlay(showingPresets, showingBuildBuilder);
        Recalculate();
    }

    public void ResetLayout()
    {
        _layoutInitialized = false;
        _windowPositionInitialized = false;
        _acquisitionViewCache.Clear();
        _root.ResetDragState();
        HideBuildPickerOverlay();
        HideBuildSaveOverlay(clearInput: true);
    }

    private void RefreshContent(
        CombatClass combatClass,
        ProgressionStageId stageId,
        bool selectingClass,
        bool showingPresets,
        bool showingBuildBuilder,
        bool showingCombatBuffsPage,
        int selectedItemId)
    {
        _entryList.Clear();
        _classSelectionContainer.RemoveAllChildren();
        _sourceList.Clear();
        _sourcePreviewContainer.RemoveAllChildren();
        SwitchContentMode(selectingClass);

        if (selectingClass)
        {
            SetContentHeader(Language.GetTextValue("Mods.ProgressionJournal.UI.ClassPageTitle"));
            PopulateClassSelection(combatClass);
            ClearAcquisitionPanel();
            return;
        }

        if (showingPresets)
        {
            var presetClassName = Language.GetTextValue($"Mods.ProgressionJournal.Classes.{combatClass}");
            var presetStageName = Language.GetTextValue(ProgressionStageCatalog.Get(stageId).LocalizationKey);
            SetContentHeader($"{presetClassName} • {presetStageName}");
            var savedBuilds = JournalSystem.GetSavedBuilds(stageId, combatClass);
            SetContentDescription(Language.GetTextValue("Mods.ProgressionJournal.UI.PresetsCount", savedBuilds.Count), 0.76f);

            if (showingBuildBuilder)
            {
                JournalContentBuilder.PopulateBuildPlanner(
                    _entryList,
                    stageId,
                    combatClass,
                    JournalSystem.GetSelectedBuildItem,
                    JournalSystem.OpenBuildSlot);
            }
            else if (savedBuilds.Count > 0)
            {
                JournalContentBuilder.PopulateSavedBuilds(_entryList, stageId, combatClass, savedBuilds);
            }
            else
            {
                _entryList.Add(CreateSourceNotice(Language.GetTextValue("Mods.ProgressionJournal.UI.SavedBuildsEmpty")));
            }

            ClearAcquisitionPanel();
            return;
        }

        var className = Language.GetTextValue($"Mods.ProgressionJournal.Classes.{combatClass}");
        var stageName = Language.GetTextValue(ProgressionStageCatalog.Get(stageId).LocalizationKey);
        SetContentHeader($"{className} • {stageName}");
        _entryList.Add(CreateOverviewPageSwitcherBlock(showingCombatBuffsPage));

        if (showingCombatBuffsPage)
        {
            JournalContentBuilder.PopulateCombatBuffs(
                _entryList,
                JournalRepository.GetCombatBuffEntries(stageId, combatClass),
                JournalSystem.SelectItem);
        }
        else
        {
            JournalContentBuilder.PopulateEntries(
                _entryList,
                stageId,
                JournalRepository.GetEntries(stageId, combatClass),
                JournalSystem.SelectItem);
        }

        RefreshAcquisitionPanel(selectedItemId);
    }

    private void RefreshAcquisitionPanel(int selectedItemId)
    {
        _sourceClearButton.SetStyle(JournalUiTheme.GetHeaderButtonStyle(danger: true));
        _sourceClearButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemClearTooltip"));
        _sourcePreviewContainer.RemoveAllChildren();
        _sourceList.Clear();

        if (selectedItemId <= ItemID.None)
        {
            ClearAcquisitionPanel();
            return;
        }

        var selectedItem = JournalItemUtilities.CreateItem(selectedItemId);
        var itemNameMaxWidth = MathF.Max(80f, _sourcePanel.GetDimensions().Width - JournalUiMetrics.AcquisitionPanelInset * 2f - 18f);
        _sourceItemName.SetText(JournalTextUtilities.TrimToPixelWidth(
            Lang.GetItemNameValue(selectedItemId),
            itemNameMaxWidth,
            JournalUiMetrics.AcquisitionPanelItemNameScale));

        if (_acquisitionViewCache.TryGetValue(selectedItemId, out var cachedView))
        {
            _sourcePreviewContainer.Append(cachedView.PreviewElement);
            AddEntriesToSourceList(cachedView.Entries);
            return;
        }

        var previewStrip = new JournalItemStrip([selectedItem])
        {
            HAlign = 0.5f
        };
        previewStrip.Top.Set(0f, 0f);
        _sourcePreviewContainer.Append(previewStrip);

        var info = JournalItemSourceResolver.GetInfo(selectedItemId);
        var builtEntries = new List<UIElement>();
        if (!info.HasAnySources)
        {
            var notice = CreateSourceNotice(Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemNoData"));
            _sourceList.Add(notice);
            _acquisitionViewCache[selectedItemId] = new CachedAcquisitionView(previewStrip, [notice]);
            return;
        }

        if (info.Recipes.Count > 0)
        {
            builtEntries.Add(CreateSourceSectionHeader("Mods.ProgressionJournal.UI.SelectedItemCrafts"));
            builtEntries.AddRange(info.Recipes.Select(CreateRecipeSourceCard));
        }

        if (info.Drops.Count > 0)
        {
            builtEntries.Add(CreateSourceSectionHeader("Mods.ProgressionJournal.UI.SelectedItemDrops"));
            builtEntries.AddRange(CreateDropSourceCards(info.Drops));
        }

        if (info.Shops.Count > 0)
        {
            builtEntries.Add(CreateSourceSectionHeader("Mods.ProgressionJournal.UI.SelectedItemShops"));
            builtEntries.AddRange(info.Shops.Select(CreateShopSourceCard));
        }

        AddEntriesToSourceList(builtEntries);

        _acquisitionViewCache[selectedItemId] = new CachedAcquisitionView(previewStrip, builtEntries);
    }

    private void ClearAcquisitionPanel()
    {
        _sourcePreviewContainer.RemoveAllChildren();
        _sourceItemName.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemEmpty"));
        _sourceList.Clear();
        _sourceList.Add(CreateSourceNotice(Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemSelectPrompt")));
    }

    private static UIText CreateSourceSectionHeader(string localizationKey)
    {
        var header = JournalUiElementFactory.CreateSectionHeader(Language.GetTextValue(localizationKey));
        header.Width.Set(0f, 1f);
        return header;
    }

    private UIPanel CreateRecipeSourceCard(JournalRecipeSource recipe)
    {
        var panel = JournalUiElementFactory.CreatePanel();
        panel.Width.Set(0f, 1f);

        var top = JournalUiMetrics.BlockVerticalPadding;
        top = AppendDetailLabel(panel, "Mods.ProgressionJournal.UI.SelectedItemIngredients", top);
        top = AppendItemRows(panel, recipe.Ingredients, top);

        if (recipe.Stations.Count > 0)
        {
            top = AppendDetailLabel(panel, "Mods.ProgressionJournal.UI.SelectedItemStations", top + 4f);
            top = AppendItemRows(panel, recipe.Stations, top);
        }

        if (recipe.Conditions.Count > 0)
        {
            top = AppendConditionContent(panel, recipe.Conditions, top + 6f);
        }

        panel.Height.Set(top + JournalUiMetrics.BlockVerticalPadding, 0f);
        return panel;
    }

    private UIPanel CreateDropSourceCard(JournalDropSource drop)
    {
        var panel = JournalUiElementFactory.CreatePanel();
        panel.Width.Set(0f, 1f);

        var top = JournalUiMetrics.BlockVerticalPadding;
        var sourceLabelKey = drop.SourceItemId.HasValue
            ? "Mods.ProgressionJournal.UI.SelectedItemFromItem"
            : "Mods.ProgressionJournal.UI.SelectedItemSource";
        if (JournalAcquisitionVisuals.TryCreateSourceToken(drop, out var sourceToken))
        {
            top = AppendDetailLabel(panel, sourceLabelKey, top);
            top = AppendTokenRows(panel, [sourceToken], top);

            if (drop.SourceNpcType is { } sourceNpcType)
            {
                var npcLocationTokens = JournalAcquisitionVisuals.GetNpcLocationTokens(sourceNpcType);
                if (npcLocationTokens.Count > 0)
                {
                    top = AppendTokenRows(panel, npcLocationTokens, top + 6f);
                }
            }
        }
        else
        {
            top = AppendTextLines(panel, [$"{Language.GetTextValue(sourceLabelKey)}: {drop.SourceName}"], top);
        }

        var lines = new List<string>
        {
            $"{Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemChance")}: {FormatDropRate(drop.DropRate)}"
        };

        if (drop.StackMax > 1 || drop.StackMin > 1)
        {
            lines.Add($"{Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemStack")}: {FormatStackRange(drop.StackMin, drop.StackMax)}");
        }

        top = AppendTextLines(panel, lines, top + 8f);

        if (drop.Conditions.Count > 0)
        {
            top = AppendConditionContent(panel, drop.Conditions, top + 6f);
        }

        panel.Height.Set(top + JournalUiMetrics.BlockVerticalPadding, 0f);
        return panel;
    }

    private UIPanel CreateAggregatedNpcDropSourceCard(IReadOnlyList<JournalDropSource> drops)
    {
        var panel = JournalUiElementFactory.CreatePanel();
        panel.Width.Set(0f, 1f);

        var top = JournalUiMetrics.BlockVerticalPadding;
        top = AppendTextLines(
            panel,
            [$"{Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemSource")}: {Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemFromAnyEnemy")}"],
            top);

        var npcTypes = drops
            .Select(static drop => drop.SourceNpcType)
            .Where(static npcType => npcType.HasValue)
            .Select(static npcType => npcType!.Value)
            .Distinct()
            .ToArray();
        var commonLocationTokens = JournalAcquisitionVisuals.GetCommonNpcLocationTokens(npcTypes);
        if (commonLocationTokens.Count > 0)
        {
            top = AppendTokenRows(panel, commonLocationTokens, top + 6f);
        }

        var primaryDrop = drops[0];
        var lines = new List<string>
        {
            $"{Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemChance")}: {FormatDropRate(primaryDrop.DropRate)}"
        };

        if (primaryDrop.StackMax > 1 || primaryDrop.StackMin > 1)
        {
            lines.Add($"{Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemStack")}: {FormatStackRange(primaryDrop.StackMin, primaryDrop.StackMax)}");
        }

        top = AppendTextLines(panel, lines, top + 8f);

        if (primaryDrop.Conditions.Count > 0)
        {
            top = AppendConditionContent(panel, primaryDrop.Conditions, top + 6f);
        }

        panel.Height.Set(top + JournalUiMetrics.BlockVerticalPadding, 0f);
        return panel;
    }

    private UIPanel CreateShopSourceCard(JournalShopSource shop)
    {
        var panel = JournalUiElementFactory.CreatePanel();
        panel.Width.Set(0f, 1f);

        var top = JournalUiMetrics.BlockVerticalPadding;
        top = AppendDetailLabel(panel, "Mods.ProgressionJournal.UI.SelectedItemTrade", top);
        top = AppendTokenRows(panel, [JournalAcquisitionVisuals.CreateSourceToken(shop)], top);

        var lines = new List<string>();
        if (!shop.ShopName.Equals("Shop", StringComparison.OrdinalIgnoreCase))
        {
            lines.Add($"{Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemShopName")}: {shop.ShopName}");
        }

        if (lines.Count > 0)
        {
            top = AppendTextLines(panel, lines, top + 8f);
        }

        if (shop.Conditions.Count > 0)
        {
            top = AppendConditionContent(panel, shop.Conditions, top + 6f);
        }

        panel.Height.Set(top + JournalUiMetrics.BlockVerticalPadding, 0f);
        return panel;
    }

    private static UIElement CreateSourceNotice(string text)
    {
        const float noticeLineHeight = 24f;

        var container = new UIElement();
        container.Width.Set(0f, 1f);
        var wrappedLines = JournalTextUtilities.WrapToPixelWidth(
            text,
            JournalUiMetrics.AcquisitionPanelMinWidth - JournalUiMetrics.AcquisitionPanelInset * 2f,
            JournalUiMetrics.AcquisitionPanelNoticeScale);
        var top = 10f;

        foreach (var line in wrappedLines)
        {
            var notice = new UIText(line, JournalUiMetrics.AcquisitionPanelNoticeScale)
            {
                TextColor = JournalUiTheme.ContentDescriptionText
            };
            notice.Left.Set(JournalUiMetrics.AcquisitionPanelInset, 0f);
            notice.Top.Set(top, 0f);
            notice.Width.Set(-(JournalUiMetrics.AcquisitionPanelInset * 2f), 1f);
            container.Append(notice);
            top += noticeLineHeight;
        }

        container.Height.Set(top + 8f, 0f);
        return container;
    }

    private static float AppendDetailLabel(UIElement parent, string localizationKey, float top)
    {
        var label = new UIText(Language.GetTextValue(localizationKey), JournalUiMetrics.AcquisitionPanelLabelScale, true);
        label.Left.Set(JournalUiMetrics.BlockHorizontalPadding, 0f);
        label.Top.Set(top, 0f);
        label.Width.Set(-(JournalUiMetrics.BlockHorizontalPadding * 2f), 1f);
        label.TextColor = JournalUiTheme.SectionHeaderText;
        parent.Append(label);
        return top + JournalUiMetrics.AcquisitionPanelTextLineHeight;
    }

    private float AppendTextLines(UIElement parent, IEnumerable<string> lines, float top)
    {
        var maxWidth = GetSourceTextMaxWidth();

        foreach (var line in lines.Where(static line => !string.IsNullOrWhiteSpace(line)))
        {
            var wrappedLines = JournalTextUtilities.WrapToPixelWidth(line, maxWidth, JournalUiMetrics.AcquisitionPanelTextScale);

            foreach (var wrappedLine in wrappedLines)
            {
                var text = new UIText(wrappedLine, JournalUiMetrics.AcquisitionPanelTextScale);
                text.Left.Set(JournalUiMetrics.BlockHorizontalPadding, 0f);
                text.Top.Set(top, 0f);
                text.Width.Set(-(JournalUiMetrics.BlockHorizontalPadding * 2f), 1f);
                text.TextColor = JournalUiTheme.ContentDescriptionText;
                parent.Append(text);
                top += JournalUiMetrics.AcquisitionPanelTextLineHeight;
            }
        }

        return top;
    }

    private float AppendItemRows(UIElement parent, IReadOnlyList<Item> items, float top)
    {
        foreach (var rowItems in ChunkItems(items, GetSourcePanelItemSlotsPerRow()))
        {
            var strip = new JournalItemStrip(rowItems);
            strip.Left.Set(JournalUiMetrics.BlockHorizontalPadding, 0f);
            strip.Top.Set(top, 0f);
            parent.Append(strip);
            top += JournalUiMetrics.RowHeight;
        }

        return top;
    }

    private IEnumerable<UIElement> CreateDropSourceCards(IReadOnlyList<JournalDropSource> drops)
    {
        return GroupDropSourcesForDisplay(drops)
            .Select(group => group.Count > 1 && group.All(static drop => drop is { SourceNpcType: not null, SourceItemId: null })
                ? (UIElement)CreateAggregatedNpcDropSourceCard(group)
                : CreateDropSourceCard(group[0]));
    }

    private static IReadOnlyList<JournalDropSource>[] GroupDropSourcesForDisplay(IReadOnlyList<JournalDropSource> drops)
    {
        return drops
            .OrderByDescending(static source => source.DropRate)
            .ThenBy(static source => source.SourceName, StringComparer.CurrentCultureIgnoreCase)
            .GroupBy(static drop => new
            {
                drop.DropRate,
                drop.StackMin,
                drop.StackMax,
                Conditions = CreateConditionGroupSignature(drop.Conditions),
                IsNpcSource = drop is { SourceNpcType: not null, SourceItemId: null }
            })
            .Select(static group =>
            {
                var groupedDrops = group.ToArray();
                return groupedDrops.Length >= 4 && group.Key.IsNpcSource
                    ? (IReadOnlyList<JournalDropSource>)groupedDrops
                    : [groupedDrops[0]];
            })
            .ToArray();
    }

    private float AppendConditionContent(UIElement parent, IReadOnlyList<string> conditions, float top)
    {
        var visuals = JournalAcquisitionVisuals.SplitConditions(conditions);

        if (visuals.Tokens.Count > 0)
        {
            top = AppendTokenRows(parent, visuals.Tokens, top);
        }

        if (visuals.RemainingText.Count > 0)
        {
            top = AppendConditionTextList(parent, visuals.RemainingText, visuals.Tokens.Count > 0 ? top + 2f : top);
        }

        return top;
    }

    private float AppendConditionTextList(UIElement parent, IReadOnlyList<string> conditions, float top)
    {
        return conditions.Count == 0 ? top : AppendTextLines(parent, [string.Join(" • ", conditions)], top);
    }

    private float AppendTokenRows(UIElement parent, IReadOnlyList<JournalSourceTokenData> tokens, float top)
    {
        if (tokens.Count == 0)
        {
            return top;
        }

        const float left = JournalUiMetrics.BlockHorizontalPadding;
        var maxWidth = GetSourceTextMaxWidth();
        const float spacing = 6f;
        var rows = new List<List<JournalSourceTokenData>>();
        var currentRow = new List<JournalSourceTokenData>();
        var currentRowWidth = 0f;

        foreach (var tokenData in tokens)
        {
            var tokenWidth = JournalSourceToken.GetTokenSize(tokenData);
            var projectedWidth = currentRow.Count == 0
                ? tokenWidth
                : currentRowWidth + spacing + tokenWidth;

            if (currentRow.Count > 0 && projectedWidth > maxWidth)
            {
                rows.Add(currentRow);
                currentRow = [];
                currentRowWidth = 0f;
            }

            currentRow.Add(tokenData);
            currentRowWidth = currentRow.Count == 1
                ? tokenWidth
                : currentRowWidth + spacing + tokenWidth;
        }

        if (currentRow.Count > 0)
        {
            rows.Add(currentRow);
        }

        var rowTop = top;
        foreach (var row in rows)
        {
            var rowWidth = row.Sum(static token => JournalSourceToken.GetTokenSize(token)) + spacing * (row.Count - 1);
            var currentX = left + MathF.Max(0f, (maxWidth - rowWidth) * 0.5f);
            var rowHeight = 0f;

            foreach (var tokenData in row)
            {
                var tokenSize = JournalSourceToken.GetTokenSize(tokenData);
                var token = new JournalSourceToken(tokenData);
                token.Left.Set(currentX, 0f);
                token.Top.Set(rowTop, 0f);
                parent.Append(token);
                currentX += tokenSize + spacing;
                rowHeight = MathF.Max(rowHeight, tokenSize);
            }

            rowTop += rowHeight + spacing;
        }

        return rowTop - spacing;
    }

    private static string CreateConditionGroupSignature(IReadOnlyList<string> conditions)
    {
        return string.Join(
            '\n',
            conditions
                .Where(static condition => !string.IsNullOrWhiteSpace(condition))
                .Select(static condition => string.Join(' ', condition.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)))
                .OrderBy(static condition => condition, StringComparer.CurrentCultureIgnoreCase));
    }

    private static IEnumerable<Item[]> ChunkItems(IReadOnlyList<Item> items, int chunkSize)
    {
        for (var index = 0; index < items.Count; index += chunkSize)
        {
            var count = Math.Min(chunkSize, items.Count - index);
            var chunk = new Item[count];
            for (var offset = 0; offset < count; offset++)
            {
                chunk[offset] = items[index + offset].Clone();
            }

            yield return chunk;
        }
    }

    private static string FormatDropRate(float dropRate)
    {
        return dropRate <= 0f ? "0%" : $"{dropRate * 100f:0.##}%";
    }

    private static string FormatStackRange(int stackMin, int stackMax)
    {
        return stackMin == stackMax ? stackMin.ToString() : $"{stackMin}-{stackMax}";
    }

    private float GetSourceTextMaxWidth()
    {
        var sourceWidth = _sourceList.GetDimensions().Width;
        if (sourceWidth <= 0f)
        {
            sourceWidth = JournalUiMetrics.AcquisitionPanelMinWidth - JournalUiMetrics.ScrollbarWidth - 8f;
        }

        return MathF.Max(80f, sourceWidth - JournalUiMetrics.BlockHorizontalPadding * 2f);
    }

    private int GetSourcePanelItemSlotsPerRow()
    {
        var maxWidth = GetSourceTextMaxWidth();

        for (var slots = 4; slots >= 1; slots--)
        {
            if (JournalItemStrip.GetVisualWidth(slots) <= maxWidth)
            {
                return slots;
            }
        }

        return 1;
    }

    private void SetContentHeader(string title)
    {
        _contentTitle.SetText(title);
        _contentDescription.SetText(string.Empty);
    }

    private void SetContentDescription(string text, float textScale = JournalUiMetrics.ContentDescriptionScale)
    {
        _contentDescription.SetText(text, textScale, false);
    }

    private static UIPanel CreateOverviewPageSwitcherBlock(bool showingCombatBuffsPage)
    {
        var panel = JournalUiElementFactory.CreatePanel();
        panel.Width.Set(0f, 1f);
        panel.Height.Set(40f, 0f);

        var currentPageLabel = Language.GetTextValue(
            showingCombatBuffsPage
                ? "Mods.ProgressionJournal.UI.CombatBuffsTitle"
                : "Mods.ProgressionJournal.UI.OverviewTab");
        var targetPageLabel = Language.GetTextValue(
            showingCombatBuffsPage
                ? "Mods.ProgressionJournal.UI.OverviewTab"
                : "Mods.ProgressionJournal.UI.CombatBuffsTitle");

        var previousButton = JournalUiElementFactory.CreateIconButton(
            BestiaryBackButtonTexturePath,
            22f,
            22f,
            () => JournalSystem.CycleOverviewPage(-1),
            0.95f);
        previousButton.Left.Set(JournalUiMetrics.BlockHorizontalPadding + 6f, 0f);
        previousButton.Top.Set(9f, 0f);
        previousButton.SetHoverText(targetPageLabel);
        panel.Append(previousButton);

        var nextButton = JournalUiElementFactory.CreateIconButton(
            BestiaryForwardButtonTexturePath,
            22f,
            22f,
            () => JournalSystem.CycleOverviewPage(1),
            0.95f);
        nextButton.Left.Set(-(JournalUiMetrics.BlockHorizontalPadding + 28f), 1f);
        nextButton.Top.Set(9f, 0f);
        nextButton.SetHoverText(targetPageLabel);
        panel.Append(nextButton);

        var label = new UIText(currentPageLabel, JournalUiMetrics.ContentPageLabelScale, true)
        {
            HAlign = 0.5f,
            VAlign = 0.5f,
            TextColor = JournalUiTheme.ContentDescriptionText
        };
        panel.Append(label);

        return panel;
    }

    private void AddEntriesToSourceList(IEnumerable<UIElement> entries)
    {
        foreach (var entry in entries)
        {
            _sourceList.Add(entry);
        }
    }

    private void RefreshBuildPickerOverlay(CombatClass combatClass, ProgressionStageId stageId, bool showingPresets, bool showingBuildBuilder)
    {
        if (!showingPresets || !showingBuildBuilder || JournalSystem.ShowingBuildSaveDialog || JournalSystem.ActiveBuildSlotKey is not { } slotKey)
        {
            HideBuildPickerOverlay();
            return;
        }

        if (_buildPickerOverlay.Parent is null)
        {
            _root.Append(_buildPickerOverlay);
        }

        if (_buildPickerPanel.Parent is null)
        {
            _root.Append(_buildPickerPanel);
        }

        var rootDimensions = _root.GetDimensions();
        var panelWidth = MathF.Min(JournalUiMetrics.BuildPickerWidth, rootDimensions.Width - 48f);
        var panelHeight = MathF.Min(JournalUiMetrics.BuildPickerHeight, rootDimensions.Height - 48f);
        _buildPickerPanel.Width.Set(panelWidth, 0f);
        _buildPickerPanel.Height.Set(panelHeight, 0f);
        _buildPickerPanel.Left.Set((rootDimensions.Width - panelWidth) * 0.5f, 0f);
        _buildPickerPanel.Top.Set((rootDimensions.Height - panelHeight) * 0.5f, 0f);

        _buildPickerTitle.SetText(JournalBuildPlannerCatalog.GetSlotDisplayName(slotKey, combatClass));
        _buildPickerList.Clear();

        var candidates = JournalRepository.GetBuildCandidates(
            stageId,
            combatClass,
            slotKey);
        if (candidates.Count == 0)
        {
            _buildPickerList.Add(CreateSourceNotice(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildPickerEmpty")));
            return;
        }

        var highlightedItemIds = JournalSystem.GetHighlightedBuildItemIds(slotKey);
        var blockedItemIds = JournalSystem.GetBlockedBuildItemIds(slotKey);
        var slotsPerRow = GetBuildPickerSlotsPerRow(panelWidth);
        for (var index = 0; index < candidates.Count; index += slotsPerRow)
        {
            var rowCandidates = candidates.Skip(index).Take(slotsPerRow).ToArray();
            _buildPickerList.Add(CreateBuildCandidateRow(rowCandidates, highlightedItemIds, blockedItemIds));
        }
    }

    private void RefreshBuildSaveOverlay(bool showingPresets, bool showingBuildBuilder)
    {
        if (!showingPresets || !showingBuildBuilder || !JournalSystem.ShowingBuildSaveDialog)
        {
            HideBuildSaveOverlay(clearInput: true);
            return;
        }

        var dialogOpened = _buildSavePanel.Parent is null;
        if (_buildSaveOverlay.Parent is null)
        {
            _root.Append(_buildSaveOverlay);
        }

        if (_buildSavePanel.Parent is null)
        {
            _root.Append(_buildSavePanel);
        }

        if (dialogOpened)
        {
            _buildSaveNameInput.SetText(string.Empty);
            _buildSaveNameInput.Focused = true;
            _buildSaveMessage.SetText(string.Empty);
        }

        var rootDimensions = _root.GetDimensions();
        var panelWidth = MathF.Min(440f, rootDimensions.Width - 48f);
        const float panelHeight = 210f;
        _buildSavePanel.Width.Set(panelWidth, 0f);
        _buildSavePanel.Height.Set(panelHeight, 0f);
        _buildSavePanel.Left.Set((rootDimensions.Width - panelWidth) * 0.5f, 0f);
        _buildSavePanel.Top.Set((rootDimensions.Height - panelHeight) * 0.5f, 0f);
    }

    private void HideBuildPickerOverlay()
    {
        if (_buildPickerOverlay.Parent is not null)
        {
            _root.RemoveChild(_buildPickerOverlay);
        }

        if (_buildPickerPanel.Parent is not null)
        {
            _root.RemoveChild(_buildPickerPanel);
        }
    }

    private void HideBuildSaveOverlay(bool clearInput)
    {
        _buildSaveNameInput.Focused = false;

        if (_buildSaveOverlay.Parent is not null)
        {
            _root.RemoveChild(_buildSaveOverlay);
        }

        if (_buildSavePanel.Parent is not null)
        {
            _root.RemoveChild(_buildSavePanel);
        }

        if (clearInput)
        {
            _buildSaveNameInput.SetText(string.Empty);
            _buildSaveMessage.SetText(string.Empty);
        }
    }

    private static UIElement CreateBuildCandidateRow(
        IReadOnlyList<JournalBuildCandidate> candidates,
        IReadOnlySet<int> highlightedItemIds,
        IReadOnlySet<int> blockedItemIds)
    {
        var items = candidates
            .Select(static candidate => JournalItemUtilities.CreateItem(candidate.ItemId))
            .ToArray();
        var row = new UIElement
        {
            HAlign = 0.5f
        };
        row.Width.Set(GetBuildCandidateRowWidth(candidates.Count), 0f);
        row.Height.Set(JournalUiMetrics.BuildSlotSize, 0f);

        var left = 0f;
        for (var index = 0; index < candidates.Count; index++)
        {
            var index1 = index;
            var slot = new JournalBuildCandidateSlot(
                items[index],
                highlightedItemIds.Contains(candidates[index].ItemId),
                blockedItemIds.Contains(candidates[index].ItemId),
                () => JournalSystem.SelectActiveBuildItem(candidates[index1].ItemId));
            slot.Left.Set(left, 0f);
            row.Append(slot);
            left += JournalUiMetrics.BuildSlotSize + JournalUiMetrics.BuildSlotGap;
        }

        return row;
    }

    private static int GetBuildPickerSlotsPerRow(float panelWidth)
    {
        var availableWidth = panelWidth - (JournalUiMetrics.BuildPickerInset * 2f + JournalUiMetrics.ScrollbarWidth + 4f);
        return Math.Max(1, (int)((availableWidth + JournalUiMetrics.BuildSlotGap) / (JournalUiMetrics.BuildSlotSize + JournalUiMetrics.BuildSlotGap)));
    }

    private static float GetBuildCandidateRowWidth(int slotCount)
    {
        if (slotCount <= 0)
        {
            return 0f;
        }

        return slotCount * JournalUiMetrics.BuildSlotSize + (slotCount - 1) * JournalUiMetrics.BuildSlotGap;
    }

    private void EnsureLayout()
    {
        if (_layoutInitialized && _layoutScreenWidth == Main.screenWidth && _layoutScreenHeight == Main.screenHeight)
        {
            return;
        }

        var width = MathF.Min(JournalUiMetrics.RootMaxWidth, Main.screenWidth - JournalUiMetrics.RootHorizontalMargin);
        var height = MathF.Min(JournalUiMetrics.RootMaxHeight, Main.screenHeight - JournalUiMetrics.RootVerticalMargin);
        _root.Width.Set(width, 0f);
        _root.Height.Set(height, 0f);
        if (!_windowPositionInitialized)
        {
            var defaultPosition = GetDefaultWindowPosition(width, height);
            _root.Left.Set(defaultPosition.X, 0f);
            _root.Top.Set(defaultPosition.Y, 0f);
            _windowPositionInitialized = true;
        }

        _root.Recalculate();

        JournalStageButtonPresenter.Layout(_stageButtons, _stageListContainer, JournalOrdering.StageSelection);
        _layoutInitialized = true;
        _layoutScreenWidth = Main.screenWidth;
        _layoutScreenHeight = Main.screenHeight;
    }

    private void InitializeHeader()
    {
        _closeButton = JournalUiElementFactory.CreateIconButton(
            BestiarySearchCancelTexturePath,
            JournalUiMetrics.CloseTabWidth,
            JournalUiMetrics.ActionTabHeight,
            () => JournalSystem.HideView(),
            1.12f);
        _closeButton.Left.Set(-JournalUiMetrics.CloseTabWidth, 1f);
        _closeButton.Top.Set(0f, 0f);
        _root.Append(_closeButton);

        InitializeContentTabs();
    }

    private void InitializeStagePanel()
    {
        _stagePanel = JournalUiElementFactory.CreatePanel();
        _root.AddDragTarget(_stagePanel);
        _stagePanel.Left.Set(JournalUiMetrics.OuterPadding, 0f);
        _stagePanel.Top.Set(JournalUiMetrics.HeaderHeight + JournalUiMetrics.OuterPadding, 0f);
        _stagePanel.Width.Set(JournalUiMetrics.StagePanelWidth, 0f);
        _stagePanel.Height.Set(-(JournalUiMetrics.HeaderHeight + JournalUiMetrics.OuterPadding * 2f), 1f);
        _root.Append(_stagePanel);

        _stagePanelTitle = new UIText(string.Empty, JournalUiMetrics.StagePanelTitleScale, true)
        {
            HAlign = 0.5f
        };
        _stagePanelTitle.Top.Set(JournalUiMetrics.StagePanelTitleTop, 0f);
        _stagePanel.Append(_stagePanelTitle);

        _progressionModeToggleButton = JournalUiElementFactory.CreateTextButton(
            string.Empty,
            JournalUiMetrics.StageProgressionToggleSize,
            JournalUiMetrics.StageProgressionToggleSize,
            () => JournalSystem.ToggleProgressionMode(),
            0.75f);
        _progressionModeToggleButton.Left.Set(-(JournalUiMetrics.StageProgressionToggleRightInset + JournalUiMetrics.StageProgressionToggleSize), 1f);
        _progressionModeToggleButton.Top.Set(JournalUiMetrics.StageProgressionToggleTop, 0f);
        _stagePanel.Append(_progressionModeToggleButton);

        _stageListContainer = new UIElement();
        _root.AddDragTarget(_stageListContainer);
        _stageListContainer.Left.Set(JournalUiMetrics.StageListLeft, 0f);
        _stageListContainer.Top.Set(JournalUiMetrics.StageListTop, 0f);
        _stageListContainer.Width.Set(-JournalUiMetrics.StageListHorizontalInset, 1f);
        _stageListContainer.Height.Set(-JournalUiMetrics.StageListBottomInset, 1f);
        _stagePanel.Append(_stageListContainer);

        foreach (var stage in ProgressionStageCatalog.All)
        {
            var capturedStage = stage.Id;
            var button = JournalUiElementFactory.CreateStageButton(() => JournalSystem.SelectStage(capturedStage));
            button.Left.Set(0f, 0f);
            button.Width.Set(0f, 1f);
            _stageListContainer.Append(button);
            _stageButtons[capturedStage] = button;
        }
    }

    private void InitializeMainPanel()
    {
        _mainPanel = JournalUiElementFactory.CreatePanel();
        _root.AddDragTarget(_mainPanel);
        _mainPanel.Left.Set(JournalUiMetrics.OuterPadding + JournalUiMetrics.StagePanelWidth + JournalUiMetrics.PanelGap, 0f);
        _mainPanel.Top.Set(JournalUiMetrics.HeaderHeight + JournalUiMetrics.OuterPadding, 0f);
        _mainPanel.Width.Set(-(JournalUiMetrics.OuterPadding * 2f + JournalUiMetrics.StagePanelWidth + JournalUiMetrics.PanelGap), 1f);
        _mainPanel.Height.Set(-(JournalUiMetrics.HeaderHeight + JournalUiMetrics.OuterPadding * 2f), 1f);
        _root.Append(_mainPanel);

        _contentPanel = new UIElement();
        _root.AddDragTarget(_contentPanel);
        _contentPanel.Left.Set(JournalUiMetrics.ContentInset, 0f);
        _contentPanel.Top.Set(JournalUiMetrics.ContentInset, 0f);
        _contentPanel.Width.Set(-JournalUiMetrics.ContentInset * 2f, 1f);
        _contentPanel.Height.Set(-(JournalUiMetrics.ContentInset * 2f), 1f);
        _mainPanel.Append(_contentPanel);

        _contentTitle = new UIText(string.Empty, JournalUiMetrics.ContentTitleScale, true)
        {
            HAlign = 0.5f
        };
        _contentTitle.Top.Set(JournalUiMetrics.ContentTitleTop, 0f);
        _contentPanel.Append(_contentTitle);

        _buildBuilderButton = JournalUiElementFactory.CreateIconButton(
            CraftingWindowToggleTexturePath,
            30f,
            30f,
            () => JournalSystem.ShowBuildBuilderPage(),
            0.9f);
        _buildBuilderButton.Left.Set(-38f, 1f);
        _buildBuilderButton.Top.Set(8f, 0f);
        _buildBuilderButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildBuilderTab"));

        _buildSaveButton = JournalUiElementFactory.CreateTextButton(
            Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveButton"),
            196f,
            30f,
            () => JournalSystem.OpenBuildSaveDialog(),
            0.88f);
        _buildSaveButton.Left.Set(-242f, 1f);
        _buildSaveButton.Top.Set(8f, 0f);
        _buildSaveButton.SetStyle(JournalUiTheme.GetOverlayActionButtonStyle());
        _buildSaveButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveTooltip"));

        _contentDescription = new UIText(string.Empty, JournalUiMetrics.ContentDescriptionScale);
        _contentDescription.Left.Set(JournalUiMetrics.ContentDescriptionLeft, 0f);
        _contentDescription.Top.Set(JournalUiMetrics.ContentDescriptionTop, 0f);
        _contentDescription.TextColor = JournalUiTheme.ContentDescriptionText;
        _contentPanel.Append(_contentDescription);

        _classSelectionContainer = new UIElement();
        _root.AddDragTarget(_classSelectionContainer);
        _classSelectionContainer.Left.Set(JournalUiMetrics.ContentBodyLeft, 0f);
        _classSelectionContainer.Top.Set(JournalUiMetrics.ContentBodyTop, 0f);
        _classSelectionContainer.Width.Set(-JournalUiMetrics.ContentBodyHorizontalInset, 1f);
        _classSelectionContainer.Height.Set(-JournalUiMetrics.ContentBodyBottomInset, 1f);
        _contentPanel.Append(_classSelectionContainer);

        _entryList = [];
        _root.AddDragTarget(_entryList);
        _entryList.Left.Set(JournalUiMetrics.ContentBodyLeft, 0f);
        _entryList.Top.Set(JournalUiMetrics.ContentBodyTop, 0f);
        _entryList.Width.Set(-JournalUiMetrics.EntryListWidthInset, 1f);
        _entryList.Height.Set(-JournalUiMetrics.ContentBodyBottomInset, 1f);
        _entryList.ListPadding = JournalUiMetrics.EntryListPadding;
        _contentPanel.Append(_entryList);

        _scrollbar = new UIScrollbar();
        _scrollbar.Width.Set(JournalUiMetrics.ScrollbarWidth, 0f);
        _scrollbar.Left.Set(-JournalUiMetrics.ScrollbarOffset, 1f);
        _scrollbar.Top.Set(JournalUiMetrics.ContentBodyTop, 0f);
        _scrollbar.Height.Set(-JournalUiMetrics.ContentBodyBottomInset, 1f);
        _contentPanel.Append(_scrollbar);
        _entryList.SetScrollbar(_scrollbar);

        InitializeAcquisitionPanel();
    }

    private void RefreshBuildActionButtons(bool selectingClass, bool showingPresets, bool showingBuildBuilder)
    {
        ToggleContentButton(_buildBuilderButton, !selectingClass && showingPresets && !showingBuildBuilder);
        ToggleContentButton(_buildSaveButton, !selectingClass && showingPresets && showingBuildBuilder);
    }

    private void ToggleContentButton(UIElement element, bool visible)
    {
        if (visible)
        {
            if (element.Parent is null)
            {
                _contentPanel.Append(element);
            }

            return;
        }

        if (element.Parent is not null)
        {
            _contentPanel.RemoveChild(element);
        }
    }

    private void InitializeAcquisitionPanel()
    {
        _sourcePanel = JournalUiElementFactory.CreatePanel();
        _root.AddDragTarget(_sourcePanel);
        _sourcePanel.Top.Set(JournalUiMetrics.ContentBodyTop, 0f);
        _sourcePanel.Width.Set(JournalUiMetrics.AcquisitionPanelWidth, 0f);
        _sourcePanel.Height.Set(-JournalUiMetrics.ContentBodyBottomInset, 1f);

        _sourceClearButton = JournalUiElementFactory.CreateIconButton(
            BestiarySearchCancelTexturePath,
            24f,
            24f,
            () => JournalSystem.ClearSelectedItem(),
            0.72f);
        _sourceClearButton.Left.Set(-30f, 1f);
        _sourceClearButton.Top.Set(6f, 0f);
        _sourcePanel.Append(_sourceClearButton);

        _sourcePreviewContainer = new UIElement();
        _root.AddDragTarget(_sourcePreviewContainer);
        _sourcePreviewContainer.Left.Set(JournalUiMetrics.AcquisitionPanelInset, 0f);
        _sourcePreviewContainer.Top.Set(JournalUiMetrics.AcquisitionPanelPreviewTop, 0f);
        _sourcePreviewContainer.Width.Set(-(JournalUiMetrics.AcquisitionPanelInset * 2f), 1f);
        _sourcePreviewContainer.Height.Set(40f, 0f);
        _sourcePanel.Append(_sourcePreviewContainer);

        _sourceItemName = new UIText(string.Empty, JournalUiMetrics.AcquisitionPanelItemNameScale, true)
        {
            HAlign = 0.5f,
            TextColor = JournalUiTheme.RootTitleText
        };
        _sourceItemName.Width.Set(-(JournalUiMetrics.AcquisitionPanelInset * 2f), 1f);
        _sourceItemName.Top.Set(JournalUiMetrics.AcquisitionPanelNameTop, 0f);
        _sourcePanel.Append(_sourceItemName);

        _sourceList = [];
        _root.AddDragTarget(_sourceList);
        _sourceList.Left.Set(JournalUiMetrics.AcquisitionPanelInset, 0f);
        _sourceList.Top.Set(JournalUiMetrics.AcquisitionPanelContentTop, 0f);
        _sourceList.Width.Set(-(JournalUiMetrics.AcquisitionPanelInset * 2f + JournalUiMetrics.ScrollbarWidth + 4f), 1f);
        _sourceList.Height.Set(-(JournalUiMetrics.AcquisitionPanelContentTop + JournalUiMetrics.AcquisitionPanelInset), 1f);
        _sourceList.ListPadding = JournalUiMetrics.EntryListPadding;
        _sourcePanel.Append(_sourceList);

        _sourceScrollbar = new UIScrollbar();
        _sourceScrollbar.Width.Set(JournalUiMetrics.ScrollbarWidth, 0f);
        _sourceScrollbar.Left.Set(-(JournalUiMetrics.ScrollbarWidth + 4f), 1f);
        _sourceScrollbar.Top.Set(JournalUiMetrics.AcquisitionPanelContentTop, 0f);
        _sourceScrollbar.Height.Set(-(JournalUiMetrics.AcquisitionPanelContentTop + JournalUiMetrics.AcquisitionPanelInset), 1f);
        _sourcePanel.Append(_sourceScrollbar);
        _sourceList.SetScrollbar(_sourceScrollbar);
    }

    private void InitializeBuildPickerOverlay()
    {
        _buildPickerOverlay = new JournalDimOverlay(() => JournalSystem.CloseBuildSlotPicker());

        _buildPickerPanel = JournalUiElementFactory.CreatePanel();
        _buildPickerPanel.SetPadding(0f);
        _buildPickerPanel.BackgroundColor = JournalUiTheme.RootBackground * JournalUiTheme.RootBackgroundOpacity;
        _buildPickerPanel.BorderColor = JournalUiTheme.RootBorder;

        _buildPickerTitle = new UIText(string.Empty, JournalUiMetrics.BuildPickerTitleScale, true)
        {
            HAlign = 0.5f,
            TextColor = JournalUiTheme.RootTitleText
        };
        _buildPickerTitle.Top.Set(JournalUiMetrics.BuildPickerHeaderTop, 0f);
        _buildPickerPanel.Append(_buildPickerTitle);

        _buildPickerCloseButton = JournalUiElementFactory.CreateIconButton(
            BestiarySearchCancelTexturePath,
            24f,
            24f,
            () => JournalSystem.CloseBuildSlotPicker(),
            0.8f);
        _buildPickerCloseButton.Left.Set(-34f, 1f);
        _buildPickerCloseButton.Top.Set(8f, 0f);
        _buildPickerPanel.Append(_buildPickerCloseButton);

        _buildPickerList = [];
        _buildPickerList.Left.Set(JournalUiMetrics.BuildPickerInset, 0f);
        _buildPickerList.Top.Set(JournalUiMetrics.BuildPickerListTop, 0f);
        _buildPickerList.Width.Set(-(JournalUiMetrics.BuildPickerInset * 2f + JournalUiMetrics.ScrollbarWidth + 4f), 1f);
        _buildPickerList.Height.Set(-(JournalUiMetrics.BuildPickerListTop + JournalUiMetrics.BuildPickerListBottomInset), 1f);
        _buildPickerList.ListPadding = JournalUiMetrics.EntryListPadding;
        _buildPickerPanel.Append(_buildPickerList);

        _buildPickerScrollbar = new UIScrollbar();
        _buildPickerScrollbar.Width.Set(JournalUiMetrics.ScrollbarWidth, 0f);
        _buildPickerScrollbar.Left.Set(-(JournalUiMetrics.ScrollbarWidth + JournalUiMetrics.BuildPickerInset), 1f);
        _buildPickerScrollbar.Top.Set(JournalUiMetrics.BuildPickerListTop, 0f);
        _buildPickerScrollbar.Height.Set(-(JournalUiMetrics.BuildPickerListTop + JournalUiMetrics.BuildPickerListBottomInset), 1f);
        _buildPickerPanel.Append(_buildPickerScrollbar);
        _buildPickerList.SetScrollbar(_buildPickerScrollbar);
    }

    private void InitializeBuildSaveOverlay()
    {
        _buildSaveOverlay = new JournalDimOverlay(() => JournalSystem.CloseBuildSaveDialog());

        _buildSavePanel = JournalUiElementFactory.CreatePanel();
        _buildSavePanel.SetPadding(0f);
        _buildSavePanel.BackgroundColor = JournalUiTheme.RootBackground * JournalUiTheme.RootBackgroundOpacity;
        _buildSavePanel.BorderColor = JournalUiTheme.RootBorder;

        _buildSaveTitle = new UIText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveDialogTitle"), 0.58f, true)
        {
            HAlign = 0.5f,
            TextColor = JournalUiTheme.RootTitleText
        };
        _buildSaveTitle.Top.Set(14f, 0f);
        _buildSavePanel.Append(_buildSaveTitle);

        var inputBackground = JournalUiElementFactory.CreatePanel();
        inputBackground.Left.Set(24f, 0f);
        inputBackground.Top.Set(62f, 0f);
        inputBackground.Width.Set(-48f, 1f);
        inputBackground.Height.Set(42f, 0f);
        _buildSavePanel.Append(inputBackground);

        _buildSaveNameInput = new JournalTextInput(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveDialogHint"));
        _buildSaveNameInput.Left.Set(12f, 0f);
        _buildSaveNameInput.Top.Set(10f, 0f);
        _buildSaveNameInput.Width.Set(-24f, 1f);
        _buildSaveNameInput.Height.Set(20f, 0f);
        inputBackground.Append(_buildSaveNameInput);

        _buildSaveMessage = new UIText(string.Empty, 0.78f)
        {
            TextColor = new Color(224, 146, 146)
        };
        _buildSaveMessage.Left.Set(24f, 0f);
        _buildSaveMessage.Top.Set(112f, 0f);
        _buildSaveMessage.Width.Set(-48f, 1f);
        _buildSavePanel.Append(_buildSaveMessage);

        _buildSaveCancelButton = JournalUiElementFactory.CreateTextButton(
            Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveCancel"),
            0f,
            32f,
            () => JournalSystem.CloseBuildSaveDialog(),
            0.88f);
        _buildSaveCancelButton.Left.Set(24f, 0f);
        _buildSaveCancelButton.Top.Set(-50f, 1f);
        _buildSaveCancelButton.Width.Set(-18f, 0.5f);
        _buildSaveCancelButton.SetStyle(JournalUiTheme.GetDefaultTextButtonStyle());
        _buildSavePanel.Append(_buildSaveCancelButton);

        _buildSaveConfirmButton = JournalUiElementFactory.CreateTextButton(
            Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveConfirm"),
            0f,
            32f,
            SaveBuildFromDialog,
            0.88f);
        _buildSaveConfirmButton.Left.Set(6f, 0.5f);
        _buildSaveConfirmButton.Top.Set(-50f, 1f);
        _buildSaveConfirmButton.Width.Set(-30f, 0.5f);
        _buildSaveConfirmButton.SetStyle(JournalUiTheme.GetOverlayActionButtonStyle());
        _buildSavePanel.Append(_buildSaveConfirmButton);
    }

    private void InitializeContentTabs()
    {
        _contentTabsPanel = new UIElement();
        _root.AddDragTarget(_contentTabsPanel);
        _contentTabsPanel.Left.Set(JournalUiMetrics.HeaderTabsLeft, 0f);
        _contentTabsPanel.Top.Set(JournalUiMetrics.HeaderTabsTop, 0f);
        _contentTabsPanel.Width.Set(-(JournalUiMetrics.HeaderTabsLeft + JournalUiMetrics.HeaderTabsRightInset), 1f);
        _contentTabsPanel.Height.Set(JournalUiMetrics.TopTabsHeight, 0f);
        _root.Append(_contentTabsPanel);

        const float widthOffset = -2f * JournalUiMetrics.TopTabsGap / 3f;

        _classButton = JournalUiElementFactory.CreateTextButton(string.Empty, 0f, JournalUiMetrics.TopTabsButtonHeight, () => JournalSystem.ShowClassSelection(), 0.92f);
        _classButton.Left.Set(0f, 0f);
        _classButton.Top.Set(JournalUiMetrics.TopTabsButtonTop, 0f);
        _classButton.Width.Set(widthOffset, 1f / 3f);
        _contentTabsPanel.Append(_classButton);

        _overviewTabButton = JournalUiElementFactory.CreateTextButton(string.Empty, 0f, JournalUiMetrics.TopTabsButtonHeight, () => JournalSystem.ShowOverviewTab(), 0.92f);
        _overviewTabButton.Left.Set(JournalUiMetrics.TopTabsGap / 3f, 1f / 3f);
        _overviewTabButton.Top.Set(JournalUiMetrics.TopTabsButtonTop, 0f);
        _overviewTabButton.Width.Set(widthOffset, 1f / 3f);
        _contentTabsPanel.Append(_overviewTabButton);

        _presetsTabButton = JournalUiElementFactory.CreateTextButton(string.Empty, 0f, JournalUiMetrics.TopTabsButtonHeight, () => JournalSystem.ShowPresetsTab(), 0.92f);
        _presetsTabButton.Left.Set(JournalUiMetrics.TopTabsGap * 2f / 3f, 2f / 3f);
        _presetsTabButton.Top.Set(JournalUiMetrics.TopTabsButtonTop, 0f);
        _presetsTabButton.Width.Set(widthOffset, 1f / 3f);
        _contentTabsPanel.Append(_presetsTabButton);
    }

    private void UpdateStaticText(bool progressionModeEnabled)
    {
        _stagePanelTitle.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.StageSelectorTitle"));
        _progressionModeToggleButton.SetText(progressionModeEnabled ? "x" : "✓");
        _progressionModeToggleButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.ProgressionModeToggleTooltip"));
        _buildBuilderButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildBuilderTab"));
        _buildSaveButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveButton"));
        _buildSaveButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveTooltip"));
        _buildSaveTitle.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveDialogTitle"));
        _buildSaveNameInput.HintText = Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveDialogHint");
        _buildSaveCancelButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveCancel"));
        _buildSaveConfirmButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveConfirm"));
        _classButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.Class"));
        _overviewTabButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.OverviewTab"));
        _presetsTabButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.PresetsTab"));
    }

    private void UpdateNavigationStyles(bool selectingClass, bool showingPresets)
    {
        _closeButton.SetStyle(JournalUiTheme.GetHeaderButtonStyle(danger: true));
        _progressionModeToggleButton.SetStyle(JournalUiTheme.GetDefaultTextButtonStyle());
        _classButton.SetStyle(JournalUiTheme.GetTabButtonStyle(selectingClass));
        _overviewTabButton.SetStyle(JournalUiTheme.GetTabButtonStyle(!selectingClass && !showingPresets));
        _presetsTabButton.SetStyle(JournalUiTheme.GetTabButtonStyle(!selectingClass && showingPresets));
    }

    private void PopulateClassSelection(CombatClass selectedClass)
    {
        var top = 0f;
        var index = 0;

        foreach (var combatClass in JournalOrdering.ClassSelection)
        {
            var capturedClass = combatClass;
            var panel = new JournalClassButton(capturedClass, selectedClass == capturedClass, JournalUiMetrics.ClassSelectionButtonHeight);
            var isLeftColumn = index % 2 == 0;
            panel.Left.Set(isLeftColumn ? 0f : JournalUiMetrics.ClassSelectionButtonGap, isLeftColumn ? 0f : JournalUiMetrics.ClassSelectionButtonWidth);
            panel.Top.Set(top, 0f);
            panel.Width.Set(-6f, JournalUiMetrics.ClassSelectionButtonWidth);
            panel.OnLeftClick += (_, _) => JournalSystem.SelectClass(capturedClass);
            _classSelectionContainer.Append(panel);

            index++;
            if (index % 2 == 0)
            {
                top += JournalUiMetrics.ClassSelectionButtonHeight + JournalUiMetrics.ClassSelectionButtonGap;
            }
        }
    }

    private void SwitchContentMode(bool selectingClass)
    {
        if (selectingClass)
        {
            if (_entryList.Parent is not null)
            {
                _contentPanel.RemoveChild(_entryList);
            }

            if (_scrollbar.Parent is not null)
            {
                _contentPanel.RemoveChild(_scrollbar);
            }

            if (_classSelectionContainer.Parent is null)
            {
                _contentPanel.Append(_classSelectionContainer);
            }

            return;
        }

        if (_classSelectionContainer.Parent is not null)
        {
            _contentPanel.RemoveChild(_classSelectionContainer);
        }

        if (_entryList.Parent is null)
        {
            _contentPanel.Append(_entryList);
        }

        if (_scrollbar.Parent is null)
        {
            _contentPanel.Append(_scrollbar);
        }
    }

    private void ApplyNavigationLayout(bool hasSelectedClass)
    {
        if (hasSelectedClass)
        {
            if (_stagePanel.Parent is null)
            {
                _root.Append(_stagePanel);
                _layoutInitialized = false;
            }

            if (_contentTabsPanel.Parent is null)
            {
                _root.Append(_contentTabsPanel);
                _layoutInitialized = false;
            }

            _mainPanel.Left.Set(JournalUiMetrics.OuterPadding + JournalUiMetrics.StagePanelWidth + JournalUiMetrics.PanelGap, 0f);
            _mainPanel.Width.Set(-(JournalUiMetrics.OuterPadding * 2f + JournalUiMetrics.StagePanelWidth + JournalUiMetrics.PanelGap), 1f);
            _contentPanel.Top.Set(JournalUiMetrics.ContentInset, 0f);
            _contentPanel.Height.Set(-(JournalUiMetrics.ContentInset * 2f), 1f);
            return;
        }

        if (_stagePanel.Parent is not null)
        {
            _root.RemoveChild(_stagePanel);
            _layoutInitialized = false;
        }

        if (_contentTabsPanel.Parent is not null)
        {
            _root.RemoveChild(_contentTabsPanel);
            _layoutInitialized = false;
        }

        _mainPanel.Left.Set(JournalUiMetrics.OuterPadding, 0f);
        _mainPanel.Width.Set(-(JournalUiMetrics.OuterPadding * 2f), 1f);
        _contentPanel.Top.Set(JournalUiMetrics.ContentInset, 0f);
        _contentPanel.Height.Set(-(JournalUiMetrics.ContentInset * 2f), 1f);
    }

    private void ApplyContentLayout(bool selectingClass, bool showingPresets)
    {
        _classSelectionContainer.Top.Set(JournalUiMetrics.ContentBodyTop, 0f);
        _classSelectionContainer.Height.Set(-JournalUiMetrics.ContentBodyBottomInset, 1f);

        var showSourcePanel = !selectingClass && !showingPresets;
        if (showSourcePanel)
        {
            var contentWidth = MathF.Max(
                0f,
                _contentPanel.GetDimensions().Width - JournalUiMetrics.ContentBodyLeft - JournalUiMetrics.EntryListWidthInset);
            var sourceWidth = MathF.Min(JournalUiMetrics.AcquisitionPanelWidth, contentWidth * 0.38f);
            sourceWidth = MathF.Max(JournalUiMetrics.AcquisitionPanelMinWidth, sourceWidth);

            if (contentWidth - sourceWidth - JournalUiMetrics.ContentColumnGap < JournalUiMetrics.EntryListMinWidth)
            {
                sourceWidth = MathF.Max(
                    JournalUiMetrics.AcquisitionPanelMinWidth,
                    contentWidth - JournalUiMetrics.ContentColumnGap - JournalUiMetrics.EntryListMinWidth);
            }

            _sourcePanel.Width.Set(sourceWidth, 0f);
            _entryList.Width.Set(-(JournalUiMetrics.EntryListWidthInset + sourceWidth + JournalUiMetrics.ContentColumnGap), 1f);
            _scrollbar.Left.Set(-(sourceWidth
                + JournalUiMetrics.ContentColumnGap * 1.33f
                + JournalUiMetrics.ScrollbarWidth * 0.5f), 1f);
            _sourcePanel.Left.Set(-sourceWidth, 1f);

            if (_sourcePanel.Parent is null)
            {
                _contentPanel.Append(_sourcePanel);
            }
        }
        else
        {
            _entryList.Width.Set(-JournalUiMetrics.EntryListWidthInset, 1f);
            _scrollbar.Left.Set(-JournalUiMetrics.ScrollbarOffset, 1f);

            if (_sourcePanel.Parent is not null)
            {
                _contentPanel.RemoveChild(_sourcePanel);
            }
        }
    }

    private static Vector2 GetDefaultWindowPosition(float width, float height)
    {
        var topOffset = Main.screenHeight >= JournalUiMetrics.LargeScreenThreshold ? JournalUiMetrics.LargeScreenTopOffset : 0f;
        return new Vector2(
            (Main.screenWidth - width) * 0.5f,
            (Main.screenHeight - height) * 0.5f + topOffset);
    }

    private void SaveBuildFromDialog()
    {
        if (JournalSystem.TrySaveCurrentBuild(_buildSaveNameInput.CurrentString, out var errorMessage))
        {
            _buildSaveMessage.SetText(string.Empty);
            return;
        }

        _buildSaveMessage.SetText(errorMessage);
        _buildSaveNameInput.Focused = true;
    }

    private static JournalSystem JournalSystem => ModContent.GetInstance<JournalSystem>();
}
