using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalConditionAlertIcon : UIElement
{
    private const int EmoteTextureIndex = 48;
    private const int EmoteColumns = 8;
    private const int EmoteFrames = 2;

    public JournalConditionAlertIcon()
    {
        Width.Set(48f, 0f);
        Height.Set(40f, 0f);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        var texture = TextureAssets.Extra[EmoteTextureIndex].Value;
        var emoteId = EmoteID.EmotionAlert;
        var animationFrame = (int)(Main.GameUpdateCount / 24 % EmoteFrames);
        var sourceRectangle = texture.Frame(
            EmoteColumns,
            EmoteBubble.EMOTE_SHEET_VERTICAL_FRAMES,
            emoteId * EmoteFrames % EmoteColumns + animationFrame,
            1 + emoteId / (EmoteColumns / EmoteFrames));
        var bounds = GetDimensions().ToRectangle();
        var scale = MathF.Min(
            bounds.Width / (float)sourceRectangle.Width,
            bounds.Height / (float)sourceRectangle.Height);

        spriteBatch.Draw(
            texture,
            bounds.Center.ToVector2(),
            sourceRectangle,
            Color.White,
            0f,
            sourceRectangle.Size() * 0.5f,
            scale,
            SpriteEffects.None,
            0f);
    }
}
