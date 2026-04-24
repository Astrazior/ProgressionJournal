using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace ProgressionJournal.Systems;

public sealed class JournalSystem : ModSystem
{
    private static readonly IReadOnlyList<CombatClass> ClassOrder = JournalOrdering.ClassSelection;
    private static readonly IReadOnlyList<ProgressionStageId> StageOrder = JournalOrdering.StageSelection;

    private UserInterface? _journalInterface;
    private JournalUiState? _journalState;
    private UserInterface? _buttonInterface;
    private JournalButtonUiState? _buttonState;

    public bool Visible { get; private set; }

    public bool ShowingPresets { get; private set; }

    public bool ShowingCombatBuffsPage { get; private set; }

    public bool SelectingClass { get; private set; } = true;

    public bool HasSelectedClass { get; private set; }

    public CombatClass SelectedClass { get; private set; } = CombatClass.Melee;

    public ProgressionStageId SelectedStage { get; private set; } = ProgressionStageCatalog.GetCurrentStageId();

    public bool ProgressionModeEnabled { get; private set; } = true;

    public int SelectedItemId { get; private set; }

    public string? ActiveBuildSlotKey { get; private set; }

    private readonly Dictionary<string, int> _buildSelections = new();

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

    public override void PostUpdateInput()
    {
        if (Main.dedServ)
        {
            return;
        }

        if (ProgressionJournal.ToggleJournalKeybind?.JustPressed == true)
        {
            ToggleView();
        }
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
        ActiveBuildSlotKey = null;
        CoerceSelectedStage();
        _journalInterface?.SetState(_journalState);
        RefreshView();
    }

    public void HideView()
    {
        Visible = false;
        ActiveBuildSlotKey = null;
        _journalInterface?.SetState(null);
    }

    public void CycleClass(int direction)
    {
        SelectedClass = Cycle(ClassOrder, SelectedClass, direction);
        RefreshView();
    }

    public void SelectClass(CombatClass combatClass)
    {
        SelectedClass = combatClass;
        HasSelectedClass = true;
        SelectingClass = false;
        ShowingPresets = false;
        ActiveBuildSlotKey = null;
        CoerceBuildSelections();
        RefreshView();
    }

    public void ShowClassSelection()
    {
        SelectingClass = true;
        RefreshView();
    }

    public void CycleStage(int direction)
    {
        SelectedStage = Cycle(GetAvailableStageOrder(), SelectedStage, direction);
        RefreshView();
    }

    public void SelectStage(ProgressionStageId stageId)
    {
        if (!ProgressionStageCatalog.IsAvailable(stageId, ProgressionModeEnabled))
        {
            return;
        }

        SelectedStage = stageId;
        ActiveBuildSlotKey = null;
        CoerceBuildSelections();
        RefreshView();
    }

    public void ToggleProgressionMode()
    {
        ProgressionModeEnabled = !ProgressionModeEnabled;
        CoerceSelectedStage();
        ActiveBuildSlotKey = null;
        CoerceBuildSelections();
        RefreshView();
    }

    public void ShowOverviewTab()
    {
        SelectingClass = false;
        ShowingPresets = false;
        ActiveBuildSlotKey = null;
        RefreshView();
    }

    public void ShowPresetsTab()
    {
        SelectingClass = false;
        ShowingPresets = true;
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
            SelectedClass,
            SelectedStage,
            SelectingClass,
            ShowingPresets,
            ShowingCombatBuffsPage,
            ProgressionModeEnabled,
            HasSelectedClass,
            SelectedItemId);
    }

    public void OpenBuildSlot(string slotKey)
    {
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

    public void ClearBuildItem(string slotKey)
    {
        if (!_buildSelections.Remove(slotKey))
        {
            return;
        }

        if (string.Equals(ActiveBuildSlotKey, slotKey, System.StringComparison.OrdinalIgnoreCase))
        {
            ActiveBuildSlotKey = null;
        }

        RefreshView();
    }

    public int GetSelectedBuildItem(string slotKey)
    {
        return _buildSelections.GetValueOrDefault(slotKey, ItemID.None);
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
            .Where(pair => !string.Equals(pair.Key, slotKey, System.StringComparison.OrdinalIgnoreCase)
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
        ActiveBuildSlotKey = null;
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

    private static bool ShouldDrawJournalButton => !Main.gameMenu && Main.playerInventory;

    private IReadOnlyList<ProgressionStageId> GetAvailableStageOrder()
    {
        return ProgressionModeEnabled ? ProgressionStageCatalog.GetAvailableStageIds(true) : StageOrder;
    }

    private void CoerceSelectedStage()
    {
        if (ProgressionStageCatalog.IsAvailable(SelectedStage, ProgressionModeEnabled))
        {
            return;
        }

        SelectedStage = ProgressionStageCatalog.GetCurrentStageId();
    }

    private void CoerceBuildSelections()
    {
        if (!HasSelectedClass || _buildSelections.Count == 0)
        {
            return;
        }

        var invalidSelections = _buildSelections
            .Where(pair => !JournalRepository.IsBuildSelectionValid(SelectedStage, SelectedClass, pair.Key, pair.Value))
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
                .OrderBy(static pair => pair.Key, System.StringComparer.OrdinalIgnoreCase)
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
}
