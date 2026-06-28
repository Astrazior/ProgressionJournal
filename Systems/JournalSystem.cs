using Microsoft.Xna.Framework;
using System.Diagnostics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace ProgressionJournal.Systems;

public sealed class JournalSystem : ModSystem
{
    private UserInterface? _journalInterface;
    private JournalUiState? _journalState;
    private UserInterface? _buttonInterface;
    private JournalButtonUiState? _buttonState;

    public bool Visible { get; private set; }

    public bool ShowingPresets { get; private set; }

    public bool ShowingBuildBuilder { get; private set; }

    public bool ShowingBuildSaveDialog { get; private set; }

    public bool ShowingBuildExportDialog { get; private set; }

    public bool ShowingSharedBuildPreview => SharedBuildPreview is not null;

    public bool ShowingProfileManager { get; private set; }

    public JournalSavedBuild? SharedBuildPreview { get; private set; }

    public bool SelectingClass { get; private set; } = true;

    public bool HasSelectedClass { get; private set; }

    public static string SelectedProfileId => JournalProfileRegistry.Active.Id;

    public string SelectedClassId { get; private set; } = JournalClassIds.Melee;

    public string SelectedStageId { get; private set; } = JournalStageIds.FromLegacy(ProgressionStageCatalog.GetCurrentStageId());

    public CombatClass SelectedClass => JournalClassIds.ToLegacy(SelectedClassId);

    public ProgressionStageId SelectedStage => JournalStageIds.TryToLegacy(SelectedStageId, out var stageId)
        ? stageId
        : ProgressionStageId.PreBoss;

    public bool ProgressionModeEnabled { get; private set; } = true;

    public bool BuildAllItemsEnabled { get; private set; }

    public int SelectedItemId { get; private set; }

    public string? ActiveBuildSlotKey { get; private set; }

    public string? EditingBuildName => _editingBuild?.Name;

    private readonly Dictionary<string, int> _buildSelections = new();

    private JournalSavedBuild? _editingBuild;

    private JournalSavedBuild? _exportingBuild;

    private readonly Queue<string> _pendingWarmupClassIds = new();
    private string _pendingWarmupProfileId = string.Empty;
    private string _pendingWarmupStageId = string.Empty;

    public override void Load()
    {
        if (Main.dedServ)
        {
            return;
        }

        _journalInterface = new UserInterface();
        _journalState = new JournalUiState(this);
        _journalState.Activate();

        _buttonInterface = new UserInterface();
        _buttonState = new JournalButtonUiState();
        _buttonState.Activate();
        _buttonInterface.SetState(_buttonState);
    }
    
    public override void Unload()
    {
        Visible = false;
        ShowingPresets = false;
        ShowingBuildBuilder = false;
        ShowingBuildSaveDialog = false;
        ShowingBuildExportDialog = false;
        ShowingProfileManager = false;
        SelectingClass = true;
        HasSelectedClass = false;

        ActiveBuildSlotKey = null;
        _editingBuild = null;
        _exportingBuild = null;
        SharedBuildPreview = null;
        SelectedItemId = ItemID.None;

        _buildSelections.Clear();

        // Mod reload can call Unload while the main thread is still drawing the old UI.
        // Do not mutate its element collections here; releasing the references is sufficient.
        _journalInterface = null;
        _journalState = null;
        _buttonInterface = null;
        _buttonState = null;
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (Visible)
        {
            _journalInterface?.Update(gameTime);
        }

        if (ShouldDrawJournalButton)
        {
            _buttonInterface?.Update(gameTime);
        }

        WarmPendingContentCache();
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (ShouldDrawJournalButton && _buttonInterface is not null)
        {
            var inventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));

