using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using ProgressionJournal.Common.Data;
using ProgressionJournal.Common.Progression;
using ProgressionJournal.Common.UI;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace ProgressionJournal.Common.Systems;

public sealed class ProgressionJournalUISystem : ModSystem
{
	private static readonly CombatClass[] ClassOrder =
	{
		CombatClass.Melee,
		CombatClass.Ranged,
		CombatClass.Magic,
		CombatClass.Summoner
	};

	private static readonly ProgressionStageId[] StageOrder =
		ProgressionStageCatalog.All.Select(stage => stage.Id).ToArray();

	private UserInterface? _userInterface;
	private ProgressionJournalUIState? _uiState;
	private UserInterface? _inventoryButtonInterface;
	private InventoryJournalButtonState? _inventoryButtonState;

	public bool Visible { get; private set; }

	public CombatClass SelectedClass { get; private set; } = CombatClass.Melee;

	public ProgressionStageId SelectedStage { get; private set; } = ProgressionStageCatalog.GetCurrentStageId();

	public override void Load()
	{
		if (Main.dedServ) {
			return;
		}

		_userInterface = new UserInterface();
		_uiState = new ProgressionJournalUIState();
		_uiState.Activate();

		_inventoryButtonInterface = new UserInterface();
		_inventoryButtonState = new InventoryJournalButtonState();
		_inventoryButtonState.Activate();
		_inventoryButtonInterface.SetState(_inventoryButtonState);
	}

	public override void UpdateUI(GameTime gameTime)
	{
		if (Visible) {
			_userInterface?.Update(gameTime);
		}

		if (ShouldDrawInventoryButton) {
			_inventoryButtonInterface?.Update(gameTime);
		}
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		if (ShouldDrawInventoryButton && _inventoryButtonInterface is not null) {
			int inventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));

			if (inventoryIndex >= 0) {
				layers.Insert(inventoryIndex + 1, new LegacyGameInterfaceLayer(
					"ProgressionJournal: Inventory Button",
					DrawInventoryButtonInterface,
					InterfaceScaleType.UI));
			}
		}

		if (!Visible || _userInterface is null) {
			return;
		}

		int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
		if (mouseTextIndex < 0) {
			mouseTextIndex = layers.Count;
		}

		layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
			"ProgressionJournal: Journal",
			DrawInterface,
			InterfaceScaleType.UI));
	}

	public override void PostUpdateInput()
	{
		if (Main.dedServ) {
			return;
		}

		if (ProgressionJournal.ToggleJournalKeybind?.JustPressed == true) {
			ToggleView(syncStage: true);
		}
	}

	public void ToggleView(bool syncStage = false)
	{
		if (Visible) {
			HideView();
			return;
		}

		ShowView(syncStage);
	}

	public void ShowView(bool syncStage = false)
	{
		if (syncStage) {
			SelectedStage = ProgressionStageCatalog.GetCurrentStageId();
		}

		Visible = true;
		_userInterface?.SetState(_uiState);
		RefreshView();
	}

	public void HideView()
	{
		Visible = false;
		_userInterface?.SetState(null);
	}

	public void SyncStageWithWorld()
	{
		SelectedStage = ProgressionStageCatalog.GetCurrentStageId();
		RefreshView();
	}

	public void CycleClass(int direction)
	{
		SelectedClass = Cycle(ClassOrder, SelectedClass, direction);
		RefreshView();
	}

	public void CycleStage(int direction)
	{
		SelectedStage = Cycle(StageOrder, SelectedStage, direction);
		RefreshView();
	}

	public void RefreshView()
	{
		if (!Visible) {
			return;
		}

		_uiState?.Refresh(SelectedClass, SelectedStage);
	}

	private bool DrawInterface()
	{
		_userInterface?.Draw(Main.spriteBatch, new GameTime());
		return true;
	}

	private bool DrawInventoryButtonInterface()
	{
		_inventoryButtonInterface?.Draw(Main.spriteBatch, new GameTime());
		return true;
	}

	private static bool ShouldDrawInventoryButton => !Main.gameMenu && Main.playerInventory;

	private static T Cycle<T>(T[] values, T current, int direction)
	{
		int currentIndex = Array.IndexOf(values, current);
		int nextIndex = (currentIndex + direction + values.Length) % values.Length;
		return values[nextIndex];
	}
}
