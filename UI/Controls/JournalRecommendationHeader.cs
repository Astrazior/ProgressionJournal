using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace ProgressionJournal.UI;

public sealed class JournalRecommendationHeader(string title, Color accentColor) : UIElement
{
    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        var dimensions = GetInnerDimensions();
        var font = FontAssets.MouseText.Value;
        const float textScale = 1.06f;

        var titleSize = font.MeasureString(title) * textScale;
        var centerX = dimensions.X + dimensions.Width * 0.5f;
        var textX = centerX - titleSize.X * 0.5f;
        var textY = dimensions.Y + (dimensions.Height - titleSize.Y) * 0.5f - 1f;

        Utils.DrawBorderStringFourWay(
            spriteBatch,
            font,
            title,
            textX,
            textY,
            JournalUiTheme.GetRecommendationHeaderTextColor(accentColor),
            Color.Black * 0.7f,
            Vector2.Zero,
            textScale);
    }
}
