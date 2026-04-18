namespace ProgressionJournal.Data.Repositories;

public static partial class JournalRepository
{
    private static JournalEntry Entry(
        string key,
        JournalItemCategory category,
        CombatClass classes,
        JournalItemGroup itemGroup,
        params StageEvaluation[] evaluations)
    {
        return new JournalEntry(key, category, classes, [itemGroup], evaluations);
    }

    private static JournalEntry EventEntry(
        string key,
        JournalItemCategory category,
        CombatClass classes,
        JournalEventCategory eventCategory,
        JournalItemGroup itemGroup,
        params StageEvaluation[] evaluations)
    {
        return new JournalEntry(key, category, classes, [itemGroup], evaluations, eventCategory);
    }

    private static JournalEntry SupportEntry(
        string key,
        JournalItemCategory category,
        CombatClass classes,
        JournalItemGroup itemGroup,
        params StageEvaluation[] evaluations)
    {
        return new JournalEntry(key, category, classes, [itemGroup], evaluations, isSupportWeapon: true);
    }

    private static JournalEntry EventSupportEntry(
        string key,
        JournalItemCategory category,
        CombatClass classes,
        JournalEventCategory eventCategory,
        JournalItemGroup itemGroup,
        params StageEvaluation[] evaluations)
    {
        return new JournalEntry(key, category, classes, [itemGroup], evaluations, eventCategory, true);
    }

    private static JournalEntry Entry(
        string key,
        JournalItemCategory category,
        CombatClass classes,
        int itemId,
        params StageEvaluation[] evaluations)
    {
        return new JournalEntry(key, category, classes, [itemId], evaluations);
    }

    private static JournalEntry EventEntry(
        string key,
        JournalItemCategory category,
        CombatClass classes,
        JournalEventCategory eventCategory,
        int itemId,
        params StageEvaluation[] evaluations)
    {
        return new JournalEntry(key, category, classes, [itemId], evaluations, eventCategory);
    }

    private static JournalEntry SupportEntry(
        string key,
        JournalItemCategory category,
        CombatClass classes,
        int itemId,
        params StageEvaluation[] evaluations)
    {
        return new JournalEntry(key, category, classes, [itemId], evaluations, isSupportWeapon: true);
    }

    private static JournalEntry EventSupportEntry(
        string key,
        JournalItemCategory category,
        CombatClass classes,
        JournalEventCategory eventCategory,
        int itemId,
        params StageEvaluation[] evaluations)
    {
        return new JournalEntry(key, category, classes, [itemId], evaluations, eventCategory, true);
    }

    private static JournalEntry Set(
        string key,
        JournalItemCategory category,
        CombatClass classes,
        JournalItemGroup firstGroup,
        JournalItemGroup secondGroup,
        JournalItemGroup thirdGroup,
        params StageEvaluation[] evaluations)
    {
        return new JournalEntry(key, category, classes, [firstGroup, secondGroup, thirdGroup], evaluations);
    }

    private static JournalEntry EventSet(
        string key,
        JournalItemCategory category,
        CombatClass classes,
        JournalEventCategory eventCategory,
        JournalItemGroup firstGroup,
        JournalItemGroup secondGroup,
        JournalItemGroup thirdGroup,
        params StageEvaluation[] evaluations)
    {
        return new JournalEntry(key, category, classes, [firstGroup, secondGroup, thirdGroup], evaluations, eventCategory);
    }

    private static JournalEntry Set(
        string key,
        JournalItemCategory category,
        CombatClass classes,
        int firstItemId,
        int secondItemId,
        int thirdItemId,
        params StageEvaluation[] evaluations)
    {
        return new JournalEntry(key, category, classes, [firstItemId, secondItemId, thirdItemId], evaluations);
    }

    private static JournalEntry EventSet(
        string key,
        JournalItemCategory category,
        CombatClass classes,
        JournalEventCategory eventCategory,
        int firstItemId,
        int secondItemId,
        int thirdItemId,
        params StageEvaluation[] evaluations)
    {
        return new JournalEntry(key, category, classes, [firstItemId, secondItemId, thirdItemId], evaluations, eventCategory);
    }

    private static StageEvaluation Eval(ProgressionStageId stageId, RecommendationTier tier)
    {
        return new StageEvaluation(stageId, tier);
    }

    private static JournalItemGroup Group(params int[] itemIds)
    {
        return new JournalItemGroup(itemIds);
    }
}

