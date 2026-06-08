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
        JournalItemCategory.Armor,
        JournalItemCategory.Accessory,
        JournalItemCategory.Ammunition,
        JournalItemCategory.Support,
        JournalItemCategory.ClassSpecific,
        JournalItemCategory.Buff
    ];

    public static int GetTierOrder(RecommendationTier tier) => tier switch
    {
        RecommendationTier.Recommended => 0,
        RecommendationTier.Additional => 1,
        RecommendationTier.NotRecommended => 2,
        RecommendationTier.Useless => 3,
        RecommendationTier.FromGuide => 4,
        _ => int.MaxValue
    };

    public static int GetCategoryOrder(JournalItemCategory category) => category switch
    {
        JournalItemCategory.Weapon => 0,
        JournalItemCategory.Armor => 1,
        JournalItemCategory.Accessory => 2,
        JournalItemCategory.Ammunition => 3,
        JournalItemCategory.Support => 4,
        JournalItemCategory.ClassSpecific => 5,
        JournalItemCategory.Buff => 6,
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

