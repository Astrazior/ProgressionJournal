using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using ProgressionJournal.Systems;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace ProgressionJournal.UI.States;

public sealed class JournalUiState : UIState
{
    private const string BestiarySearchCancelTexturePath = "Images/UI/SearchCancel";
    private static readonly JournalBuffCategory[] PersistentBuffCategories =
    [
        JournalBuffCategory.Station,
        JournalBuffCategory.Passive
    ];
    private static readonly JournalBuffCategory[] ConsumableBuffCategories =
    [
        JournalBuffCategory.Basic,
        JournalBuffCategory.Potion,
        JournalBuffCategory.Eternal,
        JournalBuffCategory.Food,
        JournalBuffCategory.Flask
    ];

    private readonly Dictionary<ProgressionStageId, JournalStageButton> _stageButtons = new();
    private UIPanel _root = null!;
    private UIPanel _stagePanel = null!;
    private UIPanel _mainPanel = null!;
    private UIElement _contentTabsPanel = null!;
    private UIPanel _contentPanel = null!;
    private UIText _title = null!;
    private UIText _stagePanelTitle = null!;
    private JournalIconButton _closeButton = null!;
    private JournalTextButton _classButton = null!;
    private JournalTextButton _overviewTabButton = null!;
    private JournalTextButton _presetsTabButton = null!;
    private UIText _contentTitle = null!;
    private UIText _contentDescription = null!;
    private UIElement _stageListContainer = null!;
    private UIElement _classSelectionContainer = null!;
    private UIList _entryList = null!;
    private UIScrollbar _scrollbar = null!;
    private JournalIconTextButton _combatBuffsButton = null!;
    private JournalCombatBuffPanel _combatBuffPanel = null!;
    private UIElement _combatBuffOverlay = null!;
    private UIPanel _combatBuffOverlayPanel = null!;
    private UIText _combatBuffOverlayTitle = null!;
    private JournalIconButton _combatBuffOverlayCloseButton = null!;
    private JournalCombatBuffPanel _combatBuffOverlayContent = null!;
    private bool _showingAllCombatBuffs;
    private bool _hasAvailableCombatBuffs;
    private bool _layoutInitialized;
    private int _layoutScreenWidth;
    private int _layoutScreenHeight;

    public override void OnInitialize()
    {
        _root = new UIPanel();
        _root.SetPadding(0f);
        _root.HAlign = 0.5f;
        _root.VAlign = 0.5f;
        _root.BackgroundColor = JournalUiTheme.RootBackground * JournalUiTheme.RootBackgroundOpacity;
        _root.BorderColor = JournalUiTheme.RootBorder;
        Append(_root);

        InitializeHeader();
        InitializeStagePanel();
        InitializeMainPanel();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (_root.ContainsPoint(Main.MouseScreen))
        {
            Main.LocalPlayer.mouseInterface = true;
            Main.blockMouse = true;
        }

        if (!Main.keyState.IsKeyDown(Keys.Escape) || !Main.oldKeyState.IsKeyUp(Keys.Escape)) return;
        if (_showingAllCombatBuffs)
        {
            HideCombatBuffOverlay();
            return;
        }

        JournalSystem.HideView();
    }

    public void Refresh(CombatClass combatClass, ProgressionStageId stageId, bool selectingClass, bool showingPresets, bool hasSelectedClass)
    {
        ApplyNavigationLayout(hasSelectedClass);
        ApplyContentLayout(selectingClass, showingPresets);
        EnsureLayout();
        UpdateStaticText();
        UpdateNavigationStyles(selectingClass, showingPresets);
        JournalStageButtonPresenter.Refresh(_stageButtons, stageId);
        RefreshContent(combatClass, stageId, selectingClass, showingPresets);
        Recalculate();
    }

    public void ResetLayout()
    {
        _layoutInitialized = false;
    }

