using Terraria;
using Terraria.ModLoader;

namespace ProgressionJournal;

public sealed class ProgressionJournal : Mod
{
	public static ProgressionJournal? Instance { get; private set; }

	internal static ModKeybind? ToggleJournalKeybind { get; private set; }

	public override void Load()
	{
		Instance = this;

		if (!Main.dedServ) {
			ToggleJournalKeybind = KeybindLoader.RegisterKeybind(this, "Toggle Progression Journal", "P");
		}
	}

	public override void Unload()
	{
		ToggleJournalKeybind = null;
		Instance = null;
	}
}
