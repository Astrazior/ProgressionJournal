using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using ProgressionJournal.Data;
using ProgressionJournal.Systems;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace ProgressionJournal.UI;

public sealed class JournalUIState : UIState
{
	private UIPanel _root = null!;
	private UIText _title = null!;
	private UIText _stageLabel = null!;
	private UIText _classLabel = null!;
	private UIList _entryList = null!;
	private UITextPanel<string> _overviewTabButton = null!;
	private UITextPanel<string> _presetsTabButton = null!;
	private UITextPanel<string> _syncButton = null!;
	private UITextPanel<string> _closeButton = null!;

	public override void OnInitialize()
	{
		_root = new UIPanel();
		_root.SetPadding(14f);
		_root.BackgroundColor = new Color(16, 24, 34) * 0.98f;
		_root.BorderColor = new Color(86, 108, 132);
		Append(_root);

		_title = new UIText(string.Empty, 1.05f, true);
		_title.Left.Set(0f, 0f);
		_title.Top.Set(0f, 0f);
		_root.Append(_title);

		_closeButton = CreateButton(string.Empty, 110f, 34f, () => JournalSystem.HideView());
		_closeButton.Left.Set(-110f, 1f);
		_closeButton.Top.Set(0f, 0f);
		_root.Append(_closeButton);

		_syncButton = CreateButton(string.Empty, 160f, 34f, () => JournalSystem.SyncStageWithWorld());
		_syncButton.Left.Set(-280f, 1f);
		_syncButton.Top.Set(0f, 0f);
		_root.Append(_syncButton);

		InitializeSelectors();
		InitializeTabs();
		InitializeEntryList();
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);

		if (_root.ContainsPoint(Main.MouseScreen)) {
			Main.LocalPlayer.mouseInterface = true;
			Main.blockMouse = true;
		}

