using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalDimOverlay : UIElement
{
    public JournalDimOverlay(Action? onClick = null)
    {
        var onClick1 = onClick;
        Width.Set(0f, 1f);
        Height.Set(0f, 1f);

        if (onClick1 is not null)
        {
            OnLeftClick += (_, _) => onClick1();
        }
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        var dimensions = GetDimensions().ToRectangle();
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, dimensions, new Color(4, 8, 14, 190));
    }
}
