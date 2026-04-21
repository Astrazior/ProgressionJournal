using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalStageButton : JournalHoverPanel
{
    private const float DefaultTextScale = 0.9f;
    private const float IconPadding = 6f;
    private const float IconOverlap = 10f;

    private static readonly Asset<Texture2D> CompletedMarkerTexture =
        ModContent.Request<Texture2D>("ProgressionJournal/Assets/UI/StageCompletedCheck");
    private static readonly Asset<Texture2D> LockedMarkerTexture =
        Main.Assets.Request<Texture2D>("Images/UI/Bestiary/Icon_Locked");

    private readonly List<(HeadTextureKind Kind, int Slot)> _headSlots = [];
    private UIText _label;
    private float _textScale;
    private string _tooltipText = string.Empty;
    private bool _isCompleted;
    private bool _isInteractable = true;
    private bool _showLockedMarker;
    private Color _textColor;
    private JournalButtonStyle _style;

    private enum HeadTextureKind
    {
        Boss,
        Town
    }

    public JournalStageButton(Action onClick)
    {
        _textScale = DefaultTextScale;
        _textColor = JournalUiTheme.GetStageButtonStyle(active: false).Text;
        SetPadding(0f);

        _label = CreateLabel(string.Empty);
        Append(_label);
        SetStyle(JournalUiTheme.GetStageButtonStyle(active: false));

        OnLeftClick += (_, _) =>
        {
            if (_isInteractable)
            {
                onClick();
            }
        };
    }

    public void SetTextDisplay(string text, float textScale)
    {
        _headSlots.Clear();

        if (Math.Abs(_textScale - textScale) >= 0.001f)
        {
            _textScale = textScale;
            RemoveChild(_label);
            _label = CreateLabel(text);
            Append(_label);
            return;
        }

        _label.SetText(text);
    }

    public void SetTooltip(string tooltipText)
    {
        _tooltipText = tooltipText;
    }

    public void SetCompleted(bool isCompleted)
    {
        _isCompleted = isCompleted;
    }

    public void SetInteractable(bool isInteractable)
    {
        _isInteractable = isInteractable;
    }

    public void SetBossHeadDisplay(params int[] bossHeadSlots)
    {
        _showLockedMarker = false;
        _headSlots.Clear();
        foreach (var bossHeadSlot in bossHeadSlots)
        {
            _headSlots.Add((HeadTextureKind.Boss, bossHeadSlot));
        }

        _label.SetText(string.Empty);
    }

    public void SetNpcHeadDisplay(int npcHeadSlot)
    {
        _showLockedMarker = false;
        _headSlots.Clear();
        _headSlots.Add((HeadTextureKind.Town, npcHeadSlot));
        _label.SetText(string.Empty);
    }

    public void SetLockedDisplay()
    {
        _showLockedMarker = true;
        _headSlots.Clear();
        _label.SetText(string.Empty);
    }

    public void SetStyle(JournalButtonStyle style)
    {
        _style = style;
        BackgroundColor = style.Background;
        BorderColor = style.Border;
        SetTextColor(style.Text);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var canHighlight = _isInteractable && IsMouseHovering;

        BackgroundColor = canHighlight
            ? Color.Lerp(_style.Background, Color.White, 0.12f)
            : _style.Background;
        BorderColor = canHighlight
            ? Color.Lerp(_style.Border, Color.White, 0.24f)
            : _style.Border;
        SetTextColor(canHighlight
            ? Color.Lerp(_style.Text, Color.White, 0.16f)
            : _style.Text);

        base.DrawSelf(spriteBatch);

        if (IsMouseHovering && !string.IsNullOrWhiteSpace(_tooltipText))
        {
            Main.hoverItemName = _tooltipText;
        }

        if (_isCompleted)
        {
            DrawCompletedMarker(spriteBatch);
        }

        if (_headSlots.Count == 0)
        {
            if (_showLockedMarker)
            {
                DrawLockedMarker(spriteBatch);
            }

            return;
        }

        var dimensions = GetInnerDimensions();
        var maxWidth = Math.Max(0f, dimensions.Width - IconPadding * 2f);
        var maxHeight = Math.Max(0f, dimensions.Height - IconPadding * 2f);
        if (maxWidth <= 0f || maxHeight <= 0f)
        {
            return;
        }

        var slotWidth = (maxWidth + IconOverlap * (_headSlots.Count - 1)) / _headSlots.Count;
        if (slotWidth <= 0f)
        {
            return;
        }

        var totalWidth = slotWidth * _headSlots.Count - IconOverlap * (_headSlots.Count - 1);
        var startX = dimensions.Center().X - totalWidth * 0.5f + slotWidth * 0.5f;
        var shadowColor = new Color(10, 12, 20) * 0.55f;
        var iconColor = canHighlight ? Color.White : new Color(235, 240, 245);

        for (var index = 0; index < _headSlots.Count; index++)
        {
            if (!TryGetHeadTexture(_headSlots[index], out var texture))
            {
                continue;
            }

            var iconWidth = texture.Width;
            var iconHeight = texture.Height;
            var scale = MathF.Min(slotWidth / iconWidth, maxHeight / iconHeight);
            scale = MathF.Min(scale, 1.35f);

            var drawPosition = new Vector2(startX + index * (slotWidth - IconOverlap), dimensions.Center().Y);
            var origin = new Vector2(iconWidth * 0.5f, iconHeight * 0.5f);

            spriteBatch.Draw(texture, drawPosition + new Vector2(1f, 2f), null, shadowColor, 0f, origin, scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture, drawPosition, null, iconColor, 0f, origin, scale, SpriteEffects.None, 0f);
        }
    }

    private void SetTextColor(Color color)
    {
        _textColor = color;
        _label.TextColor = color;
    }
    private UIText CreateLabel(string text)
    {
        return new UIText(text, _textScale)
        {
            HAlign = 0.5f,
            VAlign = 0.5f,
            TextColor = _textColor
        };
    }

    private static bool TryGetHeadTexture((HeadTextureKind Kind, int Slot) head, out Texture2D texture)
    {
        switch (head.Kind)
        {
            case HeadTextureKind.Boss when head.Slot >= 0 && head.Slot < TextureAssets.NpcHeadBoss.Length:
                texture = TextureAssets.NpcHeadBoss[head.Slot].Value;
                return true;

            case HeadTextureKind.Town when head.Slot >= 0 && head.Slot < TextureAssets.NpcHead.Length:
                texture = TextureAssets.NpcHead[head.Slot].Value;
                return true;

            default:
                texture = null!;
                return false;
        }
    }

    private void DrawCompletedMarker(SpriteBatch spriteBatch)
    {
        var dimensions = GetDimensions();
        var texture = CompletedMarkerTexture.Value;
        var position = new Vector2(dimensions.X + dimensions.Width - 16f, dimensions.Y + 6f);
        spriteBatch.Draw(texture, position, IsMouseHovering ? Color.White : Color.White * 0.92f);
    }

    private void DrawLockedMarker(SpriteBatch spriteBatch)
    {
        var dimensions = GetInnerDimensions();
        var texture = LockedMarkerTexture.Value;
        var maxWidth = Math.Max(1f, dimensions.Width - IconPadding * 2f);
        var maxHeight = Math.Max(1f, dimensions.Height - IconPadding * 2f);
        var scale = MathF.Min(maxWidth / texture.Width, maxHeight / texture.Height);
        scale = MathF.Min(scale, 1.1f);

        var center = dimensions.Center();
        var position = new Vector2(center.X, center.Y);
        var origin = new Vector2(texture.Width * 0.5f, texture.Height * 0.5f);
        var drawColor = _isInteractable && IsMouseHovering ? Color.White : new Color(235, 240, 245);

        spriteBatch.Draw(texture, position, null, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
    }
}

