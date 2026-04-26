using Terraria;
using Terraria.ModLoader;
using ProgressionJournal.Api;
using ProgressionJournal.Systems;

namespace ProgressionJournal;

public sealed class ProgressionJournal : Mod
{
	internal const string ToggleJournalKeybindName = "ToggleProgressionJournal";

	public static ProgressionJournal? Instance { get; private set; }

	internal static ModKeybind? ToggleJournalKeybind { get; private set; }

	internal static bool IsUnloading { get; private set; }

	public override void Load()
	{
		IsUnloading = false;
		Instance = this;

		if (Main.dedServ) return;
		ToggleJournalKeybind = KeybindLoader.RegisterKeybind(this, ToggleJournalKeybindName, "P");
		JournalBuildChat.RegisterTags();
	}

	public override void Unload()
	{
		IsUnloading = true;

		JournalBuildChat.Unload();

		JournalRepository.ClearExternalContent();
		ToggleJournalKeybind = null;
		Instance = null;
	}

	public override object Call(params object[] args)
	{
		return ProgressionJournalApi.HandleCall(args);
	}
}
