using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace ProgressionJournal.UI.Visuals.Elements;

public sealed class JournalSourceSectionHeader : UIElement
{
    private const float IconSize = 30f;
    private const float TextScale = 0.7f;

    private readonly string _title;
    private readonly int _iconItemId;
    private readonly Color _accent;

    public JournalSourceSectionHeader(string title, int iconItemId, Color accent)
    {
        _title = title;
        _iconItemId = iconItemId;
        _accent = accent;
        Height.Set(42f, 0f);
        IgnoresMouseInteraction = true;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        var bounds = GetDimensions();
        var pixel = TextureAssets.MagicPixel.Value;
        var font = FontAssets.MouseText.Value;
        var textSize = font.MeasureString(_title) * TextScale;
        var iconCenter = new Vector2(bounds.X + IconSize * 0.5f + 2f, bounds.Y + bounds.Height * 0.5f);

        if (JournalItemUtilities.TryCreateItem(_iconItemId, out var item))
        {
            Main.instance.LoadItem(item.type);
            var texture = TextureAssets.Item[item.type].Value;
            var source = Main.itemAnimations[item.type]?.GetFrame(texture) ?? texture.Bounds;
            var scale = MathF.Min(IconSize / source.Width, IconSize / source.Height);
            spriteBatch.Draw(texture, iconCenter, source, Color.White, 0f, source.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }

        var textX = bounds.X + IconSize + 12f;
        var textY = bounds.Y + (bounds.Height - textSize.Y) * 0.5f + 1f;
        Utils.DrawBorderStringFourWay(
            spriteBatch,
            font,
            _title,
            textX,
            textY,
            Color.Lerp(JournalUiTheme.RootTitleText, _accent, 0.24f),
            Color.Black * 0.7f,
            Vector2.Zero,
            TextScale);

        var lineX = textX + textSize.X + 10f;
        var lineWidth = Math.Max(0, (int)(bounds.X + bounds.Width - lineX - 5f));
        if (lineWidth > 0)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle((int)lineX, (int)(bounds.Y + bounds.Height * 0.5f + 2f), lineWidth, 1),
                _accent * 0.42f);
        }
    }
}
