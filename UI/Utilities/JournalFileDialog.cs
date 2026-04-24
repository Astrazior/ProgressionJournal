using System;
using System.IO;
using Terraria.Utilities.FileBrowser;

namespace ProgressionJournal.UI.Utilities;

public static class JournalFileDialog
{
    private static readonly ExtensionFilter[] BuildFileFilters =
    [
        new("Progression Journal build", ["pjbuild.json", "json"]),
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

    private static string EnsureBuildFileExtension(string filePath)
    {
        if (filePath.EndsWith(".pjbuild.json", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Path.GetExtension(filePath), ".json", StringComparison.OrdinalIgnoreCase))
        {
            return filePath;
        }

        return $"{filePath}.pjbuild.json";
    }
}
