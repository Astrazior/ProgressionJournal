using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalBuildActionButton : JournalHoverPanel
{
    private const string FavoriteIconTexturePath = "Images/UI/Bestiary/Icon_Rank_Light";
    private const string ExportIconTexturePath = "Images/UI/IconQuickload";
    private const float Padding = 2f;

    private readonly ButtonKind _kind;
    private readonly Asset<Texture2D>? _iconTexture;
    private readonly Color _iconColor;
    private readonly Color _hoverIconColor;
    private string? _hoverText;

    private JournalBuildActionButton(
        ButtonKind kind,
        Action onClick,
        Color iconColor,
        Color hoverIconColor)
    {
        _kind = kind;
        var onClick1 = onClick;
        _iconColor = iconColor;
        _hoverIconColor = hoverIconColor;

        SetPadding(0f);
        Width.Set(30f, 0f);
        Height.Set(30f, 0f);
        BackgroundColor = Color.Transparent;
        BorderColor = Color.Transparent;
        OnLeftClick += (_, _) => onClick1();

        if (kind == ButtonKind.Favorite)
        {
            _iconTexture = Main.Assets.Request<Texture2D>(FavoriteIconTexturePath);
        }
        else if (kind == ButtonKind.Export)
        {
            _iconTexture = Main.Assets.Request<Texture2D>(ExportIconTexturePath);
        }
    }

    public static JournalBuildActionButton CreateFavorite(bool active, Action onClick)
    {
        return new JournalBuildActionButton(
            ButtonKind.Favorite,
            onClick,
            active ? new Color(255, 226, 82) : new Color(164, 176, 188),
            active ? new Color(255, 242, 142) : new Color(222, 230, 238));
    }

    public static JournalBuildActionButton CreateTrash(Action onClick)
    {
        return new JournalBuildActionButton(
            ButtonKind.Trash,
            onClick,
            new Color(208, 196, 188),
            new Color(255, 164, 164));
    }

    public static JournalBuildActionButton CreateEdit(Action onClick)
    {
        return new JournalBuildActionButton(
            ButtonKind.Edit,
            onClick,
            Color.White,
            Color.White);
    }

    public static JournalBuildActionButton CreateExport(Action onClick)
    {
        return new JournalBuildActionButton(
            ButtonKind.Export,
            onClick,
            Color.White,
            Color.White);
    }

    public void SetHoverText(string hoverText)
    {
        _hoverText = hoverText;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        DrawIcon(spriteBatch);

        if (IsMouseHovering && !string.IsNullOrWhiteSpace(_hoverText))
        {
            Main.hoverItemName = _hoverText;
        }
    }

    private void DrawIcon(SpriteBatch spriteBatch)
    {
        var dimensions = GetDimensions().ToRectangle();
        var texture = GetIconTexture();
        var availableSize = MathF.Max(1f, MathF.Min(dimensions.Width, dimensions.Height) - Padding * 2f);
        var scale = MathF.Min(availableSize / texture.Width, availableSize / texture.Height);
        if (IsMouseHovering)
        {
            scale *= 1.08f;
        }

        spriteBatch.Draw(
            texture,
            dimensions.Center.ToVector2(),
            null,
            IsMouseHovering ? _hoverIconColor : _iconColor,
            0f,
            texture.Size() * 0.5f,
            scale,
            SpriteEffects.None,
            0f);
    }

    private Texture2D GetIconTexture()
    {
        if (_kind == ButtonKind.Trash)
        {
            return TextureAssets.Trash.Value;
        }

        if (_kind != ButtonKind.Edit)
        {
            return _iconTexture!.Value;
        }

        Main.instance.LoadItem(ItemID.Wrench);
        return TextureAssets.Item[ItemID.Wrench].Value;
    }

    private enum ButtonKind
    {
        Favorite,
        Edit,
        Export,
        Trash
    }
}
