using Terraria;
using Terraria.ModLoader;
using ProgressionJournal.Api;
using ProgressionJournal.Systems;
using System.IO;

namespace ProgressionJournal;

public sealed class ProgressionJournal : Mod
{
	internal const string ToggleJournalKeybindName = "ToggleProgressionJournal";

	public static ProgressionJournal? Instance { get; private set; }

	internal static ModKeybind? ToggleJournalKeybind { get; private set; }

	public override void Load()
	{
		Instance = this;

		if (Main.dedServ) return;
		ToggleJournalKeybind = KeybindLoader.RegisterKeybind(this, ToggleJournalKeybindName, "P");
		JournalBuildChat.RegisterTags();
	}

	public override void Unload()
	{
		JournalRepository.ClearExternalContent();
		ToggleJournalKeybind = null;
		Instance = null;
	}

	public override object Call(params object[] args)
	{
		return ProgressionJournalApi.HandleCall(args);
	}

	public override void HandlePacket(BinaryReader reader, int whoAmI)
	{
		JournalBuildChat.HandlePacket(reader, whoAmI);
	}
}
