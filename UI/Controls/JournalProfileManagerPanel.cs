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
    private const string SortAscendingIconTexturePath = "Images/UI/Sort_0";
    private const string SortDescendingIconTexturePath = "Images/UI/Sort_1";
    private const string LockedIconTexturePath = "Images/UI/Bestiary/Icon_Locked";
    private const string AutoIconTexturePath = "Images/UI/Bestiary/Button_Sorting";
    private const float CompactTextScale = 0.66f;
    private const int LiveNameApplyDelayTicks = 8;

    private readonly UIList _stageList;
    private readonly UIScrollbar _stageScrollbar;
    private readonly UIList _contentList;
    private readonly UIScrollbar _contentScrollbar;
    private readonly UIList _pickerList;
    private readonly UIScrollbar _pickerScrollbar;
    private readonly JournalTextInput _profileNameInput = new(string.Empty);
    private readonly JournalTextInput _classNameInput = new(string.Empty);
    private readonly JournalTextInput _customClassInput = new(string.Empty);
    private readonly JournalTextInput _stageNameInput = new(string.Empty);
    private readonly JournalTextInput _searchInput = new(string.Empty);
    private readonly JournalTextInput _customEventInput = new(string.Empty);

    private string _loadedProfileId = string.Empty;
    private string _loadedClassId = string.Empty;
    private string _loadedStageId = string.Empty;
    private string _appliedSearch = string.Empty;
    private string _observedProfileName = string.Empty;
    private string _observedClassName = string.Empty;
    private string _observedStageName = string.Empty;
    private int _liveNameApplyDelay;
    private EditorPage _page;
    private EditorOverlay _overlay;
    private JournalItemCategory _pickerItemCategory;
    private RecommendationTier _pickerTier;
    private JournalBuffCategory _pickerBuffCategory;
    private int _selectedItemId;
    private int _loadedPropertyItemId;

    private enum EditorPage
    {
        Recommendations,
        CombatBuffs
    }

    private enum EditorOverlay
    {
        None,
        ItemPicker,
        ClassPicker,
        StageIconPicker,
        StageUnlockPicker,
        ItemProperties
    }

    public JournalProfileManagerPanel()
    {
        SetPadding(0f);
        BackgroundColor = JournalUiTheme.RootBackground * JournalUiTheme.RootBackgroundOpacity;
        BorderColor = JournalUiTheme.RootBorder;

        Main.instance.LoadItem(ItemID.Book);
        Main.instance.LoadItem(ItemID.Wrench);

        _stageList = new JournalSmoothScrollList { ListPadding = 6f };
        _stageScrollbar = new UIScrollbar();
        _stageList.SetScrollbar(_stageScrollbar);

        _contentList = new JournalSmoothScrollList { ListPadding = 10f };
        _contentScrollbar = new UIScrollbar();
        _contentList.SetScrollbar(_contentScrollbar);

        _pickerList = new JournalSmoothScrollList { ListPadding = 6f };
        _pickerScrollbar = new UIScrollbar();
        _pickerList.SetScrollbar(_pickerScrollbar);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        var system = ModContent.GetInstance<JournalSystem>();
        if (!string.Equals(_searchInput.CurrentString, _appliedSearch, StringComparison.CurrentCulture))
        {
            system.RefreshView();
        }

        if (system.ProfileEditor is not { } editor)
        {
            return;
        }

        var namesChanged =
            !string.Equals(_profileNameInput.CurrentString, _observedProfileName, StringComparison.CurrentCulture)
            || !string.Equals(_classNameInput.CurrentString, _observedClassName, StringComparison.CurrentCulture)
            || !string.Equals(_stageNameInput.CurrentString, _observedStageName, StringComparison.CurrentCulture);
        if (namesChanged)
        {
            _observedProfileName = _profileNameInput.CurrentString;
            _observedClassName = _classNameInput.CurrentString;
            _observedStageName = _stageNameInput.CurrentString;
            _liveNameApplyDelay = LiveNameApplyDelayTicks;
            return;
        }

        if (_liveNameApplyDelay <= 0 || --_liveNameApplyDelay > 0)
        {
            return;
        }

        editor.SetName(_profileNameInput.CurrentString);
        editor.RenameSelectedClass(_classNameInput.CurrentString);
        editor.RenameSelectedStage(_stageNameInput.CurrentString);
        system.RefreshView();
    }

    public void Refresh(JournalSystem system)
    {
        RemoveAllChildren();
        _stageList.Clear();
        _contentList.Clear();
        _pickerList.Clear();

        if (system.ProfileEditor is not { } editor)
        {
            _loadedProfileId = string.Empty;
            RefreshProfileList(system);
            return;
        }

        SyncEditorInputs(editor);
        RefreshEditor(system, editor);
    }

    private void RefreshProfileList(JournalSystem system)
    {
        AddTitle(Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileManagerTitle"));
        AddTopIconButton(TextureAssets.Item[ItemID.Book], Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileNew"), 20f, system.BeginNewProfile);
        AddTopIconButton(TextureAssets.Item[ItemID.Wrench], Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileEdit"), 66f, system.BeginEditActiveProfile);
        AddTopIconButton(TextureAssets.Camera[6], Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileImport"), 112f, system.ImportProfile);
        AddTopIconButton(ExportIconTexturePath, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileExport"), 158f, system.ExportActiveProfile);
        AddTopIconButton(CloseIconTexturePath, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileClose"), -44f, system.CloseProfileManager, true);

        ConfigureList(_contentList, _contentScrollbar, 20f, 112f, -62f, -132f);
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
                0.78f);
            button.Width.Set(profile.IsBuiltIn ? 0f : -40f, 1f);
            button.SetStyle(JournalUiTheme.GetTabButtonStyle(
                string.Equals(profile.Id, JournalProfileRegistry.Active.Id, StringComparison.OrdinalIgnoreCase)));
            row.Append(button);

            if (!profile.IsBuiltIn)
            {
                var deleteButton = JournalBuildActionButton.CreateTrash(() => system.DeleteProfile(profile.Id));
                deleteButton.Left.Set(-34f, 1f);
                deleteButton.Top.Set(9f, 0f);
                row.Append(deleteButton);
            }

            _contentList.Add(row);
        }
    }

    private void RefreshEditor(JournalSystem system, JournalProfileEditorSession editor)
    {
        AddTitle(Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileEditorTitle"));
        if (editor.Document.Classes.Count > 0)
        {
            AddTopIconButton(TextureAssets.Item[ItemID.Book], Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileSave"), 20f, () =>
            {
                editor.SetName(_profileNameInput.CurrentString);
                system.SaveProfileEditor();
            });
        }

        AddTopIconButton(BackIconTexturePath, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileCancel"), -44f, system.CloseProfileManager, true);
        AddCenteredInputBackground(_profileNameInput, 52f, 280f, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileNameHint"));
        if (!string.IsNullOrWhiteSpace(editor.SelectedClassId))
        {
            AddInputBackground(_classNameInput, 20f, 100f, 140f, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileClassLabel"));
            var deleteClassButton = JournalBuildActionButton.CreateTrash(() =>
            {
                editor.RemoveSelectedClass();
                system.RefreshView();
            });
            deleteClassButton.Left.Set(166f, 0f);
            deleteClassButton.Top.Set(103f, 0f);
            deleteClassButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileRemoveClass"));
            Append(deleteClassButton);
        }
        AddClassTabs(system, editor);
        AddStageColumn(system, editor);
        AddEditorContent(system, editor);

        if (_overlay != EditorOverlay.None)
        {
            AddEditorOverlay(system, editor);
        }
    }

    private void AddClassTabs(JournalSystem system, JournalProfileEditorSession editor)
    {
        var classes = editor.Document.Classes;
        var selectedIndex = classes.FindIndex(value =>
            string.Equals(value.Id, editor.SelectedClassId, StringComparison.OrdinalIgnoreCase));
        var tabsLeft = string.IsNullOrWhiteSpace(editor.SelectedClassId) ? 20f : 204f;
        var maxVisibleClasses = string.IsNullOrWhiteSpace(editor.SelectedClassId) ? 6 : 4;
        var firstIndex = Math.Max(0, Math.Min(selectedIndex - 2, Math.Max(0, classes.Count - maxVisibleClasses)));
        for (var index = firstIndex; index < Math.Min(classes.Count, firstIndex + maxVisibleClasses); index++)
        {
            var definition = classes[index];
            var capturedId = definition.Id;
            var button = JournalUiElementFactory.CreateTextButton(
                JournalTextUtilities.TrimToPixelWidth(definition.Name, 100f, 0.68f),
                108f,
                34f,
                () =>
                {
                    editor.SelectClass(capturedId);
                    _overlay = EditorOverlay.None;
                    system.RefreshView();
                },
                0.68f);
            button.Left.Set(tabsLeft + (index - firstIndex) * 114f, 0f);
            button.Top.Set(100f, 0f);
            button.SetStyle(JournalUiTheme.GetTabButtonStyle(
                string.Equals(definition.Id, editor.SelectedClassId, StringComparison.OrdinalIgnoreCase)));
            Append(button);
        }

        AddMainIconButton(
            TextureAssets.Item[ItemID.Wrench],
            Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileAddClassTooltip"),
            tabsLeft + Math.Min(classes.Count, maxVisibleClasses) * 114f + 4f,
            101f,
            () =>
            {
                _overlay = EditorOverlay.ClassPicker;
                _searchInput.SetText(string.Empty);
                _customClassInput.SetText(string.Empty);
                system.RefreshView();
            });

        if (classes.Count == 0)
        {
            AddLabel(Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileNoClass"), 62f, 108f, 0.72f);
        }
    }

    private void AddStageColumn(JournalSystem system, JournalProfileEditorSession editor)
    {
        var panel = JournalUiElementFactory.CreatePanel();
        panel.Left.Set(20f, 0f);
        panel.Top.Set(148f, 0f);
        panel.Width.Set(220f, 0f);
        panel.Height.Set(-168f, 1f);
        Append(panel);

        var title = new UIText(Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileStagesTitle"), 0.5f, true)
        {
            HAlign = 0.5f,
            TextColor = JournalUiTheme.SectionHeaderText
        };
        title.Top.Set(10f, 0f);
        panel.Append(title);

        _stageList.Left.Set(10f, 0f);
        _stageList.Top.Set(38f, 0f);
        _stageList.Width.Set(-40f, 1f);
        _stageList.Height.Set(-190f, 1f);
        panel.Append(_stageList);
        _stageScrollbar.Left.Set(-26f, 1f);
        _stageScrollbar.Top.Set(38f, 0f);
        _stageScrollbar.Width.Set(18f, 0f);
        _stageScrollbar.Height.Set(-190f, 1f);
        panel.Append(_stageScrollbar);

        foreach (var stage in editor.Document.Stages)
        {
            var capturedId = stage.Id;
            var button = JournalUiElementFactory.CreateStageButton(() =>
            {
                editor.SelectStage(capturedId);
                _overlay = EditorOverlay.None;
                system.RefreshView();
            });
            button.Width.Set(0f, 1f);
            button.Height.Set(38f, 0f);
            var previewProfile = new JournalProfile(editor.Document, [], [], "editor", false, false);
            JournalStageButtonPresenter.RefreshEditorButton(
                previewProfile,
                stage,
                button,
                string.Equals(stage.Id, editor.SelectedStageId, StringComparison.OrdinalIgnoreCase));
            _stageList.Add(button);
        }

        var addStageRow = new UIElement();
        addStageRow.Width.Set(0f, 1f);
        addStageRow.Height.Set(36f, 0f);
        var addStage = JournalUiElementFactory.CreateIconButton(
            TextureAssets.Item[ItemID.Book],
            32f,
            32f,
            () =>
            {
                editor.AddStage(Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileDefaultStageName"));
                system.RefreshView();
            },
            0.72f);
        addStage.HAlign = 0.5f;
        addStage.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileAddStageTooltip"));
        addStageRow.Append(addStage);
        _stageList.Add(addStageRow);

        AddStageSettings(panel, system, editor);
    }

    private void AddStageSettings(UIElement panel, JournalSystem system, JournalProfileEditorSession editor)
    {
        AddInputBackground(panel, _stageNameInput, 10f, -134f, -20f, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileStageLabel"), relativeWidth: true);
        AddPanelIconButton(panel, SortAscendingIconTexturePath, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileMoveStageUp"), 10f, -92f, () =>
        {
            editor.MoveSelectedStage(-1);
            system.RefreshView();
        });
        AddPanelIconButton(panel, SortDescendingIconTexturePath, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileMoveStageDown"), 44f, -92f, () =>
        {
            editor.MoveSelectedStage(1);
            system.RefreshView();
        });
        AddPanelTrashButton(panel, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileDeleteStage"), 78f, -92f, () =>
        {
            editor.RemoveSelectedStage();
            system.RefreshView();
        });

        var stage = editor.Document.Stages.First(value =>
            string.Equals(value.Id, editor.SelectedStageId, StringComparison.OrdinalIgnoreCase));
        var accessoryButton = AddPanelIconButton(
            panel,
            TextureAssets.Item[ItemID.DemonHeart],
            Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileAccessorySlotsTooltip", stage.AccessorySlots),
            112f,
            -92f,
            () =>
            {
                editor.SetSelectedStageAccessorySlots(stage.AccessorySlots >= 7 ? 0 : stage.AccessorySlots + 1);
                system.RefreshView();
            });
        accessoryButton.SetBadgeText(stage.AccessorySlots.ToString());
        AddPanelIconButton(panel, TextureAssets.Item[ItemID.GoldenKey], Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileAlwaysAvailableTooltip"), 146f, -92f, () =>
        {
            editor.SetSelectedStageAlwaysAvailable();
            system.RefreshView();
        });
        AddPanelIconButton(panel, LockedIconTexturePath, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileChooseUnlockConditionTooltip"), 180f, -92f, () =>
        {
            _overlay = EditorOverlay.StageUnlockPicker;
            _searchInput.SetText(string.Empty);
            system.RefreshView();
        });
        AddPanelIconButton(panel, AutoIconTexturePath, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileAutoStageIconTooltip"), 10f, -52f, () =>
        {
            editor.ClearSelectedStageIcon();
            system.RefreshView();
        });
        AddPanelIconButton(panel, TextureAssets.Camera[6], Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileChooseStageIconTooltip"), 44f, -52f, () =>
        {
            _overlay = EditorOverlay.StageIconPicker;
            _searchInput.SetText(string.Empty);
            system.RefreshView();
        });
    }

    private void AddEditorContent(JournalSystem system, JournalProfileEditorSession editor)
    {
        var panel = JournalUiElementFactory.CreatePanel();
        panel.Left.Set(252f, 0f);
        panel.Top.Set(148f, 0f);
        panel.Width.Set(-272f, 1f);
        panel.Height.Set(-168f, 1f);
        Append(panel);

        AddPanelButton(
            panel,
            Language.GetTextValue("Mods.ProgressionJournal.UI.OverviewTab"),
            10f,
            10f,
            150f,
            () => { _page = EditorPage.Recommendations; system.RefreshView(); },
            _page == EditorPage.Recommendations);
        AddPanelButton(
            panel,
            Language.GetTextValue("Mods.ProgressionJournal.UI.CombatBuffsTitle"),
            166f,
            10f,
            150f,
            () => { _page = EditorPage.CombatBuffs; system.RefreshView(); },
            _page == EditorPage.CombatBuffs);

        _contentList.Left.Set(10f, 0f);
        _contentList.Top.Set(54f, 0f);
        _contentList.Width.Set(-42f, 1f);
        _contentList.Height.Set(-64f, 1f);
        panel.Append(_contentList);
        _contentScrollbar.Left.Set(-26f, 1f);
        _contentScrollbar.Top.Set(54f, 0f);
        _contentScrollbar.Width.Set(18f, 0f);
        _contentScrollbar.Height.Set(-64f, 1f);
        panel.Append(_contentScrollbar);

        if (string.IsNullOrWhiteSpace(editor.SelectedClassId))
        {
            _contentList.Add(JournalUiElementFactory.CreateSectionHeader(
                Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileAddClassPrompt")));
            return;
        }

        if (_page == EditorPage.CombatBuffs)
        {
            AddCombatBuffEditor(system, editor);
            return;
        }

        var entries = editor.GetSelectedEntries();
        foreach (var tier in new[]
        {
            RecommendationTier.Recommended,
            RecommendationTier.Additional,
            RecommendationTier.NotRecommended,
            RecommendationTier.Useless
        })
        {
            var capturedTier = tier;
            _contentList.Add(JournalContentBuilder.CreateEditableRecommendationBlock(
                tier,
                entries.Where(value => value.Evaluation.Tier == tier).ToArray(),
                category =>
                {
                    _pickerTier = capturedTier;
                    _pickerItemCategory = category;
                    _overlay = EditorOverlay.ItemPicker;
                    _searchInput.SetText(string.Empty);
                    system.RefreshView();
                },
                itemId =>
                {
                    _selectedItemId = itemId;
                    _overlay = EditorOverlay.ItemProperties;
                    system.RefreshView();
                }));
        }
    }

    private void AddCombatBuffEditor(JournalSystem system, JournalProfileEditorSession editor)
    {
        var panel = new JournalCombatBuffPanel(
            Enum.GetValues<JournalBuffCategory>(),
            "Mods.ProgressionJournal.UI.CombatBuffsTitle",
            showTitle: false,
            autoHeight: true,
            slotsPerRow: 6,
            onItemSelected: itemId =>
            {
                editor.RemoveCombatBuff(itemId);
                system.RefreshView();
            },
            onAddCategory: category =>
            {
                _pickerBuffCategory = category;
                _overlay = EditorOverlay.ItemPicker;
                _searchInput.SetText(string.Empty);
                system.RefreshView();
            });
        panel.Width.Set(0f, 1f);
        panel.SetEntries(editor.GetSelectedCombatBuffEntries());
        _contentList.Add(panel);
    }

    private void AddEditorOverlay(JournalSystem system, JournalProfileEditorSession editor)
    {
        var shade = new JournalDimOverlay(() =>
        {
            _overlay = EditorOverlay.None;
            system.RefreshView();
        });
        Append(shade);

        var panel = JournalUiElementFactory.CreatePanel();
        panel.Left.Set(70f, 0f);
        panel.Top.Set(62f, 0f);
        panel.Width.Set(-140f, 1f);
        panel.Height.Set(-92f, 1f);
        Append(panel);

        var closeButton = JournalUiElementFactory.CreateIconButton(CloseIconTexturePath, 32f, 32f, () =>
        {
            _overlay = EditorOverlay.None;
            system.RefreshView();
        }, 0.82f);
        closeButton.Left.Set(-42f, 1f);
        closeButton.Top.Set(10f, 0f);
        closeButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileClose"));
        panel.Append(closeButton);

        switch (_overlay)
        {
            case EditorOverlay.ClassPicker:
                PopulateClassPicker(panel, system, editor);
                break;
            case EditorOverlay.StageIconPicker:
            case EditorOverlay.StageUnlockPicker:
                PopulateNpcPicker(panel, system, editor);
                break;
            case EditorOverlay.ItemProperties:
                PopulateItemProperties(panel, system, editor);
                break;
            default:
                PopulateItemPicker(panel, system, editor);
                break;
        }
    }

    private void PopulateClassPicker(UIElement panel, JournalSystem system, JournalProfileEditorSession editor)
    {
        AddOverlayTitle(panel, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileClassCatalog"));
        AddInputBackground(panel, _searchInput, 20f, 52f, 340f, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileSearchClasses"));
        AddInputBackground(panel, _customClassInput, 372f, 52f, 220f, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileCustomClass"));
        AddPanelIconButton(panel, TextureAssets.Item[ItemID.Wrench], Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileCreateClass"), 602f, 55f, () =>
        {
            editor.AddClass(_customClassInput.CurrentString);
            _customClassInput.SetText(string.Empty);
            _overlay = EditorOverlay.None;
            system.RefreshView();
        });

        ConfigurePickerList(panel, 100f);
        var search = _searchInput.CurrentString.Trim();
        _appliedSearch = _searchInput.CurrentString;
        foreach (var candidate in JournalDamageClassCatalog.GetCandidates().Where(value =>
            string.IsNullOrWhiteSpace(search)
            || value.DisplayName.Contains(search, StringComparison.CurrentCultureIgnoreCase)
            || value.SourceName.Contains(search, StringComparison.CurrentCultureIgnoreCase)))
        {
            var captured = candidate;
            var exists = editor.Document.Classes.Any(value =>
                string.Equals(value.Id, candidate.Id, StringComparison.OrdinalIgnoreCase)
                || value.DamageClassNames.Intersect(candidate.DamageClassNames, StringComparer.OrdinalIgnoreCase).Any());
            var button = JournalUiElementFactory.CreateTextButton(
                $"{candidate.DisplayName} ({candidate.SourceName})",
                0f,
                40f,
                () =>
                {
                    editor.AddClass(captured.Id, captured.DisplayName, captured.DamageClassNames);
                    _overlay = EditorOverlay.None;
                    system.RefreshView();
                },
                0.72f);
            button.Width.Set(0f, 1f);
            button.SetStyle(JournalUiTheme.GetTabButtonStyle(exists));
            _pickerList.Add(button);
        }
    }

    private void PopulateNpcPicker(UIElement panel, JournalSystem system, JournalProfileEditorSession editor)
    {
        AddOverlayTitle(panel, Language.GetTextValue(
            _overlay == EditorOverlay.StageIconPicker
                ? "Mods.ProgressionJournal.UI.ProfileChooseStageIcon"
                : "Mods.ProgressionJournal.UI.ProfileChooseUnlockBoss"));
        AddInputBackground(panel, _searchInput, 20f, 52f, 500f, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileBossSearchHint"));
        ConfigurePickerList(panel, 100f);

        var search = _searchInput.CurrentString;
        _appliedSearch = search;
        foreach (var candidate in JournalStageIconCatalog.GetCandidates(search))
        {
            var captured = candidate;
            var button = JournalUiElementFactory.CreateIconTextButton(
                TextureAssets.NpcHeadBoss[candidate.HeadSlot],
                $"{candidate.DisplayName} ({candidate.ModDisplayName})",
                0f,
                44f,
                () =>
                {
                    if (_overlay == EditorOverlay.StageIconPicker)
                    {
                        editor.SetSelectedStageIcon(captured.ModName, captured.InternalName);
                    }
                    else
                    {
                        editor.SetSelectedStageUnlockBoss(captured.ModName, captured.InternalName);
                    }

                    _overlay = EditorOverlay.None;
                    system.RefreshView();
                },
                0.72f);
            button.Width.Set(0f, 1f);
            _pickerList.Add(button);
        }
    }

    private void PopulateItemPicker(UIElement panel, JournalSystem system, JournalProfileEditorSession editor)
    {
        var isBuff = _page == EditorPage.CombatBuffs;
        var title = isBuff
            ? GetBuffCategoryName(_pickerBuffCategory)
            : $"{GetTierName(_pickerTier)} · {Language.GetTextValue($"Mods.ProgressionJournal.Categories.{_pickerItemCategory}")}";
        AddOverlayTitle(panel, title);
        AddInputBackground(panel, _searchInput, 20f, 52f, 500f, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileItemSearchHint"));
        ConfigurePickerList(panel, 100f);

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
            .Take(300)
            .ToArray();

        const int columns = 11;
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
                var selected = isBuff ? editor.ContainsCombatBuff(itemId) : editor.ContainsItem(itemId);
                var slot = new JournalBuildCandidateSlot(
                    item,
                    selected,
                    disabled: false,
                    () =>
                    {
                        if (isBuff)
                        {
                            editor.AddCombatBuff(itemId, _pickerBuffCategory);
                        }
                        else
                        {
                            editor.AddItem(itemId, _pickerItemCategory, _pickerTier);
                        }

                        system.RefreshView();
                    },
                    () =>
                    {
                        if (isBuff)
                        {
                            editor.RemoveCombatBuff(itemId);
                        }
                        else
                        {
                            editor.RemoveItem(itemId);
                        }

                        system.RefreshView();
                    });
                slot.Left.Set(column * (JournalUiMetrics.BuildSlotSize + 5f), 0f);
                row.Append(slot);
            }

            _pickerList.Add(row);
        }
    }

    private void PopulateItemProperties(UIElement panel, JournalSystem system, JournalProfileEditorSession editor)
    {
        var entry = editor.FindSelectedItemEntry(_selectedItemId);
        if (entry is null || !ContentSamples.ItemsByType.TryGetValue(_selectedItemId, out var item))
        {
            _overlay = EditorOverlay.None;
            return;
        }

        AddOverlayTitle(panel, item.HoverName);
        var evaluation = entry.Evaluations.First(value =>
            string.Equals(value.StageId, editor.SelectedStageId, StringComparison.OrdinalIgnoreCase));
        AddPanelButton(panel, Language.GetTextValue($"Mods.ProgressionJournal.Categories.{entry.Category}"), 20f, 64f, 180f, () =>
        {
            var values = Enum.GetValues<JournalItemCategory>();
            var next = values[(Array.IndexOf(values, entry.Category) + 1) % values.Length];
            editor.SetItemPlacement(_selectedItemId, next, evaluation.Tier);
            system.RefreshView();
        });
        AddPanelButton(panel, GetTierName(evaluation.Tier), 210f, 64f, 190f, () =>
        {
            var values = new[]
            {
                RecommendationTier.Recommended,
                RecommendationTier.Additional,
                RecommendationTier.NotRecommended,
                RecommendationTier.Useless
            };
            var next = values[(Array.IndexOf(values, evaluation.Tier) + 1) % values.Length];
            editor.SetItemPlacement(_selectedItemId, entry.Category, next);
            system.RefreshView();
        });
        AddPanelButton(
            panel,
            Language.GetTextValue(entry.IsSupportWeapon
                ? "Mods.ProgressionJournal.UI.ProfileSupportWeaponOn"
                : "Mods.ProgressionJournal.UI.ProfileSupportWeaponOff"),
            410f,
            64f,
            190f,
            () =>
            {
                editor.SetItemSupportWeapon(_selectedItemId, !entry.IsSupportWeapon);
                system.RefreshView();
            });

        AddLabelTo(panel, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileEventLabel"), 20f, 120f, 0.72f);
        var eventValues = Enum.GetValues<JournalEventCategory>();
        for (var index = 0; index < eventValues.Length; index++)
        {
            var eventCategory = eventValues[index];
            var captured = eventCategory;
            AddPanelButton(
                panel,
                eventCategory.GetDisplayName(),
                20f + (index % 3) * 218f,
                150f + (index / 3) * 40f,
                208f,
                () =>
                {
                    editor.SetItemEvent(_selectedItemId, captured, string.Empty);
                    system.RefreshView();
                },
                entry.EventCategory == eventCategory);
        }

        AddPanelButton(panel, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileNoEvent"), 20f, 270f, 150f, () =>
        {
            editor.SetItemEvent(_selectedItemId, null, string.Empty);
            _customEventInput.SetText(string.Empty);
            system.RefreshView();
        }, entry.EventCategory is null && string.IsNullOrWhiteSpace(entry.CustomEventName));
        AddInputBackground(panel, _customEventInput, 180f, 270f, 300f, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileCustomEvent"));
        AddPanelButton(panel, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileApply"), 490f, 270f, 110f, () =>
        {
            editor.SetItemEvent(_selectedItemId, null, _customEventInput.CurrentString);
            system.RefreshView();
        }, entry.EventCategory is null && !string.IsNullOrWhiteSpace(entry.CustomEventName));
        AddPanelTrashButton(panel, Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileDeleteItem"), 20f, 330f, () =>
        {
            editor.RemoveItem(_selectedItemId);
            _overlay = EditorOverlay.None;
            system.RefreshView();
        });
    }

    private void SyncEditorInputs(JournalProfileEditorSession editor)
    {
        if (!string.Equals(_loadedProfileId, editor.Document.Id, StringComparison.OrdinalIgnoreCase))
        {
            _loadedProfileId = editor.Document.Id;
            _profileNameInput.SetText(editor.Document.Name);
            _searchInput.SetText(string.Empty);
            _appliedSearch = string.Empty;
            _classNameInput.SetText(string.Empty);
            _overlay = EditorOverlay.None;
        }

        if (!string.Equals(_loadedClassId, editor.SelectedClassId, StringComparison.OrdinalIgnoreCase))
        {
            _loadedClassId = editor.SelectedClassId;
            var definition = editor.Document.Classes.FirstOrDefault(value =>
                string.Equals(value.Id, editor.SelectedClassId, StringComparison.OrdinalIgnoreCase));
            _classNameInput.SetText(definition?.Name ?? string.Empty);
        }

        if (!string.Equals(_loadedStageId, editor.SelectedStageId, StringComparison.OrdinalIgnoreCase))
        {
            _loadedStageId = editor.SelectedStageId;
            var stage = editor.Document.Stages.First(value =>
                string.Equals(value.Id, editor.SelectedStageId, StringComparison.OrdinalIgnoreCase));
            _stageNameInput.SetText(stage.Name);
        }

        if (_overlay == EditorOverlay.ItemProperties && _loadedPropertyItemId != _selectedItemId)
        {
            _loadedPropertyItemId = _selectedItemId;
            var entry = editor.FindSelectedItemEntry(_selectedItemId);
            _customEventInput.SetText(entry?.CustomEventName ?? string.Empty);
        }
        else if (_overlay != EditorOverlay.ItemProperties)
        {
            _loadedPropertyItemId = 0;
        }
    }

    private void ConfigureList(UIList list, UIScrollbar scrollbar, float left, float top, float width, float height)
    {
        list.Left.Set(left, 0f);
        list.Top.Set(top, 0f);
        list.Width.Set(width, 1f);
        list.Height.Set(height, 1f);
        Append(list);
        scrollbar.Left.Set(-32f, 1f);
        scrollbar.Top.Set(top, 0f);
        scrollbar.Width.Set(20f, 0f);
        scrollbar.Height.Set(height, 1f);
        Append(scrollbar);
    }

    private void ConfigurePickerList(UIElement panel, float top)
    {
        _pickerList.Left.Set(20f, 0f);
        _pickerList.Top.Set(top, 0f);
        _pickerList.Width.Set(-62f, 1f);
        _pickerList.Height.Set(-top - 20f, 1f);
        panel.Append(_pickerList);
        _pickerScrollbar.Left.Set(-32f, 1f);
        _pickerScrollbar.Top.Set(top, 0f);
        _pickerScrollbar.Width.Set(20f, 0f);
        _pickerScrollbar.Height.Set(-top - 20f, 1f);
        panel.Append(_pickerScrollbar);
    }

    private static void AddOverlayTitle(UIElement panel, string text)
    {
        var title = new UIText(text, 0.66f, true)
        {
            HAlign = 0.5f,
            TextColor = JournalUiTheme.RootTitleText
        };
        title.Top.Set(15f, 0f);
        panel.Append(title);
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

    private void AddTopIconButton(string texturePath, string hoverText, float left, Action action, bool alignRight = false)
    {
        AddTopIconButton(Main.Assets.Request<Texture2D>(texturePath), hoverText, left, action, alignRight);
    }

    private void AddMainIconButton(
        Asset<Texture2D> texture,
        string hoverText,
        float left,
        float top,
        Action action)
    {
        var button = JournalUiElementFactory.CreateIconButton(texture, 32f, 32f, action, 0.72f);
        button.Left.Set(left, 0f);
        button.Top.Set(top, 0f);
        button.SetHoverText(hoverText);
        Append(button);
    }

    private static JournalIconButton AddPanelIconButton(
        UIElement parent,
        string texturePath,
        string hoverText,
        float left,
        float top,
        Action action)
    {
        return AddPanelIconButton(
            parent,
            Main.Assets.Request<Texture2D>(texturePath),
            hoverText,
            left,
            top,
            action);
    }

    private static JournalIconButton AddPanelIconButton(
        UIElement parent,
        Asset<Texture2D> texture,
        string hoverText,
        float left,
        float top,
        Action action)
    {
        var button = JournalUiElementFactory.CreateIconButton(texture, 30f, 30f, action, 0.72f);
        button.Left.Set(left, 0f);
        button.Top.Set(top, top < 0f ? 1f : 0f);
        button.SetHoverText(hoverText);
        parent.Append(button);
        return button;
    }

    private static void AddPanelTrashButton(
        UIElement parent,
        string hoverText,
        float left,
        float top,
        Action action)
    {
        var button = JournalBuildActionButton.CreateTrash(action);
        button.Left.Set(left, 0f);
        button.Top.Set(top, top < 0f ? 1f : 0f);
        button.SetHoverText(hoverText);
        parent.Append(button);
    }

    private static void AddPanelButton(
        UIElement parent,
        string text,
        float left,
        float top,
        float width,
        Action action,
        bool active = false,
        bool alignRight = false)
    {
        var button = JournalUiElementFactory.CreateTextButton(text, width, 32f, action, CompactTextScale);
        button.Left.Set(left, alignRight ? 1f : 0f);
        button.Top.Set(top, top < 0f ? 1f : 0f);
        button.SetStyle(active
            ? JournalUiTheme.GetTabButtonStyle(true)
            : JournalUiTheme.GetDefaultTextButtonStyle());
        parent.Append(button);
    }

    private void AddInputBackground(
        JournalTextInput input,
        float left,
        float top,
        float width,
        string hint)
    {
        AddInputBackground(this, input, left, top, width, hint);
    }

    private void AddCenteredInputBackground(
        JournalTextInput input,
        float top,
        float width,
        string hint)
    {
        var background = JournalUiElementFactory.CreatePanel();
        background.HAlign = 0.5f;
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
        input.Parent?.RemoveChild(input);
        background.Append(input);
    }

    private static void AddInputBackground(
        UIElement parent,
        JournalTextInput input,
        float left,
        float top,
        float width,
        string hint,
        bool relativeWidth = false)
    {
        var background = JournalUiElementFactory.CreatePanel();
        background.Left.Set(left, 0f);
        background.Top.Set(top, top < 0f ? 1f : 0f);
        background.Width.Set(width, relativeWidth ? 1f : 0f);
        background.Height.Set(36f, 0f);
        background.BackgroundColor = JournalUiTheme.PanelBackground;
        background.BorderColor = JournalUiTheme.PanelBorder;
        parent.Append(background);

        input.HintText = hint;
        input.Left.Set(8f, 0f);
        input.Top.Set(8f, 0f);
        input.Width.Set(-16f, 1f);
        input.Parent?.RemoveChild(input);
        background.Append(input);
    }

    private void AddLabel(string text, float left, float top, float scale)
    {
        AddLabelTo(this, text, left, top, scale);
    }

    private static void AddLabelTo(UIElement parent, string text, float left, float top, float scale)
    {
        var label = new UIText(text, scale)
        {
            TextColor = JournalUiTheme.ContentDescriptionText
        };
        label.Left.Set(left, 0f);
        label.Top.Set(top, 0f);
        parent.Append(label);
    }

    private static string GetTierName(RecommendationTier tier) => tier switch
    {
        RecommendationTier.Recommended => Language.GetTextValue("Mods.ProgressionJournal.UI.RecommendedBlock"),
        RecommendationTier.Additional => Language.GetTextValue("Mods.ProgressionJournal.UI.AdditionalBlock"),
        RecommendationTier.NotRecommended => Language.GetTextValue("Mods.ProgressionJournal.UI.NotRecommendedBlock"),
        RecommendationTier.Useless => Language.GetTextValue("Mods.ProgressionJournal.UI.UselessBlock"),
        _ => Language.GetTextValue("Mods.ProgressionJournal.UI.FromGuideBlock")
    };

    private static string GetBuffCategoryName(JournalBuffCategory category) => Language.GetTextValue(category switch
    {
        JournalBuffCategory.Station => "Mods.ProgressionJournal.UI.CombatBuffStations",
        JournalBuffCategory.Passive => "Mods.ProgressionJournal.UI.CombatBuffPassive",
        JournalBuffCategory.Basic => "Mods.ProgressionJournal.UI.CombatBuffBasic",
        JournalBuffCategory.Potion => "Mods.ProgressionJournal.UI.CombatBuffPotions",
        JournalBuffCategory.Eternal => "Mods.ProgressionJournal.UI.CombatBuffEternal",
        JournalBuffCategory.Food => "Mods.ProgressionJournal.UI.CombatBuffFood",
        JournalBuffCategory.Flask => "Mods.ProgressionJournal.UI.CombatBuffFlasks",
        _ => string.Empty
    });
}