		if (Main.keyState.IsKeyDown(Keys.Escape) && Main.oldKeyState.IsKeyUp(Keys.Escape)) {
			JournalSystem.HideView();
		}
	}

	public void Refresh(CombatClass combatClass, ProgressionStageId stageId, bool showingPresets)
	{
		ApplyLayout(Main.playerInventory);
		_title.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.Title"));
		_stageLabel.SetText($"{Language.GetTextValue("Mods.ProgressionJournal.UI.Stage")}: {Language.GetTextValue(ProgressionStageCatalog.Get(stageId).LocalizationKey)}");
		_classLabel.SetText($"{Language.GetTextValue("Mods.ProgressionJournal.UI.Class")}: {Language.GetTextValue($"Mods.ProgressionJournal.Classes.{combatClass}")}");
		_syncButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.UseCurrentStage"));
		_closeButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.Close"));
		_overviewTabButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.OverviewTab"));
		_presetsTabButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.PresetsTab"));
		ApplyTabStyles(showingPresets);

		_entryList.Clear();

		if (showingPresets) {
			AppendPresets(stageId, combatClass);
			return;
		}

		AppendEntries(stageId, combatClass);
	}

	private void ApplyLayout(bool inventoryOpen)
	{
		var width = MathF.Min(640f, Main.screenWidth - 80f);
		var height = MathF.Min(580f, Main.screenHeight - 90f);
		var left = inventoryOpen
			? Math.Clamp(Main.screenWidth * 0.38f, 250f, Main.screenWidth - width - 40f)
			: (Main.screenWidth - width) * 0.5f;
		var top = MathF.Max(40f, (Main.screenHeight - height) * 0.5f);

		_root.Left.Set(left, 0f);
		_root.Top.Set(top, 0f);
		_root.Width.Set(width, 0f);
		_root.Height.Set(height, 0f);
		_root.Recalculate();
	}

	private void InitializeSelectors()
	{
		var previousStageButton = CreateButton("<", 36f, 32f, () => JournalSystem.CycleStage(-1));
		previousStageButton.Left.Set(0f, 0f);
		previousStageButton.Top.Set(52f, 0f);
		_root.Append(previousStageButton);

		_stageLabel = new UIText(string.Empty, 0.9f);
		_stageLabel.Left.Set(48f, 0f);
		_stageLabel.Top.Set(58f, 0f);
		_stageLabel.Width.Set(-96f, 1f);
		_root.Append(_stageLabel);

		var nextStageButton = CreateButton(">", 36f, 32f, () => JournalSystem.CycleStage(1));
		nextStageButton.Left.Set(-36f, 1f);
		nextStageButton.Top.Set(52f, 0f);
		_root.Append(nextStageButton);

		var previousClassButton = CreateButton("<", 36f, 32f, () => JournalSystem.CycleClass(-1));
		previousClassButton.Left.Set(0f, 0f);
		previousClassButton.Top.Set(92f, 0f);
		_root.Append(previousClassButton);

		_classLabel = new UIText(string.Empty, 0.9f);
		_classLabel.Left.Set(48f, 0f);
		_classLabel.Top.Set(98f, 0f);
		_classLabel.Width.Set(-96f, 1f);
		_root.Append(_classLabel);

		var nextClassButton = CreateButton(">", 36f, 32f, () => JournalSystem.CycleClass(1));
		nextClassButton.Left.Set(-36f, 1f);
		nextClassButton.Top.Set(92f, 0f);
		_root.Append(nextClassButton);
	}

	private void InitializeTabs()
	{
		_overviewTabButton = CreateButton(string.Empty, 150f, 34f, () => JournalSystem.ShowOverviewTab());
		_overviewTabButton.Left.Set(0f, 0f);
		_overviewTabButton.Top.Set(136f, 0f);
		_root.Append(_overviewTabButton);

		_presetsTabButton = CreateButton(string.Empty, 150f, 34f, () => JournalSystem.ShowPresetsTab());
		_presetsTabButton.Left.Set(160f, 0f);
		_presetsTabButton.Top.Set(136f, 0f);
		_root.Append(_presetsTabButton);
	}

	private void InitializeEntryList()
	{
		_entryList = new UIList();
		_entryList.Left.Set(0f, 0f);
		_entryList.Top.Set(182f, 0f);
		_entryList.Width.Set(-26f, 1f);
		_entryList.Height.Set(-182f, 1f);
		_entryList.ListPadding = 8f;
		_root.Append(_entryList);

		var scrollbar = new UIScrollbar();
		scrollbar.Left.Set(-20f, 1f);
		scrollbar.Top.Set(182f, 0f);
		scrollbar.Height.Set(-182f, 1f);
		_root.Append(scrollbar);
		_entryList.SetScrollbar(scrollbar);
	}

	private void AppendEntries(ProgressionStageId stageId, CombatClass combatClass)
	{
		var entries = JournalRepository.GetEntries(stageId, combatClass);
		var groupedEntries = entries.ToLookup(entry => entry.Evaluation.Tier);

		if (entries.Count == 0) {
			_entryList.Add(CreateSectionHeader(Language.GetTextValue("Mods.ProgressionJournal.UI.EmptyState")));
			return;
		}

		RecommendationTier[] tierOrder =
		[
			RecommendationTier.Recommended,
			RecommendationTier.Situational,
			RecommendationTier.NotRecommended,
			RecommendationTier.Useless
		];

		foreach (var tier in tierOrder) {
			var tierEntries = groupedEntries[tier].ToArray();

			if (tierEntries.Length == 0) {
				continue;
			}

			_entryList.Add(CreateSectionHeader(Language.GetTextValue($"Mods.ProgressionJournal.Tiers.{tier}")));

			foreach (var entry in tierEntries) {
				_entryList.Add(new JournalEntryPanel(entry));
			}
		}
	}

	private void AppendPresets(ProgressionStageId stageId, CombatClass combatClass)
	{
		var presets = JournalRepository.GetPresets(stageId, combatClass);

		if (presets.Count == 0) {
			_entryList.Add(CreateSectionHeader(Language.GetTextValue("Mods.ProgressionJournal.UI.PresetsEmptyState")));
			return;
		}

		foreach (var preset in presets) {
			_entryList.Add(new JournalPresetPanel(preset));
		}
	}

	private void ApplyTabStyles(bool showingPresets)
	{
		StyleTabButton(_overviewTabButton, !showingPresets);
		StyleTabButton(_presetsTabButton, showingPresets);
	}

	private static void StyleTabButton(UITextPanel<string> button, bool active)
	{
		button.BackgroundColor = active ? new Color(57, 95, 68) : new Color(44, 62, 84);
		button.BorderColor = active ? new Color(132, 190, 144) : new Color(96, 124, 160);
	}

	private static UITextPanel<string> CreateButton(string text, float width, float height, Action onClick)
	{
		var button = new UITextPanel<string>(text, 0.85f, false);
		button.Width.Set(width, 0f);
		button.Height.Set(height, 0f);
		button.BackgroundColor = new Color(44, 62, 84);
		button.BorderColor = new Color(96, 124, 160);
		button.OnLeftClick += (_, _) => onClick();
		return button;
	}

	private static UIText CreateSectionHeader(string text)
	{
		var header = new UIText(text, 0.86f, true);
		header.Height.Set(26f, 0f);
		header.TextColor = new Color(240, 220, 140);
		return header;
	}

	private static JournalSystem JournalSystem => ModContent.GetInstance<JournalSystem>();
}
