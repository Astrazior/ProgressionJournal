using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ProgressionJournal.Data.Repositories;

public static class JournalBuildStorage
{
    private const string BuildDirectoryName = "Builds";
    private const string BuildFormat = "ProgressionJournalBuild";
    private const string FileExtension = ".json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static readonly JsonSerializerOptions CompactSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static IReadOnlyList<JournalSavedBuild>? _cachedBuilds;

    public static IReadOnlyList<JournalSavedBuild> GetBuilds(ProgressionStageId stageId, CombatClass combatClass)
    {
        EnsureLoaded();

        return _cachedBuilds!
            .Where(build => build.StageId == stageId && build.CombatClass == combatClass)
            .OrderByDescending(build => build.IsFavorite)
            .ThenByDescending(build => build.FavoriteSortKey)
            .ThenBy(build => build.Name, StringComparer.CurrentCultureIgnoreCase)
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

            var itemReferences = CreateItemReferences(selectedItems);
            var normalizedSelections = GetLoadedSelectedItems(itemReferences);

            if (normalizedSelections.Count == 0)
            {
                errorMessage = Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveNoItems");
                return false;
            }

            var document = new JournalBuildDocument
            {
                Format = BuildFormat,
                Version = 2,
                Name = name.Trim(),
                CombatClass = combatClass.ToString(),
                StageId = stageId.ToString(),
                IsFavorite = false,
                FavoriteSortKey = 0L,
                SelectedItems = normalizedSelections,
                SelectedItemRefs = CreateItemReferenceDocuments(itemReferences)
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

    public static bool UpdateBuild(
        JournalSavedBuild build,
        string name,
        CombatClass combatClass,
        ProgressionStageId stageId,
        IReadOnlyDictionary<string, int> selectedItems,
        out string errorMessage)
    {
        try
        {
            if (!TryGetSafeBuildPath(build.SourcePath, out var filePath) || !File.Exists(filePath))
            {
                errorMessage = Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveFailed");
                return false;
            }

            var itemReferences = CreateItemReferences(selectedItems);
            var normalizedSelections = GetLoadedSelectedItems(itemReferences);

            if (normalizedSelections.Count == 0)
            {
                errorMessage = Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveNoItems");
                return false;
            }

            var document = JsonSerializer.Deserialize<JournalBuildDocument>(File.ReadAllText(filePath), SerializerOptions)
                ?? new JournalBuildDocument();
            document.Version = 2;
            document.Format = BuildFormat;
            document.Name = name.Trim();
            document.CombatClass = combatClass.ToString();
            document.StageId = stageId.ToString();
            document.SelectedItems = normalizedSelections;
            document.SelectedItemRefs = CreateItemReferenceDocuments(itemReferences);

            File.WriteAllText(filePath, JsonSerializer.Serialize(document, SerializerOptions), Encoding.UTF8);
            Reload();
            errorMessage = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            LogWarning($"Failed to update build json at '{build.SourcePath}'.", exception);
            errorMessage = Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSaveFailed");
            return false;
        }
    }

    public static bool DeleteBuild(JournalSavedBuild build)
    {
        try
        {
            if (!TryGetSafeBuildPath(build.SourcePath, out var filePath) || !File.Exists(filePath))
            {
                return false;
            }

            File.Delete(filePath);
            Reload();
            return true;
        }
        catch (Exception exception)
        {
            LogWarning($"Failed to delete build json from '{build.SourcePath}'.", exception);
            return false;
        }
    }

    public static bool ExportBuild(JournalSavedBuild build, string exportPath)
    {
        if (string.IsNullOrWhiteSpace(exportPath))
        {
            return false;
        }

        try
        {
            if (!TryGetSafeBuildPath(build.SourcePath, out var filePath) || !File.Exists(filePath))
            {
                return false;
            }

            var document = JsonSerializer.Deserialize<JournalBuildDocument>(File.ReadAllText(filePath), SerializerOptions);
            if (document is null)
            {
                return false;
            }
            NormalizeDocumentCollections(document);

            document.Format = BuildFormat;
            document.Version = 2;
            document.IsFavorite = false;
            document.FavoriteSortKey = 0L;
            var itemReferences = ReadItemReferences(document);
            document.SelectedItems = GetLoadedSelectedItems(itemReferences);
            document.SelectedItemRefs = CreateItemReferenceDocuments(itemReferences);

            var directoryPath = Path.GetDirectoryName(Path.GetFullPath(exportPath));
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(exportPath, JsonSerializer.Serialize(document, SerializerOptions), Encoding.UTF8);
            return true;
        }
        catch (Exception exception)
        {
            LogWarning($"Failed to export build json from '{build.SourcePath}'.", exception);
            return false;
        }
    }

    public static bool TryExportBuildPayload(JournalSavedBuild build, out string payload)
    {
        payload = string.Empty;

        try
        {
            var document = CreateDocument(build.Name, build.CombatClass, build.StageId, build.ItemReferences);
            var json = JsonSerializer.Serialize(document, CompactSerializerOptions);
            payload = ToBase64Url(Encoding.UTF8.GetBytes(json));
            return true;
        }
        catch (Exception exception)
        {
            LogWarning($"Failed to create build chat payload for '{build.SourcePath}'.", exception);
            return false;
        }
    }

    public static bool TryReadBuildPayload(string payload, out JournalSavedBuild build)
    {
        build = null!;

        try
        {
            if (string.IsNullOrWhiteSpace(payload)
                || !TryReadBuildDocumentFromJson(
                    Encoding.UTF8.GetString(FromBase64Url(payload)),
                    out var document,
                    out var combatClass,
                    out var stageId,
                    out var itemReferences))
            {
                return false;
            }

            build = new JournalSavedBuild(
                document.Name.Trim(),
                combatClass,
                stageId,
                itemReferences,
                isFavorite: false,
                favoriteSortKey: 0L,
                sourcePath: string.Empty);
            return true;
        }
        catch (Exception exception)
        {
            LogWarning("Failed to read build chat payload.", exception);
            return false;
        }
    }

    public static bool ImportBuild(string filePath, out string importedName)
    {
        importedName = string.Empty;

        try
        {
            Directory.CreateDirectory(GetBuildDirectoryPath());

            if (!TryReadBuildDocument(filePath, out var document, out _, out _, out var itemReferences))
            {
                return false;
            }

            document.Format = BuildFormat;
            document.Version = 2;
            document.Name = document.Name.Trim();
            document.IsFavorite = false;
            document.FavoriteSortKey = 0L;
            document.SelectedItems = GetLoadedSelectedItems(itemReferences);
            document.SelectedItemRefs = CreateItemReferenceDocuments(itemReferences);

            var destinationPath = GetUniqueBuildFilePath(document.Name);
            File.WriteAllText(destinationPath, JsonSerializer.Serialize(document, SerializerOptions), Encoding.UTF8);
            importedName = document.Name;
            Reload();
            return true;
        }
        catch (Exception exception)
        {
            LogWarning($"Failed to import build json from '{filePath}'.", exception);
            return false;
        }
    }

    public static bool ImportBuild(JournalSavedBuild build, out string importedName)
    {
        importedName = string.Empty;

        try
        {
            Directory.CreateDirectory(GetBuildDirectoryPath());

            var document = CreateDocument(build.Name, build.CombatClass, build.StageId, build.ItemReferences);
            var destinationPath = GetUniqueBuildFilePath(document.Name);
            File.WriteAllText(destinationPath, JsonSerializer.Serialize(document, SerializerOptions), Encoding.UTF8);
            importedName = document.Name;
            Reload();
            return true;
        }
        catch (Exception exception)
        {
            LogWarning($"Failed to import shared build '{build.Name}'.", exception);
            return false;
        }
    }

    public static bool SetFavorite(JournalSavedBuild build, bool isFavorite)
    {
        try
        {
            if (!TryGetSafeBuildPath(build.SourcePath, out var filePath))
            {
                return false;
            }

            var document = JsonSerializer.Deserialize<JournalBuildDocument>(File.ReadAllText(filePath), SerializerOptions);
            if (document is null)
            {
                return false;
            }
            NormalizeDocumentCollections(document);

            document.IsFavorite = isFavorite;
            document.FavoriteSortKey = isFavorite ? DateTime.UtcNow.Ticks : 0L;
            File.WriteAllText(filePath, JsonSerializer.Serialize(document, SerializerOptions), Encoding.UTF8);
            Reload();
            return true;
        }
        catch (Exception exception)
        {
            LogWarning($"Failed to update build favorite state for '{build.SourcePath}'.", exception);
            return false;
        }
    }

    private static void EnsureLoaded()
    {
        _cachedBuilds ??= LoadBuilds();
    }

    private static List<JournalSavedBuild> LoadBuilds()
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
            if (document is not null)
            {
                NormalizeDocumentCollections(document);
            }
            if (document is null
                || string.IsNullOrWhiteSpace(document.Name)
                || string.IsNullOrWhiteSpace(document.CombatClass)
                || string.IsNullOrWhiteSpace(document.StageId)
                || (document.SelectedItems.Count == 0 && document.SelectedItemRefs.Count == 0)
                || !Enum.TryParse(document.CombatClass, ignoreCase: true, out CombatClass combatClass)
                || !Enum.TryParse(document.StageId, ignoreCase: true, out ProgressionStageId stageId))
            {
                return false;
            }

            var selectedItems = ReadItemReferences(document);

            if (selectedItems.Count == 0)
            {
                return false;
            }

            build = new JournalSavedBuild(
                document.Name.Trim(),
                combatClass,
                stageId,
                selectedItems,
                document.IsFavorite,
                document.FavoriteSortKey,
                filePath);
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
        return Path.Combine(GetRootDirectoryPath(), BuildDirectoryName);
    }

    private static string GetRootDirectoryPath() => Path.Combine(Main.SavePath, "Mods", nameof(ProgressionJournal));

    private static bool TryGetSafeBuildPath(string sourcePath, out string filePath)
    {
        filePath = string.Empty;

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return false;
        }

        var directoryPath = Path.GetFullPath(GetBuildDirectoryPath());
        var candidatePath = Path.GetFullPath(sourcePath);
        if (!candidatePath.StartsWith(directoryPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(Path.GetExtension(candidatePath), FileExtension, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        filePath = candidatePath;
        return true;
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

    private static JournalBuildDocument CreateDocument(
        string name,
        CombatClass combatClass,
        ProgressionStageId stageId,
        IReadOnlyDictionary<string, int> selectedItems)
    {
        return CreateDocument(name, combatClass, stageId, CreateItemReferences(selectedItems));
    }

    private static JournalBuildDocument CreateDocument(
        string name,
        CombatClass combatClass,
        ProgressionStageId stageId,
        IReadOnlyDictionary<string, JournalSavedBuildItemReference> itemReferences)
    {
        var normalizedSelections = GetLoadedSelectedItems(itemReferences);

        return new JournalBuildDocument
        {
            Format = BuildFormat,
            Version = 2,
            Name = name.Trim(),
            CombatClass = combatClass.ToString(),
            StageId = stageId.ToString(),
            IsFavorite = false,
            FavoriteSortKey = 0L,
            SelectedItems = normalizedSelections,
            SelectedItemRefs = CreateItemReferenceDocuments(itemReferences)
        };
    }

    private static void LogWarning(string message, Exception exception)
    {
        if (ProgressionJournal.Instance?.Logger is { } logger)
        {
            logger.Warn($"{message}{Environment.NewLine}{exception}");
        }
    }

    private static bool TryReadBuildDocument(
        string filePath,
        out JournalBuildDocument document,
        out CombatClass combatClass,
        out ProgressionStageId stageId,
        out Dictionary<string, JournalSavedBuildItemReference> selectedItems)
    {
        document = null!;
        combatClass = default;
        stageId = default;
        selectedItems = [];

        try
        {
            return TryReadBuildDocumentFromJson(File.ReadAllText(filePath), out document, out combatClass, out stageId, out selectedItems);
        }
        catch (Exception exception)
        {
            LogWarning($"Failed to read build json from '{filePath}'.", exception);
            return false;
        }
    }

    private static bool TryReadBuildDocumentFromJson(
        string json,
        out JournalBuildDocument document,
        out CombatClass combatClass,
        out ProgressionStageId stageId,
        out Dictionary<string, JournalSavedBuildItemReference> selectedItems)
    {
        document = JsonSerializer.Deserialize<JournalBuildDocument>(json, SerializerOptions)
            ?? new JournalBuildDocument();
        NormalizeDocumentCollections(document);
        combatClass = default;
        stageId = default;
        selectedItems = [];

        if ((!string.IsNullOrWhiteSpace(document.Format)
                && !string.Equals(document.Format, BuildFormat, StringComparison.OrdinalIgnoreCase))
            || string.IsNullOrWhiteSpace(document.Name)
            || string.IsNullOrWhiteSpace(document.CombatClass)
            || string.IsNullOrWhiteSpace(document.StageId)
            || (document.SelectedItems.Count == 0 && document.SelectedItemRefs.Count == 0)
            || !Enum.TryParse(document.CombatClass, ignoreCase: true, out combatClass)
            || !Enum.TryParse(document.StageId, ignoreCase: true, out stageId))
        {
            return false;
        }

        selectedItems = ReadItemReferences(document);

        return selectedItems.Count > 0;
    }

    private static Dictionary<string, JournalSavedBuildItemReference> CreateItemReferences(IReadOnlyDictionary<string, int> selectedItems)
    {
        return selectedItems
            .Where(static pair => pair.Value > ItemID.None
                && JournalBuildPlannerCatalog.TryGetSlotKind(pair.Key, out _))
            .Select(static pair => new
            {
                pair.Key,
                Reference = CreateItemReference(pair.Value)
            })
            .Where(static pair => pair.Reference is not null)
            .ToDictionary(
                static pair => pair.Key,
                static pair => pair.Reference!,
                StringComparer.OrdinalIgnoreCase);
    }

    private static void NormalizeDocumentCollections(JournalBuildDocument document)
    {
        document.SelectedItems ??= [];
        document.SelectedItemRefs ??= [];
    }

    private static JournalSavedBuildItemReference? CreateItemReference(int itemId)
    {
        if (!JournalItemUtilities.TryCreateItem(itemId, out var item))
        {
            return null;
        }

        var modItem = item.ModItem;
        var modName = modItem?.Mod.Name ?? string.Empty;
        var itemName = modItem?.Name ?? string.Empty;
        var displayName = item.HoverName;
        return new JournalSavedBuildItemReference(item.type, modName, itemName, displayName);
    }

    private static Dictionary<string, int> GetLoadedSelectedItems(IReadOnlyDictionary<string, JournalSavedBuildItemReference> itemReferences)
    {
        return itemReferences
            .Where(static pair => pair.Value.IsLoaded
                && JournalBuildPlannerCatalog.TryGetSlotKind(pair.Key, out _))
            .ToDictionary(
                static pair => pair.Key,
                static pair => pair.Value.Type,
                StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, JournalBuildItemDocument> CreateItemReferenceDocuments(
        IReadOnlyDictionary<string, JournalSavedBuildItemReference> itemReferences)
    {
        return itemReferences
            .Where(static pair => JournalBuildPlannerCatalog.TryGetSlotKind(pair.Key, out _))
            .ToDictionary(
                static pair => pair.Key,
                static pair => new JournalBuildItemDocument
                {
                    Type = pair.Value.Type,
                    Mod = pair.Value.ModName,
                    Name = pair.Value.ItemName,
                    DisplayName = pair.Value.DisplayName
                },
                StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, JournalSavedBuildItemReference> ReadItemReferences(JournalBuildDocument document)
    {
        if (document.SelectedItemRefs.Count > 0)
        {
            return document.SelectedItemRefs
                .Where(static pair => JournalBuildPlannerCatalog.TryGetSlotKind(pair.Key, out _))
                .Select(static pair => new
                {
                    pair.Key,
                    Reference = ResolveItemReference(pair.Value)
                })
                .Where(static pair => pair.Reference is not null)
                .ToDictionary(
                    static pair => pair.Key,
                    static pair => pair.Reference!,
                    StringComparer.OrdinalIgnoreCase);
        }

        return document.SelectedItems
            .Where(static pair => pair.Value > ItemID.None
                && JournalBuildPlannerCatalog.TryGetSlotKind(pair.Key, out _))
            .Select(static pair => new
            {
                pair.Key,
                Reference = CreateItemReference(pair.Value)
                    ?? new JournalSavedBuildItemReference(
                        ItemID.None,
                        string.Empty,
                        string.Empty,
                        Language.GetTextValue("Mods.ProgressionJournal.UI.BuildUnloadedItem"))
            })
            .ToDictionary(
                static pair => pair.Key,
                static pair => pair.Reference,
                StringComparer.OrdinalIgnoreCase);
    }

    private static JournalSavedBuildItemReference? ResolveItemReference(JournalBuildItemDocument document)
    {
        var modName = document.Mod?.Trim() ?? string.Empty;
        var itemName = document.Name?.Trim() ?? string.Empty;
        var displayName = document.DisplayName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(modName) || string.IsNullOrWhiteSpace(itemName))
            return CreateItemReference(document.Type);
        if (ModContent.TryFind<ModItem>(modName, itemName, out var modItem)
            && CreateItemReference(modItem.Type) is { } resolved)
        {
            return resolved;
        }

        return new JournalSavedBuildItemReference(
            ItemID.None,
            modName,
            itemName,
            string.IsNullOrWhiteSpace(displayName)
                ? Language.GetTextValue("Mods.ProgressionJournal.UI.BuildUnloadedItem")
                : displayName);

    }

    private static string ToBase64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] FromBase64Url(string payload)
    {
        var base64 = payload
            .Replace('-', '+')
            .Replace('_', '/');
        var padding = (4 - base64.Length % 4) % 4;
        return Convert.FromBase64String(base64.PadRight(base64.Length + padding, '='));
    }

    private sealed class JournalBuildDocument
    {
        public string Format { get; set; } = BuildFormat;

        public int Version { get; set; }

        public string Name { get; set; } = string.Empty;

        public string CombatClass { get; set; } = string.Empty;

        public string StageId { get; set; } = string.Empty;

        public bool IsFavorite { get; set; }

        public long FavoriteSortKey { get; set; }

        public Dictionary<string, int> SelectedItems { get; set; } = [];

        public Dictionary<string, JournalBuildItemDocument> SelectedItemRefs { get; set; } = [];
    }

    private sealed class JournalBuildItemDocument
    {
        public int Type { get; set; }

        public string Mod { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;
    }
}
