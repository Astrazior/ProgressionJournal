using Terraria;
using Terraria.ModLoader;

namespace ProgressionJournal.Systems;

public sealed class JournalNpcUnlockSystem : ModSystem
{
    public override void OnWorldLoad()
    {
        JournalNpcUnlockTracker.Clear();
    }

    public override void OnWorldUnload()
    {
        JournalNpcUnlockTracker.Clear();
    }
}

public sealed class JournalNpcUnlockGlobalNpc : GlobalNPC
{
    public override void OnKill(NPC npc)
    {
        if (npc.ModNPC is { } modNpc)
        {
            JournalNpcUnlockTracker.Record(modNpc.Mod.Name, modNpc.Name);
        }
    }
}
