using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalClassButton : JournalHoverPanel
{
    private readonly string _className;
    private readonly CombatClass _visualClass;
    private readonly UIText _title;
    private bool _selected;

    public JournalClassButton(
        JournalProfile profile,
        JournalProfileClassDocument classDefinition,
        bool selected,
        float height)
    {
        _className = JournalProfileText.GetClassName(profile, classDefinition.Id);
        _visualClass = JournalClassIds.ToLegacy(classDefinition.Id);
        _selected = selected;

        Height.Set(height, 0f);
        SetPadding(0f);

        _title = new UIText(_className, JournalUiMetrics.ClassButtonTitleScale, true)
        {
            HAlign = 0.5f
        };
        _title.Top.Set(JournalUiMetrics.ClassButtonTitleTop, 0f);
        Append(_title);

        var characterPreview = new UICharacter(JournalPreviewPlayerFactory.Create(_visualClass), false, false, 1.42f);
        characterPreview.Width.Set(JournalUiMetrics.ClassButtonPreviewWidth, 0f);
        characterPreview.Height.Set(JournalUiMetrics.ClassButtonPreviewHeight, 0f);
        characterPreview.HAlign = 0.5f;
        characterPreview.Top.Set(JournalUiMetrics.ClassButtonPreviewTop, 0f);
        characterPreview.IgnoresMouseInteraction = true;
        Append(characterPreview);

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
        base.DrawSelf(spriteBatch);

        if (IsMouseHovering)
        {
            Main.hoverItemName = _className;
        }
    }

    private void ApplyVisualState()
    {
        var palette = JournalUiTheme.GetClassPalette(_visualClass);
        var emphasis = _selected ? 1f : IsMouseHovering ? 0.52f : 0f;

        BackgroundColor = Color.Lerp(palette.Background, palette.Accent * 0.2f, emphasis);
        BorderColor = Color.Lerp(palette.Border, palette.Accent, _selected ? 0.9f : IsMouseHovering ? 0.55f : 0.18f);
        _title.TextColor = Color.Lerp(palette.Text * 0.88f, Color.White, _selected ? 0.82f : IsMouseHovering ? 0.35f : 0f);
    }
}

