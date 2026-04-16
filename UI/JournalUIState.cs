using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using ProgressionJournal.Data;
using ProgressionJournal.Systems;
using Terraria;
using Terraria.GameContent;
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
	private const float CategorySpacing = 8f;
	private const float OuterPadding = 12f;
	private const float PanelGap = 12f;
	private const float HeaderHeight = 72f;
	private const float HeaderTitleTop = 18f;
	private const float HeaderTabsTop = -12f;
	private const float HeaderTabsLeft = 18f;
	private const float HeaderTabsGap = 8f;
	private const float StagePanelWidth = 300f;
	private const float TopTabsHeight = 40f;
	private const float SyncTabWidth = 210f;
	private const float CloseTabWidth = 112f;
	private const float ActionTabHeight = 32f;
	private const float MinSingleColumnStageButtonHeight = 40f;
	private const float StageButtonGap = 6f;
	private const float StageButtonColumnGap = 8f;
	private const float StageButtonTextScale = 0.9f;
	private const float MinStageButtonTextScale = 0.76f;
	private const float StageButtonTextScaleStep = 0.02f;
	private const float StageButtonTextHorizontalPadding = 10f;

	private static readonly CombatClass[] ClassOrder =
	[
		CombatClass.Melee,
		CombatClass.Ranged,
		CombatClass.Magic,
		CombatClass.Summoner
	];

	private static readonly ProgressionStageId[] StageOrder =
		ProgressionStageCatalog.All.Select(stage => stage.Id).ToArray();

	private static readonly JournalItemCategory[] EntryCategoryOrder =
	[
		JournalItemCategory.Weapon,
		JournalItemCategory.ClassSpecific,
		JournalItemCategory.Armor,
		JournalItemCategory.Accessory
	];

	private readonly Dictionary<ProgressionStageId, JournalTextButton> _stageButtons = new();
	private UIPanel _root = null!;
	private UIPanel _stagePanel = null!;
	private UIPanel _mainPanel = null!;
	private UIElement _contentTabsPanel = null!;
	private UIPanel _contentPanel = null!;
	private UIText _title = null!;
	private UIText _stagePanelTitle = null!;
	private JournalTextButton _syncButton = null!;
	private JournalTextButton _closeButton = null!;
	private JournalTextButton _classButton = null!;
	private JournalTextButton _overviewTabButton = null!;
	private JournalTextButton _presetsTabButton = null!;
	private UIText _contentTitle = null!;
	private UIText _contentDescription = null!;
	private UIElement _stageListContainer = null!;
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
		InitializeStagePanel();
		InitializeMainPanel();
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

		_title.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.Title"));
		_syncButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.UseCurrentStage"));
		_closeButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.Close"));
		_stagePanelTitle.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.StageSelectorTitle"));
		_classButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.Class"));
		_overviewTabButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.OverviewTab"));
		_presetsTabButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.PresetsTab"));

		StyleHeaderButton(_syncButton, stageId == ProgressionStageCatalog.GetCurrentStageId(), false);
		StyleHeaderButton(_closeButton, false, true);
		StyleTabButton(_classButton, selectingClass);
		StyleTabButton(_overviewTabButton, !selectingClass && !showingPresets);
		StyleTabButton(_presetsTabButton, !selectingClass && showingPresets);

		foreach (var stage in ProgressionStageCatalog.All) {
			var button = _stageButtons[stage.Id];
			ApplyStageButtonText(button, Language.GetTextValue(stage.LocalizationKey));
			StyleStageButton(button, stage.Id == stageId);
		}

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
			string className = Language.GetTextValue($"Mods.ProgressionJournal.Classes.{combatClass}");
			string stageName = Language.GetTextValue(ProgressionStageCatalog.Get(stageId).LocalizationKey);
			_contentTitle.SetText($"{className} • {stageName}");
			_contentDescription.SetText(string.Empty);
			AppendEntries(entries);
		}

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

		var width = MathF.Min(1040f, Main.screenWidth - 32f);
		var height = MathF.Min(680f, Main.screenHeight - 48f);
		var topOffset = Main.screenHeight >= 720 ? -8f : 0f;

		_root.Left.Set(0f, 0f);
		_root.Top.Set(topOffset, 0f);
		_root.Width.Set(width, 0f);
		_root.Height.Set(height, 0f);
		_root.Recalculate();
		LayoutStageButtons();
		_layoutInitialized = true;
		_layoutScreenWidth = Main.screenWidth;
		_layoutScreenHeight = Main.screenHeight;
	}

	private void InitializeHeader()
	{
		_syncButton = CreateButton(string.Empty, SyncTabWidth, ActionTabHeight, () => JournalSystem.SyncStageWithWorld(), 0.52f);
		_syncButton.Left.Set(HeaderTabsLeft, 0f);
		_syncButton.Top.Set(HeaderTabsTop, 0f);
		_root.Append(_syncButton);

		_closeButton = CreateButton(string.Empty, CloseTabWidth, ActionTabHeight, () => JournalSystem.HideView(), 0.52f);
		_closeButton.Left.Set(-(CloseTabWidth + HeaderTabsLeft), 1f);
		_closeButton.Top.Set(HeaderTabsTop, 0f);
		_root.Append(_closeButton);

		_title = new UIText(string.Empty, 0.82f, true) {
			HAlign = 0.5f,
			VAlign = 0f
		};
		_title.Top.Set(HeaderTitleTop, 0f);
		_title.TextColor = new Color(236, 240, 245);
		_root.Append(_title);
	}

	private void InitializeStagePanel()
	{
		_stagePanel = CreatePanel();
		_stagePanel.Left.Set(OuterPadding, 0f);
		_stagePanel.Top.Set(HeaderHeight + OuterPadding, 0f);
		_stagePanel.Width.Set(StagePanelWidth, 0f);
		_stagePanel.Height.Set(-(HeaderHeight + OuterPadding * 2f), 1f);
		_root.Append(_stagePanel);

		_stagePanelTitle = new UIText(string.Empty, 0.52f, true);
		_stagePanelTitle.Left.Set(14f, 0f);
		_stagePanelTitle.Top.Set(12f, 0f);
		_stagePanel.Append(_stagePanelTitle);

		_stageListContainer = new UIElement();
		_stageListContainer.Left.Set(12f, 0f);
		_stageListContainer.Top.Set(40f, 0f);
		_stageListContainer.Width.Set(-24f, 1f);
		_stageListContainer.Height.Set(-52f, 1f);
		_stagePanel.Append(_stageListContainer);

		foreach (var stage in ProgressionStageCatalog.All) {
			var capturedStage = stage.Id;
			var button = CreateButton(string.Empty, 0f, 44f, () => JournalSystem.SelectStage(capturedStage), StageButtonTextScale);
			button.Left.Set(0f, 0f);
			button.Width.Set(0f, 1f);
			_stageListContainer.Append(button);
			_stageButtons[capturedStage] = button;
		}
	}

	private void InitializeMainPanel()
	{
		_mainPanel = CreatePanel();
		_mainPanel.Left.Set(OuterPadding + StagePanelWidth + PanelGap, 0f);
		_mainPanel.Top.Set(HeaderHeight + OuterPadding, 0f);
		_mainPanel.Width.Set(-(OuterPadding * 2f + StagePanelWidth + PanelGap), 1f);
		_mainPanel.Height.Set(-(HeaderHeight + OuterPadding * 2f), 1f);
		_root.Append(_mainPanel);

		_contentTabsPanel = new UIElement();
		_contentTabsPanel.Left.Set(12f, 0f);
		_contentTabsPanel.Top.Set(12f, 0f);
		_contentTabsPanel.Width.Set(-24f, 1f);
		_contentTabsPanel.Height.Set(TopTabsHeight, 0f);
		_mainPanel.Append(_contentTabsPanel);

		const float tabGap = 12f;
		float widthOffset = -2f * tabGap / 3f;

		_classButton = CreateButton(string.Empty, 0f, 34f, () => JournalSystem.ShowClassSelection(), 0.92f);
		_classButton.Left.Set(0f, 0f);
		_classButton.Top.Set(2f, 0f);
		_classButton.Width.Set(widthOffset, 1f / 3f);
		_contentTabsPanel.Append(_classButton);

		_overviewTabButton = CreateButton(string.Empty, 0f, 34f, () => JournalSystem.ShowOverviewTab(), 0.92f);
		_overviewTabButton.Left.Set(tabGap / 3f, 1f / 3f);
		_overviewTabButton.Top.Set(2f, 0f);
		_overviewTabButton.Width.Set(widthOffset, 1f / 3f);
		_contentTabsPanel.Append(_overviewTabButton);

		_presetsTabButton = CreateButton(string.Empty, 0f, 34f, () => JournalSystem.ShowPresetsTab(), 0.92f);
		_presetsTabButton.Left.Set(tabGap * 2f / 3f, 2f / 3f);
		_presetsTabButton.Top.Set(2f, 0f);
		_presetsTabButton.Width.Set(widthOffset, 1f / 3f);
		_contentTabsPanel.Append(_presetsTabButton);

		_contentPanel = CreatePanel();
		_contentPanel.Left.Set(12f, 0f);
		_contentPanel.Top.Set(12f + TopTabsHeight + 10f, 0f);
		_contentPanel.Width.Set(-24f, 1f);
		_contentPanel.Height.Set(-(TopTabsHeight + 34f), 1f);
		_mainPanel.Append(_contentPanel);

		_contentTitle = new UIText(string.Empty, 0.48f, true);
		_contentTitle.Left.Set(14f, 0f);
		_contentTitle.Top.Set(10f, 0f);
		_contentPanel.Append(_contentTitle);

		_contentDescription = new UIText(string.Empty, 0.38f);
		_contentDescription.Left.Set(14f, 0f);
		_contentDescription.Top.Set(34f, 0f);
		_contentDescription.TextColor = new Color(198, 214, 229);
		_contentPanel.Append(_contentDescription);

		_classSelectionContainer = new UIElement();
		_classSelectionContainer.Left.Set(14f, 0f);
		_classSelectionContainer.Top.Set(52f, 0f);
		_classSelectionContainer.Width.Set(-28f, 1f);
		_classSelectionContainer.Height.Set(-66f, 1f);
		_contentPanel.Append(_classSelectionContainer);

		_entryList = new UIList();
		_entryList.Left.Set(14f, 0f);
		_entryList.Top.Set(52f, 0f);
		_entryList.Width.Set(-38f, 1f);
		_entryList.Height.Set(-66f, 1f);
		_entryList.ListPadding = 6f;
		_contentPanel.Append(_entryList);

		_scrollbar = new UIScrollbar();
		_scrollbar.Left.Set(-18f, 1f);
		_scrollbar.Top.Set(52f, 0f);
		_scrollbar.Height.Set(-66f, 1f);
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

		AppendTierBlock(
			entries,
			RecommendationTier.Recommended,
			"Mods.ProgressionJournal.UI.RecommendedBlock",
			new Color(22, 56, 33),
			new Color(90, 196, 116));

		AppendTierBlock(
			entries,
			RecommendationTier.Additional,
			"Mods.ProgressionJournal.UI.AdditionalBlock",
			new Color(44, 54, 26),
			new Color(190, 178, 94));

		AppendTierBlock(
			entries,
			RecommendationTier.NotRecommended,
			"Mods.ProgressionJournal.UI.NotRecommendedBlock",
			new Color(64, 34, 48),
			new Color(205, 116, 160));

		AppendTierBlock(
			entries,
			RecommendationTier.Useless,
			"Mods.ProgressionJournal.UI.UselessBlock",
			new Color(76, 22, 22),
			new Color(228, 72, 72));
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
			int entrySlots = Math.Max(1, entry.Entry.ItemGroups.Count);

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

	private void AppendTierBlock(
		IReadOnlyList<JournalStageEntry> entries,
		RecommendationTier tier,
		string titleKey,
		Color backgroundColor,
		Color borderColor)
	{
		var tiers = new[] { tier };
		if (!HasEntriesForAnyTier(entries, tiers)) {
			return;
		}

		_entryList.Add(CreateRecommendationBlock(
			Language.GetTextValue(titleKey),
			GetEntriesForTiers(entries, tiers),
			backgroundColor,
			borderColor));
	}

	private static JournalStageEntry[] GetEntriesForTiers(IReadOnlyList<JournalStageEntry> entries, RecommendationTier[] tiers)
	{
		return entries
			.Where(entry => tiers.Contains(entry.Evaluation.Tier))
			.OrderBy(entry => Array.IndexOf(tiers, entry.Evaluation.Tier))
			.ThenBy(entry => GetCategoryOrder(entry.Entry.Category))
			.ThenByDescending(GetCategoryStrength)
			.ThenBy(entry => GetEntryDisplayOrderOverride(entry.Entry.Key))
			.ThenBy(entry => entry.Entry.GetDisplayName(), StringComparer.CurrentCultureIgnoreCase)
			.ToArray();
	}

	private static int GetEntryDisplayOrderOverride(string entryKey) => entryKey switch
	{
		"sandstormOrBlizzardBottlePreBoss" => 0,
		"balloonBundlesPreBoss" => 1,
		_ => int.MaxValue
	};

	private static int GetCategoryOrder(JournalItemCategory category) => category switch
	{
		JournalItemCategory.Weapon => 0,
		JournalItemCategory.ClassSpecific => 1,
		JournalItemCategory.Armor => 2,
		JournalItemCategory.Accessory => 3,
		_ => int.MaxValue
	};

	private static int GetCategoryStrength(JournalStageEntry entry) => entry.Entry.Category switch
	{
		JournalItemCategory.Weapon => GetWeaponStrength(entry),
		JournalItemCategory.Armor => GetArmorStrength(entry),
		JournalItemCategory.ClassSpecific => 0,
		_ => 0
	};

	private static int GetWeaponStrength(JournalStageEntry entry)
	{
		int bestDamage = 0;

		foreach (var itemId in entry.Entry.ItemIds) {
			var item = new Item();
			item.SetDefaults(itemId);
			bestDamage = Math.Max(bestDamage, item.damage);
		}

		return bestDamage;
	}

	private static int GetArmorStrength(JournalStageEntry entry)
	{
		int totalDefense = 0;

		foreach (var group in entry.Entry.ItemGroups) {
			int bestDefenseInGroup = 0;

			foreach (var itemId in group.ItemIds) {
				var item = new Item();
				item.SetDefaults(itemId);
				bestDefenseInGroup = Math.Max(bestDefenseInGroup, item.defense);
			}

			totalDefense += bestDefenseInGroup;
		}

		return totalDefense;
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

	private static void StyleHeaderButton(JournalTextButton button, bool active, bool danger)
	{
		if (danger) {
			button.BackgroundColor = new Color(52, 39, 44);
			button.BorderColor = new Color(98, 76, 84);
			button.SetTextColor(new Color(234, 224, 228));
			return;
		}

		button.BackgroundColor = active ? new Color(49, 78, 67) : new Color(31, 44, 58);
		button.BorderColor = active ? new Color(100, 149, 127) : new Color(79, 100, 122);
		button.SetTextColor(new Color(224, 230, 236));
	}

	private static void StyleStageButton(JournalTextButton button, bool active)
	{
		button.BackgroundColor = active ? new Color(60, 88, 114) : new Color(29, 42, 58);
		button.BorderColor = active ? new Color(156, 196, 230) : new Color(88, 115, 142);
		button.SetTextColor(new Color(226, 233, 240));
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

		foreach (var category in EntryCategoryOrder) {
			var categoryEntries = entries.Where(entry => entry.Entry.Category == category).ToArray();
			if (categoryEntries.Length == 0) {
				continue;
			}

			foreach (var rowEntries in ChunkEntries(categoryEntries, EntrySlotsPerRow)) {
				var row = CreateSlotRow(rowEntries);
				row.Left.Set(BlockHorizontalPadding, 0f);
				row.Top.Set(top, 0f);
				block.Append(row);
				top += RowHeight + RowSpacing;
			}

			top += CategorySpacing;
		}

		if (top >= CategorySpacing) {
			top -= CategorySpacing;
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
			left += JournalEntrySlot.GetVisualWidth(entries[index].Entry.ItemGroups.Count) + EntrySpacing;
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
			totalWidth += JournalEntrySlot.GetVisualWidth(entries[index].Entry.ItemGroups.Count);

			if (index < entries.Count - 1) {
				totalWidth += EntrySpacing;
			}
		}

		return totalWidth;
	}

	private void LayoutStageButtons()
	{
		if (_stageButtons.Count == 0) {
			return;
		}

		float availableHeight = _stageListContainer.GetInnerDimensions().Height;
		float availableWidth = _stageListContainer.GetInnerDimensions().Width;
		if (availableHeight <= 0f) {
			return;
		}

		int columns = GetStageButtonColumnCount(availableHeight);
		int rows = (int)MathF.Ceiling(StageOrder.Length / (float)columns);
		float buttonHeight = (availableHeight - StageButtonGap * (rows - 1)) / rows;
		float buttonWidth = columns == 1
			? availableWidth
			: (availableWidth - StageButtonColumnGap * (columns - 1)) / columns;

		for (int index = 0; index < StageOrder.Length; index++) {
			var stageId = StageOrder[index];
			var button = _stageButtons[stageId];
			int row = index / columns;
			int column = index % columns;
			bool isTrailingSingleButton = columns > 1 && StageOrder.Length % columns != 0 && index == StageOrder.Length - 1;
			float top = row * (buttonHeight + StageButtonGap);
			float left = isTrailingSingleButton ? 0f : column * (buttonWidth + StageButtonColumnGap);

			button.Left.Set(left, 0f);
			button.Height.Set(buttonHeight, 0f);
			button.Top.Set(top, 0f);
			button.Width.Set(isTrailingSingleButton ? availableWidth : buttonWidth, 0f);
		}

		_stageListContainer.Recalculate();
	}

	private static int GetStageButtonColumnCount(float availableHeight)
	{
		float singleColumnButtonHeight = (availableHeight - StageButtonGap * (StageOrder.Length - 1)) / StageOrder.Length;
		return singleColumnButtonHeight >= MinSingleColumnStageButtonHeight ? 1 : 2;
	}

	private static void ApplyStageButtonText(JournalTextButton button, string text)
	{
		float availableWidth = button.GetInnerDimensions().Width - StageButtonTextHorizontalPadding * 2f;
		if (availableWidth <= 0f) {
			button.SetText(text, StageButtonTextScale);
			return;
		}

		float textScale = StageButtonTextScale;
		while (textScale > MinStageButtonTextScale && GetScaledTextWidth(text, textScale) > availableWidth) {
			textScale -= StageButtonTextScaleStep;
		}

		if (GetScaledTextWidth(text, textScale) <= availableWidth) {
			button.SetText(text, textScale);
			return;
		}

		button.SetText(TrimToPixelWidth(text, availableWidth, MinStageButtonTextScale), MinStageButtonTextScale);
	}

	private static string TrimToPixelWidth(string text, float maxWidth, float textScale)
	{
		if (string.IsNullOrWhiteSpace(text)) {
			return text;
		}

		if (GetScaledTextWidth(text, textScale) <= maxWidth) {
			return text;
		}

		const string ellipsis = "...";
		for (int length = text.Length - 1; length > 0; length--) {
			string candidate = text[..length].TrimEnd() + ellipsis;
			if (GetScaledTextWidth(candidate, textScale) <= maxWidth) {
				return candidate;
			}
		}

		return ellipsis;
	}

	private static float GetScaledTextWidth(string text, float textScale)
	{
		return FontAssets.MouseText.Value.MeasureString(text).X * textScale;
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
