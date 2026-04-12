using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using ProgressionJournal.Common.Data;
using ProgressionJournal.Common.Progression;
using ProgressionJournal.Common.Systems;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace ProgressionJournal.Common.UI;

public sealed class ProgressionJournalUIState : UIState
{
	private UIPanel _root = null!;
	private UIText _title = null!;
	private UIText _stageLabel = null!;
	private UIText _classLabel = null!;
	private UIList _entryList = null!;
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

		_syncButton = CreateButton(string.Empty, 150f, 34f, () => JournalSystem.SyncStageWithWorld());
		_syncButton.Left.Set(-270f, 1f);
		_syncButton.Top.Set(0f, 0f);
		_root.Append(_syncButton);

		InitializeSelectors();
		InitializeEntryList();
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);

		if (_root.ContainsPoint(Main.MouseScreen)) {
			Main.LocalPlayer.mouseInterface = true;
		}

		if (Main.keyState.IsKeyDown(Keys.Escape) && Main.oldKeyState.IsKeyUp(Keys.Escape)) {
			JournalSystem.HideView();
		}
	}

	public void Refresh(CombatClass combatClass, ProgressionStageId stageId)
	{
		ApplyLayout(Main.playerInventory);
		_title.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.Title"));
		_stageLabel.SetText($"{Language.GetTextValue("Mods.ProgressionJournal.UI.Stage")}: {Language.GetTextValue(ProgressionStageCatalog.Get(stageId).LocalizationKey)}");
		_classLabel.SetText($"{Language.GetTextValue("Mods.ProgressionJournal.UI.Class")}: {Language.GetTextValue($"Mods.ProgressionJournal.Classes.{combatClass}")}");
		_syncButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.UseCurrentStage"));
		_closeButton.SetText(Language.GetTextValue("Mods.ProgressionJournal.UI.Close"));

		_entryList.Clear();

		IReadOnlyList<JournalStageEntry> entries = JournalDatabase.GetEntries(stageId, combatClass);
		IEnumerable<IGrouping<RecommendationTier, JournalStageEntry>> groupedEntries = entries.GroupBy(entry => entry.Evaluation.Tier);

		if (entries.Count == 0) {
			_entryList.Add(CreateSectionHeader(Language.GetTextValue("Mods.ProgressionJournal.UI.EmptyState")));
			return;
		}

		RecommendationTier[] tierOrder =
		{
			RecommendationTier.Recommended,
			RecommendationTier.Situational,
			RecommendationTier.NotRecommended,
			RecommendationTier.Useless
		};

		foreach (RecommendationTier tier in tierOrder) {
			List<JournalStageEntry> tierEntries = groupedEntries
				.FirstOrDefault(group => group.Key == tier)?
				.ToList() ?? new List<JournalStageEntry>();

			if (tierEntries.Count == 0) {
				continue;
			}

			_entryList.Add(CreateSectionHeader(Language.GetTextValue($"Mods.ProgressionJournal.Tiers.{tier}")));

			foreach (JournalStageEntry entry in tierEntries) {
				_entryList.Add(new JournalEntryPanel(entry));
			}
		}
	}

	private void ApplyLayout(bool inventoryOpen)
	{
		float left = inventoryOpen ? MathF.Min(620f, Main.screenWidth * 0.42f) : 180f;
		float rightMargin = inventoryOpen ? 36f : 180f;

		_root.Left.Set(left, 0f);
		_root.Top.Set(70f, 0f);
		_root.Width.Set(-(left + rightMargin), 1f);
		_root.Height.Set(-140f, 1f);
		_root.Recalculate();
	}

	private void InitializeSelectors()
	{
		UITextPanel<string> previousStageButton = CreateButton("<", 36f, 32f, () => JournalSystem.CycleStage(-1));
		previousStageButton.Left.Set(0f, 0f);
		previousStageButton.Top.Set(52f, 0f);
		_root.Append(previousStageButton);

		_stageLabel = new UIText(string.Empty, 0.9f);
		_stageLabel.Left.Set(48f, 0f);
		_stageLabel.Top.Set(58f, 0f);
		_stageLabel.Width.Set(-96f, 1f);
		_root.Append(_stageLabel);

		UITextPanel<string> nextStageButton = CreateButton(">", 36f, 32f, () => JournalSystem.CycleStage(1));
		nextStageButton.Left.Set(-36f, 1f);
		nextStageButton.Top.Set(52f, 0f);
		_root.Append(nextStageButton);

		UITextPanel<string> previousClassButton = CreateButton("<", 36f, 32f, () => JournalSystem.CycleClass(-1));
		previousClassButton.Left.Set(0f, 0f);
		previousClassButton.Top.Set(92f, 0f);
		_root.Append(previousClassButton);

		_classLabel = new UIText(string.Empty, 0.9f);
		_classLabel.Left.Set(48f, 0f);
		_classLabel.Top.Set(98f, 0f);
		_classLabel.Width.Set(-96f, 1f);
		_root.Append(_classLabel);

		UITextPanel<string> nextClassButton = CreateButton(">", 36f, 32f, () => JournalSystem.CycleClass(1));
		nextClassButton.Left.Set(-36f, 1f);
		nextClassButton.Top.Set(92f, 0f);
		_root.Append(nextClassButton);
	}

	private void InitializeEntryList()
	{
		_entryList = new UIList();
		_entryList.Left.Set(0f, 0f);
		_entryList.Top.Set(138f, 0f);
		_entryList.Width.Set(-26f, 1f);
		_entryList.Height.Set(-138f, 1f);
		_entryList.ListPadding = 8f;
		_root.Append(_entryList);

		UIScrollbar scrollbar = new();
		scrollbar.Left.Set(-20f, 1f);
		scrollbar.Top.Set(138f, 0f);
		scrollbar.Height.Set(-138f, 1f);
		_root.Append(scrollbar);
		_entryList.SetScrollbar(scrollbar);
	}

	private static UITextPanel<string> CreateButton(string text, float width, float height, Action onClick)
	{
		UITextPanel<string> button = new(text, 0.85f, false);
		button.Width.Set(width, 0f);
		button.Height.Set(height, 0f);
		button.BackgroundColor = new Color(44, 62, 84);
		button.BorderColor = new Color(96, 124, 160);
		button.OnLeftClick += (_, _) => onClick();
		return button;
	}

	private static UIText CreateSectionHeader(string text)
	{
		UIText header = new(text, 0.86f, true);
		header.Height.Set(26f, 0f);
		header.TextColor = new Color(240, 220, 140);
		return header;
	}

	private static ProgressionJournalUISystem JournalSystem => ModContent.GetInstance<ProgressionJournalUISystem>();
}
