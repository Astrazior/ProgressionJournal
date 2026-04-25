using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProgressionJournal.Systems;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalSavedBuildCharacterPreview(
    Player previewPlayer,
    Func<float> getShadeOpacity,
    float characterScale)
    : UICharacter(previewPlayer, false, false, characterScale)
{
    private readonly JournalPreviewDrawPlayer _previewDrawPlayer = previewPlayer.GetModPlayer<JournalPreviewDrawPlayer>();

    public override void Draw(SpriteBatch spriteBatch)
    {
        _previewDrawPlayer.ShadeOpacity = MathHelper.Clamp(getShadeOpacity(), 0f, 1f);
        base.Draw(spriteBatch);
    }
}
