using System.Collections.Generic;
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

    public int SelectedItemId { get; private set; }

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
        _journalState?.ResetLayout();
        _journalInterface?.SetState(_journalState);
        RefreshView();
    }

    public void HideView()
    {
        Visible = false;
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
        RefreshView();
    }

    public void ShowClassSelection()
    {
        SelectingClass = true;
        RefreshView();
    }

    public void CycleStage(int direction)
    {
        SelectedStage = Cycle(StageOrder, SelectedStage, direction);
        RefreshView();
    }

    public void SelectStage(ProgressionStageId stageId)
    {
        SelectedStage = stageId;
        RefreshView();
    }

    public void ShowOverviewTab()
    {
        SelectingClass = false;
        ShowingPresets = false;
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

        ShowingCombatBuffsPage = direction > 0 ? !ShowingCombatBuffsPage : !ShowingCombatBuffsPage;
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

        _journalState?.Refresh(
            SelectedClass,
            SelectedStage,
            SelectingClass,
            ShowingPresets,
            ShowingCombatBuffsPage,
            HasSelectedClass,
            SelectedItemId);
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
