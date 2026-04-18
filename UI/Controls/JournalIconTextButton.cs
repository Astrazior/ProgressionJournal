using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalIconTextButton : JournalHoverPanel
{
    private const float IconPaddingLeft = 10f;
    private const float IconSize = 20f;
    private const float TextLeft = 36f;
    private const float TextRight = 10f;

    private readonly Asset<Texture2D> _iconTexture;
    private readonly UIText _label;
    private JournalButtonStyle _style;
    private string? _hoverText;

    public JournalIconTextButton(Asset<Texture2D> iconTexture, string text, float textScale, Action onClick)
    {
        _iconTexture = iconTexture;
        SetPadding(0f);

        _label = new UIText(text, textScale)
        {
            VAlign = 0.5f
        };
        _label.Left.Set(TextLeft, 0f);
        _label.Width.Set(-(TextLeft + TextRight), 1f);
        Append(_label);

        SetStyle(JournalUiTheme.GetDefaultTextButtonStyle());
        OnLeftClick += (_, _) => onClick();
    }

    public void SetText(string text) => _label.SetText(text);

    public void SetHoverText(string hoverText) => _hoverText = hoverText;

    public void SetStyle(JournalButtonStyle style)
    {
        _style = style;
        BackgroundColor = style.Background;
        BorderColor = style.Border;
        _label.TextColor = style.Text;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        BackgroundColor = IsMouseHovering
            ? Color.Lerp(_style.Background, Color.White, 0.14f)
            : _style.Background;
        BorderColor = IsMouseHovering
            ? Color.Lerp(_style.Border, Color.White, 0.28f)
            : _style.Border;
        _label.TextColor = IsMouseHovering
            ? Color.Lerp(_style.Text, Color.White, 0.18f)
            : _style.Text;

        base.DrawSelf(spriteBatch);

        var dimensions = GetDimensions().ToRectangle();
        var texture = _iconTexture.Value;
        var scale = MathF.Min(IconSize / texture.Width, IconSize / texture.Height);
        var iconCenter = new Vector2(dimensions.X + IconPaddingLeft + IconSize * 0.5f, dimensions.Center.Y);
        var drawColor = IsMouseHovering ? Color.White : Color.White * 0.95f;

        spriteBatch.Draw(
            texture,
            iconCenter,
            null,
            drawColor,
            0f,
            new Vector2(texture.Width * 0.5f, texture.Height * 0.5f),
            scale,
            SpriteEffects.None,
            0f);

        if (IsMouseHovering && !string.IsNullOrWhiteSpace(_hoverText))
        {
            Main.hoverItemName = _hoverText;
        }
    }
}