    private void RefreshContent(CombatClass combatClass, ProgressionStageId stageId, bool selectingClass, bool showingPresets)
    {
        _entryList.Clear();
        _classSelectionContainer.RemoveAllChildren();
        SwitchContentMode(selectingClass);
        _hasAvailableCombatBuffs = false;
        _combatBuffPanel.SetEntries([]);
        _combatBuffOverlayContent.SetEntries([]);

        if (selectingClass)
        {
            HideCombatBuffOverlay();
            SetContentHeader(Language.GetTextValue("Mods.ProgressionJournal.UI.ClassPageTitle"));
            PopulateClassSelection(combatClass);
            return;
        }

        if (showingPresets)
        {
            HideCombatBuffOverlay();
            SetContentHeader(Language.GetTextValue("Mods.ProgressionJournal.UI.PresetsHeadline"));
            JournalContentBuilder.PopulateDevelopmentNotice(
                _entryList,
                Language.GetTextValue("Mods.ProgressionJournal.UI.InDevelopment"));
            return;
        }

        var className = Language.GetTextValue($"Mods.ProgressionJournal.Classes.{combatClass}");
        var stageName = Language.GetTextValue(ProgressionStageCatalog.Get(stageId).LocalizationKey);
        SetContentHeader($"{className} • {stageName}");
        JournalContentBuilder.PopulateEntries(_entryList, stageId, JournalRepository.GetEntries(stageId, combatClass));
        var persistentCombatBuffs = JournalRepository.GetPersistentCombatBuffEntries(stageId, combatClass);
        var consumableCombatBuffs = JournalRepository.GetConsumableCombatBuffEntries(stageId, combatClass);
        _hasAvailableCombatBuffs = consumableCombatBuffs.Count > 0;
        _combatBuffPanel.SetEntries(persistentCombatBuffs);
        PopulateCombatBuffOverlay(consumableCombatBuffs);

        if (!_hasAvailableCombatBuffs)
        {
            HideCombatBuffOverlay();
        }

        ApplyContentLayout(selectingClass, showingPresets);
    }

    private void SetContentHeader(string title)
    {
        _contentTitle.SetText(title);
        _contentDescription.SetText(string.Empty);
    }

    private void EnsureLayout()
    {
        if (_layoutInitialized && _layoutScreenWidth == Main.screenWidth && _layoutScreenHeight == Main.screenHeight)
        {
            return;
        }

        var width = MathF.Min(JournalUiMetrics.RootMaxWidth, Main.screenWidth - JournalUiMetrics.RootHorizontalMargin);
        var height = MathF.Min(JournalUiMetrics.RootMaxHeight, Main.screenHeight - JournalUiMetrics.RootVerticalMargin);
        var topOffset = Main.screenHeight >= JournalUiMetrics.LargeScreenThreshold ? JournalUiMetrics.LargeScreenTopOffset : 0f;

        _root.Left.Set(0f, 0f);
        _root.Top.Set(topOffset, 0f);
        _root.Width.Set(width, 0f);
        _root.Height.Set(height, 0f);
        _root.Recalculate();

        JournalStageButtonPresenter.Layout(_stageButtons, _stageListContainer);
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

        _title = new UIText(string.Empty, 0.82f, true)
        {
            HAlign = 0.5f,
            VAlign = 0f,
            TextColor = JournalUiTheme.RootTitleText
        };
        _title.Top.Set(JournalUiMetrics.HeaderTitleTop, 0f);
        _root.Append(_title);
    }

