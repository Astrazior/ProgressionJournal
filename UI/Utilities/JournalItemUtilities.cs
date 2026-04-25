using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ProgressionJournal.UI.Utilities;

public static class JournalItemUtilities
{
    public static bool IsValidItemId(int itemId)
    {
        return itemId > ItemID.None && itemId < ItemLoader.ItemCount;
    }

    public static bool TryCreateItem(int itemId, out Item item)
    {
        item = new Item();
        if (!IsValidItemId(itemId))
        {
            item.TurnToAir();
            return false;
        }

        item.SetDefaults(itemId);
        return !item.IsAir;
    }

    public static Item CreateItem(int itemId)
    {
        TryCreateItem(itemId, out var item);
        return item;
    }
}

