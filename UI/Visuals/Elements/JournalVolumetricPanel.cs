using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Elements;

namespace ProgressionJournal.UI.Visuals.Elements;

public class JournalVolumetricPanel : UIPanel
{
    public bool DrawShadow { get; init; } = true;
    public bool PreservePalette { get; init; }
    protected string? FrameTextureOverridePath { get; init; }
    public string? BackgroundTextureOverridePath { get; init; }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        JournalVolumetricPanelRenderer.Draw(
            spriteBatch,
            GetDimensions().ToRectangle(),
            BackgroundColor,
            BorderColor,
            DrawShadow,
            PreservePalette,
            FrameTextureOverridePath,
            BackgroundTextureOverridePath);
    }
}
