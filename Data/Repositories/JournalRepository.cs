namespace ProgressionJournal.Data.Repositories;

public static partial class JournalRepository
{
    private static readonly Lazy<IReadOnlyList<JournalEntry>> Entries = new(BuildEntries);
    private static readonly Lazy<IReadOnlyList<JournalCombatBuffEntry>> CombatBuffEntries = new(BuildCombatBuffEntries);
    private static readonly List<JournalEntry> ExternalEntries = [];

    public static IReadOnlyList<JournalStageEntry> GetEntries(ProgressionStageId stageId, CombatClass combatClass)
    {
        return GetEntries(
            JournalProfileIds.Vanilla,
            JournalStageIds.FromLegacy(stageId),
            JournalClassIds.FromLegacy(combatClass));
    }

    public static IReadOnlyList<JournalStageEntry> GetEntries(string profileId, string stageId, string classId)
    {
        if (JournalProfileRegistry.TryGet(profileId, out var profile))
            return profile.Entries
                .Where(entry => entry.AppliesToClass(classId)
                                || entry.WikiRecommendations.Any(value =>
                                    string.Equals(value.StageId, stageId, StringComparison.OrdinalIgnoreCase)
                                    && value.ClassIds.Contains(classId)))
                .Select(entry => CreateStageEntry(profile.Id, stageId, classId, entry))
                .Where(static entry => entry is not null)
                .Select(static entry => entry!)
                .OrderByDescending(static entry => entry.IsWikiRecommendation)
                .ThenBy(entry => JournalOrdering.GetTierOrder(entry.Evaluation.Tier))
                .ThenBy(entry => JournalOrdering.GetCategoryOrder(entry.Entry.Category))
                .ThenBy(entry => GetDisplayOrderOverride(profile.Id, stageId, entry.Entry.Key))
                .ThenBy(entry => entry.Entry.GetDisplayName(), StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
        {
            if (!JournalProfileRegistry.IsLoaded
                && string.Equals(profileId, JournalProfileIds.Vanilla, StringComparison.OrdinalIgnoreCase))
            {
                return Entries.Value
                    .Where(entry => entry.AppliesToClass(classId) && entry.TryGetEvaluation(profileId, stageId, out _))
                    .Select(entry => new JournalStageEntry(entry, entry.GetEvaluation(profileId, stageId)))
                    .OrderBy(entry => JournalOrdering.GetTierOrder(entry.Evaluation.Tier))
                    .ThenBy(entry => JournalOrdering.GetCategoryOrder(entry.Entry.Category))
                    .ThenBy(entry => GetDisplayOrderOverride(profileId, stageId, entry.Entry.Key))
                    .ThenBy(entry => entry.Entry.GetDisplayName(), StringComparer.CurrentCultureIgnoreCase)
                    .ToArray();
            }

            profile = JournalProfileRegistry.Active;
        }

        return profile.Entries
            .Where(entry => entry.AppliesToClass(classId)
                || entry.WikiRecommendations.Any(value =>
                    string.Equals(value.StageId, stageId, StringComparison.OrdinalIgnoreCase)
                    && value.ClassIds.Contains(classId)))
            .Select(entry => CreateStageEntry(profile.Id, stageId, classId, entry))
            .Where(static entry => entry is not null)
            .Select(static entry => entry!)
            .OrderByDescending(static entry => entry.IsWikiRecommendation)
            .ThenBy(entry => JournalOrdering.GetTierOrder(entry.Evaluation.Tier))
            .ThenBy(entry => JournalOrdering.GetCategoryOrder(entry.Entry.Category))
            .ThenBy(entry => GetDisplayOrderOverride(profile.Id, stageId, entry.Entry.Key))
            .ThenBy(entry => entry.Entry.GetDisplayName(), StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static JournalStageEntry? CreateStageEntry(
        string profileId,
        string stageId,
        string classId,
        JournalEntry entry)
    {
        var wiki = entry.WikiRecommendations.FirstOrDefault(value =>
            string.Equals(value.StageId, stageId, StringComparison.OrdinalIgnoreCase)
            && value.ClassIds.Contains(classId));
        if (wiki is not null)
        {
            return new JournalStageEntry(
                entry,
                new StageEvaluation(stageId, RecommendationTier.FromGuide, scope: JournalEvaluationScope.StageOnly),
                wiki);
        }

        return entry.TryGetEvaluation(profileId, stageId, out var evaluation)
            ? new JournalStageEntry(entry, evaluation)
            : null;
    }

    public static IReadOnlyList<JournalEntry> GetAllVanillaEntries() => Entries.Value;

    private static List<JournalEntry> BuildEntries()
    {
        List<JournalEntry> entries = [];
        AddWeaponEntries(entries);
        AddClassSpecificEntries(entries);
        AddArmorEntries(entries);
        AddAccessoryEntries(entries);
        entries.AddRange(ExternalEntries);
        return entries;
    }

    public static void RegisterExternalEntry(JournalEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (ExternalEntries.Any(existing => string.Equals(existing.Key, entry.Key, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"An external journal entry with key '{entry.Key}' is already registered.");
        }

        ExternalEntries.Add(entry);

        if (!Entries.IsValueCreated) return;
        var currentEntries = JournalProfileRegistry.TryGet(JournalProfileIds.Vanilla, out var vanillaProfile)
            ? vanillaProfile.Entries
            : Entries.Value;
        JournalProfileRegistry.RefreshVanillaProfile(currentEntries.Concat([entry]).ToArray());
    }

    internal static void ClearExternalContent()
    {
        ExternalEntries.Clear();
    }

    private static int GetDisplayOrderOverride(string profileId, string stageId, string entryKey)
    {
        return string.Equals(profileId, JournalProfileIds.Vanilla, StringComparison.OrdinalIgnoreCase)
            && JournalStageIds.TryToLegacy(stageId, out var legacyStage)
                ? JournalOrdering.GetStageEntryDisplayOrderOverride(legacyStage, entryKey)
                : int.MaxValue;
    }
}
