using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalBuildFilterIconButton : JournalHoverPanel
{
    private bool _active;
    private string? _hoverText;
    private Texture2D? _iconTexture;
    private string? _iconTexturePath;
    private Rectangle? _iconSourceRectangle;
    private int _itemIconId;
    private bool _useOptionTileStyle;

    public JournalBuildFilterIconButton(Action onClick)
    {
        var onClick1 = onClick;
        SetPadding(0f);
        OnLeftClick += (_, _) => onClick1();
    }

    public void SetActive(bool active)
    {
        _active = active;
    }

    public void UseOptionTileStyle()
    {
        _useOptionTileStyle = true;
    }

    public void SetHoverText(string hoverText)
    {
        _hoverText = hoverText;
    }

    public void SetIconTexture(Texture2D? iconTexture, Rectangle? sourceRectangle = null)
    {
        _iconTexture = iconTexture;
        _iconTexturePath = null;
        _iconSourceRectangle = sourceRectangle;
        if (iconTexture is not null)
        {
            _itemIconId = 0;
        }
    }

    public void SetIconAsset(string? iconTexturePath, Rectangle? sourceRectangle = null)
    {
        _iconTexture = null;
        _iconTexturePath = iconTexturePath;
        _iconSourceRectangle = sourceRectangle;
        if (!string.IsNullOrWhiteSpace(iconTexturePath))
        {
            _itemIconId = 0;
        }
    }

    public void SetItemIcon(int itemIconId)
    {
        _itemIconId = itemIconId;
        if (itemIconId <= 0) return;

        _iconTexture = null;
        _iconTexturePath = null;
        _iconSourceRectangle = null;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (_useOptionTileStyle)
        {
            DrawOptionTile(spriteBatch);
            return;
        }

        BorderColor = Color.Transparent;
        var bounds = GetDimensions().ToRectangle();
        var iconColor = _active
            ? JournalUiTheme.SectionHeaderText
            : IsMouseHovering
                ? Color.White
                : JournalUiTheme.RootTitleText * 0.86f;
        DrawDynamicIcon(spriteBatch, bounds, iconColor);

        if (IsMouseHovering && !string.IsNullOrWhiteSpace(_hoverText))
        {
            Main.hoverItemName = _hoverText;
        }
    }

    private void DrawOptionTile(SpriteBatch spriteBatch)
    {
        var dimensions = GetDimensions();
        var bounds = dimensions.ToRectangle();
        var panelTexture = Main.Assets.Request<Texture2D>(_active
            ? "Images/UI/CharCreation/PanelGrayscale"
            : "Images/UI/CharCreation/CategoryPanel").Value;

        Utils.DrawSplicedPanel(
            spriteBatch,
            panelTexture,
            (int)dimensions.X,
            (int)dimensions.Y,
            (int)dimensions.Width,
            (int)dimensions.Height,
            10,
            10,
            10,
            10,
            Color.White);

        if (IsMouseHovering && !_active)
        {
            Fill(spriteBatch, bounds.X + 4, bounds.Y + 4, bounds.Width - 8, bounds.Height - 8, Color.White * 0.08f);
        }

        DrawDynamicIcon(spriteBatch, bounds, Color.White);

        if (IsMouseHovering && !string.IsNullOrWhiteSpace(_hoverText))
        {
            Main.hoverItemName = _hoverText;
        }
    }

    private void DrawDynamicIcon(SpriteBatch spriteBatch, Rectangle bounds, Color color)
    {
        if (_iconTexture is not null)
        {
            DrawTextureCentered(spriteBatch, _iconTexture, _iconSourceRectangle, bounds, color, 0.7f);
            return;
        }

        if (!string.IsNullOrWhiteSpace(_iconTexturePath))
        {
            try
            {
                var iconTexture = _iconTexturePath.StartsWith("ProgressionJournal/", StringComparison.Ordinal)
                    ? ModContent.Request<Texture2D>(_iconTexturePath).Value
                    : Main.Assets.Request<Texture2D>(_iconTexturePath).Value;
                DrawTextureCentered(spriteBatch, iconTexture, _iconSourceRectangle, bounds, color, 0.7f);
                return;
            }
            catch
            {
                return;
            }
        }

        if (_itemIconId <= 0 || _itemIconId >= TextureAssets.Item.Length) return;
        Main.instance.LoadItem(_itemIconId);
        DrawTextureCentered(spriteBatch, TextureAssets.Item[_itemIconId].Value, null, bounds, Color.White, 0.7f);
    }

    private static void DrawTextureCentered(
        SpriteBatch spriteBatch,
        Texture2D texture,
        Rectangle? sourceRectangle,
        Rectangle bounds,
        Color color,
        float maxBoundsScale)
    {
        var sourceWidth = sourceRectangle?.Width ?? texture.Width;
        var sourceHeight = sourceRectangle?.Height ?? texture.Height;
        var maxWidth = bounds.Width * maxBoundsScale;
        var maxHeight = bounds.Height * maxBoundsScale;
        var scale = MathF.Min(maxWidth / sourceWidth, maxHeight / sourceHeight);
        var position = new Vector2(bounds.X + bounds.Width * 0.5f, bounds.Y + bounds.Height * 0.5f);
        var origin = new Vector2(sourceWidth * 0.5f, sourceHeight * 0.5f);
        spriteBatch.Draw(texture, position, sourceRectangle, color, 0f, origin, scale, SpriteEffects.None, 0f);
    }

    private static void Fill(SpriteBatch spriteBatch, int x, int y, int width, int height, Color color)
    {
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(x, y, width, height), color);
    }
}
