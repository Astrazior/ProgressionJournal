using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProgressionJournal.Systems;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using static ProgressionJournal.Data.Repositories.JournalRepository;

namespace ProgressionJournal.UI.States;

public sealed class JournalUiState(JournalSystem journalSystem) : UIState
{
    private const string BestiarySearchCancelTexturePath = "Images/UI/SearchCancel";
    private const string BestiaryBackButtonTexturePath = "Images/UI/Bestiary/Button_Back";
    private const string CraftingWindowToggleTexturePath = "Images/UI/Craft_Toggle_0";
    private const string BuildPickerFilterIconTexturePath = "Images/UI/Bestiary/Button_Filtering";
    private const string BuildPickerSortIconTexturePath = "Images/UI/Bestiary/Button_Sorting";
    private const string BuildPickerSortDescendingIconTexturePath = "Images/UI/Sort_1";
    private const string BuildPickerSortAscendingIconTexturePath = "Images/UI/Sort_0";
    private const string BuildPickerMagicIconTexturePath = "Images/Mana";
    private const int BuildPickerVanillaIconItemId = ItemID.Book;
    private const int BuildPickerModsIconItemId = ItemID.Wrench;
    private const int BuildPickerMeleeIconItemId = ItemID.WoodenSword;
    private const int BuildPickerMenuMaxColumns = 6;
    private const float BuildPickerModeToggleWidth = 66f;
    private const float BuildPickerMenuPadding = 5f;
    private const float BuildPickerMenuButtonSize = 40f;
    private const float BuildPickerMenuGap = 5f;
    private static readonly Rectangle BuildPickerBestiaryButtonIconSourceRectangle = new(4, 4, 22, 22);

    private readonly Dictionary<string, JournalStageButton> _stageButtons =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, CachedAcquisitionView> _acquisitionViewCache = new();
    private JournalDraggablePanel _root = null!;
    private UIPanel _stagePanel = null!;
    private UIPanel _mainPanel = null!;
    private UIElement _contentTabsPanel = null!;
    private UIElement _contentPanel = null!;
    private UIText _stagePanelTitle = null!;
    private JournalProgressionModeToggle _progressionModeToggleButton = null!;
    private JournalIconTextButton _profileButton = null!;
    private JournalIconButton _closeButton = null!;
    private JournalTextButton _classButton = null!;
    private JournalTextButton _overviewTabButton = null!;
    private JournalTextButton _presetsTabButton = null!;
    private UIText _contentTitle = null!;
    private UIText _contentDescription = null!;
    private JournalIconButton _buildImportButton = null!;
    private JournalIconButton _buildBuilderButton = null!;
    private JournalIconButton _buildBackButton = null!;
    private JournalIconButton _buildSaveButton = null!;
    private JournalSmoothScrollGrid _stageListContainer = null!;
    private UIScrollbar _stageScrollbar = null!;
    private UIList _classSelectionContainer = null!;
    private UIScrollbar _classSelectionScrollbar = null!;
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
    private JournalBuildFilterIconButton _buildPickerFilterButton = null!;
    private JournalBuildItemModeToggle _buildPickerAllItemsButton = null!;
    private JournalBuildFilterIconButton _buildPickerSortButton = null!;
    private UIPanel _buildPickerFilterMenuPanel = null!;
    private UIPanel _buildPickerSortMenuPanel = null!;
    private UIPanel _buildPickerSearchBackground = null!;
    private JournalTextInput _buildPickerSearchInput = null!;
    private UIList _buildPickerList = null!;
    private UIScrollbar _buildPickerScrollbar = null!;
    private JournalDimOverlay _buildSaveOverlay = null!;
    private UIPanel _buildSavePanel = null!;
    private UIText _buildSaveTitle = null!;
    private JournalTextInput _buildSaveNameInput = null!;
    private UIText _buildSaveMessage = null!;
    private JournalTextButton _buildSaveConfirmButton = null!;
    private JournalTextButton _buildSaveCancelButton = null!;
    private JournalDimOverlay _buildExportOverlay = null!;
    private UIPanel _buildExportPanel = null!;
    private JournalIconButton _buildExportFileButton = null!;
    private JournalIconButton _buildExportChatButton = null!;
    private JournalIconButton _buildExportCloseButton = null!;
    private JournalDimOverlay _sharedBuildOverlay = null!;
    private UIPanel _sharedBuildPanel = null!;
    private UIText _sharedBuildTitle = null!;
    private UIText _sharedBuildMeta = null!;
    private JournalIconButton _sharedBuildCloseIconButton = null!;
    private UIList _sharedBuildList = null!;
    private UIScrollbar _sharedBuildScrollbar = null!;
    private JournalTextButton _sharedBuildAddButton = null!;
    private JournalTextButton _sharedBuildCloseButton = null!;
    private JournalDimOverlay _profileManagerOverlay = null!;
    private JournalProfileManagerPanel _profileManagerPanel = null!;
    private bool _layoutInitialized;
    private int _layoutScreenWidth;
    private int _layoutScreenHeight;
    private bool _windowPositionInitialized;
    private BuildPickerTab _activeBuildPickerTab = BuildPickerTab.Vanilla;
    private BuildPickerPowerSort _buildPickerPowerSort = BuildPickerPowerSort.None;
    private BuildPickerDamageFilter _buildPickerDamageFilter = BuildPickerDamageFilter.None;
    private string? _activeBuildPickerSlotKey;
    private string? _selectedBuildPickerModName;
    private string _appliedBuildPickerSearchText = string.Empty;
    private bool _buildPickerFilterMenuOpen;
    private bool _buildPickerSortMenuOpen;
    private string _renderedProfileId = string.Empty;
    private int _renderedAcquisitionItemId = ItemID.None;

    private sealed record CachedAcquisitionView(UIElement PreviewElement, IReadOnlyList<UIElement> Entries);

    private enum BuildPickerTab
    {
        Vanilla,
        Mods
    }

    private enum BuildPickerPowerSort
    {
        None,
        Descending,
        Ascending
    }

    private enum BuildPickerDamageFilter
    {
        None,
        Melee,
        Ranged,
        Magic,
        Summon,
        Other
    }

    public override void OnInitialize()
    {
        _root = new JournalDraggablePanel();
        _root.SetPadding(0f);
        _root.BackgroundColor = JournalUiTheme.RootBackground * JournalUiTheme.RootBackgroundOpacity;
        _root.BorderColor = JournalUiTheme.RootBorder;
        _root.PositionChanged += PositionProfileButton;
        Append(_root);

        InitializeHeader();
        InitializeStagePanel();
        InitializeMainPanel();
        InitializeBuildPickerOverlay();
        InitializeBuildSaveOverlay();
        InitializeBuildExportOverlay();
        InitializeSharedBuildOverlay();
        InitializeProfileManagerOverlay();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (_root.ContainsPoint(Main.MouseScreen)
            || (_profileButton.Parent is not null && _profileButton.ContainsPoint(Main.MouseScreen)))
        {
            Main.LocalPlayer.mouseInterface = true;
            Main.blockMouse = true;
        }

        var currentBuildPickerSearchText = _buildPickerSearchInput.CurrentString;
        if (JournalSystem.ActiveBuildSlotKey is not null
            && !string.Equals(currentBuildPickerSearchText, _appliedBuildPickerSearchText, StringComparison.CurrentCulture))
        {
            JournalSystem.RefreshView();
        }

        if (!Main.keyState.IsKeyDown(Keys.Escape) || !Main.oldKeyState.IsKeyUp(Keys.Escape))
        {
            return;
        }

        if (JournalSystem.ShowingSharedBuildPreview)
        {
            JournalSystem.CloseSharedBuildPreview();
            return;
        }

        if (JournalSystem.ShowingProfileManager)
        {
            JournalSystem.CloseProfileManager();
            return;
        }

        if (JournalSystem.ShowingBuildExportDialog)
        {
            JournalSystem.CloseBuildExportDialog();
            return;
        }

        if (JournalSystem.ShowingBuildSaveDialog)
        {
            JournalSystem.CloseBuildSaveDialog();
            return;
        }

        if (JournalSystem.ActiveBuildSlotKey is not null)
        {
            if (_buildPickerFilterMenuOpen || _buildPickerSortMenuOpen)
            {
                HideBuildPickerMenus();
                JournalSystem.RefreshView();
                return;
            }

            JournalSystem.CloseBuildSlotPicker();
            return;
        }

        JournalSystem.HideView();
    }

    public void Refresh(
        string profileId,
        string classId,
        string stageId,
        bool selectingClass,
        bool showingPresets,
        bool showingBuildBuilder,
        bool progressionModeEnabled,
        bool hasSelectedClass,
        int selectedItemId)
    {
        var profile = JournalProfileRegistry.TryGet(profileId, out var registeredProfile)
            ? registeredProfile
            : JournalProfileRegistry.Active;
        if (!string.Equals(_renderedProfileId, profile.Id, StringComparison.OrdinalIgnoreCase))
        {
            _acquisitionViewCache.Clear();
        }

        EnsureProfileNavigation(profile, stageId);
        ApplyNavigationLayout(hasSelectedClass);
        EnsureLayout();
        ApplyContentLayout(selectingClass, showingPresets);
        RefreshBuildActionButtons(selectingClass, showingPresets, showingBuildBuilder);
        UpdateStaticText(progressionModeEnabled);
        UpdateNavigationStyles(selectingClass, showingPresets);
        JournalStageButtonPresenter.Refresh(profile, _stageButtons, stageId, progressionModeEnabled);
        RefreshContent(profile, classId, stageId, selectingClass, showingPresets, showingBuildBuilder, selectedItemId);
        RefreshBuildPickerOverlay(profile.Id, classId, stageId, showingPresets, showingBuildBuilder);
        RefreshBuildSaveOverlay(showingPresets, showingBuildBuilder);
        RefreshBuildExportOverlay(showingPresets, showingBuildBuilder);
        RefreshSharedBuildPreviewOverlay();
        RefreshProfileManagerOverlay();
        RefreshProfileButtonVisibility();
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
        HideBuildExportOverlay();
        HideSharedBuildPreviewOverlay();
        HideProfileManagerOverlay();
    }

    public void ResetProfileNavigation()
    {
        _renderedProfileId = string.Empty;
        _layoutInitialized = false;
        _acquisitionViewCache.Clear();
    }

    private void RefreshContent(
        JournalProfile profile,
        string classId,
        string stageId,
        bool selectingClass,
        bool showingPresets,
        bool showingBuildBuilder,
        int selectedItemId)
    {
        _entryList.Clear();
        _classSelectionContainer.Clear();
        _sourceList.Clear();
        _sourcePreviewContainer.RemoveAllChildren();
        SwitchContentMode(selectingClass);

        if (selectingClass)
        {
            SetContentHeader(Language.GetTextValue("Mods.ProgressionJournal.UI.ClassPageTitle"));
            PopulateClassSelection(profile, classId);
            ClearAcquisitionPanel();
            return;
        }

        if (showingPresets)
        {
            var presetClassName = JournalProfileText.GetClassName(profile, classId);
            var presetStageName = JournalProfileText.GetStageName(profile, stageId);
            SetContentHeader($"{presetClassName} • {presetStageName}");
            var savedBuilds = JournalSystem.GetSavedBuilds(profile.Id, stageId, classId);
            SetContentDescription(Language.GetTextValue("Mods.ProgressionJournal.UI.PresetsCount", savedBuilds.Count), 0.76f);

            if (showingBuildBuilder)
            {
                JournalContentBuilder.PopulateBuildPlanner(
                    _entryList,
                    profile.Id,
                    stageId,
                    classId,
                    JournalSystem.GetSelectedBuildItem,
                    JournalSystem.OpenBuildSlot);
            }
            else if (savedBuilds.Count > 0)
            {
                JournalContentBuilder.PopulateSavedBuilds(_entryList, profile.Id, stageId, classId, savedBuilds);
            }
            else
            {
                _entryList.Add(CreateSourceNotice(Language.GetTextValue("Mods.ProgressionJournal.UI.SavedBuildsEmpty")));
            }

            ClearAcquisitionPanel();
            return;
        }

        var className = JournalProfileText.GetClassName(profile, classId);
        var stageName = JournalProfileText.GetStageName(profile, stageId);
        SetContentHeader($"{className} • {stageName}");
        JournalContentBuilder.PopulateEntries(
            _entryList,
            profile.Id,
            stageId,
            GetEntries(profile.Id, stageId, classId),
            JournalSystem.SelectItem);
        JournalContentBuilder.PopulateCombatBuffs(
            _entryList,
            profile.Id,
            GetCombatBuffEntries(profile.Id, stageId, classId),
            JournalSystem.SelectItem);

        RefreshAcquisitionPanel(selectedItemId);
    }

