using Terraria;

namespace ProgressionJournal.UI.Utilities;

public static class JournalItemUtilities
{
    public static Item CreateItem(int itemId)
    {
        var item = new Item();
        item.SetDefaults(itemId);
        return item;
    }
}