    private void InitializeStagePanel()
    {
        _stagePanel = JournalUiElementFactory.CreatePanel();
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

        _stageListContainer = new UIElement();
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
        _mainPanel.Left.Set(JournalUiMetrics.OuterPadding + JournalUiMetrics.StagePanelWidth + JournalUiMetrics.PanelGap, 0f);
        _mainPanel.Top.Set(JournalUiMetrics.HeaderHeight + JournalUiMetrics.OuterPadding, 0f);
        _mainPanel.Width.Set(-(JournalUiMetrics.OuterPadding * 2f + JournalUiMetrics.StagePanelWidth + JournalUiMetrics.PanelGap), 1f);
        _mainPanel.Height.Set(-(JournalUiMetrics.HeaderHeight + JournalUiMetrics.OuterPadding * 2f), 1f);
        _root.Append(_mainPanel);

        InitializeContentTabs();

        _contentPanel = JournalUiElementFactory.CreatePanel();
        _contentPanel.Left.Set(JournalUiMetrics.ContentInset, 0f);
        _contentPanel.Top.Set(JournalUiMetrics.ContentInset + JournalUiMetrics.TopTabsHeight + JournalUiMetrics.ContentPanelTopOffset, 0f);
        _contentPanel.Width.Set(-JournalUiMetrics.ContentInset * 2f, 1f);
        _contentPanel.Height.Set(-(JournalUiMetrics.TopTabsHeight + 34f), 1f);
        _mainPanel.Append(_contentPanel);

        _contentTitle = new UIText(string.Empty, JournalUiMetrics.ContentTitleScale, true)
        {
            HAlign = 0.5f
        };
        _contentTitle.Top.Set(JournalUiMetrics.ContentTitleTop, 0f);
        _contentPanel.Append(_contentTitle);

        _contentDescription = new UIText(string.Empty, JournalUiMetrics.ContentDescriptionScale);
        _contentDescription.Left.Set(JournalUiMetrics.ContentDescriptionLeft, 0f);
        _contentDescription.Top.Set(JournalUiMetrics.ContentDescriptionTop, 0f);
        _contentDescription.TextColor = JournalUiTheme.ContentDescriptionText;
        _contentPanel.Append(_contentDescription);

        _combatBuffPanel = new JournalCombatBuffPanel(PersistentBuffCategories, "Mods.ProgressionJournal.UI.CombatBuffsTitle");
        _combatBuffPanel.Top.Set(JournalUiMetrics.ContentBodyTop, 0f);
        _combatBuffPanel.Width.Set(JournalUiMetrics.CombatBuffPanelWidth, 0f);
        _combatBuffPanel.Height.Set(-JournalUiMetrics.ContentBodyBottomInset, 1f);
        _contentPanel.Append(_combatBuffPanel);

        _combatBuffsButton = JournalUiElementFactory.CreateIconTextButton(
            TextureAssets.Item[ItemID.HealingPotion],
            string.Empty,
            JournalUiMetrics.CombatBuffButtonWidth,
            JournalUiMetrics.CombatBuffButtonHeight,
            ToggleCombatBuffOverlay,
            0.90f);
        _combatBuffsButton.SetStyle(JournalUiTheme.GetOverlayActionButtonStyle());
        _contentPanel.Append(_combatBuffsButton);

        _classSelectionContainer = new UIElement();
        _classSelectionContainer.Left.Set(JournalUiMetrics.ContentBodyLeft, 0f);
        _classSelectionContainer.Top.Set(JournalUiMetrics.ContentBodyTop, 0f);
        _classSelectionContainer.Width.Set(-JournalUiMetrics.ContentBodyHorizontalInset, 1f);
        _classSelectionContainer.Height.Set(-JournalUiMetrics.ContentBodyBottomInset, 1f);
        _contentPanel.Append(_classSelectionContainer);

        _entryList = [];
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

        InitializeCombatBuffOverlay();
    }

    private void InitializeContentTabs()
    {
        _contentTabsPanel = new UIElement();
        _contentTabsPanel.Left.Set(JournalUiMetrics.ContentInset, 0f);
        _contentTabsPanel.Top.Set(JournalUiMetrics.ContentInset, 0f);
        _contentTabsPanel.Width.Set(-JournalUiMetrics.ContentInset * 2f, 1f);
        _contentTabsPanel.Height.Set(JournalUiMetrics.TopTabsHeight, 0f);
        _mainPanel.Append(_contentTabsPanel);

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

    private void UpdateStaticText()
    {
        _title.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.Title"));
        _stagePanelTitle.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.StageSelectorTitle"));
        _classButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.Class"));
        _overviewTabButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.OverviewTab"));
        _presetsTabButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.PresetsTab"));
        var combatConsumablesText = Language.GetTextValue("Mods.ProgressionJournal.UI.CombatConsumablesButton");
        _combatBuffsButton.SetText(combatConsumablesText);
    }

    private void UpdateNavigationStyles(bool selectingClass, bool showingPresets)
    {
        _closeButton.SetStyle(JournalUiTheme.GetHeaderButtonStyle(danger: true));
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
                _mainPanel.Append(_contentTabsPanel);
                _layoutInitialized = false;
            }

            _mainPanel.Left.Set(JournalUiMetrics.OuterPadding + JournalUiMetrics.StagePanelWidth + JournalUiMetrics.PanelGap, 0f);
            _mainPanel.Width.Set(-(JournalUiMetrics.OuterPadding * 2f + JournalUiMetrics.StagePanelWidth + JournalUiMetrics.PanelGap), 1f);
            _contentPanel.Top.Set(JournalUiMetrics.ContentInset + JournalUiMetrics.TopTabsHeight + JournalUiMetrics.ContentPanelTopOffset, 0f);
            _contentPanel.Height.Set(-(JournalUiMetrics.TopTabsHeight + 34f), 1f);
            return;
        }

