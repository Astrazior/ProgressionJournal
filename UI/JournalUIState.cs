using System;
using System.Collections.Generic;
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
	private const int EntrySlotsPerRow = 6;
	private const float EntrySpacing = 4f;
	private const float BlockHorizontalPadding = 14f;
	private const float BlockVerticalPadding = 12f;
	private const float BlockTitleHeight = 28f;
	private const float RowHeight = 56f;
	private const float RowSpacing = 6f;

	private static readonly CombatClass[] ClassOrder =
	[
		CombatClass.Melee,
		CombatClass.Ranged,
		CombatClass.Magic,
		CombatClass.Summoner
	];

	private UIPanel _root = null!;
	private UIPanel _headerPanel = null!;
	private UIPanel _controlsPanel = null!;
	private UIPanel _contentPanel = null!;
	private UIText _title = null!;
	private UIText _subtitle = null!;
	private JournalTextButton _syncButton = null!;
	private JournalTextButton _closeButton = null!;
	private JournalTextButton _previousStageButton = null!;
	private JournalTextButton _nextStageButton = null!;
	private JournalTextButton _classButton = null!;
	private JournalTextButton _overviewTabButton = null!;
	private JournalTextButton _presetsTabButton = null!;
	private UIText _stageLabel = null!;
	private UIText _contentTitle = null!;
	private UIText _contentDescription = null!;
	private UIElement _classSelectionContainer = null!;
	private UIList _entryList = null!;
	private UIScrollbar _scrollbar = null!;
	private bool _layoutInitialized;
	private int _layoutScreenWidth;
	private int _layoutScreenHeight;

	public override void OnInitialize()
	{
		_root = new UIPanel();
		_root.SetPadding(0f);
		_root.HAlign = 0.5f;
		_root.VAlign = 0.5f;
		_root.BackgroundColor = new Color(12, 20, 30) * 0.98f;
		_root.BorderColor = new Color(78, 101, 124);
		Append(_root);

		InitializeHeader();
		InitializeControls();
		InitializeContent();
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

	public void Refresh(CombatClass combatClass, ProgressionStageId stageId, bool selectingClass, bool showingPresets)
	{
		EnsureLayout();

		string className = Language.GetTextValue($"Mods.ProgressionJournal.Classes.{combatClass}");
		string stageName = TrimForUi(Language.GetTextValue(ProgressionStageCatalog.Get(stageId).LocalizationKey), 18);

		_title.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.Title"));
		_subtitle.SetText(string.Empty);
		_syncButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.UseCurrentStage"));
		_closeButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.Close"));
		_stageLabel.SetText(stageName);
		_classButton.SetText($"{Language.GetTextValue("Mods.ProgressionJournal.UI.Class")}: {className}");
		_overviewTabButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.OverviewTab"));
		_presetsTabButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.PresetsTab"));

		StyleTabButton(_classButton, selectingClass);
		StyleTabButton(_overviewTabButton, !selectingClass && !showingPresets);
		StyleTabButton(_presetsTabButton, !selectingClass && showingPresets);

		_entryList.Clear();
		_classSelectionContainer.RemoveAllChildren();
		SwitchContentMode(selectingClass);

		if (selectingClass) {
			_contentTitle.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.ClassPageTitle"));
			_contentDescription.SetText(string.Empty);
			PopulateClassSelection(combatClass);
		}
		else if (showingPresets) {
			var presets = JournalRepository.GetPresets(stageId, combatClass);
			_contentTitle.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.PresetsHeadline"));
			_contentDescription.SetText(string.Empty);
			AppendPresets(presets);
		}
		else {
			var entries = JournalRepository.GetEntries(stageId, combatClass);
			_contentTitle.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.OverviewHeadline"));
			_contentDescription.SetText(string.Empty);
			AppendEntries(entries);
		}

		// UIText and dynamic child trees need an explicit layout pass, otherwise the first
		// frame can use stale geometry and the window appears to jump on the next refresh.
		Recalculate();
	}

	public void ResetLayout()
	{
		_layoutInitialized = false;
	}

	private void EnsureLayout()
	{
		if (_layoutInitialized && _layoutScreenWidth == Main.screenWidth && _layoutScreenHeight == Main.screenHeight) {
			return;
		}

		var width = MathF.Min(760f, Main.screenWidth - 48f);
		var height = MathF.Min(620f, Main.screenHeight - 48f);
		var topOffset = Main.screenHeight >= 720 ? -8f : 0f;

		_root.Left.Set(0f, 0f);
		_root.Top.Set(topOffset, 0f);
		_root.Width.Set(width, 0f);
		_root.Height.Set(height, 0f);
		_root.Recalculate();
		_layoutInitialized = true;
		_layoutScreenWidth = Main.screenWidth;
		_layoutScreenHeight = Main.screenHeight;
	}

	private void InitializeHeader()
	{
		_headerPanel = CreatePanel();
		_headerPanel.Left.Set(12f, 0f);
		_headerPanel.Top.Set(12f, 0f);
		_headerPanel.Width.Set(-24f, 1f);
		_headerPanel.Height.Set(68f, 0f);
		_root.Append(_headerPanel);

		_title = new UIText(string.Empty, 0.72f, true);
		_title.Left.Set(14f, 0f);
		_title.VAlign = 0.5f;
		_headerPanel.Append(_title);

		_subtitle = new UIText(string.Empty, 0.36f);
		_subtitle.Left.Set(14f, 0f);
		_subtitle.Top.Set(34f, 0f);
		_subtitle.TextColor = new Color(198, 214, 229);
		_headerPanel.Append(_subtitle);

		_syncButton = CreateButton(string.Empty, 138f, 32f, () => JournalSystem.SyncStageWithWorld(), 0.56f);
		_syncButton.Left.Set(-250f, 1f);
		_syncButton.Top.Set(16f, 0f);
		_headerPanel.Append(_syncButton);

		_closeButton = CreateButton(string.Empty, 102f, 32f, () => JournalSystem.HideView(), 0.56f);
		_closeButton.Left.Set(-108f, 1f);
		_closeButton.Top.Set(16f, 0f);
		_closeButton.BackgroundColor = new Color(76, 48, 48);
		_closeButton.BorderColor = new Color(152, 94, 94);
		_headerPanel.Append(_closeButton);
	}

	private void InitializeControls()
	{
		_controlsPanel = CreatePanel();
		_controlsPanel.Left.Set(12f, 0f);
		_controlsPanel.Top.Set(88f, 0f);
		_controlsPanel.Width.Set(-24f, 1f);
		_controlsPanel.Height.Set(74f, 0f);
		_root.Append(_controlsPanel);

		_previousStageButton = CreateButton("<", 34f, 30f, () => JournalSystem.CycleStage(-1), 0.66f);
		_previousStageButton.Left.Set(10f, 0f);
		_previousStageButton.Top.Set(7f, 0f);
		_controlsPanel.Append(_previousStageButton);

		_stageLabel = new UIText(string.Empty, 0.5f, true);
		_stageLabel.Left.Set(48f, 0f);
		_stageLabel.Top.Set(10f, 0f);
		_stageLabel.Width.Set(280f, 0f);
		_controlsPanel.Append(_stageLabel);

		_nextStageButton = CreateButton(">", 34f, 30f, () => JournalSystem.CycleStage(1), 0.66f);
		_nextStageButton.Left.Set(336f, 0f);
		_nextStageButton.Top.Set(7f, 0f);
		_controlsPanel.Append(_nextStageButton);

		_classButton = CreateButton(string.Empty, 150f, 30f, () => JournalSystem.ShowClassSelection(), 0.58f);
		_classButton.Left.Set(10f, 0f);
		_classButton.Top.Set(39f, 0f);
		_controlsPanel.Append(_classButton);

		_overviewTabButton = CreateButton(string.Empty, 122f, 30f, () => JournalSystem.ShowOverviewTab(), 0.58f);
		_overviewTabButton.Left.Set(170f, 0f);
		_overviewTabButton.Top.Set(39f, 0f);
		_controlsPanel.Append(_overviewTabButton);

		_presetsTabButton = CreateButton(string.Empty, 108f, 30f, () => JournalSystem.ShowPresetsTab(), 0.58f);
		_presetsTabButton.Left.Set(302f, 0f);
		_presetsTabButton.Top.Set(39f, 0f);
		_controlsPanel.Append(_presetsTabButton);
	}

	private void InitializeContent()
	{
		_contentPanel = CreatePanel();
		_contentPanel.Left.Set(12f, 0f);
		_contentPanel.Top.Set(170f, 0f);
		_contentPanel.Width.Set(-24f, 1f);
		_contentPanel.Height.Set(-182f, 1f);
		_root.Append(_contentPanel);

		_contentTitle = new UIText(string.Empty, 0.48f, true);
		_contentTitle.Left.Set(14f, 0f);
		_contentTitle.Top.Set(10f, 0f);
		_contentPanel.Append(_contentTitle);

		_contentDescription = new UIText(string.Empty, 0.38f);
		_contentDescription.Left.Set(14f, 0f);
		_contentDescription.Top.Set(36f, 0f);
		_contentDescription.TextColor = new Color(198, 214, 229);
		_contentPanel.Append(_contentDescription);

		_classSelectionContainer = new UIElement();
		_classSelectionContainer.Left.Set(14f, 0f);
		_classSelectionContainer.Top.Set(68f, 0f);
		_classSelectionContainer.Width.Set(-28f, 1f);
		_classSelectionContainer.Height.Set(-82f, 1f);
		_contentPanel.Append(_classSelectionContainer);

		_entryList = new UIList();
		_entryList.Left.Set(14f, 0f);
		_entryList.Top.Set(68f, 0f);
		_entryList.Width.Set(-38f, 1f);
		_entryList.Height.Set(-82f, 1f);
		_entryList.ListPadding = 6f;
		_contentPanel.Append(_entryList);

		_scrollbar = new UIScrollbar();
		_scrollbar.Left.Set(-18f, 1f);
		_scrollbar.Top.Set(68f, 0f);
		_scrollbar.Height.Set(-82f, 1f);
		_contentPanel.Append(_scrollbar);
		_entryList.SetScrollbar(_scrollbar);
	}

	private void PopulateClassSelection(CombatClass selectedClass)
	{
		float buttonWidth = 0.5f;
		float buttonHeight = 58f;
		float top = 0f;
		int index = 0;

		foreach (var combatClass in ClassOrder) {
			var capturedClass = combatClass;
			var panel = CreateClassSelectionButton(capturedClass, selectedClass == capturedClass, buttonHeight);
			panel.Left.Set(index % 2 == 0 ? 0f : 12f, index % 2 == 0 ? 0f : buttonWidth);
			panel.Top.Set(top, 0f);
			panel.Width.Set(-6f, buttonWidth);
			panel.OnLeftClick += (_, _) => JournalSystem.SelectClass(capturedClass);
			_classSelectionContainer.Append(panel);

			index++;
			if (index % 2 == 0) {
				top += buttonHeight + 12f;
			}
		}
	}

	private void AppendEntries(IReadOnlyList<JournalStageEntry> entries)
	{
		if (entries.Count == 0) {
			_entryList.Add(CreateSectionHeader(Language.GetTextValue("Mods.ProgressionJournal.UI.EmptyState")));
			return;
		}

		var recommendedTiers = new[] { RecommendationTier.Recommended, RecommendationTier.Situational };
		if (HasEntriesForAnyTier(entries, recommendedTiers)) {
			_entryList.Add(CreateRecommendationBlock(
				Language.GetTextValue("Mods.ProgressionJournal.UI.RecommendedBlock"),
				GetEntriesForTiers(entries, recommendedTiers),
				new Color(26, 48, 37),
				new Color(108, 176, 128)));
		}

		var notRecommendedTiers = new[] { RecommendationTier.NotRecommended, RecommendationTier.Useless };
		if (HasEntriesForAnyTier(entries, notRecommendedTiers)) {
			_entryList.Add(CreateRecommendationBlock(
				Language.GetTextValue("Mods.ProgressionJournal.UI.NotRecommendedBlock"),
				GetEntriesForTiers(entries, notRecommendedTiers),
				new Color(52, 34, 34),
				new Color(176, 116, 116)));
		}
	}

	private void AppendPresets(IReadOnlyList<JournalPreset> presets)
	{
		if (presets.Count == 0) {
			_entryList.Add(CreateSectionHeader(Language.GetTextValue("Mods.ProgressionJournal.UI.PresetsEmptyState")));
			return;
		}

		foreach (var preset in presets) {
			_entryList.Add(new JournalPresetPanel(preset));
		}
	}

	private static IEnumerable<JournalStageEntry[]> ChunkEntries(IReadOnlyList<JournalStageEntry> entries, int maxSlotsPerRow)
	{
		var row = new List<JournalStageEntry>();
		int occupiedSlots = 0;

		foreach (var entry in entries) {
			int entrySlots = Math.Max(1, entry.Entry.ItemIds.Count);

			if (row.Count > 0 && occupiedSlots + entrySlots > maxSlotsPerRow) {
				yield return row.ToArray();
				row.Clear();
				occupiedSlots = 0;
			}

			row.Add(entry);
			occupiedSlots += entrySlots;
		}

		if (row.Count > 0) {
			yield return row.ToArray();
		}
	}

	private static bool HasEntriesForAnyTier(IReadOnlyList<JournalStageEntry> entries, RecommendationTier[] tiers)
	{
		return entries.Any(entry => tiers.Contains(entry.Evaluation.Tier));
	}

	private static JournalStageEntry[] GetEntriesForTiers(IReadOnlyList<JournalStageEntry> entries, RecommendationTier[] tiers)
	{
		return entries
			.Where(entry => tiers.Contains(entry.Evaluation.Tier))
			.OrderBy(entry => Array.IndexOf(tiers, entry.Evaluation.Tier))
			.ThenBy(entry => entry.Entry.Category)
			.ThenBy(entry => entry.Entry.GetDisplayName(), StringComparer.CurrentCultureIgnoreCase)
			.ToArray();
	}

	private static UIPanel CreateClassSelectionButton(CombatClass combatClass, bool active, float height)
	{
		var panel = CreatePanel();
		panel.Height.Set(height, 0f);
		panel.BackgroundColor = active ? new Color(49, 82, 61) : new Color(26, 38, 52);
		panel.BorderColor = active ? new Color(128, 192, 146) : new Color(88, 115, 142);

		var title = new UIText(Language.GetTextValue($"Mods.ProgressionJournal.Classes.{combatClass}"), 0.5f, true);
		title.Left.Set(14f, 0f);
		title.VAlign = 0.5f;
		panel.Append(title);

		return panel;
	}

	private static UIPanel CreatePanel()
	{
		var panel = new UIPanel();
		panel.SetPadding(0f);
		panel.BackgroundColor = new Color(21, 33, 45);
		panel.BorderColor = new Color(88, 115, 142);
		return panel;
	}

	private static JournalTextButton CreateButton(string text, float width, float height, Action onClick, float textScale = 0.48f)
	{
		var button = new JournalTextButton(text, textScale, onClick);
		button.Width.Set(width, 0f);
		button.Height.Set(height, 0f);
		return button;
	}

	private static void StyleTabButton(JournalTextButton button, bool active)
	{
		button.BackgroundColor = active ? new Color(58, 100, 71) : new Color(38, 54, 73);
		button.BorderColor = active ? new Color(130, 194, 149) : new Color(100, 127, 156);
		button.SetTextColor(new Color(226, 233, 240));
	}

	private static UIText CreateSectionHeader(string text)
	{
		var header = new UIText(text, 0.56f, true);
		header.Height.Set(22f, 0f);
		header.VAlign = 0.5f;
		header.TextColor = new Color(240, 220, 140);
		return header;
	}

	private static UIPanel CreateRecommendationBlock(
		string title,
		IReadOnlyList<JournalStageEntry> entries,
		Color backgroundColor,
		Color borderColor)
	{
		var block = CreatePanel();
		block.Width.Set(0f, 1f);
		block.SetPadding(0f);
		block.BackgroundColor = backgroundColor;
		block.BorderColor = borderColor;

		float top = BlockVerticalPadding;

		var titleText = new UIText(title, 0.5f, true);
		titleText.Left.Set(BlockHorizontalPadding, 0f);
		titleText.Top.Set(top, 0f);
		titleText.TextColor = new Color(235, 239, 242);
		block.Append(titleText);
		top += BlockTitleHeight;

		foreach (var rowEntries in ChunkEntries(entries, EntrySlotsPerRow)) {
			var row = CreateSlotRow(rowEntries);
			row.Left.Set(BlockHorizontalPadding, 0f);
			row.Top.Set(top, 0f);
			block.Append(row);
			top += RowHeight + RowSpacing;
		}

		block.Height.Set(top + 4f, 0f);
		return block;
	}

	private static UIElement CreateSlotRow(JournalStageEntry[] entries)
	{
		var row = new UIElement();
		row.Width.Set(GetRowWidth(entries), 0f);
		row.Height.Set(RowHeight, 0f);

		float left = 0f;

		for (int index = 0; index < entries.Length; index++) {
			var slot = new JournalEntrySlot(entries[index]);
			slot.Left.Set(left, 0f);
			row.Append(slot);
			left += JournalEntrySlot.GetVisualWidth(entries[index].Entry.ItemIds.Count) + EntrySpacing;
		}

		return row;
	}

	private static float GetRowWidth(IReadOnlyList<JournalStageEntry> entries)
	{
		if (entries.Count <= 0) {
			return 0f;
		}

		float totalWidth = 0f;

		for (int index = 0; index < entries.Count; index++) {
			totalWidth += JournalEntrySlot.GetVisualWidth(entries[index].Entry.ItemIds.Count);

			if (index < entries.Count - 1) {
				totalWidth += EntrySpacing;
			}
		}

		return totalWidth;
	}

	private static string TrimForUi(string text, int maxLength)
	{
		if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength) {
			return text;
		}

		return text[..(maxLength - 3)].TrimEnd() + "...";
	}

	private void SwitchContentMode(bool selectingClass)
	{
		if (selectingClass) {
			if (_entryList.Parent is not null) {
				_contentPanel.RemoveChild(_entryList);
			}

			if (_scrollbar.Parent is not null) {
				_contentPanel.RemoveChild(_scrollbar);
			}

			if (_classSelectionContainer.Parent is null) {
				_contentPanel.Append(_classSelectionContainer);
			}

			return;
		}

		if (_classSelectionContainer.Parent is not null) {
			_contentPanel.RemoveChild(_classSelectionContainer);
		}

		if (_entryList.Parent is null) {
			_contentPanel.Append(_entryList);
		}

		if (_scrollbar.Parent is null) {
			_contentPanel.Append(_scrollbar);
		}
	}

	private static JournalSystem JournalSystem => ModContent.GetInstance<JournalSystem>();
}
