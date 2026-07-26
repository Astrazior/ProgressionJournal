using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalScrollbar : UIScrollbar
{
    private static readonly Asset<Texture2D> TrackTexture =
        Main.Assets.Request<Texture2D>("Images/UI/Scrollbar");
    private static readonly Asset<Texture2D> HandleTexture =
        Main.Assets.Request<Texture2D>("Images/UI/ScrollbarInner");

    private bool _isDragging;
    private float _dragYOffset;

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);
        if (evt.Target != this)
        {
            return;
        }

        var handle = GetHandleRectangle();
        if (!handle.Contains(evt.MousePosition.ToPoint()))
        {
            return;
        }

        _isDragging = true;
        _dragYOffset = evt.MousePosition.Y - handle.Y;
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        base.LeftMouseUp(evt);
        _isDragging = false;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var inner = GetInnerDimensions();
        if (_isDragging)
        {
            var relativeMouseY =
                UserInterface.ActiveInstance.MousePosition.Y
                - inner.Y
                - _dragYOffset;
            ViewPosition = MathHelper.Clamp(
                relativeMouseY / inner.Height * MaxViewSize,
                0f,
                MaxViewSize - ViewSize);
        }

        var bounds = GetDimensions().ToRectangle();
        var handle = GetHandleRectangle();
        var handleHovered = IsMouseHovering
                            && handle.Contains(Main.MouseScreen.ToPoint());

        DrawBar(
            spriteBatch,
            TrackTexture.Value,
            bounds,
            new Color(72, 78, 86));
        DrawBar(
            spriteBatch,
            HandleTexture.Value,
            handle,
            handleHovered || _isDragging
                ? Color.White
                : Color.White * 0.85f);
    }

    private Rectangle GetHandleRectangle()
    {
        var inner = GetInnerDimensions();
        var maxViewSize = Math.Max(1f, MaxViewSize);
        var handleY = (int)(inner.Y + inner.Height * (ViewPosition / maxViewSize)) - 3;
        var handleHeight = (int)(inner.Height * (ViewSize / maxViewSize)) + 7;
        return new Rectangle(
            (int)inner.X,
            handleY,
            Math.Min(20, Math.Max(1, (int)inner.Width)),
            Math.Max(7, handleHeight));
    }

    private static void DrawBar(
        SpriteBatch spriteBatch,
        Texture2D texture,
        Rectangle bounds,
        Color color)
    {
        spriteBatch.Draw(
            texture,
            new Rectangle(bounds.X, bounds.Y - 6, bounds.Width, 6),
            new Rectangle(0, 0, texture.Width, 6),
            color);
        spriteBatch.Draw(
            texture,
            bounds,
            new Rectangle(0, 6, texture.Width, 4),
            color);
        spriteBatch.Draw(
            texture,
            new Rectangle(bounds.X, bounds.Bottom, bounds.Width, 6),
            new Rectangle(0, texture.Height - 6, texture.Width, 6),
            color);
    }
}
