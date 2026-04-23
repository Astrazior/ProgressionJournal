using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalBuildEquipmentSlot : UIElement
{
    private readonly string _shortLabel;
    private readonly string _hoverText;
    private readonly Func<int> _getSelectedItemId;

    public JournalBuildEquipmentSlot(
        string shortLabel,
        string hoverText,
        Func<int> getSelectedItemId,
        Action onClick,
        Action onRightClick)
    {
        _shortLabel = shortLabel;
        _hoverText = hoverText;
        _getSelectedItemId = getSelectedItemId;
        Width.Set(JournalUiMetrics.BuildSlotSize, 0f);
        Height.Set(JournalUiMetrics.BuildSlotSize, 0f);
        OnLeftClick += (_, _) => onClick();
        OnRightClick += (_, _) => onRightClick();
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        var itemId = _getSelectedItemId();
        var dimensions = GetInnerDimensions().ToRectangle();
        var oldScale = Main.inventoryScale;

        try
        {
            Main.inventoryScale = 1f;

            if (itemId > ItemID.None)
            {
                var item = JournalItemUtilities.CreateItem(itemId);
                Main.instance.LoadItem(item.type);
                ItemSlot.Draw(spriteBatch, ref item, ItemSlot.Context.TrashItem, dimensions.TopLeft());
            }
            else
            {
                spriteBatch.Draw(TextureAssets.InventoryBack9.Value, dimensions, Color.White * 0.45f);
                Utils.DrawBorderStringFourWay(
                    spriteBatch,
                    FontAssets.MouseText.Value,
                    _shortLabel,
                    dimensions.X + 8f,
                    dimensions.Y + 13f,
                    JournalUiTheme.RootTitleText,
                    Color.Black,
                    Vector2.Zero,
                    0.72f);
            }
        }
        finally
        {
            Main.inventoryScale = oldScale;
        }

        if (!IsMouseHovering)
        {
            return;
        }

        if (itemId > ItemID.None)
        {
            var hoverItem = JournalItemUtilities.CreateItem(itemId);
            Main.HoverItem = hoverItem;
            Main.hoverItemName = hoverItem.HoverName;
        }
        else
        {
            Main.HoverItem = new Item();
            Main.hoverItemName = _hoverText;
            Main.mouseText = true;
        }

        DrawOutline(spriteBatch, dimensions, JournalUiTheme.PanelBorder);
    }

    private static void DrawOutline(SpriteBatch spriteBatch, Rectangle rectangle, Color color)
    {
        var texture = TextureAssets.MagicPixel.Value;
        spriteBatch.Draw(texture, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, 1), color);
        spriteBatch.Draw(texture, new Rectangle(rectangle.X, rectangle.Bottom - 1, rectangle.Width, 1), color);
        spriteBatch.Draw(texture, new Rectangle(rectangle.X, rectangle.Y, 1, rectangle.Height), color);
        spriteBatch.Draw(texture, new Rectangle(rectangle.Right - 1, rectangle.Y, 1, rectangle.Height), color);
    }
}
