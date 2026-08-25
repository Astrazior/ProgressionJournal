using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace ProgressionJournal.UI.Visuals.Elements;

public sealed class JournalMechanicalBossDivider : UIElement
{
    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);
        JournalTooltipRenderer.DrawRule(spriteBatch, GetInnerDimensions().ToRectangle());
    }
}

public sealed class JournalMechanicalBossHelpMarker(string hoverText) : UIElement
{
    private const float MarkerTextScale = 0.72f;

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        var dimensions = GetInnerDimensions();
        var hovered = dimensions.ToRectangle().Contains(Main.MouseScreen.ToPoint());
        const string markerText = "?";
        var font = FontAssets.MouseText.Value;
        var textSize = font.MeasureString(markerText) * MarkerTextScale;
        Utils.DrawBorderStringFourWay(
            spriteBatch,
            font,
            markerText,
            dimensions.X + (dimensions.Width - textSize.X) * 0.5f,
            dimensions.Y + (dimensions.Height - textSize.Y) * 0.5f + 2f,
            hovered ? Color.White : JournalUiTheme.SectionHeaderText,
            Color.Black * 0.7f,
            Vector2.Zero,
            MarkerTextScale);

        if (hovered)
        {
            JournalTooltip.Request(hoverText);
        }
    }
}
