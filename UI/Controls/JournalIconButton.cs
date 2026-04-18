using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalIconButton : JournalHoverPanel
{
    private const float IconPadding = 2f;

    private readonly Asset<Texture2D> _iconTexture;
    private readonly float _iconScale;

    public JournalIconButton(Asset<Texture2D> iconTexture, float iconScale, Action onClick)
    {
        _iconTexture = iconTexture;
        _iconScale = iconScale;
        SetPadding(0f);
        SetStyle(JournalUiTheme.GetDefaultTextButtonStyle());
        OnLeftClick += (_, _) => onClick();
    }

    public void SetStyle(JournalButtonStyle style)
    {
        BackgroundColor = Color.Transparent;
        BorderColor = Color.Transparent;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
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

            spriteBatch.Draw(texture, center + new Vector2(-1f, 0f), null, glowColor, 0f, new Vector2(texture.Width * 0.5f, texture.Height * 0.5f), glowScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture, center + new Vector2(1f, 0f), null, glowColor, 0f, new Vector2(texture.Width * 0.5f, texture.Height * 0.5f), glowScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture, center + new Vector2(0f, -1f), null, glowColor, 0f, new Vector2(texture.Width * 0.5f, texture.Height * 0.5f), glowScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture, center + new Vector2(0f, 1f), null, glowColor, 0f, new Vector2(texture.Width * 0.5f, texture.Height * 0.5f), glowScale, SpriteEffects.None, 0f);
            scale *= 1.04f;
        }

        var drawColor = IsMouseHovering ? Color.White : Color.White * 0.95f;

        spriteBatch.Draw(
            texture,
            dimensions.Center.ToVector2(),
            null,
            drawColor,
            0f,
            new Vector2(texture.Width * 0.5f, texture.Height * 0.5f),
            scale,
            SpriteEffects.None,
            0f);
    }
}
