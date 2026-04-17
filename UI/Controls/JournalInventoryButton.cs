using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProgressionJournal.Systems;
using ReLogic.Content;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalInventoryButton : UIElement
{
    private static readonly Asset<Texture2D> IconTexture =
        ModContent.Request<Texture2D>("ProgressionJournal/Assets/UI/JournalButtonIcon");

    public JournalInventoryButton()
    {
        Width.Set(JournalUiMetrics.InventoryButtonSize, 0f);
        Height.Set(JournalUiMetrics.InventoryButtonSize, 0f);
        OnLeftClick += (_, _) => JournalSystem.ToggleView();
    }

    public void UpdatePlacement()
    {
        var defensePosition = AccessorySlotLoader.DefenseIconPosition;
        if (defensePosition == Vector2.Zero)
        {
            return;
        }

        var x = defensePosition.X
            - JournalUiMetrics.InventoryButtonAccessorySlotStep * 2f
            - Width.Pixels
            - JournalUiMetrics.InventoryButtonHorizontalSpacing;
        var y = defensePosition.Y - Height.Pixels - JournalUiMetrics.InventoryButtonVerticalSpacing;

        Left.Set(MathF.Round(x), 0f);
        Top.Set(MathF.Round(y), 0f);
        Recalculate();
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        var dimensions = GetDimensions();
        var icon = IconTexture.Value;
        var scale = MathF.Min((dimensions.Width - 2f) / icon.Width, (dimensions.Height - 2f) / icon.Height);
        var pulseScale = JournalSystem.Visible ? 1.06f : IsMouseHovering ? 1.02f : 1f;
        var origin = icon.Size() * 0.5f;
        var center = dimensions.Center();
        var glowColor = JournalSystem.Visible
            ? JournalUiTheme.InventoryButtonActiveGlow
            : IsMouseHovering
                ? JournalUiTheme.InventoryButtonHoverGlow
                : Color.Transparent;

        if (glowColor != Color.Transparent)
        {
            var glowScale = scale * (pulseScale + 0.08f);
            spriteBatch.Draw(icon, center + new Vector2(-1f, 0f), null, glowColor * 0.22f, 0f, origin, glowScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(icon, center + new Vector2(1f, 0f), null, glowColor * 0.22f, 0f, origin, glowScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(icon, center + new Vector2(0f, -1f), null, glowColor * 0.22f, 0f, origin, glowScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(icon, center + new Vector2(0f, 1f), null, glowColor * 0.22f, 0f, origin, glowScale, SpriteEffects.None, 0f);
        }

        spriteBatch.Draw(icon, center + new Vector2(1f, 2f), null, JournalUiTheme.InventoryButtonShadow * 0.55f, 0f, origin, scale * pulseScale, SpriteEffects.None, 0f);
        spriteBatch.Draw(icon, center, null, Color.White, 0f, origin, scale * pulseScale, SpriteEffects.None, 0f);

        if (IsMouseHovering)
        {
            Main.hoverItemName = Language.GetTextValue("Mods.ProgressionJournal.UI.InventoryButtonTooltip");
        }
    }

    private static JournalSystem JournalSystem => ModContent.GetInstance<JournalSystem>();
}

