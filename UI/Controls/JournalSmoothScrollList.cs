using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalSmoothScrollList : UIList
{
    private readonly JournalSmoothScrollController _scroll = new();

    public override void ScrollWheel(UIScrollWheelEvent evt)
    {
        ViewPosition = _scroll.BeginScroll(ViewPosition);
        base.ScrollWheel(evt);
        ViewPosition = _scroll.EndScroll(ViewPosition);
    }

    public override void Update(GameTime gameTime)
    {
        ViewPosition = _scroll.Update(gameTime, ViewPosition);
        base.Update(gameTime);
    }
}
