using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalDimOverlay : UIElement
{
    private readonly Action? _onClick;

    public JournalDimOverlay(Action? onClick = null)
    {
        _onClick = onClick;
        Width.Set(0f, 1f);
        Height.Set(0f, 1f);

        if (_onClick is not null)
        {
            OnLeftClick += (_, _) => _onClick();
        }
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        var dimensions = GetDimensions().ToRectangle();
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, dimensions, new Color(4, 8, 14, 190));
    }
}