            if (inventoryIndex >= 0)
            {
                layers.Insert(inventoryIndex + 1, new LegacyGameInterfaceLayer(
                    "ProgressionJournal: Journal Button",
                    DrawJournalButtonInterface,
                    InterfaceScaleType.UI));
            }
        }

        if (!Visible || _journalInterface is null)
        {
            return;
        }

        var mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
        if (mouseTextIndex < 0)
        {
            mouseTextIndex = layers.Count;
        }

        layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
            "ProgressionJournal: Journal Window",
            DrawJournalInterface,
            InterfaceScaleType.UI));
        layers.Insert(mouseTextIndex + 1, new LegacyGameInterfaceLayer(
            "ProgressionJournal: Journal Tooltip",
            DrawJournalTooltipInterface,
            InterfaceScaleType.UI));
    }

    public void ToggleView()
    {
        if (Visible)
        {
            HideView();
            return;
        }

        ShowView();
    }

    public void ShowView()
    {
        CloseConflictingInterfaces();
        Visible = true;
        SelectingClass = !HasSelectedClass;
        ShowingPresets = false;
        ShowingBuildBuilder = false;
        ShowingBuildSaveDialog = false;
        ShowingBuildExportDialog = false;
        ShowingProfileManager = false;
        ActiveBuildSlotKey = null;
        _editingBuild = null;
        _exportingBuild = null;
        SharedBuildPreview = null;
        JournalBuildStorage.Reload();
        CoerceSelectedStage();
        _journalInterface?.SetState(_journalState);
        RefreshView();
    }

    public void HideView()
    {
        Visible = false;
        ShowingBuildSaveDialog = false;
        ShowingBuildExportDialog = false;
        ActiveBuildSlotKey = null;
        _editingBuild = null;
        _exportingBuild = null;
        SharedBuildPreview = null;
        ShowingProfileManager = false;
        _journalInterface?.SetState(null);
    }

    public void CycleClass(int direction)
    {
        var classOrder = JournalProfileRegistry.Active.Classes.Select(static value => value.Id).ToArray();
        SelectedClassId = Cycle(classOrder, SelectedClassId, direction);
        SelectedItemId = ItemID.None;
        RefreshView();
    }

    public void SelectClass(CombatClass combatClass)
    {
        SelectClass(JournalClassIds.FromLegacy(combatClass));
    }

    public void SelectClass(string classId)
    {
        if (!JournalProfileRegistry.Active.Classes.Any(value => string.Equals(value.Id, classId, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        SelectedClassId = classId;
        HasSelectedClass = true;
        SelectingClass = false;
        ShowingPresets = false;
        ShowingBuildBuilder = false;
        ShowingBuildSaveDialog = false;
        ShowingBuildExportDialog = false;
        ActiveBuildSlotKey = null;
        _editingBuild = null;
        _exportingBuild = null;
        SelectedItemId = ItemID.None;
        CoerceBuildSelections();
        RefreshView();
    }

    public void ShowClassSelection()
    {
        SelectingClass = true;
        ShowingBuildBuilder = false;
        ShowingBuildSaveDialog = false;
        ShowingBuildExportDialog = false;
        ActiveBuildSlotKey = null;
        _editingBuild = null;
        _exportingBuild = null;
        SelectedItemId = ItemID.None;
        RefreshView();
    }

    public void CycleStage(int direction)
    {
        SelectedStageId = Cycle(GetAvailableStageOrder(), SelectedStageId, direction);
        SelectedItemId = ItemID.None;
        RefreshView();
    }

    public void SelectStage(ProgressionStageId stageId)
    {
        SelectStage(JournalStageIds.FromLegacy(stageId));
    }

    public void SelectStage(string stageId)
    {
        var stage = JournalProfileRegistry.Active.Stages.FirstOrDefault(
            value => string.Equals(value.Id, stageId, StringComparison.OrdinalIgnoreCase));
        if (stage is null || !IsStageAvailable(stage))
        {
            return;
        }

        SelectedStageId = stageId;
        ShowingBuildSaveDialog = false;
        ShowingBuildExportDialog = false;
        ActiveBuildSlotKey = null;
        _editingBuild = null;
        _exportingBuild = null;
        SelectedItemId = ItemID.None;
        CoerceBuildSelections();
        RefreshView();
    }

    public void ToggleProgressionMode()
    {
        ProgressionModeEnabled = !ProgressionModeEnabled;
        CoerceSelectedStage();
        ShowingBuildSaveDialog = false;
        ShowingBuildExportDialog = false;
        ActiveBuildSlotKey = null;
        _editingBuild = null;
        _exportingBuild = null;
        SelectedItemId = ItemID.None;
        CoerceBuildSelections();
        RefreshView();
    }

    public void SelectProfile(string profileId)
    {
        if (!JournalProfileRegistry.Select(profileId))
        {
            return;
        }

        SelectedClassId = JournalProfileRegistry.Active.Classes[0].Id;
        SelectedStageId = GetCurrentStageId();
        HasSelectedClass = false;
        SelectingClass = true;
        ShowingPresets = false;
        ShowingBuildBuilder = false;
        ActiveBuildSlotKey = null;
        _editingBuild = null;
        _exportingBuild = null;
        _buildSelections.Clear();
        SelectedItemId = ItemID.None;
        _journalState?.ResetProfileNavigation();
        QueueSelectedStageContentWarmup();
        RefreshView();
    }

    public void OpenProfileManager()
    {
        ShowingProfileManager = true;
        ShowingBuildSaveDialog = false;
        ShowingBuildExportDialog = false;
        ActiveBuildSlotKey = null;
        RefreshView();
    }

    public void CloseProfileManager()
    {
        ShowingProfileManager = false;
        RefreshView();
    }

    public void ShowOverviewTab()
    {
        SelectingClass = false;
        ShowingPresets = false;
        ShowingBuildBuilder = false;
        ShowingBuildSaveDialog = false;
        ShowingBuildExportDialog = false;
        ShowingProfileManager = false;
        ActiveBuildSlotKey = null;
        _editingBuild = null;
        _exportingBuild = null;
        RefreshView();
    }

    public void ShowPresetsTab()
    {
        SelectingClass = false;
        ShowingPresets = true;
        ShowingBuildBuilder = false;
        ShowingBuildSaveDialog = false;
        ShowingBuildExportDialog = false;
        ActiveBuildSlotKey = null;
        _editingBuild = null;
        _exportingBuild = null;
        JournalBuildStorage.Reload();
        RefreshView();
    }

    public void ShowBuildBuilderPage()
    {
        SelectingClass = false;
        ShowingPresets = true;
        ShowingBuildBuilder = true;
        ShowingBuildSaveDialog = false;
        ShowingBuildExportDialog = false;
        ActiveBuildSlotKey = null;
        _editingBuild = null;
        _exportingBuild = null;
        RefreshView();
    }

    public void ToggleBuildAllItems()
    {
        if (!ShowingBuildBuilder)
        {
            return;
        }

        BuildAllItemsEnabled = !BuildAllItemsEnabled;
        RefreshView();
    }

    public void EditSavedBuild(JournalSavedBuild build)
    {
        if (!JournalProfileRegistry.Select(build.ProfileId))
        {
            return;
        }

        SelectedClassId = build.ClassId;
        SelectedStageId = build.StageId;
        HasSelectedClass = true;
        SelectingClass = false;
        ShowingPresets = true;
        ShowingBuildBuilder = true;
        ShowingBuildSaveDialog = false;
        ShowingBuildExportDialog = false;
        ActiveBuildSlotKey = null;
        _editingBuild = build;
        _exportingBuild = null;
        _buildSelections.Clear();

        foreach (var selection in build.SelectedItems.Where(static pair => pair.Value > ItemID.None))
        {
            _buildSelections[selection.Key] = selection.Value;
        }

        BuildAllItemsEnabled = _buildSelections.Any(pair => !JournalRepository.IsBuildSelectionValid(
            SelectedProfileId,
            SelectedStageId,
            SelectedClassId,
            pair.Key,
            pair.Value));
        CoerceBuildSelections();
        RefreshView();
    }

    public void SelectItem(int itemId)
    {
        if (itemId <= ItemID.None)
        {
            return;
        }

        SelectedItemId = itemId;
        RefreshView();
    }

    public void ClearSelectedItem()
    {
        if (SelectedItemId == ItemID.None)
        {
            return;
        }

        SelectedItemId = ItemID.None;
        RefreshView();
    }

    public void RefreshView()
    {
        if (!Visible)
        {
            return;
        }

        CoerceBuildSelections();

        var stopwatch = Stopwatch.StartNew();
        _journalState?.Refresh(
            SelectedProfileId,
            SelectedClassId,
            SelectedStageId,
            SelectingClass,
            ShowingPresets,
            ShowingBuildBuilder,
            ProgressionModeEnabled,
            HasSelectedClass,
            SelectedItemId);
        LogSlowUiOperation(
            $"RefreshView profile={SelectedProfileId} stage={SelectedStageId} class={SelectedClassId} selectingClass={SelectingClass}",
            stopwatch.Elapsed);
    }

    public void OpenBuildSlot(string slotKey)
    {
        if (ShowingBuildSaveDialog)
        {
            return;
        }

        ActiveBuildSlotKey = slotKey;
        RefreshView();
    }

    public void CloseBuildSlotPicker()
    {
        if (ActiveBuildSlotKey is null)
        {
            return;
        }

        ActiveBuildSlotKey = null;
        RefreshView();
    }

    public void OpenBuildSaveDialog()
    {
        if (!ShowingPresets || !ShowingBuildBuilder)
        {
            return;
        }

        ShowingBuildSaveDialog = true;
        ShowingBuildExportDialog = false;
        ActiveBuildSlotKey = null;
        _exportingBuild = null;
        RefreshView();
    }

    public void CloseBuildSaveDialog()
    {
        if (!ShowingBuildSaveDialog)
        {
            return;
        }

        ShowingBuildSaveDialog = false;
        RefreshView();
    }

    public void CloseBuildExportDialog()
    {
        if (!ShowingBuildExportDialog)
        {
            return;
        }

        ShowingBuildExportDialog = false;
        _exportingBuild = null;
        RefreshView();
    }

    public void SelectActiveBuildItem(int itemId)
    {
        if (ActiveBuildSlotKey is null || itemId <= ItemID.None)
        {
            return;
        }

        if (GetBlockedBuildItemIds(ActiveBuildSlotKey).Contains(itemId))
        {
            return;
        }

        _buildSelections[ActiveBuildSlotKey] = itemId;
        ActiveBuildSlotKey = null;
        RefreshView();
    }

    public bool TrySaveCurrentBuild(string name, out string errorMessage)
    {
        CoerceBuildSelections();

        if (!ShowingBuildBuilder)
        {
            errorMessage = Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveFailed");
            return false;
        }

        var trimmedName = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            errorMessage = Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveNameRequired");
            return false;
        }

        var selectedItems = _buildSelections
            .Where(static pair => pair.Value > ItemID.None)
            .ToDictionary(
                static pair => pair.Key,
                static pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);

        var saved = _editingBuild is { } editingBuild
            ? JournalBuildStorage.UpdateBuild(editingBuild, trimmedName, SelectedProfileId, SelectedClassId, SelectedStageId, selectedItems, out errorMessage)
            : JournalBuildStorage.SaveBuild(trimmedName, SelectedProfileId, SelectedClassId, SelectedStageId, selectedItems, out errorMessage);

        if (!saved)
        {
            return false;
        }

        _buildSelections.Clear();
        _editingBuild = null;
        ShowingBuildBuilder = false;
        ShowingBuildSaveDialog = false;
        ShowingBuildExportDialog = false;
        ActiveBuildSlotKey = null;
        RefreshView();
        return true;
    }

    public void ClearBuildItem(string slotKey)
    {
        if (!_buildSelections.Remove(slotKey))
        {
            return;
        }

        if (string.Equals(ActiveBuildSlotKey, slotKey, StringComparison.OrdinalIgnoreCase))
        {
            ActiveBuildSlotKey = null;
        }

        RefreshView();
    }

    public int GetSelectedBuildItem(string slotKey)
    {
        return _buildSelections.GetValueOrDefault(slotKey, ItemID.None);
    }

    public IReadOnlyList<JournalSavedBuild> GetSavedBuilds(string profileId, string stageId, string classId)
    {
        return JournalBuildStorage.GetBuilds(profileId, stageId, classId);
    }

    public void DeleteSavedBuild(JournalSavedBuild build)
    {
        if (!JournalBuildStorage.DeleteBuild(build))
        {
            return;
        }

        RefreshView();
    }

    public void ToggleSavedBuildFavorite(JournalSavedBuild build)
    {
        if (!JournalBuildStorage.SetFavorite(build, !build.IsFavorite))
        {
            return;
        }

        RefreshView();
    }

    public void ExportSavedBuild(JournalSavedBuild build)
    {
        _exportingBuild = build;
        ShowingBuildExportDialog = true;
        ShowingBuildSaveDialog = false;
        ActiveBuildSlotKey = null;
        RefreshView();
    }

    public void ExportSelectedBuildToFile()
    {
        if (_exportingBuild is not { } build)
        {
            return;
        }

        if (!JournalFileDialog.TryShowSaveBuildDialog(out var exportPath))
        {
            return;
        }

        ShowingBuildExportDialog = false;
        _exportingBuild = null;
        RefreshView();

        if (JournalBuildStorage.ExportBuild(build, exportPath))
        {
            Main.NewText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildExported", Path.GetFileName(exportPath)), Color.LightGreen);
            return;
        }

        Main.NewText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildExportFailed"), Color.OrangeRed);
    }

    public void ExportSelectedBuildToChat()
    {
        if (_exportingBuild is not { } build)
        {
            return;
        }

        ShowingBuildExportDialog = false;
        _exportingBuild = null;
        RefreshView();

        if (!JournalBuildStorage.TryExportBuildPayload(build, out var payload))
        {
            Main.NewText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildExportFailed"), Color.OrangeRed);
            return;
        }

        JournalBuildChat.ShareBuild(build.Name, payload);
    }

    public void ImportSavedBuilds()
    {
        if (!JournalFileDialog.TryShowOpenBuildDialog(out var importPath))
        {
            return;
        }

        if (!JournalBuildStorage.ImportBuild(importPath, out var importedName))
        {
            Main.NewText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildImportFailed"), Color.OrangeRed);
            return;
        }

        Main.NewText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildImported", importedName), Color.LightGreen);
        RefreshView();
    }

    public void ShowSharedBuildPreview(string payload)
    {
        if (!JournalBuildStorage.TryReadBuildPayload(payload, out var build))
        {
            Main.NewText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSharedInvalid"), Color.OrangeRed);
            return;
        }

        CloseActiveChatInput();

        if (!Visible)
        {
            ShowView();
        }

        if (!JournalProfileRegistry.Select(build.ProfileId))
        {
            Main.NewText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildImportFailed"), Color.OrangeRed);
            return;
        }

        SelectedClassId = build.ClassId;
        SelectedStageId = build.StageId;
        HasSelectedClass = true;
        SelectingClass = false;
        ShowingPresets = true;
        ShowingBuildBuilder = false;
        SharedBuildPreview = build;
        ShowingBuildSaveDialog = false;
        ShowingBuildExportDialog = false;
        ActiveBuildSlotKey = null;
        RefreshView();
    }

    public void CloseSharedBuildPreview()
    {
        if (SharedBuildPreview is null)
        {
            return;
        }

        SharedBuildPreview = null;
        RefreshView();
    }

    public void ImportSharedBuildPreview()
    {
        if (SharedBuildPreview is not { } build)
        {
            return;
        }

        if (!JournalBuildStorage.ImportBuild(build, out var importedName))
        {
            Main.NewText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildImportFailed"), Color.OrangeRed);
            return;
        }

        Main.NewText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildImported", importedName), Color.LightGreen);
        SharedBuildPreview = null;
        RefreshView();
    }

    public IReadOnlySet<int> GetHighlightedBuildItemIds(string slotKey)
    {
        if (!JournalBuildPlannerCatalog.TryGetSlotKind(slotKey, out var slotKind))
        {
            return EmptyBlockedItemIds;
        }

        if (!JournalBuildPlannerCatalog.DisallowsDuplicateSelections(slotKind))
        {
            return _buildSelections.TryGetValue(slotKey, out var itemId) && itemId > ItemID.None
                ? new HashSet<int> { itemId }
                : EmptyBlockedItemIds;
        }

        var highlightedItemIds = _buildSelections
            .Where(pair => pair.Value > ItemID.None
                && JournalBuildPlannerCatalog.TryGetSlotKind(pair.Key, out var pairSlotKind)
                && pairSlotKind == slotKind)
            .Select(static pair => pair.Value)
            .ToHashSet();

        return highlightedItemIds.Count == 0 ? EmptyBlockedItemIds : highlightedItemIds;
    }

    public IReadOnlySet<int> GetBlockedBuildItemIds(string slotKey)
    {
        if (!JournalBuildPlannerCatalog.TryGetSlotKind(slotKey, out var slotKind)
            || !JournalBuildPlannerCatalog.DisallowsDuplicateSelections(slotKind))
        {
            return EmptyBlockedItemIds;
        }

        var blockedItemIds = _buildSelections
            .Where(pair => !string.Equals(pair.Key, slotKey, StringComparison.OrdinalIgnoreCase)
                && pair.Value > ItemID.None
                && JournalBuildPlannerCatalog.TryGetSlotKind(pair.Key, out var pairSlotKind)
                && pairSlotKind == slotKind)
            .Select(static pair => pair.Value)
            .ToHashSet();

        return blockedItemIds.Count == 0 ? EmptyBlockedItemIds : blockedItemIds;
    }

    public override void OnWorldUnload()
    {
        Visible = false;
        ShowingBuildSaveDialog = false;
        ShowingBuildExportDialog = false;
        ActiveBuildSlotKey = null;
        _editingBuild = null;
        _exportingBuild = null;
        SharedBuildPreview = null;
        _buildSelections.Clear();
        _journalInterface?.SetState(null);
        _journalState?.ResetLayout();
    }

    private bool DrawJournalInterface()
    {
        JournalTooltip.Clear();
        _journalInterface?.Draw(Main.spriteBatch, new GameTime());
        return true;
    }

    private static bool DrawJournalTooltipInterface()
    {
        return JournalTooltip.DrawPending(Main.spriteBatch);
    }

    private bool DrawJournalButtonInterface()
    {
        _buttonInterface?.Draw(Main.spriteBatch, new GameTime());
        return true;
    }

    private static void CloseConflictingInterfaces()
    {
        Main.playerInventory = false;
        Main.editChest = false;
        Main.npcChatText = string.Empty;
        Main.InGuideCraftMenu = false;
        Main.InReforgeMenu = false;
        Main.recBigList = false;
    }

    private static void CloseActiveChatInput()
    {
        Main.drawingPlayerChat = false;
        Main.chatText = string.Empty;
        Main.chatRelease = false;
    }

    private static bool ShouldDrawJournalButton => !Main.gameMenu && Main.playerInventory;

    private IReadOnlyList<string> GetAvailableStageOrder()
    {
        return ProgressionModeEnabled
            ? JournalProfileRegistry.Active.Stages.Where(IsStageAvailable).Select(static value => value.Id).ToArray()
            : JournalProfileRegistry.Active.Stages.Select(static value => value.Id).ToArray();
    }

    private void CoerceSelectedStage()
    {
        var selected = JournalProfileRegistry.Active.Stages.FirstOrDefault(
            value => string.Equals(value.Id, SelectedStageId, StringComparison.OrdinalIgnoreCase));
        if (selected is not null && IsStageAvailable(selected))
        {
            return;
        }

        SelectedStageId = GetCurrentStageId();
    }

    private void CoerceBuildSelections()
    {
        if (!HasSelectedClass || _buildSelections.Count == 0)
        {
            return;
        }

        var invalidSelections = _buildSelections
            .Where(pair => !JournalRepository.IsBuildSelectionValid(
                SelectedProfileId,
                SelectedStageId,
                SelectedClassId,
                pair.Key,
                pair.Value,
                BuildAllItemsEnabled))
            .Select(static pair => pair.Key)
            .ToArray();

        foreach (var slotKey in invalidSelections)
        {
            _buildSelections.Remove(slotKey);
        }

        RemoveDuplicateBuildSelections();
    }

    private static readonly IReadOnlySet<int> EmptyBlockedItemIds = new HashSet<int>();

    private void RemoveDuplicateBuildSelections()
    {
        var duplicateSelectionKeys = _buildSelections
            .Where(pair => JournalBuildPlannerCatalog.TryGetSlotKind(pair.Key, out var slotKind)
                && JournalBuildPlannerCatalog.DisallowsDuplicateSelections(slotKind))
            .GroupBy(pair => new
            {
                SlotKind = JournalBuildPlannerCatalog.TryGetSlotKind(pair.Key, out var slotKind) ? slotKind : default,
                pair.Value
            })
            .SelectMany(static group => group
                .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Skip(1)
                .Select(static pair => pair.Key))
            .ToArray();

        foreach (var slotKey in duplicateSelectionKeys)
        {
            _buildSelections.Remove(slotKey);
        }
    }

    private static T Cycle<T>(IReadOnlyList<T> values, T current, int direction)
    {
        var currentIndex = FindIndex(values, current);
        var nextIndex = (currentIndex + direction + values.Count) % values.Count;
        return values[nextIndex];
    }

    private static int FindIndex<T>(IReadOnlyList<T> values, T current)
    {
        var comparer = EqualityComparer<T>.Default;

        for (var index = 0; index < values.Count; index++)
        {
            if (comparer.Equals(values[index], current))
            {
                return index;
            }
        }

        return 0;
    }

    private bool IsStageAvailable(JournalProfileStageDocument stage)
    {
        return !ProgressionModeEnabled || JournalProfileUnlockRegistry.IsUnlocked(stage, out _);
    }

    private static string GetCurrentStageId()
    {
        var current = JournalProfileRegistry.Active.Stages[0].Id;

        foreach (var stage in JournalProfileRegistry.Active.Stages)
        {
            if (JournalProfileUnlockRegistry.IsUnlocked(stage, out _))
            {
                current = stage.Id;
            }
        }

        return current;
    }

    private void QueueSelectedStageContentWarmup()
    {
        _pendingWarmupClassIds.Clear();
        _pendingWarmupProfileId = SelectedProfileId;
        _pendingWarmupStageId = SelectedStageId;

        foreach (var profileClass in JournalProfileRegistry.Active.Classes)
        {
            _pendingWarmupClassIds.Enqueue(profileClass.Id);
        }
    }

    private void WarmPendingContentCache()
    {
        if (!Visible
            || !SelectingClass
            || _pendingWarmupClassIds.Count == 0
            || !string.Equals(_pendingWarmupProfileId, SelectedProfileId, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(_pendingWarmupStageId, SelectedStageId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var classId = _pendingWarmupClassIds.Dequeue();
        var stopwatch = Stopwatch.StartNew();
        JournalRepository.GetEntries(SelectedProfileId, SelectedStageId, classId);
        JournalRepository.GetCombatBuffEntries(SelectedProfileId, SelectedStageId, classId);
        LogSlowUiOperation(
            $"WarmContentCache profile={SelectedProfileId} stage={SelectedStageId} class={classId}",
            stopwatch.Elapsed);
    }

    [Conditional("DEBUG")]
    private static void LogSlowUiOperation(string operation, TimeSpan elapsed)
    {
        if (elapsed.TotalMilliseconds < 50d)
        {
            return;
        }

        ProgressionJournal.Instance?.Logger.Info($"[Perf] {operation} took {elapsed.TotalMilliseconds:F1} ms.");
    }
}
