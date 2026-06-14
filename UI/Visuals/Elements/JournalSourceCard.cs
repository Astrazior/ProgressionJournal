using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Elements;

namespace ProgressionJournal.UI.Visuals.Elements;

public sealed class JournalSourceCard : UIPanel
{
    private readonly Color _accent;

    public JournalSourceCard(Color accent)
    {
        _accent = accent;
        SetPadding(0f);
        BackgroundColor = Color.Transparent;
        BorderColor = Color.Transparent;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);
        JournalSourceCardRenderer.Draw(
            spriteBatch,
            GetDimensions().ToRectangle(),
            _accent,
            IsMouseHovering);
    }
}
