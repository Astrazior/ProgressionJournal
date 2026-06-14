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
    public const int CurrentVersion = 1;

    private const string ProfileDirectoryName = "Profiles";
    private const string ActiveProfileFileName = "active-profile.txt";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static bool TryParseBuiltIn(string json, out JournalProfile? profile, out string error)
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

            if (!Validate(document, requireBuiltInLocalization: true, out error))
            {
                return false;
            }

            var entries = ResolveEntries(document);
            var combatBuffEntries = ResolveCombatBuffEntries(document);
            profile = new JournalProfile(
                document,
                entries,
                combatBuffEntries,
                HasVersionMismatch(document));
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
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

        return new JournalProfile(document, entries, [], hasVersionMismatch: false);
    }

    private static bool Validate(
        JournalProfileDocument document,
        bool requireBuiltInLocalization,
        out string error)
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

        if (string.IsNullOrWhiteSpace(document.Id) || document.Name.IsEmpty)
        {
            error = "Profile id and name are required.";
            return false;
        }

        if (document.Classes.Count == 0 || document.Stages.Count == 0)
        {
            error = "A profile must contain at least one class and one stage.";
            return false;
        }

        if (requireBuiltInLocalization
            && (!HasBuiltInLocales(document.Name)
                || document.Classes.Any(static value => !HasBuiltInLocales(value.Name))
                || document.Stages.Any(static value => !HasBuiltInLocales(value.Name))
                || document.Entries.SelectMany(static value => value.Wiki)
                    .Any(static value => !HasBuiltInLocales(value.Target))))
        {
            error = "Built-in profile names and Wiki targets require en-US and ru-RU text.";
            return false;
        }

        if (document.Classes.Any(static value =>
                string.IsNullOrWhiteSpace(value.Id) || value.Name.IsEmpty)
            || document.Stages.Any(static value =>
                string.IsNullOrWhiteSpace(value.Id) || value.Name.IsEmpty)
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

            if ((entry.Evaluations.Count == 0 && entry.Wiki.Count == 0)
                || entry.Evaluations.Any(value => !stageIds.Contains(value.StageId)))
            {
                error = $"Entry '{entry.Key}' references an unknown stage.";
                return false;
            }

            if (entry.Wiki.Any(value =>
                    !stageIds.Contains(value.StageId)
                    || value.Classes.Count == 0
                    || value.Classes.Any(classId => !classIds.Contains(classId))
                    || value.Target.IsEmpty
                    || string.IsNullOrWhiteSpace(value.SourceName)))
            {
                error = $"Entry '{entry.Key}' contains invalid Wiki recommendation metadata.";
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
            var buffClasses = GetCombatBuffClasses(buff);
            if (string.IsNullOrWhiteSpace(buff.Key)
                || buffClasses.Count == 0
                || buffClasses.Any(classId => !classIds.Contains(classId))
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
                entryDocument.Evaluations.Select(static value =>
                    new StageEvaluation(value.StageId, value.Tier, scope: value.Scope)),
                entryDocument.EventCategory,
                entryDocument.IsSupportWeapon,
                entryDocument.CustomEventName,
                entryDocument.EventIcon,
                entryDocument.Wiki.Select(static value => new JournalWikiRecommendation(
                    value.StageId,
                    value.Classes.ToHashSet(StringComparer.OrdinalIgnoreCase),
                    value.SourceName,
                    value.SourceUrl,
                    value.Target))));
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
                GetCombatBuffClasses(buffDocument),
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

    private static IReadOnlyList<string> GetCombatBuffClasses(JournalProfileCombatBuffDocument document)
    {
        if (document.Classes.Count > 0)
        {
            return document.Classes;
        }

        return string.IsNullOrWhiteSpace(document.ClassId) ? [] : [document.ClassId];
    }

    private static bool HasDuplicates(IEnumerable<string> values)
    {
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        return values.Any(value => string.IsNullOrWhiteSpace(value) || !seen.Add(value));
    }

    private static bool HasBuiltInLocales(JournalLocalizedText value) =>
        value.HasLocale("en-US") && value.HasLocale("ru-RU");

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
