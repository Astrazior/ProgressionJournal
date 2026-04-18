using System.Collections.Generic;
using System.Linq;

namespace ProgressionJournal.Data.Models;

public sealed class JournalCombatBuffEntry(
    string key,
    JournalBuffCategory category,
    CombatClass classes,
    IEnumerable<JournalItemGroup> itemGroups,
    ProgressionStageId availableFrom,
    ProgressionStageId? availableUntil = null,
    bool isClassSpecific = false)
{
    public string Key { get; } = key;

    public JournalBuffCategory Category { get; } = category;

    public CombatClass Classes { get; } = classes;

    public IReadOnlyList<JournalItemGroup> ItemGroups { get; } = itemGroups.ToArray();

    public ProgressionStageId AvailableFrom { get; } = availableFrom;

    public ProgressionStageId? AvailableUntil { get; } = availableUntil;

    public bool IsClassSpecific { get; } = isClassSpecific;

    public bool AppliesToClass(CombatClass combatClass) => (Classes & combatClass) != 0;

    public bool AppliesToStage(ProgressionStageId stageId)
    {
        var targetIndex = ProgressionStageCatalog.GetStageOrderIndex(stageId);
        var fromIndex = ProgressionStageCatalog.GetStageOrderIndex(AvailableFrom);
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
}