    private void RefreshAcquisitionPanel(int selectedItemId)
    {
        _sourceClearButton.SetStyle(JournalUiTheme.GetHeaderButtonStyle(danger: true));
        _sourceClearButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemClearTooltip"));
        if (_renderedAcquisitionItemId != selectedItemId)
        {
            _sourceList.ViewPosition = 0f;
            _renderedAcquisitionItemId = selectedItemId;
        }

        _sourcePreviewContainer.RemoveAllChildren();
        _sourceList.Clear();

        if (selectedItemId <= ItemID.None || !JournalItemUtilities.TryCreateItem(selectedItemId, out var selectedItem))
        {
            ClearAcquisitionPanel();
            return;
        }

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

        if (info.ShimmerSources.Count > 0)
        {
            builtEntries.Add(CreateSourceSectionHeader("Mods.ProgressionJournal.UI.SelectedItemShimmer"));
            builtEntries.AddRange(info.ShimmerSources.Select(CreateShimmerSourceCard));
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

        if (info.FishingSources.Count > 0)
        {
            builtEntries.Add(CreateSourceSectionHeader("Mods.ProgressionJournal.UI.SelectedItemFishing"));
            builtEntries.AddRange(info.FishingSources.Select(CreateFishingSourceCard));
        }

        AddEntriesToSourceList(builtEntries);

        _acquisitionViewCache[selectedItemId] = new CachedAcquisitionView(previewStrip, builtEntries);
    }

    private void ClearAcquisitionPanel()
    {
        _renderedAcquisitionItemId = ItemID.None;
        _sourcePreviewContainer.RemoveAllChildren();
        _sourceItemName.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemEmpty"));
        _sourceList.Clear();
        _sourceList.Add(CreateSourceNotice(Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemSelectPrompt")));
    }

    private static UIElement CreateSourceSectionHeader(string localizationKey)
    {
        var (iconItemId, accent) = localizationKey switch
        {
            "Mods.ProgressionJournal.UI.SelectedItemCrafts" => (ItemID.WorkBench, new Color(220, 174, 92)),
            "Mods.ProgressionJournal.UI.SelectedItemShimmer" => (ItemID.ShimmerBlock, new Color(166, 126, 224)),
            "Mods.ProgressionJournal.UI.SelectedItemDrops" => (ItemID.Gel, new Color(205, 116, 118)),
            "Mods.ProgressionJournal.UI.SelectedItemShops" => (ItemID.GoldCoin, new Color(232, 198, 92)),
            "Mods.ProgressionJournal.UI.SelectedItemFishing" => (ItemID.GoldenFishingRod, new Color(92, 176, 218)),
            _ => (ItemID.Book, JournalUiTheme.PanelBorder)
        };
        var header = new JournalSourceSectionHeader(
            Language.GetTextValue(localizationKey),
            iconItemId,
            accent);
        header.Width.Set(0f, 1f);
        return header;
    }

    private UIPanel CreateRecipeSourceCard(JournalRecipeSource recipe)
    {
        var panel = CreateSourceCard(new Color(220, 174, 92));
        panel.Width.Set(0f, 1f);

        var top = JournalUiMetrics.BlockVerticalPadding;
        top = AppendSourceGroupLabel(
            panel,
            "Mods.ProgressionJournal.UI.SelectedItemIngredients",
            top);
        top = AppendItemRows(panel, recipe.Ingredients, top);

        if (recipe.Stations.Count > 0)
        {
            top = AppendSourceGroupLabel(
                panel,
                "Mods.ProgressionJournal.UI.SelectedItemStations",
                top + 4f);
            top = AppendTokenRows(
                panel,
                recipe.Stations
                    .Select(static station => new JournalSourceTokenData(
                        JournalSourceTokenKind.Tile,
                        station.TileId,
                        station.Name))
                    .ToArray(),
                top);
        }

        if (recipe.Conditions.Count > 0)
        {
            top = AppendConditionContent(panel, recipe.Conditions, top + 6f);
        }

        panel.Height.Set(top + JournalUiMetrics.BlockVerticalPadding, 0f);
        return panel;
    }

    private UIPanel CreateShimmerSourceCard(JournalShimmerSource shimmerSource)
    {
        var panel = CreateSourceCard(new Color(166, 126, 224));
        panel.Width.Set(0f, 1f);

        var top = JournalUiMetrics.BlockVerticalPadding;
        top = AppendCenteredTextLines(
            panel,
            [Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemShimmerMethod")],
            top);
        top = AppendItemRows(panel, shimmerSource.InputItems, top + 6f);

        panel.Height.Set(top + JournalUiMetrics.BlockVerticalPadding, 0f);
        return panel;
    }

    private JournalSourceCard CreateDropSourceCard(JournalDropSource drop)
    {
        var panel = CreateSourceCard(new Color(205, 116, 118));
        panel.Width.Set(0f, 1f);

        var top = JournalUiMetrics.BlockVerticalPadding;
        var sourceLabelKey = drop.SourceItemId.HasValue
            ? "Mods.ProgressionJournal.UI.SelectedItemFromItem"
            : "Mods.ProgressionJournal.UI.SelectedItemSource";
        if (JournalAcquisitionVisuals.TryCreateSourceToken(drop, out var sourceToken))
        {
            top = AppendCenteredTokenRows(panel, [sourceToken], top);

            if (drop.SourceNpcType is { } sourceNpcType)
            {
                var npcLocationTokens = JournalAcquisitionVisuals.GetNpcLocationTokens(sourceNpcType);
                if (npcLocationTokens.Count > 0)
                {
                    top = AppendCenteredTokenRows(panel, npcLocationTokens, top + 6f);
                }
            }
        }
        else
        {
            top = AppendCenteredTextLines(panel, [$"{Language.GetTextValue(sourceLabelKey)}: {drop.SourceName}"], top);
        }

        var lines = new List<string>
        {
            $"{Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemChance")}: {FormatDropRate(drop.DropRate)}"
        };

        if (drop.StackMax > 1 || drop.StackMin > 1)
        {
            lines.Add($"{Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemStack")}: {FormatStackRange(drop.StackMin, drop.StackMax)}");
        }

        top = AppendCenteredTextLines(panel, lines, top + 8f);

        if (drop.Conditions.Count > 0)
        {
            top = AppendConditionContent(panel, drop.Conditions, top + 6f);
        }

        panel.Height.Set(top + JournalUiMetrics.BlockVerticalPadding, 0f);
        return panel;
    }

    private JournalSourceCard CreateAggregatedNpcDropSourceCard(IReadOnlyList<JournalDropSource> drops)
    {
        var panel = CreateSourceCard(new Color(205, 116, 118));
        panel.Width.Set(0f, 1f);

        var top = JournalUiMetrics.BlockVerticalPadding;
        var isGlobalDrop = IsGlobalNpcDropGroup(drops);
        if (isGlobalDrop)
        {
            top = AppendCenteredTextLines(
                panel,
                [$"{Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemSource")}: {Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemFromAnyEnemy")}"],
                top);
        }
        else
        {
            top = AppendCenteredTextLines(
                panel,
                [Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemSource")],
                top);
            top = AppendCenteredTokenRows(
                panel,
                drops
                    .Select(static drop => new JournalSourceTokenData(
                        JournalSourceTokenKind.Npc,
                        drop.SourceNpcType!.Value,
                        drop.SourceName))
                    .Distinct()
                    .ToArray(),
                top + 6f);
        }

        var npcTypes = drops
            .Select(static drop => drop.SourceNpcType)
            .Where(static npcType => npcType.HasValue)
            .Select(static npcType => npcType!.Value)
            .Distinct()
            .ToArray();
        var commonLocationTokens = JournalAcquisitionVisuals.GetCommonNpcLocationTokens(npcTypes);
        if (commonLocationTokens.Count > 0)
        {
            top = AppendCenteredTokenRows(panel, commonLocationTokens, top + 6f);
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

        top = AppendCenteredTextLines(panel, lines, top + 8f);

        if (primaryDrop.Conditions.Count > 0)
        {
            top = AppendConditionContent(panel, primaryDrop.Conditions, top + 6f);
        }

        panel.Height.Set(top + JournalUiMetrics.BlockVerticalPadding, 0f);
        return panel;
    }

    private UIPanel CreateAggregatedBossDropSourceCard(IReadOnlyList<JournalDropSource> drops)
    {
        var panel = CreateSourceCard(new Color(205, 116, 118));
        panel.Width.Set(0f, 1f);

        var top = JournalUiMetrics.BlockVerticalPadding;
        top = AppendCenteredTextLines(
            panel,
            [$"{Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemSource")}: {Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemFromAnyBoss")}"],
            top);

        var primaryDrop = drops[0];
        var lines = new List<string>
        {
            $"{Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemChance")}: {FormatDropRate(primaryDrop.DropRate)}"
        };

