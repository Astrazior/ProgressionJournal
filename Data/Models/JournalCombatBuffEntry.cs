namespace ProgressionJournal.Data.Models;

public sealed class JournalCombatBuffEntry(
    string key,
    JournalBuffCategory category,
    IEnumerable<string> classIds,
    IEnumerable<JournalItemGroup> itemGroups,
    string stageId,
    bool isClassSpecific = false)
{
    public JournalCombatBuffEntry(
        string key,
        JournalBuffCategory category,
        CombatClass classes,
        IEnumerable<JournalItemGroup> itemGroups,
        ProgressionStageId availableFrom,
        ProgressionStageId? availableUntil = null,
        bool isClassSpecific = false)
        : this(
            key,
            category,
            JournalClassIds.FromLegacyFlags(classes),
            itemGroups,
            JournalStageIds.FromLegacy(availableFrom),
            isClassSpecific)
    {
        AvailableUntil = availableUntil;
    }

    public string Key { get; } = key;

    public JournalBuffCategory Category { get; } = category;

    public IReadOnlySet<string> ClassIds { get; } =
        classIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<JournalItemGroup> ItemGroups { get; } = itemGroups.ToArray();

    public string StageId { get; } = stageId;

    public ProgressionStageId AvailableFrom =>
        JournalStageIds.TryToLegacy(StageId, out var stageId) ? stageId : ProgressionStageId.PreBoss;

    public ProgressionStageId? AvailableUntil { get; }

    public bool IsClassSpecific { get; } = isClassSpecific;

    public bool AppliesToClass(string classId) => ClassIds.Contains(classId);

    public bool AppliesToClass(CombatClass combatClass) =>
        JournalClassIds.FromLegacyFlags(combatClass).Any(AppliesToClass);

    public bool AppliesToStage(ProgressionStageId stageId)
    {
        var targetIndex = ProgressionStageCatalog.GetStageOrderIndex(stageId);
        if (!JournalStageIds.TryToLegacy(StageId, out var availableFrom))
        {
            return false;
        }

        var fromIndex = ProgressionStageCatalog.GetStageOrderIndex(availableFrom);
        if (targetIndex < fromIndex)
        {
            return false;
        }

        if (AvailableUntil is null)
        {
            return true;
        }

        var untilIndex = ProgressionStageCatalog.GetStageOrderIndex(AvailableUntil.Value);
        return targetIndex <= untilIndex;
    }

    public bool AppliesToStage(string stageId) =>
        string.Equals(StageId, stageId, StringComparison.OrdinalIgnoreCase);
}
