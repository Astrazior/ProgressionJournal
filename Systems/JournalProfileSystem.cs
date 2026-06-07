using Terraria;
using Terraria.ModLoader;

namespace ProgressionJournal.Systems;

public sealed class JournalProfileSystem : ModSystem
{
    public override void PostSetupContent()
    {
        if (Main.dedServ)
        {
            return;
        }

        JournalProfileRegistry.Load(JournalRepository.GetAllVanillaEntries());
    }

    public override void Unload()
    {
        JournalProfileRegistry.Unload();
    }
}
