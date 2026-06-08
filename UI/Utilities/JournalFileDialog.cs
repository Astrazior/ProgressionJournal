using Terraria.Utilities.FileBrowser;

namespace ProgressionJournal.UI.Utilities;

public static class JournalFileDialog
{
    private static readonly ExtensionFilter[] BuildFileFilters =
    [
        new("Progression Journal build", ["pjbuild.json", "json"]),
        new("JSON", ["json"])
    ];

    private static readonly ExtensionFilter[] ProfileFileFilters =
    [
        new("Progression Journal profile", ["pjprofile.json", "json"]),
        new("JSON", ["json"])
    ];

    public static bool TryShowOpenBuildDialog(out string filePath)
    {
        filePath = new NativeFileDialog().OpenFilePanel("Import Progression Journal build", BuildFileFilters);
        return !string.IsNullOrWhiteSpace(filePath);
    }

    public static bool TryShowSaveBuildDialog(out string filePath)
    {
        filePath = string.Empty;

        var result = nativefiledialog.NFD_SaveDialog("pjbuild.json,json", null, out var selectedPath);
        if (!string.Equals(result.ToString(), "NFD_OKAY", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(selectedPath))
        {
            return false;
        }

        filePath = EnsureBuildFileExtension(selectedPath);
        return true;
    }

    public static bool TryShowOpenProfileDialog(out string filePath)
    {
        filePath = new NativeFileDialog().OpenFilePanel("Import Progression Journal profile", ProfileFileFilters);
        return !string.IsNullOrWhiteSpace(filePath);
    }

    public static bool TryShowSaveProfileDialog(string suggestedName, out string filePath)
    {
        filePath = string.Empty;
        var result = nativefiledialog.NFD_SaveDialog("pjprofile.json,json", null, out var selectedPath);
        if (!string.Equals(result.ToString(), "NFD_OKAY", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(selectedPath))
        {
            return false;
        }

        filePath = EnsureProfileFileExtension(selectedPath);
        return true;
    }

    private static string EnsureBuildFileExtension(string filePath)
    {
        if (filePath.EndsWith(".pjbuild.json", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Path.GetExtension(filePath), ".json", StringComparison.OrdinalIgnoreCase))
        {
            return filePath;
        }

        return $"{filePath}.pjbuild.json";
    }

    private static string EnsureProfileFileExtension(string filePath)
    {
        if (filePath.EndsWith(JournalProfileStorage.FileExtension, StringComparison.OrdinalIgnoreCase)
            || string.Equals(Path.GetExtension(filePath), ".json", StringComparison.OrdinalIgnoreCase))
        {
            return filePath;
        }

        return $"{filePath}{JournalProfileStorage.FileExtension}";
    }
}
