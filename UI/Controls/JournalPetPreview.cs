using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalPetPreview : UIElement
{
    private const int PetProjectileType = ProjectileID.BabyDino;

    public JournalPetPreview()
    {
        Main.instance.LoadProjectile(PetProjectileType);
        IgnoresMouseInteraction = true;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var texture = TextureAssets.Projectile[PetProjectileType].Value;
        var frameCount = Math.Max(1, Main.projFrames[PetProjectileType]);
        var frameHeight = texture.Height / frameCount;
        var source = new Rectangle(0, 0, texture.Width, frameHeight);
        var dimensions = GetDimensions();
        var scale = MathF.Min(dimensions.Width / source.Width, dimensions.Height / source.Height);
        var position = dimensions.Center();

        spriteBatch.Draw(
            texture,
            position,
            source,
            Color.White,
            0f,
            source.Size() * 0.5f,
            MathF.Min(scale, 1.35f),
            SpriteEffects.None,
            0f);
    }
}
