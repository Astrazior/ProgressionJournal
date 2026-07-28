using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace ProgressionJournal.Systems;

public sealed class JournalInputPlayer : ModPlayer
{
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (Main.dedServ || ProgressionJournal.IsUnloading)
        {
            return;
        }

        try
        {
            if (ProgressionJournal.ToggleJournalKeybind?.JustPressed == true)
            {
                ModContent.GetInstance<JournalSystem>().ToggleView();
            }

            if (ProgressionJournal.ExportActiveSnapshotKeybind?.JustPressed == true)
            {
                Commands.ExportProgressionSnapshotCommand
                    .ExportActiveDevelopmentSnapshot(static message => Main.NewText(message));
            }
        }
        catch (KeyNotFoundException)
        {
            // Happens during hot-reload/hot-unload edge cases.
        }
    }
}
