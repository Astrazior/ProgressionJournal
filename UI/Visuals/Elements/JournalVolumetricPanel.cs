using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Elements;

namespace ProgressionJournal.UI.Visuals.Elements;

public class JournalVolumetricPanel : UIPanel
{
    public bool DrawShadow { get; init; } = true;

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        JournalVolumetricPanelRenderer.Draw(
            spriteBatch,
            GetDimensions().ToRectangle(),
            BackgroundColor,
            BorderColor,
            DrawShadow);
    }
}
