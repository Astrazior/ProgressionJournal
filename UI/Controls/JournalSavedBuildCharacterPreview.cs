using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProgressionJournal.Systems;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalSavedBuildCharacterPreview : UICharacter
{
    private readonly JournalPreviewDrawPlayer _previewDrawPlayer;
    private readonly Func<bool> _isFocused;
    private readonly float _idleShadeOpacity;
    private readonly float _focusedShadeOpacity;

    public JournalSavedBuildCharacterPreview(
        Player previewPlayer,
        Func<bool> isFocused,
        float characterScale,
        float idleShadeOpacity,
        float focusedShadeOpacity)
        : base(previewPlayer, false, false, characterScale)
    {
        _previewDrawPlayer = previewPlayer.GetModPlayer<JournalPreviewDrawPlayer>();
        _isFocused = isFocused;
        _idleShadeOpacity = MathHelper.Clamp(idleShadeOpacity, 0f, 1f);
        _focusedShadeOpacity = MathHelper.Clamp(focusedShadeOpacity, 0f, 1f);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        _previewDrawPlayer.ShadeOpacity = _isFocused() ? _focusedShadeOpacity : _idleShadeOpacity;
        base.Draw(spriteBatch);
    }
}
