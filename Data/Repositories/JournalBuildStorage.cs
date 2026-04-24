using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Terraria;
using Terraria.Localization;

namespace ProgressionJournal.Data.Repositories;

public static class JournalBuildStorage
{
    private const string BuildDirectoryName = "Builds";
    private const string FileExtension = ".json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static IReadOnlyList<JournalSavedBuild>? _cachedBuilds;

    public static IReadOnlyList<JournalSavedBuild> GetBuilds(ProgressionStageId stageId, CombatClass combatClass)
    {
        EnsureLoaded();

        return _cachedBuilds!
            .Where(build => build.StageId == stageId && build.CombatClass == combatClass)
            .OrderBy(build => build.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public static void Reload()
    {
        _cachedBuilds = LoadBuilds();
    }

    public static bool SaveBuild(
        string name,
        CombatClass combatClass,
        ProgressionStageId stageId,
        IReadOnlyDictionary<string, int> selectedItems,
        out string errorMessage)
    {
        try
        {
            Directory.CreateDirectory(GetBuildDirectoryPath());

            var normalizedSelections = selectedItems
                .Where(static pair => pair.Value > 0
                    && JournalBuildPlannerCatalog.TryGetSlotKind(pair.Key, out _))
                .ToDictionary(
                    static pair => pair.Key,
                    static pair => pair.Value,
                    StringComparer.OrdinalIgnoreCase);

            if (normalizedSelections.Count == 0)
            {
                errorMessage = Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveNoItems");
                return false;
            }

            var document = new JournalBuildDocument
            {
                Version = 1,
                Name = name.Trim(),
                CombatClass = combatClass.ToString(),
                StageId = stageId.ToString(),
                SelectedItems = normalizedSelections
            };

            var filePath = GetUniqueBuildFilePath(document.Name);
            File.WriteAllText(filePath, JsonSerializer.Serialize(document, SerializerOptions), Encoding.UTF8);
            Reload();
            errorMessage = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            LogWarning("Failed to save build json.", exception);
            errorMessage = Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveFailed");
            return false;
        }
    }

    private static void EnsureLoaded()
    {
        _cachedBuilds ??= LoadBuilds();
    }

    private static IReadOnlyList<JournalSavedBuild> LoadBuilds()
    {
        var directoryPath = GetBuildDirectoryPath();
        if (!Directory.Exists(directoryPath))
        {
            return [];
        }

        List<JournalSavedBuild> builds = [];
        foreach (var filePath in Directory.EnumerateFiles(directoryPath, $"*{FileExtension}", SearchOption.TopDirectoryOnly))
        {
            if (TryLoadBuild(filePath, out var build))
            {
                builds.Add(build);
            }
        }

        return builds;
    }

    private static bool TryLoadBuild(string filePath, out JournalSavedBuild build)
    {
        build = null!;

        try
        {
            var document = JsonSerializer.Deserialize<JournalBuildDocument>(File.ReadAllText(filePath), SerializerOptions);
            if (document is null
                || string.IsNullOrWhiteSpace(document.Name)
                || string.IsNullOrWhiteSpace(document.CombatClass)
                || string.IsNullOrWhiteSpace(document.StageId)
                || document.SelectedItems.Count == 0
                || !Enum.TryParse(document.CombatClass, ignoreCase: true, out CombatClass combatClass)
                || !Enum.TryParse(document.StageId, ignoreCase: true, out ProgressionStageId stageId))
            {
                return false;
            }

            var selectedItems = document.SelectedItems
                .Where(static pair => pair.Value > 0
                    && JournalBuildPlannerCatalog.TryGetSlotKind(pair.Key, out _))
                .ToDictionary(
                    static pair => pair.Key,
                    static pair => pair.Value,
                    StringComparer.OrdinalIgnoreCase);

            if (selectedItems.Count == 0)
            {
                return false;
            }

            build = new JournalSavedBuild(document.Name.Trim(), combatClass, stageId, selectedItems, filePath);
            return true;
        }
        catch (Exception exception)
        {
            LogWarning($"Failed to load build json from '{filePath}'.", exception);
            return false;
        }
    }

    private static string GetBuildDirectoryPath()
    {
        return Path.Combine(Main.SavePath, "Mods", nameof(ProgressionJournal), BuildDirectoryName);
    }

    private static string GetUniqueBuildFilePath(string buildName)
    {
        var directoryPath = GetBuildDirectoryPath();
        var slug = SlugifyFileName(buildName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var baseFileName = $"{slug}_{timestamp}";
        var filePath = Path.Combine(directoryPath, $"{baseFileName}{FileExtension}");
        var suffix = 1;

        while (File.Exists(filePath))
        {
            filePath = Path.Combine(directoryPath, $"{baseFileName}_{suffix}{FileExtension}");
            suffix++;
        }

        return filePath;
    }

    private static string SlugifyFileName(string buildName)
    {
        var builder = new StringBuilder(buildName.Length);
        foreach (var character in buildName.Trim())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                continue;
            }

            if (character is ' ' or '-' or '_'
                && (builder.Length == 0 || builder[^1] != '_'))
            {
                builder.Append('_');
            }
        }

        return builder.Length == 0 ? "build" : builder.ToString().Trim('_');
    }

    private static void LogWarning(string message, Exception exception)
    {
        if (ProgressionJournal.Instance?.Logger is { } logger)
        {
            logger.Warn($"{message}{Environment.NewLine}{exception}");
        }
    }

    private sealed class JournalBuildDocument
    {
        public int Version { get; set; }

        public string Name { get; set; } = string.Empty;

        public string CombatClass { get; set; } = string.Empty;

        public string StageId { get; set; } = string.Empty;

        public Dictionary<string, int> SelectedItems { get; set; } = [];
    }
}
