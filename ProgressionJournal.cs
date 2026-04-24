using Terraria;
using Terraria.ModLoader;
using ProgressionJournal.Api;

namespace ProgressionJournal;

public sealed class ProgressionJournal : Mod
{
	internal const string ToggleJournalKeybindName = "ToggleProgressionJournal";

	public static ProgressionJournal? Instance { get; private set; }

	internal static ModKeybind? ToggleJournalKeybind { get; private set; }

	public override void Load()
	{
		Instance = this;

		if (!Main.dedServ) {
			ToggleJournalKeybind = KeybindLoader.RegisterKeybind(this, ToggleJournalKeybindName, "P");
		}
	}

	public override void Unload()
	{
		JournalRepository.ClearExternalContent();
		ToggleJournalKeybind = null;
		Instance = null;
	}

	public override object? Call(params object[] args)
	{
		return ProgressionJournalApi.HandleCall(args);
	}
}
