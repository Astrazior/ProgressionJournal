using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalSmoothScrollGrid : UIGrid
{
    private readonly JournalSmoothScrollController _scroll = new();
    private UIScrollbar? _smoothScrollbar;

    public void SetSmoothScrollbar(UIScrollbar scrollbar)
    {
        _smoothScrollbar = scrollbar;
        SetScrollbar(scrollbar);
    }

    public override void ScrollWheel(UIScrollWheelEvent evt)
    {
        if (_smoothScrollbar is null)
        {
            base.ScrollWheel(evt);
            return;
        }

        _smoothScrollbar.ViewPosition = _scroll.BeginScroll(_smoothScrollbar.ViewPosition);
        base.ScrollWheel(evt);
        _smoothScrollbar.ViewPosition = _scroll.EndScroll(_smoothScrollbar.ViewPosition);
    }

    public override void Update(GameTime gameTime)
    {
        if (_smoothScrollbar is not null)
        {
            _smoothScrollbar.ViewPosition = _scroll.Update(gameTime, _smoothScrollbar.ViewPosition);
        }

        base.Update(gameTime);
    }
}