        if (primaryDrop.StackMax > 1 || primaryDrop.StackMin > 1)
        {
            lines.Add($"{Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemStack")}: {FormatStackRange(primaryDrop.StackMin, primaryDrop.StackMax)}");
        }

        top = AppendCenteredTextLines(panel, lines, top + 8f);

        if (primaryDrop.Conditions.Count > 0)
        {
            top = AppendConditionContent(panel, primaryDrop.Conditions, top + 6f);
        }

        panel.Height.Set(top + JournalUiMetrics.BlockVerticalPadding, 0f);
        return panel;
    }

    private UIPanel CreateShopSourceCard(JournalShopSource shop)
    {
        var panel = CreateSourceCard(new Color(232, 198, 92));
        panel.Width.Set(0f, 1f);

        var top = JournalUiMetrics.BlockVerticalPadding;
        top = AppendCenteredTokenRows(panel, [JournalAcquisitionVisuals.CreateSourceToken(shop)], top);

        if (shop.Conditions.Count > 0)
        {
            top = AppendConditionContent(panel, shop.Conditions, top + 6f);
        }

        panel.Height.Set(top + JournalUiMetrics.BlockVerticalPadding, 0f);
        return panel;
    }

    private UIPanel CreateFishingSourceCard(JournalFishingSource fishingSource)
    {
        var panel = CreateSourceCard(new Color(92, 176, 218));
        panel.Width.Set(0f, 1f);

        var top = JournalUiMetrics.BlockVerticalPadding;
        top = AppendCenteredTextLines(
            panel,
            [Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemFishingMethod")],
            top);

        if (fishingSource.Conditions.Count > 0)
        {
            top = AppendConditionContent(panel, fishingSource.Conditions, top + 6f);
        }

        panel.Height.Set(top + JournalUiMetrics.BlockVerticalPadding, 0f);
        return panel;
    }

    private static JournalSourceCard CreateSourceCard(Color accent)
    {
        return new JournalSourceCard(accent);
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

    private static float AppendSourceGroupLabel(
        UIElement parent,
        string localizationKey,
        float top)
    {
        var label = new UIText(
            Language.GetTextValue(localizationKey),
            JournalUiMetrics.AcquisitionPanelTextScale)
        {
            TextColor = JournalUiTheme.ContentDescriptionText
        };
        label.Left.Set(JournalUiMetrics.BlockHorizontalPadding, 0f);
        label.Top.Set(top, 0f);
        parent.Append(label);
        return top + JournalUiMetrics.AcquisitionPanelTextLineHeight;
    }

    private float AppendCenteredTextLines(UIElement parent, IEnumerable<string> lines, float top)
    {
        var maxWidth = GetSourceTextMaxWidth();

        foreach (var line in lines.Where(static line => !string.IsNullOrWhiteSpace(line)))
        {
            foreach (var wrappedLine in JournalTextUtilities.WrapToPixelWidth(
                         line,
                         maxWidth,
                         JournalUiMetrics.AcquisitionPanelTextScale))
            {
                var text = new UIText(wrappedLine, JournalUiMetrics.AcquisitionPanelTextScale)
                {
                    HAlign = 0.5f,
                    TextColor = JournalUiTheme.ContentDescriptionText
                };
                text.Top.Set(top, 0f);
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
            var strip = new JournalItemStrip(rowItems, JournalSystem.SelectItem);
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
            .Select(UIElement (group) =>
            {
                if (group.Count <= 1)
                {
                    return CreateDropSourceCard(group[0]);
                }

                if (group.All(static drop => drop is { SourceNpcType: not null, SourceItemId: null }))
                {
                    return CreateAggregatedNpcDropSourceCard(group);
                }

                return CreateAggregatedBossDropSourceCard(group);
            });
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
            .SelectMany(static group =>
            {
                var groupedDrops = group.ToArray();
                var isBossBagGroup = groupedDrops.All(static drop =>
                    drop.SourceItemId is { } itemId and >= 0
                    && itemId < ItemID.Sets.BossBag.Length
                    && ItemID.Sets.BossBag[itemId]);
                if ((group.Key.IsNpcSource && groupedDrops.Length >= 2)
                    || (isBossBagGroup && groupedDrops.Length >= 4))
                {
                    return [groupedDrops];
                }

                return groupedDrops.Select(static drop =>
                    (IReadOnlyList<JournalDropSource>)[drop]);
            })
            .ToArray();
    }

    private static bool IsGlobalNpcDropGroup(IReadOnlyList<JournalDropSource> drops) =>
        drops
            .Select(static drop => drop.SourceNpcType)
            .Where(static npcType => npcType.HasValue)
            .Distinct()
            .Count() == NPCLoader.NPCCount;

    private float AppendConditionContent(UIElement parent, IReadOnlyList<string> conditions, float top)
    {
        var area = new JournalConditionPanel();
        area.Left.Set(10f, 0f);
        area.Top.Set(top, 0f);
        area.Width.Set(-20f, 1f);

        var alertIcon = new JournalConditionAlertIcon
        {
            HAlign = 0.5f
        };
        alertIcon.Top.Set(8f, 0f);
        area.Append(alertIcon);

        var contentTop = 54f;
        var contentWidth = MathF.Max(50f, GetSourceTextMaxWidth() - 20f);
        contentTop = AppendConditionTextList(
            area,
            JournalAcquisitionVisuals.SplitConditions(conditions).RemainingText,
            contentTop,
            contentWidth);

        area.Height.Set(contentTop + 10f, 0f);
        parent.Append(area);
        return top + area.Height.Pixels;
    }

    private static float AppendConditionTextList(
        UIElement parent,
        IReadOnlyList<string> conditions,
        float top,
        float maxWidth)
    {
        if (conditions.Count == 0)
        {
            return top;
        }

        for (var conditionIndex = 0; conditionIndex < conditions.Count; conditionIndex++)
        {
            if (conditionIndex > 0)
            {
                top += 4f;
            }

            var condition = conditions[conditionIndex];
            var textColor = JournalAcquisitionVisuals.IsHardmodeCondition(condition)
                ? new Color(235, 91, 91)
                : new Color(238, 204, 94);
            foreach (var line in JournalTextUtilities.WrapToPixelWidth(
                         condition,
                         maxWidth,
                         JournalUiMetrics.AcquisitionPanelTextScale))
            {
                var text = new UIText(line, JournalUiMetrics.AcquisitionPanelTextScale)
                {
                    HAlign = 0.5f,
                    TextColor = textColor
                };
                text.Top.Set(top, 0f);
                parent.Append(text);
                top += JournalUiMetrics.AcquisitionPanelTextLineHeight;
            }
        }

        return top;
    }

    private float AppendTokenRows(UIElement parent, IReadOnlyList<JournalSourceTokenData> tokens, float top)
    {
        return AppendTokenRows(
            parent,
            tokens,
            top,
            JournalUiMetrics.BlockHorizontalPadding,
            GetSourceTextMaxWidth(),
            centerRows: false);
    }

    private float AppendCenteredTokenRows(UIElement parent, IReadOnlyList<JournalSourceTokenData> tokens, float top)
    {
        return AppendTokenRows(
            parent,
            tokens,
            top,
            JournalUiMetrics.BlockHorizontalPadding,
            GetSourceTextMaxWidth(),
            centerRows: true);
    }

    private static float AppendTokenRows(
        UIElement parent,
        IReadOnlyList<JournalSourceTokenData> tokens,
        float top,
        float left,
        float maxWidth,
        bool centerRows)
    {
        if (tokens.Count == 0)
        {
            return top;
        }

        const float spacing = 8f;
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
            var currentX = centerRows
                ? left + MathF.Max(0f, (maxWidth - rowWidth) * 0.5f)
                : left;
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

    private void AddEntriesToSourceList(IEnumerable<UIElement> entries)
    {
        foreach (var entry in entries)
        {
            _sourceList.Add(entry);
        }
    }

    private void RefreshBuildPickerOverlay(
        string profileId,
        string classId,
        string stageId,
        bool showingPresets,
        bool showingBuildBuilder)
    {
        if (!showingPresets
            || !showingBuildBuilder
            || JournalSystem.ShowingBuildSaveDialog
            || JournalSystem.ShowingBuildExportDialog
            || JournalSystem.ShowingSharedBuildPreview
            || JournalSystem.ActiveBuildSlotKey is not { } slotKey)
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
        ApplyBuildPickerToolbarLayout(panelWidth);

        if (!string.Equals(_activeBuildPickerSlotKey, slotKey, StringComparison.OrdinalIgnoreCase))
        {
            _activeBuildPickerSlotKey = slotKey;
            _activeBuildPickerTab = BuildPickerTab.Vanilla;
            _buildPickerPowerSort = BuildPickerPowerSort.None;
            _buildPickerDamageFilter = BuildPickerDamageFilter.None;
            _selectedBuildPickerModName = null;
            _buildPickerSearchInput.SetText(string.Empty);
            _appliedBuildPickerSearchText = string.Empty;
        }

        var visualClass = JournalClassIds.ToLegacy(classId);
        _buildPickerTitle.SetText(JournalBuildPlannerCatalog.GetSlotDisplayName(slotKey, visualClass));
        RefreshBuildPickerControls(profileId, classId, slotKey);
        _buildPickerList.Clear();
        _appliedBuildPickerSearchText = _buildPickerSearchInput.CurrentString;

        if (_activeBuildPickerTab == BuildPickerTab.Mods)
        {
            PopulateModBuildPicker(profileId, stageId, classId, slotKey, panelWidth);
            return;
        }

        PopulateGuideBuildPicker(profileId, stageId, classId, slotKey, panelWidth);
    }

    private void PopulateGuideBuildPicker(
        string profileId,
        string stageId,
        string classId,
        string slotKey,
        float panelWidth)
    {
        var candidates = GetBuildCandidates(
                profileId,
                stageId,
                classId,
                slotKey,
                JournalSystem.BuildAllItemsEnabled)
            .Where(MatchesBuildPickerSearch)
            .Where(MatchesBuildPickerDamageFilter)
            .ToArray();
        candidates = SortBuildPickerCandidates(candidates, slotKey).ToArray();
        if (candidates.Length == 0)
        {
            _buildPickerList.Add(CreateSourceNotice(GetBuildPickerEmptyText()));
            return;
        }

        var highlightedItemIds = JournalSystem.GetHighlightedBuildItemIds(slotKey);
        var blockedItemIds = JournalSystem.GetBlockedBuildItemIds(slotKey);
        var slotsPerRow = GetBuildPickerSlotsPerRow(panelWidth);
        for (var index = 0; index < candidates.Length; index += slotsPerRow)
        {
            var rowCandidates = candidates.Skip(index).Take(slotsPerRow).ToArray();
            _buildPickerList.Add(CreateBuildCandidateRow(rowCandidates, highlightedItemIds, blockedItemIds));
        }
    }

    private void PopulateModBuildPicker(
        string profileId,
        string stageId,
        string classId,
        string slotKey,
        float panelWidth)
    {
        var groups = GetModBuildCandidateGroups(
                profileId,
                classId,
                slotKey,
                stageId,
                JournalSystem.BuildAllItemsEnabled)
            .Select(FilterBuildCandidateGroup)
            .Where(static group => group.Candidates.Count > 0)
            .ToArray();
        groups = SortBuildPickerGroups(groups, slotKey).ToArray();
        if (groups.Length == 0)
        {
            _buildPickerList.Add(CreateSourceNotice(GetBuildPickerEmptyText()));
            return;
        }

        var highlightedItemIds = JournalSystem.GetHighlightedBuildItemIds(slotKey);
        var blockedItemIds = JournalSystem.GetBlockedBuildItemIds(slotKey);
        var slotsPerRow = GetBuildPickerSlotsPerRow(panelWidth);

        foreach (var group in groups)
        {
            _buildPickerList.Add(CreateSourceSectionHeader(group.Title));
            var candidates = SortBuildPickerCandidates(group.Candidates, slotKey).ToArray();
            for (var index = 0; index < candidates.Length; index += slotsPerRow)
            {
                var rowCandidates = candidates.Skip(index).Take(slotsPerRow).ToArray();
                _buildPickerList.Add(CreateBuildCandidateRow(rowCandidates, highlightedItemIds, blockedItemIds));
            }
        }
    }

    private void RefreshBuildPickerControls(string profileId, string classId, string slotKey)
    {
        var selectedModGroup = GetSelectedBuildPickerModGroup(profileId, classId, slotKey);

        _buildPickerFilterButton.SetActive(_buildPickerFilterMenuOpen
            || _activeBuildPickerTab == BuildPickerTab.Mods
            || _buildPickerDamageFilter != BuildPickerDamageFilter.None);
        _buildPickerAllItemsButton.SetAllItemsEnabled(JournalSystem.BuildAllItemsEnabled);
        _buildPickerAllItemsButton.SetHoverText(Language.GetTextValue(JournalSystem.BuildAllItemsEnabled
            ? "Mods.ProgressionJournal.UI.BuildProgressionItemsTooltip"
            : "Mods.ProgressionJournal.UI.BuildAllItemsTooltip"));
        _buildPickerSortButton.SetActive(_buildPickerSortMenuOpen || _buildPickerPowerSort != BuildPickerPowerSort.None);

        if (selectedModGroup is null)
        {
            _buildPickerFilterButton.SetIconAsset(
                BuildPickerFilterIconTexturePath,
                BuildPickerBestiaryButtonIconSourceRectangle);
            _buildPickerFilterButton.SetItemIcon(0);
        }
        else
        {
            ApplyBuildPickerModIcon(_buildPickerFilterButton, selectedModGroup);
        }

        _buildPickerSortButton.SetIconAsset(
            BuildPickerSortIconTexturePath,
            BuildPickerBestiaryButtonIconSourceRectangle);
        _buildPickerSortButton.SetItemIcon(0);

        _buildPickerSearchInput.HintText = Language.GetTextValue("Mods.ProgressionJournal.UI.BuildPickerSearchHint");
        _buildPickerFilterButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildPickerFilterMenuTooltip"));
        _buildPickerSortButton.SetHoverText(GetBuildPickerSortMenuTooltip());

        RefreshBuildPickerMenuPanels(profileId, classId, slotKey);
    }

    private string GetBuildPickerSortMenuTooltip()
    {
        return _buildPickerPowerSort switch
        {
            BuildPickerPowerSort.Descending => Language.GetTextValue("Mods.ProgressionJournal.UI.BuildPickerPowerDescTooltip"),
            BuildPickerPowerSort.Ascending => Language.GetTextValue("Mods.ProgressionJournal.UI.BuildPickerPowerAscTooltip"),
            _ => Language.GetTextValue("Mods.ProgressionJournal.UI.BuildPickerSortMenuTooltip")
        };
    }

    private void ToggleBuildPickerFilterMenu()
    {
        if (_activeBuildPickerSlotKey is null)
        {
            return;
        }

        _buildPickerFilterMenuOpen = !_buildPickerFilterMenuOpen;
        if (_buildPickerFilterMenuOpen)
        {
            _buildPickerSortMenuOpen = false;
        }

        JournalSystem.RefreshView();
    }

    private void ToggleBuildPickerSortMenu()
    {
        if (_activeBuildPickerSlotKey is null)
        {
            return;
        }

        _buildPickerSortMenuOpen = !_buildPickerSortMenuOpen;
        if (_buildPickerSortMenuOpen)
        {
            _buildPickerFilterMenuOpen = false;
        }

        JournalSystem.RefreshView();
    }

    private void RefreshBuildPickerMenuPanels(string profileId, string classId, string slotKey)
    {
        if (_buildPickerFilterMenuPanel.Parent is not null)
        {
            _buildPickerPanel.RemoveChild(_buildPickerFilterMenuPanel);
        }

        if (_buildPickerSortMenuPanel.Parent is not null)
        {
            _buildPickerPanel.RemoveChild(_buildPickerSortMenuPanel);
        }

        if (_buildPickerFilterMenuOpen)
        {
            RebuildBuildPickerFilterMenu(profileId, classId, slotKey);
            _buildPickerPanel.Append(_buildPickerFilterMenuPanel);
        }

        if (_buildPickerSortMenuOpen)
        {
            RebuildBuildPickerSortMenu();
            _buildPickerPanel.Append(_buildPickerSortMenuPanel);
        }
    }

    private void RebuildBuildPickerFilterMenu(string profileId, string classId, string slotKey)
    {
        _buildPickerFilterMenuPanel.RemoveAllChildren();

        var groups = GetModBuildCandidateGroups(
            profileId,
            classId,
            slotKey,
            JournalSystem.SelectedStageId,
            JournalSystem.BuildAllItemsEnabled).ToArray();
        var showDamageFilters = CanUseBuildPickerDamageFilters(slotKey);
        var sourceButtonCount = 3 + groups.Length;
        if (showDamageFilters)
        {
            ApplyBuildPickerTwoColumnFilterMenuLayout(_buildPickerFilterMenuPanel, sourceButtonCount, damageButtonCount: 5);
        }
        else
        {
            ApplyBuildPickerIconMenuLayout(_buildPickerFilterMenuPanel, sourceButtonCount, alignRight: false);
        }

        var index = 0;
        var sourceRow = 0;
        int NextSourceIndex() => showDamageFilters ? sourceRow++ * BuildPickerMenuMaxColumns : index++;

        AddBuildPickerMenuButton(
            _buildPickerFilterMenuPanel,
            NextSourceIndex(),
            CreateBuildPickerItemIconButton(SelectBuildPickerGuideFilter, BuildPickerVanillaIconItemId),
            Language.GetTextValue("Mods.ProgressionJournal.UI.BuildPickerGuideTab"),
            _activeBuildPickerTab == BuildPickerTab.Vanilla);

        AddBuildPickerMenuButton(
            _buildPickerFilterMenuPanel,
            NextSourceIndex(),
            CreateBuildPickerItemIconButton(SelectBuildPickerAllModsFilter, BuildPickerModsIconItemId),
            Language.GetTextValue("Mods.ProgressionJournal.UI.BuildPickerModTab"),
            _activeBuildPickerTab == BuildPickerTab.Mods && _selectedBuildPickerModName is null);

        foreach (var group in groups)
        {
            var modName = group.Title;
            var button = new JournalBuildFilterIconButton(() => SelectBuildPickerModFilter(modName));
            ApplyBuildPickerModIcon(button, group);
            AddBuildPickerMenuButton(
                _buildPickerFilterMenuPanel,
                NextSourceIndex(),
                button,
                group.Title,
                _activeBuildPickerTab == BuildPickerTab.Mods
                    && string.Equals(_selectedBuildPickerModName, group.Title, StringComparison.CurrentCultureIgnoreCase));
        }

        if (showDamageFilters)
        {
            AddBuildPickerDamageFilterButton(
                _buildPickerFilterMenuPanel,
                0,
                BuildPickerDamageFilter.Melee,
                itemIconId: BuildPickerMeleeIconItemId);
            AddBuildPickerDamageFilterButton(
                _buildPickerFilterMenuPanel,
                1,
                BuildPickerDamageFilter.Ranged,
                itemIconId: ItemID.WoodenBow);
            AddBuildPickerDamageFilterButton(
                _buildPickerFilterMenuPanel,
                2,
                BuildPickerDamageFilter.Magic,
                BuildPickerMagicIconTexturePath);
            AddBuildPickerDamageFilterButton(
                _buildPickerFilterMenuPanel,
                3,
                BuildPickerDamageFilter.Summon,
                ItemID.SlimeStaff);
            AddBuildPickerDamageFilterButton(
                _buildPickerFilterMenuPanel,
                4,
                BuildPickerDamageFilter.Other,
                ItemID.FallenStar);
        }

        AddBuildPickerMenuButton(
            _buildPickerFilterMenuPanel,
            showDamageFilters ? sourceRow * BuildPickerMenuMaxColumns : index,
            CreateBuildPickerAssetIconButton(ResetBuildPickerFilters, BestiarySearchCancelTexturePath),
            Language.GetTextValue("Mods.ProgressionJournal.UI.BuildPickerFilterResetTooltip"),
            HasBuildPickerFilters());
    }

    private static JournalBuildFilterIconButton CreateBuildPickerAssetIconButton(
        Action onClick,
        string texturePath,
        Rectangle? sourceRectangle = null)
    {
        var button = new JournalBuildFilterIconButton(onClick);
        button.SetIconAsset(texturePath, sourceRectangle);
        return button;
    }

    private static JournalBuildFilterIconButton CreateBuildPickerItemIconButton(Action onClick, int itemIconId)
    {
        var button = new JournalBuildFilterIconButton(onClick);
        button.SetItemIcon(itemIconId);
        return button;
    }

    private void AddBuildPickerDamageFilterButton(
        UIPanel panel,
        int row,
        BuildPickerDamageFilter filter,
        int itemIconId = 0)
    {
        AddBuildPickerDamageFilterButton(panel, row, filter, iconTexturePath: null, itemIconId);
    }

    private void AddBuildPickerDamageFilterButton(
        UIPanel panel,
        int row,
        BuildPickerDamageFilter filter,
        string? iconTexturePath,
        int itemIconId = 0)
    {
        var button = new JournalBuildFilterIconButton(() => SelectBuildPickerDamageFilter(filter));
        if (!string.IsNullOrWhiteSpace(iconTexturePath))
        {
            button.SetIconAsset(iconTexturePath);
        }
        else
        {
            button.SetItemIcon(itemIconId);
        }

        AddBuildPickerMenuButton(
            panel,
            row * BuildPickerMenuMaxColumns + 1,
            button,
            Language.GetTextValue($"Mods.ProgressionJournal.UI.BuildPickerDamage{filter}Tooltip"),
            _buildPickerDamageFilter == filter);
    }

    private void RebuildBuildPickerSortMenu()
    {
        _buildPickerSortMenuPanel.RemoveAllChildren();
        ApplyBuildPickerIconMenuLayout(_buildPickerSortMenuPanel, 3, alignRight: true);

        AddBuildPickerMenuButton(
            _buildPickerSortMenuPanel,
            0,
            CreateBuildPickerAssetIconButton(() => SetBuildPickerPowerSort(BuildPickerPowerSort.Descending), BuildPickerSortDescendingIconTexturePath),
            Language.GetTextValue("Mods.ProgressionJournal.UI.BuildPickerPowerDescTooltip"),
            _buildPickerPowerSort == BuildPickerPowerSort.Descending);

        AddBuildPickerMenuButton(
            _buildPickerSortMenuPanel,
            1,
            CreateBuildPickerAssetIconButton(() => SetBuildPickerPowerSort(BuildPickerPowerSort.Ascending), BuildPickerSortAscendingIconTexturePath),
            Language.GetTextValue("Mods.ProgressionJournal.UI.BuildPickerPowerAscTooltip"),
            _buildPickerPowerSort == BuildPickerPowerSort.Ascending);

        AddBuildPickerMenuButton(
            _buildPickerSortMenuPanel,
            2,
            CreateBuildPickerAssetIconButton(ResetBuildPickerPowerSort, BestiarySearchCancelTexturePath),
            Language.GetTextValue("Mods.ProgressionJournal.UI.BuildPickerSortResetTooltip"),
            _buildPickerPowerSort != BuildPickerPowerSort.None);
    }

    private static void ApplyBuildPickerIconMenuLayout(UIPanel panel, int buttonCount, bool alignRight)
    {
        var columns = Math.Max(1, Math.Min(BuildPickerMenuMaxColumns, buttonCount));
        var rows = Math.Max(1, (int)Math.Ceiling(buttonCount / (float)BuildPickerMenuMaxColumns));
        var width = BuildPickerMenuPadding * 2f
            + columns * BuildPickerMenuButtonSize
            + (columns - 1) * BuildPickerMenuGap;
        if (alignRight)
        {
            panel.Left.Set(-(JournalUiMetrics.BuildPickerInset + width), 1f);
        }

        panel.Width.Set(width, 0f);
        panel.Height.Set(BuildPickerMenuPadding * 2f
            + rows * BuildPickerMenuButtonSize
            + (rows - 1) * BuildPickerMenuGap,
            0f);
    }

    private static void ApplyBuildPickerTwoColumnFilterMenuLayout(
        UIPanel panel,
        int sourceButtonCount,
        int damageButtonCount)
    {
        var rows = Math.Max(1, Math.Max(sourceButtonCount, damageButtonCount));
        panel.Width.Set(BuildPickerMenuPadding * 2f
            + 2 * BuildPickerMenuButtonSize
            + BuildPickerMenuGap,
            0f);
        panel.Height.Set(BuildPickerMenuPadding * 2f
            + rows * BuildPickerMenuButtonSize
            + (rows - 1) * BuildPickerMenuGap,
            0f);
    }

    private static void AddBuildPickerMenuButton(
        UIPanel panel,
        int index,
        JournalBuildFilterIconButton button,
        string hoverText,
        bool active)
    {
        var column = index % BuildPickerMenuMaxColumns;
        var row = index / BuildPickerMenuMaxColumns;
        button.Left.Set(BuildPickerMenuPadding + column * (BuildPickerMenuButtonSize + BuildPickerMenuGap), 0f);
        button.Top.Set(BuildPickerMenuPadding + row * (BuildPickerMenuButtonSize + BuildPickerMenuGap), 0f);
        button.Width.Set(BuildPickerMenuButtonSize, 0f);
        button.Height.Set(BuildPickerMenuButtonSize, 0f);
        button.UseOptionTileStyle();
        button.SetHoverText(hoverText);
        button.SetActive(active);
        panel.Append(button);
    }

    private void ApplyBuildPickerToolbarLayout(float panelWidth)
    {
        const float buttonSize = 34f;
        const float buttonGap = 4f;
        const float searchSortGap = 12f;

        var firstSlotLeft = GetBuildPickerFirstSlotLeft(panelWidth);
        var filterButtonLeft = MathF.Max(0f, firstSlotLeft - buttonSize - buttonGap);
        var sortButtonLeft = -(JournalUiMetrics.BuildPickerInset + buttonSize);
        var allItemsButtonLeft = sortButtonLeft - BuildPickerModeToggleWidth - buttonGap;
        var searchRightInset = JournalUiMetrics.BuildPickerInset
            + buttonSize
            + BuildPickerModeToggleWidth
            + buttonGap
            + searchSortGap;

        _buildPickerFilterButton.Left.Set(filterButtonLeft, 0f);
        _buildPickerFilterMenuPanel.Left.Set(filterButtonLeft, 0f);

        _buildPickerAllItemsButton.Left.Set(allItemsButtonLeft, 1f);
        _buildPickerSortButton.Left.Set(sortButtonLeft, 1f);
        _buildPickerSortMenuPanel.Left.Set(sortButtonLeft, 1f);

        _buildPickerSearchBackground.Left.Set(firstSlotLeft, 0f);
        _buildPickerSearchBackground.Width.Set(-(firstSlotLeft + searchRightInset), 1f);
    }

    private void HideBuildPickerMenus()
    {
        _buildPickerFilterMenuOpen = false;
        _buildPickerSortMenuOpen = false;

        if (_buildPickerFilterMenuPanel.Parent is not null)
        {
            _buildPickerPanel.RemoveChild(_buildPickerFilterMenuPanel);
        }

        if (_buildPickerSortMenuPanel.Parent is not null)
        {
            _buildPickerPanel.RemoveChild(_buildPickerSortMenuPanel);
        }
    }

    private void SelectBuildPickerGuideFilter()
    {
        _activeBuildPickerTab = BuildPickerTab.Vanilla;
        _selectedBuildPickerModName = null;
        JournalSystem.RefreshView();
    }

    private void SelectBuildPickerAllModsFilter()
    {
        if (_activeBuildPickerTab == BuildPickerTab.Mods && _selectedBuildPickerModName is null)
        {
            _activeBuildPickerTab = BuildPickerTab.Vanilla;
        }
        else
        {
            _activeBuildPickerTab = BuildPickerTab.Mods;
        }

        _selectedBuildPickerModName = null;

        JournalSystem.RefreshView();
    }

    private void SelectBuildPickerModFilter(string modName)
    {
        if (_activeBuildPickerTab == BuildPickerTab.Mods
            && string.Equals(_selectedBuildPickerModName, modName, StringComparison.CurrentCultureIgnoreCase))
        {
            _activeBuildPickerTab = BuildPickerTab.Vanilla;
            _selectedBuildPickerModName = null;
        }
        else
        {
            _activeBuildPickerTab = BuildPickerTab.Mods;
            _selectedBuildPickerModName = modName;
        }

        JournalSystem.RefreshView();
    }

    private void SelectBuildPickerDamageFilter(BuildPickerDamageFilter filter)
    {
        _buildPickerDamageFilter = _buildPickerDamageFilter == filter ? BuildPickerDamageFilter.None : filter;
        JournalSystem.RefreshView();
    }

    private void SetBuildPickerPowerSort(BuildPickerPowerSort sort)
    {
        _buildPickerPowerSort = _buildPickerPowerSort == sort ? BuildPickerPowerSort.None : sort;
        JournalSystem.RefreshView();
    }

    private void ResetBuildPickerPowerSort()
    {
        _buildPickerPowerSort = BuildPickerPowerSort.None;
        JournalSystem.RefreshView();
    }

    private void ResetBuildPickerFilters()
    {
        _activeBuildPickerTab = BuildPickerTab.Vanilla;
        _selectedBuildPickerModName = null;
        _buildPickerDamageFilter = BuildPickerDamageFilter.None;
        _buildPickerPowerSort = BuildPickerPowerSort.None;
        _buildPickerSearchInput.SetText(string.Empty);
        JournalSystem.RefreshView();
    }

    private bool HasBuildPickerFilters()
    {
        return _activeBuildPickerTab != BuildPickerTab.Vanilla
            || _selectedBuildPickerModName is not null
            || _buildPickerDamageFilter != BuildPickerDamageFilter.None
            || _buildPickerPowerSort != BuildPickerPowerSort.None
            || !string.IsNullOrWhiteSpace(_buildPickerSearchInput.CurrentString);
    }

    private JournalBuildCandidateGroup FilterBuildCandidateGroup(JournalBuildCandidateGroup group)
    {
        if (_selectedBuildPickerModName is not null
            && !string.Equals(group.Title, _selectedBuildPickerModName, StringComparison.CurrentCultureIgnoreCase))
        {
            return new JournalBuildCandidateGroup(group.Title, [], group.IconItemId);
        }

        var candidates = group.Candidates;
        var searchText = _buildPickerSearchInput.CurrentString.Trim();
        if (string.IsNullOrWhiteSpace(searchText)
            || group.Title.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
        {
            return new JournalBuildCandidateGroup(
                group.Title,
                candidates.Where(MatchesBuildPickerDamageFilter).ToArray(),
                group.IconItemId);
        }

        return new JournalBuildCandidateGroup(
            group.Title,
            candidates
                .Where(MatchesBuildPickerSearch)
                .Where(MatchesBuildPickerDamageFilter)
                .ToArray(),
            group.IconItemId);
    }

    private bool MatchesBuildPickerSearch(JournalBuildCandidate candidate)
    {
        var searchText = _buildPickerSearchInput.CurrentString.Trim();
        return string.IsNullOrWhiteSpace(searchText)
            || Lang.GetItemNameValue(candidate.ItemId).Contains(searchText, StringComparison.CurrentCultureIgnoreCase);
    }

    private bool MatchesBuildPickerDamageFilter(JournalBuildCandidate candidate)
    {
        if (_buildPickerDamageFilter == BuildPickerDamageFilter.None)
        {
            return true;
        }

        return JournalItemUtilities.TryCreateItem(candidate.ItemId, out var item)
            && item.damage > 0
            && GetBuildPickerDamageFilter(item) == _buildPickerDamageFilter;
    }

    private IEnumerable<JournalBuildCandidate> SortBuildPickerCandidates(IEnumerable<JournalBuildCandidate> candidates, string slotKey)
    {
        if (_buildPickerPowerSort == BuildPickerPowerSort.None)
        {
            return candidates;
        }

        return _buildPickerPowerSort == BuildPickerPowerSort.Descending
            ? candidates
                .OrderBy(candidate => GetBuildCandidatePower(candidate, slotKey) <= 0)
                .ThenByDescending(candidate => GetBuildCandidatePower(candidate, slotKey))
                .ThenBy(candidate => Lang.GetItemNameValue(candidate.ItemId), StringComparer.CurrentCultureIgnoreCase)
            : candidates
                .OrderBy(candidate => GetBuildCandidatePower(candidate, slotKey) <= 0)
                .ThenBy(candidate => GetBuildCandidatePower(candidate, slotKey))
                .ThenBy(candidate => Lang.GetItemNameValue(candidate.ItemId), StringComparer.CurrentCultureIgnoreCase);
    }

    private IEnumerable<JournalBuildCandidateGroup> SortBuildPickerGroups(IEnumerable<JournalBuildCandidateGroup> groups, string slotKey)
    {
        if (_buildPickerPowerSort == BuildPickerPowerSort.None)
        {
            return groups;
        }

        return _buildPickerPowerSort == BuildPickerPowerSort.Descending
            ? groups
                .OrderBy(group => GetBuildPickerGroupPower(group, slotKey, useMinimumPower: false) <= 0)
                .ThenByDescending(group => GetBuildPickerGroupPower(group, slotKey, useMinimumPower: false))
                .ThenBy(group => group.Title, StringComparer.CurrentCultureIgnoreCase)
            : groups
                .OrderBy(group => GetBuildPickerGroupPower(group, slotKey, useMinimumPower: true) <= 0)
                .ThenBy(group => GetBuildPickerGroupPower(group, slotKey, useMinimumPower: true))
                .ThenBy(group => group.Title, StringComparer.CurrentCultureIgnoreCase);
    }

    private static int GetBuildPickerGroupPower(JournalBuildCandidateGroup group, string slotKey, bool useMinimumPower)
    {
        var powers = group.Candidates
            .Select(candidate => GetBuildCandidatePower(candidate, slotKey))
            .Where(static power => power > 0)
            .ToArray();

        if (powers.Length == 0)
        {
            return 0;
        }

        return useMinimumPower ? powers.Min() : powers.Max();
    }

    private static int GetBuildCandidatePower(JournalBuildCandidate candidate, string slotKey)
    {
        if (!JournalItemUtilities.TryCreateItem(candidate.ItemId, out var item))
        {
            return 0;
        }

        if (!JournalBuildPlannerCatalog.TryGetSlotKind(slotKey, out var slotKind))
        {
            return 0;
        }

        return slotKind is JournalBuildSlotKind.ArmorHead or JournalBuildSlotKind.ArmorBody or JournalBuildSlotKind.ArmorLegs
            ? item.defense
            : item.damage;
    }

    private static bool CanUseBuildPickerDamageFilters(string slotKey)
    {
        return JournalBuildPlannerCatalog.TryGetSlotKind(slotKey, out var slotKind)
            && slotKind is JournalBuildSlotKind.PrimaryWeapon
                or JournalBuildSlotKind.SupportWeapon
                or JournalBuildSlotKind.ClassSpecific;
    }

    private static BuildPickerDamageFilter GetBuildPickerDamageFilter(Item item)
    {
        if (item.CountsAsClass(DamageClass.Melee))
        {
            return BuildPickerDamageFilter.Melee;
        }

        if (item.CountsAsClass(DamageClass.Ranged))
        {
            return BuildPickerDamageFilter.Ranged;
        }

        if (item.CountsAsClass(DamageClass.Magic))
        {
            return BuildPickerDamageFilter.Magic;
        }

        return item.CountsAsClass(DamageClass.Summon) ? BuildPickerDamageFilter.Summon : BuildPickerDamageFilter.Other;
    }

    private JournalBuildCandidateGroup? GetSelectedBuildPickerModGroup(string profileId, string classId, string slotKey)
    {
        if (_activeBuildPickerTab != BuildPickerTab.Mods || _selectedBuildPickerModName is null)
        {
            return null;
        }

        return GetModBuildCandidateGroups(
                profileId,
                classId,
                slotKey,
                JournalSystem.SelectedStageId,
                JournalSystem.BuildAllItemsEnabled)
            .FirstOrDefault(group => string.Equals(group.Title, _selectedBuildPickerModName, StringComparison.CurrentCultureIgnoreCase));
    }

    private static void ApplyBuildPickerModIcon(JournalBuildFilterIconButton button, JournalBuildCandidateGroup? group)
    {
        button.SetIconTexture(null);
        button.SetItemIcon(0);

        if (group is null)
        {
            return;
        }

        if (TryGetBuildPickerModIcon(group, out var iconTexture))
        {
            button.SetIconTexture(iconTexture);
            return;
        }

        button.SetItemIcon(group.IconItemId);
    }

    private static bool TryGetBuildPickerModIcon(JournalBuildCandidateGroup group, out Texture2D? texture)
    {
        texture = null;
        if (group.IconItemId <= 0)
        {
            return false;
        }

        var item = JournalItemUtilities.CreateItem(group.IconItemId);
        var mod = item.ModItem?.Mod;
        if (mod is null)
        {
            return false;
        }

        if (!mod.RequestAssetIfExists<Texture2D>("icon", out var iconAsset)
            && !mod.RequestAssetIfExists("Icon", out iconAsset))
        {
            return false;
        }

        texture = iconAsset.Value;
        return true;
    }

    private string GetBuildPickerEmptyText()
    {
        return string.IsNullOrWhiteSpace(_buildPickerSearchInput.CurrentString)
            ? Language.GetTextValue("Mods.ProgressionJournal.UI.BuildPickerEmpty")
            : Language.GetTextValue("Mods.ProgressionJournal.UI.BuildPickerSearchEmpty");
    }

    private void RefreshBuildSaveOverlay(bool showingPresets, bool showingBuildBuilder)
    {
        if (!showingPresets
            || !showingBuildBuilder
            || JournalSystem.ShowingBuildExportDialog
            || JournalSystem.ShowingSharedBuildPreview
            || !JournalSystem.ShowingBuildSaveDialog)
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
            _buildSaveNameInput.SetText(JournalSystem.EditingBuildName ?? string.Empty);
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

    private void RefreshBuildExportOverlay(bool showingPresets, bool showingBuildBuilder)
    {
        if (!showingPresets || showingBuildBuilder || JournalSystem.ShowingSharedBuildPreview || !JournalSystem.ShowingBuildExportDialog)
        {
            HideBuildExportOverlay();
            return;
        }

        if (_buildExportOverlay.Parent is null)
        {
            _root.Append(_buildExportOverlay);
        }

        if (_buildExportPanel.Parent is null)
        {
            _root.Append(_buildExportPanel);
        }

        var rootDimensions = _root.GetDimensions();
        const float panelWidth = 156f;
        const float panelHeight = 92f;
        _buildExportPanel.Width.Set(panelWidth, 0f);
        _buildExportPanel.Height.Set(panelHeight, 0f);
        _buildExportPanel.Left.Set((rootDimensions.Width - panelWidth) * 0.5f, 0f);
        _buildExportPanel.Top.Set((rootDimensions.Height - panelHeight) * 0.5f, 0f);
    }

    private void RefreshSharedBuildPreviewOverlay()
    {
        if (JournalSystem.SharedBuildPreview is not { } build)
        {
            HideSharedBuildPreviewOverlay();
            return;
        }

        if (_sharedBuildOverlay.Parent is null)
        {
            _root.Append(_sharedBuildOverlay);
        }

        if (_sharedBuildPanel.Parent is null)
        {
            _root.Append(_sharedBuildPanel);
        }

        var rootDimensions = _root.GetDimensions();
        var panelWidth = MathF.Min(720f, rootDimensions.Width - 48f);
        var panelHeight = MathF.Min(560f, rootDimensions.Height - 48f);
        _sharedBuildPanel.Width.Set(panelWidth, 0f);
        _sharedBuildPanel.Height.Set(panelHeight, 0f);
        _sharedBuildPanel.Left.Set((rootDimensions.Width - panelWidth) * 0.5f, 0f);
        _sharedBuildPanel.Top.Set((rootDimensions.Height - panelHeight) * 0.5f, 0f);

        _sharedBuildTitle.SetText(JournalTextUtilities.TrimToPixelWidth(
            build.Name,
            panelWidth - 96f,
            JournalUiMetrics.BuildPickerTitleScale));
        var buildProfile = JournalProfileRegistry.TryGet(build.ProfileId, out var registeredBuildProfile)
            ? registeredBuildProfile
            : JournalProfileRegistry.Active;
        _sharedBuildMeta.SetText(
            $"{JournalProfileText.GetClassName(buildProfile, build.ClassId)} • {JournalProfileText.GetStageName(buildProfile, build.StageId)}");

        _sharedBuildList.Clear();
        _sharedBuildList.Add(CreateSharedBuildPreview(build));
        AddSharedBuildSection(
            Language.GetTextValue("Mods.ProgressionJournal.UI.Weapons"),
            GetSelectedItems(
                build,
                JournalBuildPlannerCatalog.PrimaryWeaponSlotKey,
                JournalBuildPlannerCatalog.SupportWeaponSlotKey,
                JournalBuildPlannerCatalog.ClassSpecificSlotKey));
        AddSharedBuildSection(
            Language.GetTextValue("Mods.ProgressionJournal.UI.ArmorLabel"),
            GetSelectedItems(
                build,
                JournalBuildPlannerCatalog.ArmorHeadSlotKey,
                JournalBuildPlannerCatalog.ArmorBodySlotKey,
                JournalBuildPlannerCatalog.ArmorLegsSlotKey));
        AddSharedBuildSection(
            Language.GetTextValue("Mods.ProgressionJournal.UI.Accessories"),
            Enumerable.Range(1, JournalBuildPlannerCatalog.MaxAccessorySlotCount)
                .Select(slotIndex => build.GetSelectedItemReference(JournalBuildPlannerCatalog.GetAccessorySlotKey(slotIndex)))
                .Where(static itemReference => itemReference is not null)
                .Select(static itemReference => itemReference!)
                .ToArray());
        AddSharedBuildSection(
            Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSlotPotion"),
            Enumerable.Range(1, JournalBuildPlannerCatalog.PotionSlotCount)
                .Select(slotIndex => build.GetSelectedItemReference(JournalBuildPlannerCatalog.GetPotionSlotKey(slotIndex)))
                .Where(static itemReference => itemReference is not null)
                .Select(static itemReference => itemReference!)
                .ToArray());
        AddSharedBuildSection(
            Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSlotFood"),
            Enumerable.Range(1, JournalBuildPlannerCatalog.FoodSlotCount)
                .Select(slotIndex => build.GetSelectedItemReference(JournalBuildPlannerCatalog.GetFoodSlotKey(slotIndex)))
                .Where(static itemReference => itemReference is not null)
                .Select(static itemReference => itemReference!)
                .ToArray());
    }

    private void HideBuildPickerOverlay()
    {
        HideBuildPickerMenus();

        if (_buildPickerOverlay.Parent is not null)
        {
            _root.RemoveChild(_buildPickerOverlay);
        }

        if (_buildPickerPanel.Parent is not null)
        {
            _root.RemoveChild(_buildPickerPanel);
        }

        _activeBuildPickerSlotKey = null;
        _appliedBuildPickerSearchText = _buildPickerSearchInput.CurrentString;
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

        if (!clearInput) return;
        _buildSaveNameInput.SetText(string.Empty);
        _buildSaveMessage.SetText(string.Empty);
    }

    private void HideBuildExportOverlay()
    {
        if (_buildExportOverlay.Parent is not null)
        {
            _root.RemoveChild(_buildExportOverlay);
        }

        if (_buildExportPanel.Parent is not null)
        {
            _root.RemoveChild(_buildExportPanel);
        }
    }

    private void HideSharedBuildPreviewOverlay()
    {
        if (_sharedBuildOverlay.Parent is not null)
        {
            _root.RemoveChild(_sharedBuildOverlay);
        }

        if (_sharedBuildPanel.Parent is not null)
        {
            _root.RemoveChild(_sharedBuildPanel);
        }

        _sharedBuildList.Clear();
    }

    private UIElement CreateBuildCandidateRow(
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

    private static UIElement CreateSharedBuildPreview(JournalSavedBuild build)
    {
        var container = new UIElement();
        container.Width.Set(0f, 1f);
        container.Height.Set(194f, 0f);

        var previewPanel = JournalUiElementFactory.CreatePanel();
        previewPanel.Width.Set(148f, 0f);
        previewPanel.Height.Set(184f, 0f);
        previewPanel.HAlign = 0.5f;
        previewPanel.BackgroundColor = JournalUiTheme.PresetPanelBackground;
        previewPanel.BorderColor = JournalUiTheme.PresetPanelBorder;
        container.Append(previewPanel);

        var characterPreview = new JournalSavedBuildCharacterPreview(
            JournalPreviewPlayerFactory.CreateSavedBuildPreview(build, build.ProfileId, build.StageId),
            static () => 0f,
            1.18f);
        characterPreview.Width.Set(118f, 0f);
        characterPreview.Height.Set(152f, 0f);
        characterPreview.HAlign = 0.5f;
        characterPreview.Top.Set(18f, 0f);
        characterPreview.IgnoresMouseInteraction = true;
        previewPanel.Append(characterPreview);

        return container;
    }

    private void AddSharedBuildSection(string title, IReadOnlyList<JournalSavedBuildItemReference> itemReferences)
    {
        if (itemReferences.Count == 0)
        {
            return;
        }

        _sharedBuildList.Add(CreateSharedBuildSection(title, itemReferences));
    }

    private static UIElement CreateSharedBuildSection(string title, IReadOnlyList<JournalSavedBuildItemReference> itemReferences)
    {
        const int itemsPerRow = 8;

        var panel = JournalUiElementFactory.CreatePanel();
        panel.Width.Set(0f, 1f);
        panel.BackgroundColor = JournalUiTheme.PresetPanelBackground;
        panel.BorderColor = JournalUiTheme.PresetPanelBorder;

        var top = JournalUiMetrics.BlockVerticalPadding;
        var titleElement = new UIText(title, JournalUiMetrics.BuildPanelHeaderScale, true)
        {
            TextColor = JournalUiTheme.SectionHeaderText
        };
        titleElement.Left.Set(JournalUiMetrics.BlockHorizontalPadding, 0f);
        titleElement.Top.Set(top, 0f);
        panel.Append(titleElement);
        top += 26f;

        for (var index = 0; index < itemReferences.Count; index += itemsPerRow)
        {
            var rowItems = itemReferences
                .Skip(index)
                .Take(itemsPerRow)
                .ToArray();
            var strip = new JournalItemStrip(rowItems);
            strip.Left.Set(JournalUiMetrics.BlockHorizontalPadding, 0f);
            strip.Top.Set(top, 0f);
            panel.Append(strip);
            top += JournalUiMetrics.BuildSlotSize + 6f;
        }

        panel.Height.Set(top + JournalUiMetrics.BlockVerticalPadding - 6f, 0f);
        return panel;
    }

    private static JournalSavedBuildItemReference[] GetSelectedItems(JournalSavedBuild build, params string[] slotKeys)
    {
        return slotKeys
            .Select(build.GetSelectedItemReference)
            .Where(static itemReference => itemReference is not null)
            .Select(static itemReference => itemReference!)
            .ToArray();
    }

    private static int GetBuildPickerSlotsPerRow(float panelWidth)
    {
        var availableWidth = panelWidth - (JournalUiMetrics.BuildPickerInset * 2f + JournalUiMetrics.ScrollbarWidth + 4f);
        return Math.Max(1, (int)((availableWidth + JournalUiMetrics.BuildSlotGap) / (JournalUiMetrics.BuildSlotSize + JournalUiMetrics.BuildSlotGap)));
    }

    private static float GetBuildPickerFirstSlotLeft(float panelWidth)
    {
        var availableWidth = panelWidth - (JournalUiMetrics.BuildPickerInset * 2f + JournalUiMetrics.ScrollbarWidth + 4f);
        var rowWidth = GetBuildCandidateRowWidth(GetBuildPickerSlotsPerRow(panelWidth));
        return JournalUiMetrics.BuildPickerInset + MathF.Max(0f, (availableWidth - rowWidth) * 0.5f);
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
        var height = MathF.Min(
            JournalUiMetrics.RootMaxHeight,
            Main.screenHeight - JournalUiMetrics.RootVerticalMargin - JournalUiMetrics.ProfileButtonHeight);
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
        PositionProfileButton();

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

        _progressionModeToggleButton = new JournalProgressionModeToggle(
            () => JournalSystem.ToggleProgressionMode());
        _progressionModeToggleButton.Width.Set(JournalUiMetrics.StageProgressionToggleSize, 0f);
        _progressionModeToggleButton.Height.Set(JournalUiMetrics.StageProgressionToggleSize, 0f);
        _progressionModeToggleButton.Left.Set(-(JournalUiMetrics.StageProgressionToggleRightInset + JournalUiMetrics.StageProgressionToggleSize), 1f);
        _progressionModeToggleButton.Top.Set(JournalUiMetrics.StageProgressionToggleTop, 0f);
        _stagePanel.Append(_progressionModeToggleButton);

        _stageListContainer = new JournalSmoothScrollGrid
        {
            ListPadding = JournalUiMetrics.StageButtonGap,
            ManualSortMethod = _ => { }
        };
        _root.AddDragTarget(_stageListContainer);
        _stageListContainer.Left.Set(JournalUiMetrics.StageListLeft, 0f);
        _stageListContainer.Top.Set(JournalUiMetrics.StageListTop, 0f);
        _stageListContainer.Width.Set(-JournalUiMetrics.StageListHorizontalInset, 1f);
        _stageListContainer.Height.Set(-JournalUiMetrics.StageListBottomInset, 1f);
        _stagePanel.Append(_stageListContainer);

        _stageScrollbar = new UIScrollbar();
        _stageScrollbar.Width.Set(JournalUiMetrics.ScrollbarWidth, 0f);
        _stageScrollbar.Left.Set(-JournalUiMetrics.StageScrollbarOffset, 1f);
        _stageScrollbar.Top.Set(JournalUiMetrics.StageListTop, 0f);
        _stageScrollbar.Height.Set(-JournalUiMetrics.StageListBottomInset, 1f);
        _stagePanel.Append(_stageScrollbar);
        _stageListContainer.SetSmoothScrollbar(_stageScrollbar);

        _profileButton = JournalUiElementFactory.CreateIconTextButton(
            TextureAssets.Item[ItemID.Book],
            string.Empty,
            JournalUiMetrics.StagePanelWidth,
            JournalUiMetrics.ProfileButtonHeight,
            () => JournalSystem.OpenProfileManager(),
            JournalUiMetrics.ProfileButtonTextScale);
        Append(_profileButton);
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

        _buildImportButton = JournalUiElementFactory.CreateIconButton(
            TextureAssets.Camera[6],
            30f,
            30f,
            () => JournalSystem.ImportSavedBuilds(),
            0.9f);
        _buildImportButton.Left.Set(-76f, 1f);
        _buildImportButton.Top.Set(8f, 0f);
        _buildImportButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildImportTooltip"));

        _buildBuilderButton = JournalUiElementFactory.CreateIconButton(
            CraftingWindowToggleTexturePath,
            30f,
            30f,
            () => JournalSystem.ShowBuildBuilderPage(),
            0.9f);
        _buildBuilderButton.Left.Set(-38f, 1f);
        _buildBuilderButton.Top.Set(8f, 0f);
        _buildBuilderButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildBuilderTab"));

        _buildBackButton = JournalUiElementFactory.CreateIconButton(
            BestiaryBackButtonTexturePath,
            30f,
            30f,
            () => JournalSystem.ShowPresetsTab(),
            0.9f);
        _buildBackButton.Left.Set(-76f, 1f);
        _buildBackButton.Top.Set(8f, 0f);
        _buildBackButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildBackTooltip"));

        _buildSaveButton = JournalUiElementFactory.CreateIconButton(
            TextureAssets.Item[ItemID.Book],
            30f,
            30f,
            () => JournalSystem.OpenBuildSaveDialog(),
            0.9f);
        _buildSaveButton.Left.Set(-38f, 1f);
        _buildSaveButton.Top.Set(8f, 0f);
        _buildSaveButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveTooltip"));

        _contentDescription = new UIText(string.Empty, JournalUiMetrics.ContentDescriptionScale);
        _contentDescription.Left.Set(JournalUiMetrics.ContentDescriptionLeft, 0f);
        _contentDescription.Top.Set(JournalUiMetrics.ContentDescriptionTop, 0f);
        _contentDescription.TextColor = JournalUiTheme.ContentDescriptionText;
        _contentPanel.Append(_contentDescription);

        _classSelectionContainer = new JournalSmoothScrollList
        {
            ListPadding = JournalUiMetrics.ClassSelectionButtonGap
        };
        _root.AddDragTarget(_classSelectionContainer);
        _classSelectionContainer.Left.Set(JournalUiMetrics.ContentBodyLeft, 0f);
        _classSelectionContainer.Top.Set(JournalUiMetrics.ContentBodyTop, 0f);
        _classSelectionContainer.Width.Set(-JournalUiMetrics.EntryListWidthInset, 1f);
        _classSelectionContainer.Height.Set(-JournalUiMetrics.ContentBodyBottomInset, 1f);
        _contentPanel.Append(_classSelectionContainer);

        _classSelectionScrollbar = new UIScrollbar();
        _classSelectionScrollbar.Width.Set(JournalUiMetrics.ScrollbarWidth, 0f);
        _classSelectionScrollbar.Left.Set(-JournalUiMetrics.ScrollbarOffset, 1f);
        _classSelectionScrollbar.Top.Set(JournalUiMetrics.ContentBodyTop, 0f);
        _classSelectionScrollbar.Height.Set(-JournalUiMetrics.ContentBodyBottomInset, 1f);
        _contentPanel.Append(_classSelectionScrollbar);
        _classSelectionContainer.SetScrollbar(_classSelectionScrollbar);

        _entryList = new JournalSmoothScrollList();
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
        ToggleContentButton(_buildImportButton, !selectingClass && showingPresets && !showingBuildBuilder);
        ToggleContentButton(_buildBuilderButton, !selectingClass && showingPresets && !showingBuildBuilder);
        ToggleContentButton(_buildBackButton, !selectingClass && showingPresets && showingBuildBuilder);
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

        _sourceList = new JournalSmoothScrollList
        {
            ManualSortMethod = _ => { }
        };
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
        const float filterButtonSize = 34f;
        const float filterButtonGap = 8f;
        const float filterButtonLeft = 4f;
        const float sortButtonLeft = -(JournalUiMetrics.BuildPickerInset + filterButtonSize);
        const float allItemsButtonLeft = sortButtonLeft - BuildPickerModeToggleWidth - 4f;

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

        _buildPickerFilterButton = new JournalBuildFilterIconButton(ToggleBuildPickerFilterMenu);
        _buildPickerFilterButton.SetIconAsset(
            BuildPickerFilterIconTexturePath,
            BuildPickerBestiaryButtonIconSourceRectangle);
        _buildPickerFilterButton.Left.Set(filterButtonLeft, 0f);
        _buildPickerFilterButton.Top.Set(84f, 0f);
        _buildPickerFilterButton.Width.Set(filterButtonSize, 0f);
        _buildPickerFilterButton.Height.Set(filterButtonSize, 0f);
        _buildPickerPanel.Append(_buildPickerFilterButton);

        _buildPickerAllItemsButton = new JournalBuildItemModeToggle(JournalSystem.ToggleBuildAllItems);
        _buildPickerAllItemsButton.Left.Set(allItemsButtonLeft, 1f);
        _buildPickerAllItemsButton.Top.Set(84f, 0f);
        _buildPickerAllItemsButton.Width.Set(BuildPickerModeToggleWidth, 0f);
        _buildPickerAllItemsButton.Height.Set(filterButtonSize, 0f);
        _buildPickerAllItemsButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildAllItemsTooltip"));
        _buildPickerPanel.Append(_buildPickerAllItemsButton);

        _buildPickerSortButton = new JournalBuildFilterIconButton(ToggleBuildPickerSortMenu);
        _buildPickerSortButton.SetIconAsset(
            BuildPickerSortIconTexturePath,
            BuildPickerBestiaryButtonIconSourceRectangle);
        _buildPickerSortButton.Left.Set(sortButtonLeft, 1f);
        _buildPickerSortButton.Top.Set(84f, 0f);
        _buildPickerSortButton.Width.Set(filterButtonSize, 0f);
        _buildPickerSortButton.Height.Set(filterButtonSize, 0f);
        _buildPickerPanel.Append(_buildPickerSortButton);

        _buildPickerFilterMenuPanel = JournalUiElementFactory.CreatePanel();
        _buildPickerFilterMenuPanel.SetPadding(0f);
        _buildPickerFilterMenuPanel.Left.Set(filterButtonLeft, 0f);
        _buildPickerFilterMenuPanel.Top.Set(122f, 0f);
        _buildPickerFilterMenuPanel.BackgroundColor = JournalUiTheme.RootBackground * 0.96f;
        _buildPickerFilterMenuPanel.BorderColor = Color.Transparent;

        _buildPickerSortMenuPanel = JournalUiElementFactory.CreatePanel();
        _buildPickerSortMenuPanel.SetPadding(0f);
        _buildPickerSortMenuPanel.Left.Set(sortButtonLeft, 1f);
        _buildPickerSortMenuPanel.Top.Set(122f, 0f);
        _buildPickerSortMenuPanel.BackgroundColor = JournalUiTheme.RootBackground * 0.96f;
        _buildPickerSortMenuPanel.BorderColor = Color.Transparent;

        _buildPickerSearchBackground = JournalUiElementFactory.CreatePanel();
        _buildPickerSearchBackground.Left.Set(JournalUiMetrics.BuildPickerInset, 0f);
        _buildPickerSearchBackground.Top.Set(83f, 0f);
        _buildPickerSearchBackground.Width.Set(-(JournalUiMetrics.BuildPickerInset * 2f + filterButtonSize + filterButtonGap), 1f);
        _buildPickerSearchBackground.Height.Set(34f, 0f);
        _buildPickerPanel.Append(_buildPickerSearchBackground);

        _buildPickerSearchInput = new JournalTextInput(string.Empty);
        _buildPickerSearchInput.Left.Set(10f, 0f);
        _buildPickerSearchInput.Top.Set(7f, 0f);
        _buildPickerSearchInput.Width.Set(-20f, 1f);
        _buildPickerSearchInput.Height.Set(20f, 0f);
        _buildPickerSearchBackground.Append(_buildPickerSearchInput);

        _buildPickerList = new JournalSmoothScrollList();
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

    private void InitializeBuildExportOverlay()
    {
        _buildExportOverlay = new JournalDimOverlay(() => JournalSystem.CloseBuildExportDialog());

        _buildExportPanel = JournalUiElementFactory.CreatePanel();
        _buildExportPanel.SetPadding(0f);
        _buildExportPanel.BackgroundColor = JournalUiTheme.RootBackground * JournalUiTheme.RootBackgroundOpacity;
        _buildExportPanel.BorderColor = JournalUiTheme.RootBorder;

        _buildExportCloseButton = JournalUiElementFactory.CreateIconButton(
            BestiarySearchCancelTexturePath,
            24f,
            24f,
            () => JournalSystem.CloseBuildExportDialog(),
            0.8f);
        _buildExportCloseButton.Left.Set(-30f, 1f);
        _buildExportCloseButton.Top.Set(6f, 0f);
        _buildExportPanel.Append(_buildExportCloseButton);

        _buildExportFileButton = JournalUiElementFactory.CreateIconButton(
            TextureAssets.Camera[6],
            46f,
            46f,
            () => JournalSystem.ExportSelectedBuildToFile(),
            0.78f);
        _buildExportFileButton.Left.Set(24f, 0f);
        _buildExportFileButton.Top.Set(32f, 0f);
        _buildExportFileButton.EnableChrome(JournalUiTheme.GetDefaultTextButtonStyle());
        _buildExportPanel.Append(_buildExportFileButton);

        _buildExportChatButton = JournalUiElementFactory.CreateIconButton(
            TextureAssets.Chat,
            46f,
            46f,
            () => JournalSystem.ExportSelectedBuildToChat(),
            0.82f);
        _buildExportChatButton.Left.Set(86f, 0f);
        _buildExportChatButton.Top.Set(32f, 0f);
        _buildExportChatButton.EnableChrome(JournalUiTheme.GetDefaultTextButtonStyle());
        _buildExportPanel.Append(_buildExportChatButton);
    }

    private void InitializeSharedBuildOverlay()
    {
        _sharedBuildOverlay = new JournalDimOverlay(() => JournalSystem.CloseSharedBuildPreview());

        _sharedBuildPanel = JournalUiElementFactory.CreatePanel();
        _sharedBuildPanel.SetPadding(0f);
        _sharedBuildPanel.BackgroundColor = JournalUiTheme.RootBackground * JournalUiTheme.RootBackgroundOpacity;
        _sharedBuildPanel.BorderColor = JournalUiTheme.RootBorder;

        _sharedBuildTitle = new UIText(string.Empty, JournalUiMetrics.BuildPickerTitleScale, true)
        {
            HAlign = 0.5f,
            TextColor = JournalUiTheme.RootTitleText
        };
        _sharedBuildTitle.Top.Set(14f, 0f);
        _sharedBuildPanel.Append(_sharedBuildTitle);

        _sharedBuildMeta = new UIText(string.Empty, 0.72f)
        {
            HAlign = 0.5f,
            TextColor = JournalUiTheme.ContentDescriptionText
        };
        _sharedBuildMeta.Top.Set(42f, 0f);
        _sharedBuildPanel.Append(_sharedBuildMeta);

        _sharedBuildCloseIconButton = JournalUiElementFactory.CreateIconButton(
            BestiarySearchCancelTexturePath,
            24f,
            24f,
            () => JournalSystem.CloseSharedBuildPreview(),
            0.8f);
        _sharedBuildCloseIconButton.Left.Set(-34f, 1f);
        _sharedBuildCloseIconButton.Top.Set(8f, 0f);
        _sharedBuildPanel.Append(_sharedBuildCloseIconButton);

        _sharedBuildList = new JournalSmoothScrollList();
        _sharedBuildList.Left.Set(18f, 0f);
        _sharedBuildList.Top.Set(78f, 0f);
        _sharedBuildList.Width.Set(-(18f * 2f + JournalUiMetrics.ScrollbarWidth + 4f), 1f);
        _sharedBuildList.Height.Set(-142f, 1f);
        _sharedBuildList.ListPadding = JournalUiMetrics.EntryListPadding;
        _sharedBuildPanel.Append(_sharedBuildList);

        _sharedBuildScrollbar = new UIScrollbar();
        _sharedBuildScrollbar.Width.Set(JournalUiMetrics.ScrollbarWidth, 0f);
        _sharedBuildScrollbar.Left.Set(-(JournalUiMetrics.ScrollbarWidth + 18f), 1f);
        _sharedBuildScrollbar.Top.Set(78f, 0f);
        _sharedBuildScrollbar.Height.Set(-142f, 1f);
        _sharedBuildPanel.Append(_sharedBuildScrollbar);
        _sharedBuildList.SetScrollbar(_sharedBuildScrollbar);

        _sharedBuildCloseButton = JournalUiElementFactory.CreateTextButton(
            Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveCancel"),
            0f,
            32f,
            () => JournalSystem.CloseSharedBuildPreview(),
            0.88f);
        _sharedBuildCloseButton.Left.Set(24f, 0f);
        _sharedBuildCloseButton.Top.Set(-50f, 1f);
        _sharedBuildCloseButton.Width.Set(-18f, 0.5f);
        _sharedBuildCloseButton.SetStyle(JournalUiTheme.GetDefaultTextButtonStyle());
        _sharedBuildPanel.Append(_sharedBuildCloseButton);

        _sharedBuildAddButton = JournalUiElementFactory.CreateTextButton(
            Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSharedAdd"),
            0f,
            32f,
            () => JournalSystem.ImportSharedBuildPreview(),
            0.88f);
        _sharedBuildAddButton.Left.Set(6f, 0.5f);
        _sharedBuildAddButton.Top.Set(-50f, 1f);
        _sharedBuildAddButton.Width.Set(-30f, 0.5f);
        _sharedBuildAddButton.SetStyle(JournalUiTheme.GetOverlayActionButtonStyle());
        _sharedBuildPanel.Append(_sharedBuildAddButton);
    }

    private void InitializeProfileManagerOverlay()
    {
        _profileManagerOverlay = new JournalDimOverlay(() => JournalSystem.CloseProfileManager());
        _profileManagerPanel = new JournalProfileManagerPanel();
    }

    private void RefreshProfileManagerOverlay()
    {
        if (!JournalSystem.ShowingProfileManager)
        {
            HideProfileManagerOverlay();
            return;
        }

        if (_profileManagerOverlay.Parent is null)
        {
            _root.Append(_profileManagerOverlay);
        }

        if (_profileManagerPanel.Parent is null)
        {
            _root.Append(_profileManagerPanel);
        }

        var rootDimensions = _root.GetDimensions();
        var width = MathF.Min(900f, rootDimensions.Width - 40f);
        var height = MathF.Min(620f, rootDimensions.Height - 40f);
        _profileManagerPanel.Width.Set(width, 0f);
        _profileManagerPanel.Height.Set(height, 0f);
        _profileManagerPanel.Left.Set((rootDimensions.Width - width) * 0.5f, 0f);
        _profileManagerPanel.Top.Set((rootDimensions.Height - height) * 0.5f, 0f);
        _profileManagerPanel.Refresh(JournalSystem);
    }

    private void HideProfileManagerOverlay()
    {
        if (_profileManagerOverlay.Parent is not null)
        {
            _root.RemoveChild(_profileManagerOverlay);
        }

        if (_profileManagerPanel.Parent is not null)
        {
            _root.RemoveChild(_profileManagerPanel);
        }
    }

    private void RefreshProfileButtonVisibility()
    {
        var visible = JournalSystem is { ShowingProfileManager: false, ShowingBuildSaveDialog: false, ShowingBuildExportDialog: false, ShowingSharedBuildPreview: false, ActiveBuildSlotKey: null };

        if (visible)
        {
            if (_profileButton.Parent is null)
            {
                Append(_profileButton);
            }

            PositionProfileButton();
            return;
        }

        if (_profileButton.Parent is not null)
        {
            RemoveChild(_profileButton);
        }
    }

    private void PositionProfileButton()
    {
        var rootDimensions = _root.GetDimensions();
        _profileButton.Left.Set(rootDimensions.X + JournalUiMetrics.OuterPadding, 0f);
        _profileButton.Top.Set(
            rootDimensions.Y + rootDimensions.Height - JournalUiMetrics.ProfileButtonOverlap,
            0f);
        _profileButton.Recalculate();
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
        _progressionModeToggleButton.SetEnabled(progressionModeEnabled);
        _progressionModeToggleButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.ProgressionModeToggleTooltip"));
        _buildBuilderButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildBuilderTab"));
        _buildImportButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildImportTooltip"));
        _buildBackButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildBackTooltip"));
        _buildSaveButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveTooltip"));
        _buildSaveTitle.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveDialogTitle"));
        _buildSaveNameInput.HintText = Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveDialogHint");
        _buildSaveCancelButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveCancel"));
        _buildSaveConfirmButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveConfirm"));
        _buildExportFileButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildExportFileTooltip"));
        _buildExportChatButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildExportChatTooltip"));
        _sharedBuildCloseButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveCancel"));
        _sharedBuildAddButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSharedAdd"));
        _classButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.Class"));
        _overviewTabButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.OverviewTab"));
        _presetsTabButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.PresetsTab"));
        _profileButton.SetText(JournalProfileRegistry.Active.DisplayName);
        var profileIcon = JournalProfileIconResolver.GetIcon(JournalProfileRegistry.Active);
        _profileButton.SetIcon(profileIcon.Texture, profileIcon.SourceRectangle);
        _profileButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileManagerTooltip"));
    }

    private void UpdateNavigationStyles(bool selectingClass, bool showingPresets)
    {
        _closeButton.SetStyle(JournalUiTheme.GetHeaderButtonStyle(danger: true));
        _progressionModeToggleButton.SetStyle(JournalUiTheme.GetDefaultTextButtonStyle());
        _profileButton.SetStyle(JournalUiTheme.GetDefaultTextButtonStyle());
        _classButton.SetStyle(JournalUiTheme.GetTabButtonStyle(selectingClass));
        _overviewTabButton.SetStyle(JournalUiTheme.GetTabButtonStyle(!selectingClass && !showingPresets));
        _presetsTabButton.SetStyle(JournalUiTheme.GetTabButtonStyle(!selectingClass && showingPresets));
    }

    private void PopulateClassSelection(JournalProfile profile, string selectedClassId)
    {
        for (var rowIndex = 0; rowIndex < profile.Classes.Count; rowIndex += 2)
        {
            var row = new UIElement();
            row.Width.Set(0f, 1f);
            row.Height.Set(JournalUiMetrics.ClassSelectionButtonHeight, 0f);

            var rowClasses = profile.Classes.Skip(rowIndex).Take(2).ToArray();
            for (var column = 0; column < rowClasses.Length; column++)
            {
                var classDefinition = rowClasses[column];
                var capturedClassId = classDefinition.Id;
                var panel = new JournalClassButton(
                    profile,
                    classDefinition,
                    string.Equals(selectedClassId, capturedClassId, StringComparison.OrdinalIgnoreCase),
                    JournalUiMetrics.ClassSelectionButtonHeight);
                panel.Left.Set(
                    column == 0 ? 0f : JournalUiMetrics.ClassSelectionButtonGap * 0.5f,
                    column == 0 ? 0f : JournalUiMetrics.ClassSelectionButtonWidth);
                panel.Width.Set(
                    -JournalUiMetrics.ClassSelectionButtonGap * 0.5f,
                    JournalUiMetrics.ClassSelectionButtonWidth);
                panel.OnLeftClick += (_, _) => JournalSystem.SelectClass(capturedClassId);
                row.Append(panel);
            }

            _classSelectionContainer.Add(row);
        }
    }

    private void EnsureProfileNavigation(JournalProfile profile, string selectedStageId)
    {
        if (string.Equals(_renderedProfileId, profile.Id, StringComparison.OrdinalIgnoreCase)
            && _stageButtons.Count == profile.Stages.Count)
        {
            return;
        }

        _renderedProfileId = profile.Id;
        _stageButtons.Clear();
        _stageListContainer.Clear();

        var buttons = new List<JournalStageButton>(profile.Stages.Count);
        foreach (var stage in profile.Stages)
        {
            var capturedStageId = stage.Id;
            var button = JournalUiElementFactory.CreateStageButton(() => JournalSystem.SelectStage(capturedStageId));
            button.Left.Set(0f, 0f);
            button.Width.Set(-JournalUiMetrics.StageButtonColumnGap * 0.5f, 0.5f);
            button.Height.Set(JournalUiMetrics.StageButtonDefaultHeight, 0f);
            _stageButtons[capturedStageId] = button;
            buttons.Add(button);
        }

        _stageListContainer.AddRange(buttons);
        if (_stageButtons.TryGetValue(selectedStageId, out var selectedButton))
        {
            _stageListContainer.Goto(
                element => ReferenceEquals(element, selectedButton),
                center: true);
        }
        _layoutInitialized = false;
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

            if (_classSelectionScrollbar.Parent is null)
            {
                _contentPanel.Append(_classSelectionScrollbar);
            }

            return;
        }

        if (_classSelectionContainer.Parent is not null)
        {
            _contentPanel.RemoveChild(_classSelectionContainer);
        }

        if (_classSelectionScrollbar.Parent is not null)
        {
            _contentPanel.RemoveChild(_classSelectionScrollbar);
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
        _classSelectionScrollbar.Top.Set(JournalUiMetrics.ContentBodyTop, 0f);
        _classSelectionScrollbar.Height.Set(-JournalUiMetrics.ContentBodyBottomInset, 1f);

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
        var totalHeight = height + JournalUiMetrics.ProfileButtonHeight - JournalUiMetrics.ProfileButtonOverlap;
        return new Vector2(
            (Main.screenWidth - width) * 0.5f,
            (Main.screenHeight - totalHeight) * 0.5f + topOffset);
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

    private JournalSystem JournalSystem => journalSystem;
}
