using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProgressionJournal.Systems;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalProfileManagerPanel : JournalVolumetricPanel
{
    private const string CloseIconTexturePath = "Images/UI/SearchCancel";
    private const float WarningAreaHeight = 138f;
    private const float WarningTextScale = 0.68f;
    private const float WarningLineHeight = 20f;

    private readonly UIList _profileList;
    private readonly UIScrollbar _profileScrollbar;

    public JournalProfileManagerPanel()
    {
        SetPadding(0f);
        BackgroundColor = JournalUiTheme.RootBackground * JournalUiTheme.RootBackgroundOpacity;
        BorderColor = JournalUiTheme.RootBorder;

        _profileList = new JournalSmoothScrollList { ListPadding = 6f };
        _profileScrollbar = new UIScrollbar();
        _profileList.SetScrollbar(_profileScrollbar);
    }

    public void Refresh(JournalSystem system)
    {
        RemoveAllChildren();
        _profileList.Clear();

        var title = new UIText(Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileManagerTitle"), 0.74f, true)
        {
            HAlign = 0.5f,
            TextColor = JournalUiTheme.RootTitleText
        };
        title.Top.Set(12f, 0f);
        Append(title);

        var closeButton = JournalUiElementFactory.CreateIconButton(
            Main.Assets.Request<Texture2D>(CloseIconTexturePath),
            36f,
            36f,
            system.CloseProfileManager,
            0.78f);
        closeButton.Left.Set(-44f, 1f);
        closeButton.Top.Set(52f, 0f);
        closeButton.SetHoverText(Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileClose"));
        Append(closeButton);

        _profileList.Left.Set(20f, 0f);
        _profileList.Top.Set(112f, 0f);
        _profileList.Width.Set(-62f, 1f);
        _profileList.Height.Set(-(132f + WarningAreaHeight), 1f);
        Append(_profileList);

        _profileScrollbar.Left.Set(-32f, 1f);
        _profileScrollbar.Top.Set(112f, 0f);
        _profileScrollbar.Width.Set(20f, 0f);
        _profileScrollbar.Height.Set(-(132f + WarningAreaHeight), 1f);
        Append(_profileScrollbar);

        foreach (var profile in JournalProfileRegistry.All)
        {
            var warning = profile.HasVersionMismatch
                ? $"  {Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileVersionWarning")}"
                : string.Empty;
            var profileIcon = JournalProfileIconResolver.GetIcon(profile);
            var button = JournalUiElementFactory.CreateIconTextButton(
                profileIcon.Texture,
                $"{profile.DisplayName}{warning}",
                0f,
                48f,
                () => system.SelectProfile(profile.Id),
                0.78f,
                profileIcon.SourceRectangle);
            button.Width.Set(0f, 1f);
            button.SetStyle(JournalUiTheme.GetTabButtonStyle(
                string.Equals(profile.Id, JournalProfileRegistry.Active.Id, StringComparison.OrdinalIgnoreCase)));
            _profileList.Add(button);
        }

        AppendProfileWarning();
    }

    private void AppendProfileWarning()
    {
        const float horizontalInset = 20f;

        var warningContainer = new UIElement();
        warningContainer.Left.Set(horizontalInset, 0f);
        warningContainer.Top.Set(-WarningAreaHeight, 1f);
        warningContainer.Width.Set(-(horizontalInset * 2f), 1f);
        warningContainer.Height.Set(WarningAreaHeight - 12f, 0f);
        Append(warningContainer);

        var alertIcon = new JournalConditionAlertIcon();
        alertIcon.HAlign = 0.5f;
        warningContainer.Append(alertIcon);

        var maxTextWidth = MathF.Max(
            1f,
            Width.Pixels - horizontalInset * 4f);
        var lines = JournalTextUtilities.WrapToPixelWidth(
            Language.GetTextValue("Mods.ProgressionJournal.UI.ProfileManagerWarning"),
            maxTextWidth,
            WarningTextScale);

        var top = alertIcon.Height.Pixels;
        foreach (var line in lines)
        {
            var warningText = new UIText(line, WarningTextScale)
            {
                HAlign = 0.5f,
                TextColor = JournalUiTheme.SectionHeaderText
            };
            warningText.Top.Set(top, 0f);
            warningContainer.Append(warningText);
            top += WarningLineHeight;
        }
    }
}
