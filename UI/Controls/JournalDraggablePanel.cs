using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalDraggablePanel : UIPanel
{
    private readonly List<UIElement> _dragTargets = [];
    private Vector2 _dragOffset;
    private bool _dragging;

    public void AddDragTarget(UIElement element)
    {
        _dragTargets.Add(element);
    }

    public void ResetDragState()
    {
        _dragging = false;
        _dragOffset = Vector2.Zero;
    }

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        StartDragging(evt);
        base.LeftMouseDown(evt);
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        StopDragging(evt);
        base.LeftMouseUp(evt);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (_dragging)
        {
            Left.Set(Main.MouseScreen.X - _dragOffset.X, 0f);
            Top.Set(Main.MouseScreen.Y - _dragOffset.Y, 0f);
            Recalculate();
        }

        base.DrawSelf(spriteBatch);
    }

    private void StartDragging(UIMouseEvent evt)
    {
        if (evt.Target != this && !_dragTargets.Contains(evt.Target))
        {
            return;
        }

        _dragOffset = new Vector2(evt.MousePosition.X - Left.Pixels, evt.MousePosition.Y - Top.Pixels);
        _dragging = true;
    }

    private void StopDragging(UIMouseEvent evt)
    {
        if (evt.Target != this && !_dragTargets.Contains(evt.Target))
        {
            return;
        }

        _dragging = false;
        _dragOffset = Vector2.Zero;
    }
}
