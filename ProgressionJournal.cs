using Terraria;
using Terraria.ModLoader;
using ProgressionJournal.Api;
using ProgressionJournal.Systems;

namespace ProgressionJournal;

public sealed class ProgressionJournal : Mod
{
	internal const string ToggleJournalKeybindName = "ToggleProgressionJournal";
	internal const string ExportActiveSnapshotKeybindName = "ExportActiveSnapshot";

	public static ProgressionJournal? Instance { get; private set; }

	internal static ModKeybind? ToggleJournalKeybind { get; private set; }

	internal static ModKeybind? ExportActiveSnapshotKeybind { get; private set; }

	internal static bool IsUnloading { get; private set; }

	public override void Load()
	{
		IsUnloading = false;
		Instance = this;

		if (Main.dedServ) return;
		ToggleJournalKeybind = KeybindLoader.RegisterKeybind(this, ToggleJournalKeybindName, "P");
		if (!string.IsNullOrWhiteSpace(SourceFolder) && Directory.Exists(SourceFolder))
		{
			ExportActiveSnapshotKeybind =
				KeybindLoader.RegisterKeybind(this, ExportActiveSnapshotKeybindName, "None");
		}
		JournalBuildChat.RegisterTags();
	}

	public override void Unload()
	{
		IsUnloading = true;

		JournalBuildChat.Unload();
		JournalArmorSetBonusResolver.Clear();
		JournalArmorSetOverviewResolver.ClearCaches();

		JournalRepository.ClearExternalContent();
		ToggleJournalKeybind = null;
		ExportActiveSnapshotKeybind = null;
		Instance = null;
	}

	public override object Call(params object[] args)
	{
		return ProgressionJournalApi.HandleCall(args);
	}
}
