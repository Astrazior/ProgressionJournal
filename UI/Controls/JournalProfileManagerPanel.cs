using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProgressionJournal.Systems;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalProfileManagerPanel : UIPanel
{
    private const string CloseIconTexturePath = "Images/UI/SearchCancel";
    private const string BackIconTexturePath = "Images/UI/Bestiary/Button_Back";
    private const string ExportIconTexturePath = "Images/UI/IconQuickload";
    private const float ProfileListTextScale = 0.78f;
    private const float CompactButtonTextScale = 0.66f;
    private const float EditorLabelScale = 0.68f;
    private const float EditorHintScale = 0.64f;

    private readonly UIList _list;
    private readonly UIScrollbar _scrollbar;
    private readonly JournalTextInput _profileNameInput;
    private readonly JournalTextInput _classNameInput;
    private readonly JournalTextInput _stageNameInput;
    private readonly JournalTextInput _searchInput;
    private string _appliedSearch = string.Empty;
    private string _loadedEditorProfileId = string.Empty;
    private bool _showBosses;

    public JournalProfileManagerPanel()
    {
        SetPadding(0f);
        BackgroundColor = JournalUiTheme.RootBackground * JournalUiTheme.RootBackgroundOpacity;
        BorderColor = JournalUiTheme.RootBorder;

        _profileNameInput = new JournalTextInput(string.Empty);
        _classNameInput = new JournalTextInput(string.Empty);
        _stageNameInput = new JournalTextInput(string.Empty);
        _searchInput = new JournalTextInput(string.Empty);
        Main.instance.LoadItem(ItemID.Book);
        Main.instance.LoadItem(ItemID.Wrench);

        _list = new JournalSmoothScrollList
        {
            ListPadding = 6f
        };
        _list.Left.Set(20f, 0f);
        _list.Width.Set(-62f, 1f);
        _list.Top.Set(112f, 0f);
        _list.Height.Set(-132f, 1f);
        Append(_list);

        _scrollbar = new UIScrollbar();
        _scrollbar.Left.Set(-32f, 1f);
        _scrollbar.Top.Set(112f, 0f);
        _scrollbar.Width.Set(20f, 0f);
        _scrollbar.Height.Set(-132f, 1f);
        Append(_scrollbar);
        _list.SetScrollbar(_scrollbar);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (!string.Equals(_searchInput.CurrentString, _appliedSearch, StringComparison.CurrentCulture))
        {
            ModContent.GetInstance<JournalSystem>().RefreshView();
        }
    }

    public void Refresh(JournalSystem system)
    {
        RemoveTransientChildren();
        _list.Clear();

        if (system.ProfileEditor is { } editor)
        {
            RefreshEditor(system, editor);
            return;
        }

        _loadedEditorProfileId = string.Empty;
        RefreshProfileList(system);
    }

    private void RefreshProfileList(JournalSystem system)
    {
        _list.Top.Set(112f, 0f);
        _list.Height.Set(-132f, 1f);
        _scrollbar.Top.Set(112f, 0f);
        _scrollbar.Height.Set(-132f, 1f);

        AddTitle(Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileManagerTitle"));
        AddTopIconButton(TextureAssets.Item[ItemID.Book], Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileNew"), 20f, system.BeginNewProfile);
        AddTopIconButton(TextureAssets.Item[ItemID.Wrench], Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileEdit"), 66f, system.BeginEditActiveProfile);
        AddTopIconButton(TextureAssets.Camera[6], Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileImport"), 112f, system.ImportProfile);
        AddTopIconButton(ExportIconTexturePath, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileExport"), 158f, system.ExportActiveProfile);
        AddTopIconButton(CloseIconTexturePath, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileClose"), -44f, system.CloseProfileManager, alignRight: true);

        foreach (var profile in JournalProfileRegistry.All)
        {
            var suffix = profile.IsReadOnly
                ? Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileReadOnlySuffix")
                : string.Empty;
            var warning = profile.HasVersionMismatch
                ? $"  {Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileVersionWarning")}"
                : string.Empty;
            var row = new UIElement();
            row.Width.Set(0f, 1f);
            row.Height.Set(48f, 0f);

            var button = JournalUiElementFactory.CreateTextButton(
                $"{profile.Name}{suffix}{warning}",
                0f,
                48f,
                () => system.SelectProfile(profile.Id),
                ProfileListTextScale);
            button.Width.Set(profile.IsBuiltIn ? 0f : -40f, 1f);
            button.SetStyle(JournalUiTheme.GetTabButtonStyle(
                string.Equals(profile.Id, JournalProfileRegistry.Active.Id, StringComparison.OrdinalIgnoreCase)));
            row.Append(button);

            if (!profile.IsBuiltIn)
            {
                var deleteArmed = string.Equals(
                    system.PendingProfileDeleteId,
                    profile.Id,
                    StringComparison.OrdinalIgnoreCase);
                var deleteButton = JournalBuildActionButton.CreateTrash(() => system.DeleteProfile(profile.Id));
                deleteButton.Left.Set(-34f, 1f);
                deleteButton.Top.Set(9f, 0f);
                deleteButton.SetHoverText(Language.GetTextValue(
                    deleteArmed
                        ? "Mods.ProgressionJournal.UI.ProfileDeleteConfirmTooltip"
                        : "Mods.ProgressionJournal.UI.ProfileDelete"));
                row.Append(deleteButton);
            }

            _list.Add(row);
        }
    }

    private void RefreshEditor(JournalSystem system, JournalProfileEditorSession editor)
    {
        if (!string.Equals(_loadedEditorProfileId, editor.Document.Id, StringComparison.OrdinalIgnoreCase))
        {
            _loadedEditorProfileId = editor.Document.Id;
            _profileNameInput.SetText(editor.Document.Name);
            _classNameInput.SetText(string.Empty);
            _stageNameInput.SetText(string.Empty);
            _searchInput.SetText(string.Empty);
        }

        AddTitle(Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileEditorTitle"));
        AddTopIconButton(TextureAssets.Item[ItemID.Book], Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileSave"), 20f, () =>
        {
            editor.SetName(_profileNameInput.CurrentString);
            system.SaveProfileEditor();
        });
        AddTopIconButton(BackIconTexturePath, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileCancel"), -44f, system.CloseProfileManager, alignRight: true);

        AddInputBackground(_profileNameInput, 20f, 96f, 280f, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileNameHint"));

        var selectedClass = editor.Document.Classes.First(value =>
            string.Equals(value.Id, editor.SelectedClassId, StringComparison.OrdinalIgnoreCase));
        AddCompactButton("<", 320f, 96f, () => { editor.CycleClass(-1); system.RefreshView(); });
        AddLabel(
            JournalTextUtilities.TrimToPixelWidth(
                $"{Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileClassLabel")}: {selectedClass.Name}",
                180f,
                EditorLabelScale),
            362f,
            104f,
            EditorLabelScale);
        AddCompactButton(">", 548f, 96f, () => { editor.CycleClass(1); system.RefreshView(); });
        AddInputBackground(_classNameInput, 590f, 96f, 170f, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileNewClassHint"));
        AddCompactButton("+", 768f, 96f, () =>
        {
            editor.AddClass(_classNameInput.CurrentString);
            _classNameInput.SetText(string.Empty);
            system.RefreshView();
        });
        AddCompactButton("-", 810f, 96f, () => { editor.RemoveSelectedClass(); system.RefreshView(); });

        var selectedStage = editor.Document.Stages.First(value =>
            string.Equals(value.Id, editor.SelectedStageId, StringComparison.OrdinalIgnoreCase));
        AddCompactButton("<", 20f, 140f, () => { editor.CycleStage(-1); system.RefreshView(); });
        AddLabel(
            JournalTextUtilities.TrimToPixelWidth(
                $"{Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileStageLabel")}: {selectedStage.Name}",
                196f,
                EditorLabelScale),
            62f,
            148f,
            EditorLabelScale);
        AddCompactButton(">", 270f, 140f, () => { editor.CycleStage(1); system.RefreshView(); });
        AddInputBackground(_stageNameInput, 312f, 140f, 190f, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileNewStageHint"));
        AddCompactButton("+", 510f, 140f, () =>
        {
            editor.AddStage(_stageNameInput.CurrentString);
            _stageNameInput.SetText(string.Empty);
            system.RefreshView();
        });
        AddCompactButton("-", 552f, 140f, () => { editor.RemoveSelectedStage(); system.RefreshView(); });
        AddCompactButton("↑", 594f, 140f, () => { editor.MoveSelectedStage(-1); system.RefreshView(); });
        AddCompactButton("↓", 636f, 140f, () => { editor.MoveSelectedStage(1); system.RefreshView(); });
        AddCompactButton($"A: {selectedStage.AccessorySlots}", 678f, 140f, () =>
        {
            editor.SetSelectedStageAccessorySlots(selectedStage.AccessorySlots >= 7 ? 0 : selectedStage.AccessorySlots + 1);
            system.RefreshView();
        }, width: 90f);

        _list.Top.Set(272f, 0f);
        _list.Height.Set(-292f, 1f);
        _scrollbar.Top.Set(272f, 0f);
        _scrollbar.Height.Set(-292f, 1f);

        AddCompactButton(
            _showBosses
                ? Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileItemsMode")
                : Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileBossesMode"),
            20f,
            188f,
            () =>
            {
                _showBosses = !_showBosses;
                _searchInput.SetText(string.Empty);
                system.RefreshView();
            },
            width: 132f);
        AddInputBackground(
            _searchInput,
            162f,
            188f,
            300f,
            _showBosses
                ? Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileBossSearchHint")
                : Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileItemSearchHint"));

        if (_showBosses)
        {
            AddCompactButton(
                Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileAlwaysAvailable"),
                472f,
                188f,
                () => { editor.SetSelectedStageAlwaysAvailable(); system.RefreshView(); },
                width: 154f);
            PopulateBossResults(system, editor);
        }
        else
        {
            AddCompactButton(
                editor.SelectedCategory.ToString(),
                472f,
                188f,
                () => { editor.CycleCategory(1); system.RefreshView(); },
                width: 154f);
            AddCompactButton(
                GetTierName(editor.SelectedTier),
                636f,
                188f,
                () => { editor.CycleTier(1); system.RefreshView(); },
                width: 180f);
            PopulateItemResults(system, editor);
        }

        AddLabel(
            Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileEditorHint"),
            20f,
            238f,
            EditorHintScale);
    }

    private void PopulateItemResults(JournalSystem system, JournalProfileEditorSession editor)
    {
        var search = _searchInput.CurrentString.Trim();
        _appliedSearch = _searchInput.CurrentString;

        var items = ContentSamples.ItemsByType.Values
            .Where(static item => item is not null && !item.IsAir && item.type > ItemID.None)
            .Where(item => string.IsNullOrWhiteSpace(search)
                || item.HoverName.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                || (item.ModItem?.Mod.DisplayNameClean?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false))
            .GroupBy(static item => item.type)
            .Select(static group => group.First())
            .OrderBy(static item => item.ModItem?.Mod.DisplayNameClean ?? "Terraria", StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(static item => item.HoverName, StringComparer.CurrentCultureIgnoreCase)
            .Take(240)
            .ToArray();

        const int columns = 10;
        for (var index = 0; index < items.Length; index += columns)
        {
            var row = new UIElement();
            row.Width.Set(0f, 1f);
            row.Height.Set(JournalUiMetrics.BuildSlotSize, 0f);

            var rowItems = items.Skip(index).Take(columns).ToArray();
            for (var column = 0; column < rowItems.Length; column++)
            {
                var item = rowItems[column];
                var itemId = item.type;
                var slot = new JournalBuildCandidateSlot(
                    item,
                    editor.ContainsItem(itemId),
                    disabled: false,
                    () => { editor.AddItem(itemId); system.RefreshView(); },
                    () => { editor.RemoveItem(itemId); system.RefreshView(); });
                slot.Left.Set(column * (JournalUiMetrics.BuildSlotSize + 6f), 0f);
                row.Append(slot);
            }

            _list.Add(row);
        }
    }

    private void PopulateBossResults(JournalSystem system, JournalProfileEditorSession editor)
    {
        var search = _searchInput.CurrentString.Trim();
        _appliedSearch = _searchInput.CurrentString;
        var results = Enumerable.Range(1, NPCLoader.NPCCount - 1)
            .Select(type => new
            {
                Type = type,
                Name = Lang.GetNPCNameValue(type),
                ModNpc = NPCLoader.GetNPC(type)
            })
            .Where(static value => !string.IsNullOrWhiteSpace(value.Name))
            .Where(value => string.IsNullOrWhiteSpace(search)
                || value.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                || (value.ModNpc?.Mod.DisplayNameClean?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false))
            .OrderBy(value => value.ModNpc?.Mod.DisplayNameClean ?? "Terraria", StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(static value => value.Name, StringComparer.CurrentCultureIgnoreCase)
            .Take(160);

        foreach (var result in results)
        {
            var source = result.ModNpc?.Mod.DisplayNameClean ?? "Terraria";
            var button = JournalUiElementFactory.CreateTextButton(
                $"{result.Name} ({source})",
                0f,
                36f,
                () =>
                {
                    editor.SetSelectedStageBoss(
                        result.ModNpc?.Mod.Name ?? "Terraria",
                        result.ModNpc?.Name ?? result.Type.ToString());
                    system.RefreshView();
                },
                0.72f);
            button.Width.Set(0f, 1f);
            _list.Add(button);
        }
    }

    private void RemoveTransientChildren()
    {
        foreach (var child in Children
            .Where(child => child != _list && child != _scrollbar)
            .ToArray())
        {
            RemoveChild(child);
        }
    }

    private void AddTitle(string text)
    {
        var title = new UIText(text, 0.74f, true)
        {
            HAlign = 0.5f,
            TextColor = JournalUiTheme.RootTitleText
        };
        title.Top.Set(12f, 0f);
        Append(title);
    }

    private void AddTopIconButton(
        Asset<Texture2D> texture,
        string hoverText,
        float left,
        Action action,
        bool alignRight = false)
    {
        var button = JournalUiElementFactory.CreateIconButton(texture, 36f, 36f, action, 0.78f);
        button.Left.Set(left, alignRight ? 1f : 0f);
        button.Top.Set(52f, 0f);
        button.SetHoverText(hoverText);
        Append(button);
    }

    private void AddTopIconButton(
        string texturePath,
        string hoverText,
        float left,
        Action action,
        bool alignRight = false)
    {
        AddTopIconButton(
            Main.Assets.Request<Texture2D>(texturePath),
            hoverText,
            left,
            action,
            alignRight);
    }

    private void AddCompactButton(string text, float left, float top, Action action, float width = 32f)
    {
        var button = JournalUiElementFactory.CreateTextButton(text, width, 36f, action, CompactButtonTextScale);
        button.Left.Set(left, 0f);
        button.Top.Set(top, 0f);
        Append(button);
    }

    private void AddLabel(string text, float left, float top, float scale)
    {
        var label = new UIText(text, scale)
        {
            TextColor = JournalUiTheme.ContentDescriptionText
        };
        label.Left.Set(left, 0f);
        label.Top.Set(top, 0f);
        Append(label);
    }

    private void AddInputBackground(
        JournalTextInput input,
        float left,
        float top,
        float width,
        string hint)
    {
        var background = JournalUiElementFactory.CreatePanel();
        background.Left.Set(left, 0f);
        background.Top.Set(top, 0f);
        background.Width.Set(width, 0f);
        background.Height.Set(36f, 0f);
        background.BackgroundColor = JournalUiTheme.PanelBackground;
        background.BorderColor = JournalUiTheme.PanelBorder;
        Append(background);

        input.HintText = hint;
        input.Left.Set(8f, 0f);
        input.Top.Set(8f, 0f);
        input.Width.Set(-16f, 1f);
        if (input.Parent is not null)
        {
            input.Parent.RemoveChild(input);
        }

        background.Append(input);
    }

    private static string GetTierName(RecommendationTier tier)
    {
        return tier == RecommendationTier.FromGuide
            ? Language.GetTextValue("Mods.ProgressionJournal.UI.FromGuideBlock")
            : tier.ToString();
    }
}
