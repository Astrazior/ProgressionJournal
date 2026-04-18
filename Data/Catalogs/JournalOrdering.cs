using System.Collections.Generic;
using System.Linq;

namespace ProgressionJournal.Data.Catalogs;

public static class JournalOrdering
{
    public static IReadOnlyList<CombatClass> ClassSelection { get; } =
    [
        CombatClass.Melee,
        CombatClass.Ranged,
        CombatClass.Magic,
        CombatClass.Summoner
    ];

    public static IReadOnlyList<ProgressionStageId> StageSelection { get; } =
        ProgressionStageCatalog.All.Select(stage => stage.Id).ToArray();

    public static IReadOnlyList<JournalItemCategory> EntryCategories { get; } =
    [
        JournalItemCategory.Weapon,
        JournalItemCategory.ClassSpecific,
        JournalItemCategory.Armor,
        JournalItemCategory.Accessory
    ];

    public static int GetTierOrder(RecommendationTier tier) => tier switch
    {
        RecommendationTier.Recommended => 0,
        RecommendationTier.Additional => 1,
        RecommendationTier.NotRecommended => 2,
        RecommendationTier.Useless => 3,
        _ => int.MaxValue
    };

    public static int GetCategoryOrder(JournalItemCategory category) => category switch
    {
        JournalItemCategory.Weapon => 0,
        JournalItemCategory.ClassSpecific => 1,
        JournalItemCategory.Armor => 2,
        JournalItemCategory.Accessory => 3,
        _ => int.MaxValue
    };

    public static int GetStageEntryDisplayOrderOverride(ProgressionStageId stageId, string entryKey)
    {
        if (stageId == ProgressionStageId.PreBoss)
        {
            return entryKey switch
            {
                "sandstormOrBlizzardBottlePreBoss" => 0,
                "balloonBundlesPreBoss" => 1,
                _ => int.MaxValue
            };
        }

        return int.MaxValue;
    }
}

