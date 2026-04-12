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
	private static readonly CombatClass[] ClassOrder =
	[
		CombatClass.Melee,
		CombatClass.Ranged,
		CombatClass.Magic,
		CombatClass.Summoner
	];

	private readonly Dictionary<CombatClass, UITextPanel<string>> _classButtons = new();
	private UIPanel _root = null!;
	private UIPanel _headerPanel = null!;
	private UIElement _sidebarPanel = null!;
	private UIPanel _contentPanel = null!;
	private UIPanel _stageCard = null!;
	private UIPanel _classCard = null!;
	private UIPanel _legendCard = null!;
	private UIText _title = null!;
	private UIText _subtitle = null!;
	private UIText _stageCardTitle = null!;
	private UIText _stageTitle = null!;
	private UIText _stageProgress = null!;
	private UIText _worldStageLabel = null!;
	private UIText _classCardTitle = null!;
	private UIText _legendTitle = null!;
	private UIText _legendRecommended = null!;
	private UIText _legendSituational = null!;
	private UIText _legendNotRecommended = null!;
	private UIText _legendUseless = null!;
	private UIText _contentTitle = null!;
	private UIText _contentDescription = null!;
	private UIText _contentStats = null!;
	private UIList _entryList = null!;
	private UITextPanel<string> _overviewTabButton = null!;
	private UITextPanel<string> _presetsTabButton = null!;
	private UITextPanel<string> _syncButton = null!;
	private UITextPanel<string> _closeButton = null!;

	public override void OnInitialize()
	{
		_root = new UIPanel();
		_root.SetPadding(0f);
		_root.BackgroundColor = new Color(12, 20, 30) * 0.98f;
		_root.BorderColor = new Color(78, 101, 124);
		Append(_root);

		InitializeHeader();
		InitializeSidebar();
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

	public void Refresh(CombatClass combatClass, ProgressionStageId stageId, bool showingPresets)
	{
		ApplyLayout(Main.playerInventory);

		string stageName = Language.GetTextValue(ProgressionStageCatalog.Get(stageId).LocalizationKey);
		var worldStageId = ProgressionStageCatalog.GetCurrentStageId();
		string worldStageName = Language.GetTextValue(ProgressionStageCatalog.Get(worldStageId).LocalizationKey);

		_title.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.Title"));
		_subtitle.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.Subtitle"));

		_stageCardTitle.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.StageSelectorTitle"));
		_stageTitle.SetText(stageName);
		_stageProgress.SetText(Language.GetTextValue(
			"Mods.ProgressionJournal.UI.StageProgress",
			GetStageIndex(stageId) + 1,
			ProgressionStageCatalog.All.Count));
		_worldStageLabel.SetText(
			worldStageId == stageId
				? Language.GetTextValue("Mods.ProgressionJournal.UI.StageMatchesWorld")
				: Language.GetTextValue("Mods.ProgressionJournal.UI.StageDiffersFromWorld", worldStageName));

		_classCardTitle.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.ClassSelectorTitle"));
		_legendTitle.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.LegendTitle"));
		_legendRecommended.SetText(
			$"{Language.GetTextValue("Mods.ProgressionJournal.Tiers.Recommended")}: {Language.GetTextValue("Mods.ProgressionJournal.UI.TierLegendRecommended")}");
		_legendSituational.SetText(
			$"{Language.GetTextValue("Mods.ProgressionJournal.Tiers.Situational")}: {Language.GetTextValue("Mods.ProgressionJournal.UI.TierLegendSituational")}");
		_legendNotRecommended.SetText(
			$"{Language.GetTextValue("Mods.ProgressionJournal.Tiers.NotRecommended")}: {Language.GetTextValue("Mods.ProgressionJournal.UI.TierLegendNotRecommended")}");
		_legendUseless.SetText(
			$"{Language.GetTextValue("Mods.ProgressionJournal.Tiers.Useless")}: {Language.GetTextValue("Mods.ProgressionJournal.UI.TierLegendUseless")}");

		_syncButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.UseCurrentStage"));
		_closeButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.Close"));
		_overviewTabButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.OverviewTab"));
		_presetsTabButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.PresetsTab"));

		RefreshClassButtons(combatClass);
		ApplyTabStyles(showingPresets);
		_entryList.Clear();

		if (showingPresets) {
			var presets = JournalRepository.GetPresets(stageId, combatClass);
			_contentTitle.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.PresetsHeadline"));
			_contentDescription.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.PresetsDescription"));
			_contentStats.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.PresetsCount", presets.Count));
			AppendPresets(presets);
			return;
		}

		var entries = JournalRepository.GetEntries(stageId, combatClass);
		_contentTitle.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.OverviewHeadline"));
		_contentDescription.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.OverviewDescription"));
		_contentStats.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.EntriesCount", entries.Count));
		AppendEntries(entries);
	}

	private void ApplyLayout(bool inventoryOpen)
	{
		var width = MathF.Min(inventoryOpen ? 780f : 960f, Main.screenWidth - 48f);
		var height = MathF.Min(680f, Main.screenHeight - 48f);
		var left = inventoryOpen
			? MathF.Max(Main.screenWidth - width - 24f, 16f)
			: (Main.screenWidth - width) * 0.5f;
		var top = MathF.Max(20f, (Main.screenHeight - height) * 0.5f);

		_root.Left.Set(left, 0f);
		_root.Top.Set(top, 0f);
		_root.Width.Set(width, 0f);
		_root.Height.Set(height, 0f);
		_root.Recalculate();
	}

	private void InitializeHeader()
	{
		_headerPanel = CreateCard();
		_headerPanel.Left.Set(12f, 0f);
		_headerPanel.Top.Set(12f, 0f);
		_headerPanel.Width.Set(-24f, 1f);
		_headerPanel.Height.Set(80f, 0f);
		_headerPanel.BackgroundColor = new Color(18, 34, 46);
		_headerPanel.BorderColor = new Color(95, 125, 151);
		_root.Append(_headerPanel);

		_title = new UIText(string.Empty, 0.76f, true);
		_title.Left.Set(16f, 0f);
		_title.Top.Set(14f, 0f);
		_headerPanel.Append(_title);

		_subtitle = new UIText(string.Empty, 0.52f);
		_subtitle.Left.Set(16f, 0f);
		_subtitle.Top.Set(40f, 0f);
		_subtitle.Width.Set(-278f, 1f);
		_subtitle.TextColor = new Color(205, 221, 236);
		_headerPanel.Append(_subtitle);

		_syncButton = CreateButton(string.Empty, 164f, 36f, () => JournalSystem.SyncStageWithWorld());
		_syncButton.Left.Set(-286f, 1f);
		_syncButton.Top.Set(20f, 0f);
		_headerPanel.Append(_syncButton);

		_closeButton = CreateButton(string.Empty, 110f, 36f, () => JournalSystem.HideView());
		_closeButton.Left.Set(-118f, 1f);
		_closeButton.Top.Set(20f, 0f);
		_closeButton.BackgroundColor = new Color(74, 45, 45);
		_closeButton.BorderColor = new Color(150, 92, 92);
		_headerPanel.Append(_closeButton);
	}

	private void InitializeSidebar()
	{
		_sidebarPanel = new UIElement();
		_sidebarPanel.Left.Set(12f, 0f);
		_sidebarPanel.Top.Set(104f, 0f);
		_sidebarPanel.Width.Set(224f, 0f);
		_sidebarPanel.Height.Set(-116f, 1f);
		_root.Append(_sidebarPanel);

		_stageCard = CreateCard();
		_stageCard.Width.Set(0f, 1f);
		_stageCard.Height.Set(132f, 0f);
		_sidebarPanel.Append(_stageCard);

		_stageCardTitle = CreateSectionText();
		_stageCardTitle.Left.Set(12f, 0f);
		_stageCardTitle.Top.Set(10f, 0f);
		_stageCard.Append(_stageCardTitle);

		var previousStageButton = CreateButton("<", 36f, 32f, () => JournalSystem.CycleStage(-1));
		previousStageButton.Left.Set(12f, 0f);
		previousStageButton.Top.Set(42f, 0f);
		_stageCard.Append(previousStageButton);

		_stageTitle = new UIText(string.Empty, 0.66f, true) {
			HAlign = 0.5f
		};
		_stageTitle.Top.Set(46f, 0f);
		_stageCard.Append(_stageTitle);

		var nextStageButton = CreateButton(">", 36f, 32f, () => JournalSystem.CycleStage(1));
		nextStageButton.Left.Set(-48f, 1f);
		nextStageButton.Top.Set(42f, 0f);
		_stageCard.Append(nextStageButton);

		_stageProgress = new UIText(string.Empty, 0.5f) {
			HAlign = 0.5f
		};
		_stageProgress.Top.Set(74f, 0f);
		_stageProgress.TextColor = new Color(224, 232, 240);
		_stageCard.Append(_stageProgress);

		_worldStageLabel = new UIText(string.Empty, 0.48f) {
			HAlign = 0.5f
		};
		_worldStageLabel.Top.Set(92f, 0f);
		_worldStageLabel.TextColor = new Color(176, 200, 220);
		_stageCard.Append(_worldStageLabel);

		_classCard = CreateCard();
		_classCard.Top.Set(144f, 0f);
		_classCard.Width.Set(0f, 1f);
		_classCard.Height.Set(214f, 0f);
		_sidebarPanel.Append(_classCard);

		_classCardTitle = CreateSectionText();
		_classCardTitle.Left.Set(12f, 0f);
		_classCardTitle.Top.Set(10f, 0f);
		_classCard.Append(_classCardTitle);

		float top = 38f;
		foreach (var combatClass in ClassOrder) {
			var capturedClass = combatClass;
			var button = CreateButton(string.Empty, 0f, 34f, () => JournalSystem.SelectClass(capturedClass));
			button.Left.Set(12f, 0f);
			button.Top.Set(top, 0f);
			button.Width.Set(-24f, 1f);
			button.TextHAlign = 0f;
			button.SetPadding(12f);
			_classButtons[combatClass] = button;
			_classCard.Append(button);
			top += 40f;
		}

		_legendCard = CreateCard();
		_legendCard.Top.Set(370f, 0f);
		_legendCard.Width.Set(0f, 1f);
		_legendCard.Height.Set(-370f, 1f);
		_sidebarPanel.Append(_legendCard);

		_legendTitle = CreateSectionText();
		_legendTitle.Left.Set(12f, 0f);
		_legendTitle.Top.Set(10f, 0f);
		_legendCard.Append(_legendTitle);

		_legendRecommended = CreateLegendLine(new Color(126, 205, 146), 36f);
		_legendSituational = CreateLegendLine(new Color(217, 191, 102), 54f);
		_legendNotRecommended = CreateLegendLine(new Color(218, 144, 94), 72f);
		_legendUseless = CreateLegendLine(new Color(214, 110, 110), 90f);
		_legendCard.Append(_legendRecommended);
		_legendCard.Append(_legendSituational);
		_legendCard.Append(_legendNotRecommended);
		_legendCard.Append(_legendUseless);
	}

	private void InitializeContent()
	{
		_contentPanel = CreateCard();
		_contentPanel.Left.Set(248f, 0f);
		_contentPanel.Top.Set(104f, 0f);
		_contentPanel.Width.Set(-260f, 1f);
		_contentPanel.Height.Set(-116f, 1f);
		_root.Append(_contentPanel);

		_overviewTabButton = CreateButton(string.Empty, 180f, 38f, () => JournalSystem.ShowOverviewTab());
		_overviewTabButton.Left.Set(14f, 0f);
		_overviewTabButton.Top.Set(14f, 0f);
		_contentPanel.Append(_overviewTabButton);

		_presetsTabButton = CreateButton(string.Empty, 180f, 38f, () => JournalSystem.ShowPresetsTab());
		_presetsTabButton.Left.Set(204f, 0f);
		_presetsTabButton.Top.Set(14f, 0f);
		_contentPanel.Append(_presetsTabButton);

		_contentTitle = new UIText(string.Empty, 0.7f, true);
		_contentTitle.Left.Set(14f, 0f);
		_contentTitle.Top.Set(68f, 0f);
		_contentPanel.Append(_contentTitle);

		_contentDescription = new UIText(string.Empty, 0.5f);
		_contentDescription.Left.Set(14f, 0f);
		_contentDescription.Top.Set(96f, 0f);
		_contentDescription.Width.Set(-28f, 1f);
		_contentDescription.TextColor = new Color(208, 220, 232);
		_contentPanel.Append(_contentDescription);

		_contentStats = new UIText(string.Empty, 0.5f);
		_contentStats.Left.Set(14f, 0f);
		_contentStats.Top.Set(116f, 0f);
		_contentStats.TextColor = new Color(170, 198, 218);
		_contentPanel.Append(_contentStats);

		_entryList = new UIList();
		_entryList.Left.Set(14f, 0f);
		_entryList.Top.Set(146f, 0f);
		_entryList.Width.Set(-40f, 1f);
		_entryList.Height.Set(-160f, 1f);
		_entryList.ListPadding = 8f;
		_contentPanel.Append(_entryList);

		var scrollbar = new UIScrollbar();
		scrollbar.Left.Set(-20f, 1f);
		scrollbar.Top.Set(146f, 0f);
		scrollbar.Height.Set(-160f, 1f);
		_contentPanel.Append(scrollbar);
		_entryList.SetScrollbar(scrollbar);
	}

	private void AppendEntries(IReadOnlyList<JournalStageEntry> entries)
	{
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

	private void RefreshClassButtons(CombatClass selectedClass)
	{
		foreach (var (combatClass, button) in _classButtons) {
			button.SetText(Language.GetTextValue($"Mods.ProgressionJournal.Classes.{combatClass}"));
			StyleClassButton(button, combatClass == selectedClass);
		}
	}

	private void ApplyTabStyles(bool showingPresets)
	{
		StyleTabButton(_overviewTabButton, !showingPresets);
		StyleTabButton(_presetsTabButton, showingPresets);
	}

	private static int GetStageIndex(ProgressionStageId stageId)
	{
		for (int index = 0; index < ProgressionStageCatalog.All.Count; index++) {
			if (ProgressionStageCatalog.All[index].Id == stageId) {
				return index;
			}
		}

		return 0;
	}

	private static UIPanel CreateCard()
	{
		var panel = new UIPanel();
		panel.SetPadding(0f);
		panel.BackgroundColor = new Color(22, 34, 46);
		panel.BorderColor = new Color(86, 114, 140);
		return panel;
	}

	private static UIText CreateSectionText()
	{
		var text = new UIText(string.Empty, 0.62f, true);
		text.TextColor = new Color(239, 223, 176);
		return text;
	}

	private static UIText CreateLegendLine(Color color, float top)
	{
		var text = new UIText(string.Empty, 0.48f);
		text.Left.Set(12f, 0f);
		text.Top.Set(top, 0f);
		text.Width.Set(-24f, 1f);
		text.TextColor = color;
		return text;
	}

	private static void StyleTabButton(UITextPanel<string> button, bool active)
	{
		button.BackgroundColor = active ? new Color(50, 91, 65) : new Color(35, 51, 69);
		button.BorderColor = active ? new Color(130, 194, 149) : new Color(98, 126, 158);
		button.TextColor = active ? Color.White : new Color(222, 232, 242);
	}

	private static void StyleClassButton(UITextPanel<string> button, bool active)
	{
		button.BackgroundColor = active ? new Color(54, 87, 67) : new Color(34, 50, 67);
		button.BorderColor = active ? new Color(135, 196, 154) : new Color(92, 120, 150);
		button.TextColor = active ? Color.White : new Color(220, 228, 236);
	}

	private static UITextPanel<string> CreateButton(string text, float width, float height, Action onClick)
	{
		var button = new UITextPanel<string>(text, 0.56f, false);
		button.Width.Set(width, width <= 0f ? 1f : 0f);
		button.Height.Set(height, 0f);
		button.BackgroundColor = new Color(35, 51, 69);
		button.BorderColor = new Color(98, 126, 158);
		button.TextColor = new Color(224, 232, 240);
		button.OnLeftClick += (_, _) => onClick();
		return button;
	}

	private static UIText CreateSectionHeader(string text)
	{
		var header = new UIText(text, 0.64f, true);
		header.Height.Set(24f, 0f);
		header.TextColor = new Color(240, 220, 140);
		return header;
	}

	private static JournalSystem JournalSystem => ModContent.GetInstance<JournalSystem>();
}
