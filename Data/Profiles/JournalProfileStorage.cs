using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ProgressionJournal.Data.Profiles;

public static class JournalProfileStorage
{
    public const string ProfileFormat = "ProgressionJournalProfile";
    public const int CurrentVersion = 2;
    public const string FileExtension = ".pjprofile.json";

    private const string ProfileDirectoryName = "Profiles";
    private const string ActiveProfileFileName = "active-profile.txt";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static IReadOnlyList<JournalProfile> LoadUserProfiles()
    {
        var directory = GetProfileDirectoryPath();
        if (!Directory.Exists(directory))
        {
            return [];
        }

        List<JournalProfile> profiles = [];
        foreach (var path in Directory.EnumerateFiles(directory, $"*{FileExtension}", SearchOption.TopDirectoryOnly))
        {
            if (TryLoad(path, isBuiltIn: false, out var profile, out _))
            {
                profiles.Add(profile!);
            }
        }

        return profiles;
    }

    public static bool TryLoad(string path, bool isBuiltIn, out JournalProfile? profile, out string error)
    {
        profile = null;

        try
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            return TryParse(json, path, isBuiltIn, out profile, out error);
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }

    public static bool TryParse(string json, string sourcePath, bool isBuiltIn, out JournalProfile? profile, out string error)
    {
        profile = null;

        try
        {
            var document = JsonSerializer.Deserialize<JournalProfileDocument>(json, SerializerOptions);
            if (document is null)
            {
                error = "Profile document is empty.";
                return false;
            }

            if (!Validate(document, out error))
            {
                return false;
            }

            var entries = ResolveEntries(document);
            var combatBuffEntries = ResolveCombatBuffEntries(document);
            profile = new JournalProfile(
                document,
                entries,
                combatBuffEntries,
                sourcePath,
                isBuiltIn,
                HasVersionMismatch(document));
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }

    public static bool Save(JournalProfileDocument document, out string path, out string error)
    {
        path = string.Empty;

        try
        {
            document.Format = ProfileFormat;
            document.Version = CurrentVersion;
            document.ReadOnly = false;

            if (!Validate(document, out error))
            {
                return false;
            }

            Directory.CreateDirectory(GetProfileDirectoryPath());
            path = Path.Combine(GetProfileDirectoryPath(), $"{SanitizeFileName(document.Id)}{FileExtension}");
            File.WriteAllText(path, JsonSerializer.Serialize(document, SerializerOptions), Encoding.UTF8);
            error = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }

    public static bool Export(JournalProfile profile, string path, out string error)
    {
        try
        {
            var document = CloneDocument(profile.Document);
            document.ReadOnly = false;
            File.WriteAllText(path, JsonSerializer.Serialize(document, SerializerOptions), Encoding.UTF8);
            error = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }

    public static bool Delete(JournalProfile profile, out string error)
    {
        if (profile.IsBuiltIn)
        {
            error = "Built-in profiles cannot be deleted.";
            return false;
        }

        try
        {
            var profileDirectory = Path.GetFullPath(GetProfileDirectoryPath())
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var profilePath = Path.GetFullPath(profile.SourcePath);
            if (!profilePath.StartsWith(profileDirectory, StringComparison.OrdinalIgnoreCase)
                || !profilePath.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase))
            {
                error = "The profile is not stored in the user profile directory.";
                return false;
            }

            if (File.Exists(profilePath))
            {
                File.Delete(profilePath);
            }

            error = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }

    public static bool Import(string path, out JournalProfile? importedProfile, out string error)
    {
        importedProfile = null;

        if (!TryLoad(path, isBuiltIn: false, out var loaded, out error) || loaded is null)
        {
            return false;
        }

        var document = CloneDocument(loaded.Document);
        document.ReadOnly = false;

        if (string.Equals(document.Id, JournalProfileIds.Vanilla, StringComparison.OrdinalIgnoreCase)
            || string.Equals(document.Id, JournalProfileIds.CalamityWiki, StringComparison.OrdinalIgnoreCase))
        {
            document.Id = CreateCopyId(document.Id);
            document.Name = $"{document.Name} (copy)";
        }

        if (!Save(document, out var destinationPath, out error))
        {
            return false;
        }

        return TryLoad(destinationPath, isBuiltIn: false, out importedProfile, out error);
    }

    public static bool CreateEditableCopy(JournalProfile source, out JournalProfile? copy, out string error)
    {
        copy = null;
        var document = CloneDocument(source.Document);
        document.Id = CreateCopyId(source.Id);
        document.Name = $"{source.Name} (copy)";
        document.Author = Main.LocalPlayer?.name ?? document.Author;
        document.ReadOnly = false;
        document.SourceRevision = string.Empty;

        if (!Save(document, out var path, out error))
        {
            return false;
        }

        return TryLoad(path, isBuiltIn: false, out copy, out error);
    }

    public static JournalProfileDocument CloneDocument(JournalProfileDocument document)
    {
        var json = JsonSerializer.Serialize(document, SerializerOptions);
        return JsonSerializer.Deserialize<JournalProfileDocument>(json, SerializerOptions)!;
    }

    public static string LoadActiveProfileId()
    {
        try
        {
            var path = Path.Combine(GetProfileDirectoryPath(), ActiveProfileFileName);
            return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8).Trim() : JournalProfileIds.Vanilla;
        }
        catch
        {
            return JournalProfileIds.Vanilla;
        }
    }

    public static void SaveActiveProfileId(string profileId)
    {
        try
        {
            Directory.CreateDirectory(GetProfileDirectoryPath());
            File.WriteAllText(
                Path.Combine(GetProfileDirectoryPath(), ActiveProfileFileName),
                profileId,
                Encoding.UTF8);
        }
        catch
        {
            // Profile selection remains usable for the current session.
        }
    }

    public static JournalProfile CreateVanillaProfile(IReadOnlyList<JournalEntry> entries)
    {
        var document = new JournalProfileDocument
        {
            Id = JournalProfileIds.Vanilla,
            Name = "Vanilla",
            Author = "Progression Journal",
            ProfileVersion = "3.0.0",
            ReadOnly = true,
            Classes =
            [
                CreateVanillaClass(JournalClassIds.Melee, "Melee", "Melee"),
                CreateVanillaClass(JournalClassIds.Ranged, "Ranged", "Ranged"),
                CreateVanillaClass(JournalClassIds.Magic, "Magic", "Magic"),
                CreateVanillaClass(JournalClassIds.Summoner, "Summoner", "Summon")
            ],
            Stages = ProgressionStageCatalog.All
                .Select(static stage => new JournalProfileStageDocument
                {
                    Id = JournalStageIds.FromLegacy(stage.Id),
                    Name = stage.LocalizationKey,
                    AccessorySlots = stage.Id >= ProgressionStageId.HardmodeEntry ? 6 : 5,
                    Unlock = new JournalUnlockConditionDocument
                    {
                        Type = "vanilla-stage",
                        Key = JournalStageIds.FromLegacy(stage.Id)
                    }
                })
                .ToList()
        };

        return new JournalProfile(document, entries, [], "builtin:vanilla", isBuiltIn: true, hasVersionMismatch: false);
    }

    public static string Serialize(JournalProfileDocument document)
    {
        return JsonSerializer.Serialize(document, SerializerOptions);
    }

    private static bool Validate(JournalProfileDocument document, out string error)
    {
        if (!string.Equals(document.Format, ProfileFormat, StringComparison.Ordinal))
        {
            error = $"Unsupported profile format '{document.Format}'.";
            return false;
        }

        if (document.Version is < 1 or > CurrentVersion)
        {
            error = $"Unsupported profile version '{document.Version}'.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(document.Id) || string.IsNullOrWhiteSpace(document.Name))
        {
            error = "Profile id and name are required.";
            return false;
        }

        if (document.Classes.Count == 0 || document.Stages.Count == 0)
        {
            error = "A profile must contain at least one class and one stage.";
            return false;
        }

        if (document.Classes.Any(static value =>
                string.IsNullOrWhiteSpace(value.Id) || string.IsNullOrWhiteSpace(value.Name))
            || document.Stages.Any(static value =>
                string.IsNullOrWhiteSpace(value.Id) || string.IsNullOrWhiteSpace(value.Name))
            || document.Entries.Any(static value => string.IsNullOrWhiteSpace(value.Key)))
        {
            error = "Class, stage, and entry ids and names cannot be empty.";
            return false;
        }

        if (HasDuplicates(document.Classes.Select(static value => value.Id))
            || HasDuplicates(document.Stages.Select(static value => value.Id))
            || HasDuplicates(document.Entries.Select(static value => value.Key))
            || HasDuplicates(document.CombatBuffs.Select(static value => value.Key)))
        {
            error = "Class, stage, entry, and combat buff ids must be unique.";
            return false;
        }

        var classIds = document.Classes.Select(static value => value.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var stageIds = document.Stages.Select(static value => value.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in document.Entries)
        {
            if (entry.Classes.Count == 0 || entry.Classes.Any(classId => !classIds.Contains(classId)))
            {
                error = $"Entry '{entry.Key}' references an unknown class.";
                return false;
            }

            if (entry.Evaluations.Count == 0 || entry.Evaluations.Any(value => !stageIds.Contains(value.StageId)))
            {
                error = $"Entry '{entry.Key}' references an unknown stage.";
                return false;
            }

            if (entry.ItemGroups.Count == 0 || entry.ItemGroups.Any(static group => group.Count == 0))
            {
                error = $"Entry '{entry.Key}' must contain item groups.";
                return false;
            }

            if (entry.ItemGroups.SelectMany(static group => group).Any(static reference =>
                    string.IsNullOrWhiteSpace(reference.Mod) || string.IsNullOrWhiteSpace(reference.Item)))
            {
                error = $"Entry '{entry.Key}' contains an invalid item reference.";
                return false;
            }
        }

        foreach (var buff in document.CombatBuffs)
        {
            if (string.IsNullOrWhiteSpace(buff.Key)
                || !classIds.Contains(buff.ClassId)
                || !stageIds.Contains(buff.StageId)
                || buff.ItemGroups.Count == 0
                || buff.ItemGroups.Any(static group => group.Count == 0)
                || buff.ItemGroups.SelectMany(static group => group).Any(static reference =>
                    string.IsNullOrWhiteSpace(reference.Mod) || string.IsNullOrWhiteSpace(reference.Item)))
            {
                error = $"Combat buff '{buff.Key}' is invalid.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private static IReadOnlyList<JournalEntry> ResolveEntries(JournalProfileDocument document)
    {
        List<JournalEntry> result = [];

        foreach (var entryDocument in document.Entries)
        {
            var groups = entryDocument.ItemGroups
                .Select(group => group
                    .Select(TryResolveItem)
                    .Where(static itemId => itemId > ItemID.None)
                    .ToArray())
                .Where(static group => group.Length > 0)
                .Select(static group => new JournalItemGroup(group))
                .ToArray();

            if (groups.Length == 0)
            {
                continue;
            }

            result.Add(new JournalEntry(
                entryDocument.Key,
                entryDocument.Category,
                entryDocument.Classes,
                groups,
                entryDocument.Evaluations.Select(static value => new StageEvaluation(value.StageId, value.Tier)),
                entryDocument.EventCategory,
                entryDocument.IsSupportWeapon,
                entryDocument.CustomEventName));
        }

        return result;
    }

    private static IReadOnlyList<JournalCombatBuffEntry> ResolveCombatBuffEntries(JournalProfileDocument document)
    {
        List<JournalCombatBuffEntry> result = [];

        foreach (var buffDocument in document.CombatBuffs)
        {
            var groups = buffDocument.ItemGroups
                .Select(group => group
                    .Select(TryResolveItem)
                    .Where(static itemId => itemId > ItemID.None)
                    .ToArray())
                .Where(static group => group.Length > 0)
                .Select(static group => new JournalItemGroup(group))
                .ToArray();
            if (groups.Length == 0)
            {
                continue;
            }

            result.Add(new JournalCombatBuffEntry(
                buffDocument.Key,
                buffDocument.Category,
                [buffDocument.ClassId],
                groups,
                buffDocument.StageId));
        }

        return result;
    }

    private static int TryResolveItem(JournalItemReferenceDocument reference)
    {
        if (string.Equals(reference.Mod, "Terraria", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(reference.Item, out var numericId)
                && ContentSamples.ItemsByType.ContainsKey(numericId))
            {
                return numericId;
            }

            if (ItemID.Search.TryGetId(reference.Item, out var vanillaId))
            {
                return vanillaId;
            }

            var normalizedName = NormalizeContentName(reference.Item);
            for (var itemId = ItemID.None + 1; itemId < ItemID.Count; itemId++)
            {
                var internalName = ItemID.Search.GetName(itemId);
                if (string.Equals(NormalizeContentName(internalName ?? string.Empty), normalizedName, StringComparison.Ordinal))
                {
                    return itemId;
                }
            }

            return ItemID.None;
        }

        return ModContent.TryFind(reference.Mod, reference.Item, out ModItem modItem)
            ? modItem.Type
            : ItemID.None;
    }

    private static bool HasVersionMismatch(JournalProfileDocument document)
    {
        foreach (var requirement in document.RequiredMods)
        {
            if (!ModLoader.TryGetMod(requirement.Name, out var mod))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(requirement.Version)
                && !mod.Version.ToString().StartsWith(requirement.Version, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static JournalProfileClassDocument CreateVanillaClass(string id, string name, string damageClassName)
    {
        return new JournalProfileClassDocument
        {
            Id = id,
            Name = name,
            DamageClassNames = [damageClassName]
        };
    }

    private static bool HasDuplicates(IEnumerable<string> values)
    {
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        return values.Any(value => string.IsNullOrWhiteSpace(value) || !seen.Add(value));
    }

    private static string CreateCopyId(string sourceId)
    {
        var normalized = sourceId.Replace("builtin.", string.Empty, StringComparison.OrdinalIgnoreCase);
        return $"user.{normalized}.{DateTime.UtcNow:yyyyMMddHHmmssfff}";
    }

    private static string SanitizeFileName(string value)
    {
        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalidCharacter, '_');
        }

        return value;
    }

    private static string NormalizeContentName(string value)
    {
        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }

    private static string GetProfileDirectoryPath()
    {
        return Path.Combine(Main.SavePath, "Mods", nameof(ProgressionJournal), ProfileDirectoryName);
    }
}
