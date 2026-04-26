using System.Collections.Generic;
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

        var keybind = ProgressionJournal.ToggleJournalKeybind;
        if (keybind is null)
        {
            return;
        }

        try
        {
            if (keybind.JustPressed)
            {
                ModContent.GetInstance<JournalSystem>().ToggleView();
            }
        }
        catch (KeyNotFoundException)
        {
            // Happens during hot-reload/hot-unload edge cases.
        }
    }
}