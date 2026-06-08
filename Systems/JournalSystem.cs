using Microsoft.Xna.Framework;
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

    public bool ShowingCombatBuffsPage { get; private set; }

    public bool ShowingBuildSaveDialog { get; private set; }

    public bool ShowingBuildExportDialog { get; private set; }

    public bool ShowingSharedBuildPreview => SharedBuildPreview is not null;

    public bool ShowingProfileManager { get; private set; }

    public JournalProfileEditorSession? ProfileEditor { get; private set; }

    public string? PendingProfileDeleteId { get; private set; }

    public JournalSavedBuild? SharedBuildPreview { get; private set; }

    public bool SelectingClass { get; private set; } = true;

    public bool HasSelectedClass { get; private set; }

    public string SelectedProfileId => JournalProfileRegistry.Active.Id;

    public string SelectedClassId { get; private set; } = JournalClassIds.Melee;

    public string SelectedStageId { get; private set; } = JournalStageIds.FromLegacy(ProgressionStageCatalog.GetCurrentStageId());

    public CombatClass SelectedClass => JournalClassIds.ToLegacy(SelectedClassId);

    public ProgressionStageId SelectedStage => JournalStageIds.TryToLegacy(SelectedStageId, out var stageId)
        ? stageId
        : ProgressionStageId.PreBoss;

    public bool ProgressionModeEnabled { get; private set; } = true;

    public int SelectedItemId { get; private set; }

    public string? ActiveBuildSlotKey { get; private set; }

    public string? EditingBuildName => _editingBuild?.Name;

    private readonly Dictionary<string, int> _buildSelections = new();

    private JournalSavedBuild? _editingBuild;

    private JournalSavedBuild? _exportingBuild;

    public override void Load()
    {
        if (Main.dedServ)
        {
            return;
        }

        _journalInterface = new UserInterface();
        _journalState = new JournalUiState();
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
        ShowingCombatBuffsPage = false;
        ShowingBuildSaveDialog = false;
        ShowingBuildExportDialog = false;
        ShowingProfileManager = false;
        SelectingClass = true;
        HasSelectedClass = false;

        ActiveBuildSlotKey = null;
        _editingBuild = null;
        _exportingBuild = null;
        SharedBuildPreview = null;
        ProfileEditor = null;
        PendingProfileDeleteId = null;
        SelectedItemId = ItemID.None;

        _buildSelections.Clear();

        _journalInterface?.SetState(null);
        _buttonInterface?.SetState(null);

        _journalState?.RemoveAllChildren();
        _buttonState?.RemoveAllChildren();

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
        ProfileEditor = null;
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
        ProfileEditor = null;
        _journalInterface?.SetState(null);
    }

    public void CycleClass(int direction)
    {
        var classOrder = JournalProfileRegistry.Active.Classes.Select(static value => value.Id).ToArray();
        SelectedClassId = Cycle(classOrder, SelectedClassId, direction);
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
        RefreshView();
    }

    public void CycleStage(int direction)
    {
        SelectedStageId = Cycle(GetAvailableStageOrder(), SelectedStageId, direction);
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
        CoerceBuildSelections();
        RefreshView();
    }

    public void SelectProfile(string profileId)
    {
        PendingProfileDeleteId = null;
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
        ShowingCombatBuffsPage = false;
        ActiveBuildSlotKey = null;
        _editingBuild = null;
        _exportingBuild = null;
        _buildSelections.Clear();
        _journalState?.ResetProfileNavigation();
        RefreshView();
    }

    public void OpenProfileManager()
    {
        ShowingProfileManager = true;
        ProfileEditor = null;
        ShowingBuildSaveDialog = false;
        ShowingBuildExportDialog = false;
        ActiveBuildSlotKey = null;
        RefreshView();
    }

    public void CloseProfileManager()
    {
        ShowingProfileManager = false;
        ProfileEditor = null;
        PendingProfileDeleteId = null;
        RefreshView();
    }

    public void BeginNewProfile()
    {
        ShowingProfileManager = true;
        ProfileEditor = JournalProfileEditorSession.CreateNew();
        RefreshView();
    }

    public void BeginEditActiveProfile()
    {
        var active = JournalProfileRegistry.Active;
        if (active.IsReadOnly)
        {
            if (!JournalProfileStorage.CreateEditableCopy(active, out var copy, out var error) || copy is null)
            {
                Main.NewText(error, Color.OrangeRed);
                return;
            }

            JournalProfileRegistry.Register(copy);
            JournalProfileRegistry.Select(copy.Id);
            active = copy;
        }

        ShowingProfileManager = true;
        ProfileEditor = JournalProfileEditorSession.FromProfile(active);
        RefreshView();
    }

    public void SaveProfileEditor()
    {
        if (ProfileEditor is null)
        {
            return;
        }

        if (!ProfileEditor.Save(out var profile, out var error) || profile is null)
        {
            Main.NewText(error, Color.OrangeRed);
            return;
        }

        JournalProfileRegistry.ReloadUserProfiles();
        JournalProfileRegistry.Select(profile.Id);
        SelectedClassId = profile.Classes[0].Id;
        SelectedStageId = profile.Stages[0].Id;
        ProfileEditor = null;
        HasSelectedClass = false;
        SelectingClass = true;
        _journalState?.ResetProfileNavigation();
        Main.NewText(Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileSaved"), Color.LightGreen);
        RefreshView();
    }

    public void ImportProfile()
    {
        if (!JournalFileDialog.TryShowOpenProfileDialog(out var path))
        {
            return;
        }

        if (!JournalProfileStorage.Import(path, out var profile, out var error) || profile is null)
        {
            Main.NewText(error, Color.OrangeRed);
            return;
        }

        JournalProfileRegistry.ReloadUserProfiles();
        JournalProfileRegistry.Select(profile.Id);
        SelectedClassId = profile.Classes[0].Id;
        SelectedStageId = profile.Stages[0].Id;
        _journalState?.ResetProfileNavigation();
        Main.NewText(Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileImported", profile.Name), Color.LightGreen);
        RefreshView();
    }

    public void ExportActiveProfile()
    {
        var profile = JournalProfileRegistry.Active;
        if (!JournalFileDialog.TryShowSaveProfileDialog(profile.Name, out var path))
        {
            return;
        }

        if (JournalProfileStorage.Export(profile, path, out var error))
        {
            Main.NewText(Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileExported", Path.GetFileName(path)), Color.LightGreen);
            return;
        }

        Main.NewText(error, Color.OrangeRed);
    }

    public void DeleteProfile(string profileId)
    {
        if (!JournalProfileRegistry.TryGet(profileId, out var profile) || profile.IsBuiltIn)
        {
            return;
        }

        if (!string.Equals(PendingProfileDeleteId, profileId, StringComparison.OrdinalIgnoreCase))
        {
            PendingProfileDeleteId = profileId;
            Main.NewText(
                Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileDeleteConfirm", profile.Name),
                Color.Orange);
            RefreshView();
            return;
        }

        PendingProfileDeleteId = null;
        if (!JournalProfileStorage.Delete(profile, out var error))
        {
            Main.NewText(error, Color.OrangeRed);
            RefreshView();
            return;
        }

        JournalProfileRegistry.ReloadUserProfiles();
        JournalProfileRegistry.Select(JournalProfileRegistry.Active.Id);
        SelectedClassId = JournalProfileRegistry.Active.Classes[0].Id;
        SelectedStageId = GetCurrentStageId();
        HasSelectedClass = false;
        SelectingClass = true;
        ShowingPresets = false;
        ShowingBuildBuilder = false;
        ShowingCombatBuffsPage = false;
        _buildSelections.Clear();
        _journalState?.ResetProfileNavigation();
        Main.NewText(
            Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileDeleted", profile.Name),
            Color.LightGreen);
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
        ProfileEditor = null;
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

        CoerceBuildSelections();
        RefreshView();
    }

    public void CycleOverviewPage(int direction)
    {
        if (SelectingClass || ShowingPresets)
        {
            return;
        }

        ShowingCombatBuffsPage = !ShowingCombatBuffsPage;
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

        _journalState?.Refresh(
            SelectedProfileId,
            SelectedClassId,
            SelectedStageId,
            SelectingClass,
            ShowingPresets,
            ShowingBuildBuilder,
            ShowingCombatBuffsPage,
            ProgressionModeEnabled,
            HasSelectedClass,
            SelectedItemId);
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
        _journalInterface?.Draw(Main.spriteBatch, new GameTime());
        return true;
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
                pair.Value))
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
}