        if (_stagePanel.Parent is not null)
        {
            _root.RemoveChild(_stagePanel);
            _layoutInitialized = false;
        }

        if (_contentTabsPanel.Parent is not null)
        {
            _mainPanel.RemoveChild(_contentTabsPanel);
            _layoutInitialized = false;
        }

        _mainPanel.Left.Set(JournalUiMetrics.OuterPadding, 0f);
        _mainPanel.Width.Set(-(JournalUiMetrics.OuterPadding * 2f), 1f);
        _contentPanel.Top.Set(JournalUiMetrics.ContentInset, 0f);
        _contentPanel.Height.Set(-24f, 1f);
    }

    private void ApplyContentLayout(bool selectingClass, bool showingPresets)
    {
        _classSelectionContainer.Top.Set(JournalUiMetrics.ContentBodyTop, 0f);
        _classSelectionContainer.Height.Set(-JournalUiMetrics.ContentBodyBottomInset, 1f);

        var showCombatBuffs = !selectingClass && !showingPresets && _combatBuffPanel.HasEntries;
        if (showCombatBuffs)
        {
            _entryList.Width.Set(-(JournalUiMetrics.EntryListWidthInset + JournalUiMetrics.CombatBuffPanelWidth + JournalUiMetrics.ContentColumnGap + JournalUiMetrics.CombatBuffPanelRightInset), 1f);
            _scrollbar.Left.Set(-(JournalUiMetrics.CombatBuffPanelWidth
                + JournalUiMetrics.CombatBuffPanelRightInset
                + JournalUiMetrics.EntryListWidthInset * 0.5f
                + JournalUiMetrics.ScrollbarWidth * 0.5f), 1f);
            _combatBuffPanel.Left.Set(-(JournalUiMetrics.CombatBuffPanelWidth + JournalUiMetrics.CombatBuffPanelRightInset), 1f);

            if (_combatBuffPanel.Parent is null)
            {
                _contentPanel.Append(_combatBuffPanel);
            }
        }
        else
        {
            _entryList.Width.Set(-JournalUiMetrics.EntryListWidthInset, 1f);
            _scrollbar.Left.Set(-JournalUiMetrics.ScrollbarOffset, 1f);

            if (_combatBuffPanel.Parent is not null)
            {
                _contentPanel.RemoveChild(_combatBuffPanel);
            }
        }

        var showCombatBuffButton = showCombatBuffs && _hasAvailableCombatBuffs;
        _combatBuffsButton.Left.Set(
            -(JournalUiMetrics.CombatBuffPanelRightInset
                + JournalUiMetrics.CombatBuffPanelWidth * 0.5f
                + JournalUiMetrics.CombatBuffButtonWidth * 0.5f),
            1f);
        _combatBuffsButton.Top.Set(
            -(JournalUiMetrics.ContentBodyBottomInset
                + JournalUiMetrics.CombatBuffButtonHeight
                + JournalUiMetrics.CombatBuffButtonBottomInset),
            1f);

        if (showCombatBuffButton)
        {
            if (_combatBuffsButton.Parent is not null)
            {
                _contentPanel.RemoveChild(_combatBuffsButton);
            }

            _contentPanel.Append(_combatBuffsButton);
        }
        else if (_combatBuffsButton.Parent is not null)
        {
            _contentPanel.RemoveChild(_combatBuffsButton);
        }
    }

    private void InitializeCombatBuffOverlay()
    {
        _combatBuffOverlay = new UIElement();
        _combatBuffOverlay.Width.Set(0f, 1f);
        _combatBuffOverlay.Height.Set(0f, 1f);

        _combatBuffOverlay.Append(new JournalDimOverlay(HideCombatBuffOverlay));

        _combatBuffOverlayPanel = JournalUiElementFactory.CreatePanel();
        _combatBuffOverlayPanel.Width.Set(JournalUiMetrics.CombatBuffOverlayWidth, 0f);
        _combatBuffOverlayPanel.Height.Set(JournalUiMetrics.CombatBuffOverlayHeight, 0f);
        _combatBuffOverlayPanel.HAlign = 0.5f;
        _combatBuffOverlayPanel.VAlign = 0.5f;
        _combatBuffOverlay.Append(_combatBuffOverlayPanel);

        _combatBuffOverlayTitle = new UIText(string.Empty, 0.5f, true)
        {
            HAlign = 0.5f,
            TextColor = JournalUiTheme.SectionHeaderText
        };
        _combatBuffOverlayTitle.Top.Set(JournalUiMetrics.CombatBuffOverlayTitleTop, 0f);
        _combatBuffOverlayPanel.Append(_combatBuffOverlayTitle);

        _combatBuffOverlayCloseButton = JournalUiElementFactory.CreateIconButton(
            BestiarySearchCancelTexturePath,
            JournalUiMetrics.CombatBuffOverlayCloseSize,
            JournalUiMetrics.CombatBuffOverlayCloseSize,
            HideCombatBuffOverlay,
            0.82f);
        _combatBuffOverlayCloseButton.Left.Set(
            -(JournalUiMetrics.CombatBuffOverlayCloseSize + JournalUiMetrics.CombatBuffOverlayCloseInset),
            1f);
        _combatBuffOverlayCloseButton.Top.Set(JournalUiMetrics.CombatBuffOverlayCloseTop, 0f);
        _combatBuffOverlayPanel.Append(_combatBuffOverlayCloseButton);

        _combatBuffOverlayContent = new JournalCombatBuffPanel(
            ConsumableBuffCategories,
            "Mods.ProgressionJournal.UI.CombatConsumablesTitle",
            showTitle: false,
            autoHeight: false,
            useConsumableOverlayLayout: true);
        _combatBuffOverlayContent.Left.Set(JournalUiMetrics.CombatBuffOverlayInset, 0f);
        _combatBuffOverlayContent.Top.Set(JournalUiMetrics.CombatBuffOverlayContentTop, 0f);
        _combatBuffOverlayContent.Width.Set(-(JournalUiMetrics.CombatBuffOverlayInset * 2f), 1f);
        _combatBuffOverlayContent.Height.Set(-(JournalUiMetrics.CombatBuffOverlayContentTop + JournalUiMetrics.CombatBuffOverlayContentBottomInset), 1f);
        _combatBuffOverlayPanel.Append(_combatBuffOverlayContent);
    }

    private void PopulateCombatBuffOverlay(IReadOnlyList<JournalCombatBuffEntry> entries)
    {
        _combatBuffOverlayContent.SetEntries(entries);

        if (entries.Count == 0)
        {
            return;
        }

        var targetHeight = JournalUiMetrics.CombatBuffOverlayContentTop
            + _combatBuffOverlayContent.ContentHeight
            + JournalUiMetrics.CombatBuffOverlayContentBottomInset;
        _combatBuffOverlayPanel.Height.Set(
            MathF.Min(JournalUiMetrics.CombatBuffOverlayHeight, targetHeight),
            0f);

        _combatBuffOverlayTitle.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.CombatConsumablesTitle"));
    }

    private void ToggleCombatBuffOverlay()
    {
        if (_showingAllCombatBuffs)
        {
            HideCombatBuffOverlay();
            return;
        }

        ShowCombatBuffOverlay();
    }

    private void ShowCombatBuffOverlay()
    {
        if (!_hasAvailableCombatBuffs)
        {
            return;
        }

        if (_combatBuffOverlay.Parent is null)
        {
            _root.Append(_combatBuffOverlay);
        }

        _showingAllCombatBuffs = true;
    }

    private void HideCombatBuffOverlay()
    {
        _showingAllCombatBuffs = false;

        if (_combatBuffOverlay.Parent is not null)
        {
            _root.RemoveChild(_combatBuffOverlay);
        }
    }

    private static JournalSystem JournalSystem => ModContent.GetInstance<JournalSystem>();
}
