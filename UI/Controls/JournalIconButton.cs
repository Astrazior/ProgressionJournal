using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalIconButton : JournalHoverPanel
{
    private const float IconPadding = 2f;

    private Asset<Texture2D> _iconTexture;
    private readonly float _iconScale;
    private readonly float _iconRotation;
    private string? _hoverText;
    private bool _showChrome;
    private JournalButtonStyle _chromeStyle = JournalUiTheme.GetDefaultTextButtonStyle();

    public JournalIconButton(Asset<Texture2D> iconTexture, float iconScale, Action onClick, float iconRotation = 0f)
    {
        _iconTexture = iconTexture;
        _iconScale = iconScale;
        _iconRotation = iconRotation;
        SetPadding(0f);
        SetStyle(JournalUiTheme.GetDefaultTextButtonStyle());
        OnLeftClick += (_, _) => onClick();
    }

    public void SetStyle(JournalButtonStyle style)
    {
        BackgroundColor = Color.Transparent;
        BorderColor = Color.Transparent;
    }

    public void SetHoverText(string hoverText)
    {
        _hoverText = hoverText;
    }

    public void SetIcon(Asset<Texture2D> iconTexture)
    {
        _iconTexture = iconTexture;
    }

    public void EnableChrome(JournalButtonStyle style)
    {
        _showChrome = true;
        _chromeStyle = style;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (_showChrome)
        {
            BackgroundColor = IsMouseHovering
                ? Color.Lerp(_chromeStyle.Background, Color.White, 0.14f)
                : _chromeStyle.Background;
            BorderColor = IsMouseHovering
                ? Color.Lerp(_chromeStyle.Border, Color.White, 0.28f)
                : _chromeStyle.Border;
            base.DrawSelf(spriteBatch);
        }

        var dimensions = GetDimensions().ToRectangle();
        var texture = _iconTexture.Value;
        var availableWidth = Math.Max(1f, dimensions.Width - IconPadding * 2f);
        var availableHeight = Math.Max(1f, dimensions.Height - IconPadding * 2f);
        var scale = MathF.Min(availableWidth / texture.Width, availableHeight / texture.Height) * _iconScale;
        if (IsMouseHovering)
        {
            var glowScale = scale * 1.08f;
            var glowColor = Color.White * 0.2f;
            var center = dimensions.Center.ToVector2();
            var origin = new Vector2(texture.Width * 0.5f, texture.Height * 0.5f);

            spriteBatch.Draw(texture, center + new Vector2(-1f, 0f), null, glowColor, _iconRotation, origin, glowScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture, center + new Vector2(1f, 0f), null, glowColor, _iconRotation, origin, glowScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture, center + new Vector2(0f, -1f), null, glowColor, _iconRotation, origin, glowScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture, center + new Vector2(0f, 1f), null, glowColor, _iconRotation, origin, glowScale, SpriteEffects.None, 0f);
            scale *= 1.04f;
        }

        var drawColor = IsMouseHovering ? Color.White : Color.White * 0.95f;
        var drawOrigin = new Vector2(texture.Width * 0.5f, texture.Height * 0.5f);

        spriteBatch.Draw(
            texture,
            dimensions.Center.ToVector2(),
            null,
            drawColor,
            _iconRotation,
            drawOrigin,
            scale,
            SpriteEffects.None,
            0f);

        if (IsMouseHovering && !string.IsNullOrWhiteSpace(_hoverText))
        {
            Main.hoverItemName = _hoverText;
        }
    }
}
