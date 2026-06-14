using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalClassButton : JournalHoverPanel
{
    private readonly string _className;
    private readonly JournalProfileClassDocument _classDefinition;
    private readonly UIText _title;
    private bool _selected;

    public JournalClassButton(
        JournalProfile profile,
        JournalProfileClassDocument classDefinition,
        bool selected,
        float height)
    {
        _className = JournalProfileText.GetClassName(profile, classDefinition.Id);
        _classDefinition = classDefinition;
        _selected = selected;

        Height.Set(height, 0f);
        SetPadding(0f);

        _title = new UIText(_className, JournalUiMetrics.ClassButtonTitleScale, true)
        {
            HAlign = 0.5f
        };
        _title.Top.Set(JournalUiMetrics.ClassButtonTitleTop + 5f, 0f);

        var previewPlayer = JournalPreviewPlayerFactory.Create(classDefinition);
        var characterPreview = new UICharacter(previewPlayer, false, false, 1.42f);
        characterPreview.Width.Set(JournalUiMetrics.ClassButtonPreviewWidth, 0f);
        characterPreview.Height.Set(JournalUiMetrics.ClassButtonPreviewHeight, 0f);
        characterPreview.HAlign = 0.5f;
        characterPreview.Top.Set(JournalUiMetrics.ClassButtonPreviewTop, 0f);
        characterPreview.IgnoresMouseInteraction = true;
        Append(characterPreview);

        if (!JournalClassIds.TryToLegacy(classDefinition.Id, out _)
            && classDefinition.PreviewArmor.Count == 0
            && classDefinition.PreviewMount is null)
        {
            var petPreview = new JournalPetPreview();
            petPreview.Left.Set(18f, 0.5f);
            petPreview.Top.Set(104f, 0f);
            petPreview.Width.Set(34f, 0f);
            petPreview.Height.Set(34f, 0f);
            Append(petPreview);
        }

        Append(_title);
        ApplyVisualState();
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        ApplyVisualState();
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        ApplyVisualState();
        JournalClassCardRenderer.Draw(
            spriteBatch,
            GetDimensions().ToRectangle(),
            JournalUiTheme.GetClassPalette(_classDefinition),
            _selected,
            IsMouseHovering);

        if (IsMouseHovering)
        {
            Main.hoverItemName = _className;
        }
    }

    private void ApplyVisualState()
    {
        var palette = JournalUiTheme.GetClassPalette(_classDefinition);
        var emphasis = _selected ? 1f : IsMouseHovering ? 0.52f : 0f;

        BackgroundColor = Color.Lerp(palette.Background, palette.Accent * 0.2f, emphasis);
        BorderColor = Color.Lerp(palette.Border, palette.Accent, _selected ? 0.9f : IsMouseHovering ? 0.55f : 0.18f);
        _title.TextColor = Color.Lerp(palette.Text * 0.88f, Color.White, _selected ? 0.82f : IsMouseHovering ? 0.35f : 0f);
    }
}

