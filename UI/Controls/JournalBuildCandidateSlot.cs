using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Localization;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalBuildCandidateSlot : UIElement
{
    private readonly Item _item;
    private readonly bool _selected;
    private readonly bool _disabled;
    private float _visualScale = 1f;

    public JournalBuildCandidateSlot(
        Item item,
        bool selected,
        bool disabled,
        Action onClick,
        Action? onRightClick = null)
    {
        _item = item.Clone();
        _selected = selected;
        _disabled = disabled;
        Width.Set(JournalUiMetrics.BuildSlotSize, 0f);
        Height.Set(JournalUiMetrics.BuildSlotSize, 0f);
        OnLeftClick += (_, _) =>
        {
            if (!_disabled)
            {
                onClick();
            }
        };
        OnRightClick += (_, _) =>
        {
            if (!_disabled)
            {
                onRightClick?.Invoke();
            }
        };
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        var targetScale = IsMouseHovering && !_disabled ? 1.08f : 1f;
        _visualScale = MathHelper.Lerp(_visualScale, targetScale, 0.35f);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        var dimensions = GetInnerDimensions().ToRectangle();
        var position = dimensions.TopLeft() + new Vector2(
            dimensions.Width * (1f - _visualScale) * 0.5f,
            dimensions.Height * (1f - _visualScale) * 0.5f);
        var oldScale = Main.inventoryScale;
        var displayItem = _item.Clone();
        try
        {
            if (!JournalItemUtilities.IsValidItemId(displayItem.type))
            {
                return;
            }

            Main.instance.LoadItem(displayItem.type);
            Main.inventoryScale = _visualScale;
            var rectangle = new Rectangle(
                (int)position.X,
                (int)position.Y,
                (int)(dimensions.Width * _visualScale),
                (int)(dimensions.Height * _visualScale));
            JournalItemSlotRenderer.Draw(
                spriteBatch,
                displayItem,
                rectangle,
                _selected ? JournalUiTheme.InventoryButtonActiveGlow : JournalUiTheme.ItemSlotDefaultAccent,
                IsMouseHovering && !_disabled,
                _disabled,
                _visualScale);
        }
        finally
        {
            Main.inventoryScale = oldScale;
        }

        if (!IsMouseHovering)
        {
            return;
        }

        var hoverItem = _item.Clone();
        if (_disabled)
        {
            hoverItem.SetNameOverride($"{hoverItem.HoverName} ({Language.GetTextValue("Mods.ProgressionJournal.UI.BuildPickerAlreadySelected")})");
        }

        Main.HoverItem = hoverItem;
        Main.hoverItemName = hoverItem.HoverName;
        Main.mouseText = true;
    }
}
