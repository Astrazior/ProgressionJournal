using System.Reflection;
using ProgressionJournal.Commands;
using Terraria;
using Terraria.GameContent.ItemDropRules;

namespace ProgressionJournal.Data.Snapshots.Collectors;

internal static class JournalSnapshotNpcDropCollector
{
    private static readonly FieldInfo? GlobalNpcDropRulesField = typeof(ItemDropDatabase).GetField(
        "_globalEntries",
        BindingFlags.Instance | BindingFlags.NonPublic);

    public static List<SnapshotDrop> Collect(
        HashSet<int> includedItems,
        HashSet<int> includedNpcs,
        Func<int, string> getItemReference,
        Func<int, string> getNpcReference,
        Func<object?, SnapshotCondition> createCondition,
        Action<string, Exception> logDebug)
    {
        List<SnapshotDrop> result = [];
        foreach (var npcId in includedNpcs)
        {
            result.AddRange(JournalSnapshotDropRuleReporter.Collect(
                Main.ItemDropsDB.GetRulesForNPCID(npcId, includeGlobalDrops: false),
                "npc",
                getNpcReference(npcId),
                includedItems,
                getItemReference,
                createCondition,
                logDebug));
        }

        result.AddRange(JournalSnapshotDropRuleReporter.Collect(
            GlobalNpcDropRulesField?.GetValue(Main.ItemDropsDB) as List<IItemDropRule>,
            "global",
            "Terraria/GlobalNPCDrops",
            includedItems,
            getItemReference,
            createCondition,
            logDebug));
        result.AddRange(JournalLegacyDirectDropAnalyzer.GetAllNpcDrops()
            .Where(drop => includedNpcs.Contains(drop.SourceNpcType)
                && includedItems.Contains(drop.TargetItemId))
            .Select(drop => new SnapshotDrop(
                "npc",
                getNpcReference(drop.SourceNpcType),
                getItemReference(drop.TargetItemId),
                drop.DropRate,
                drop.StackMin,
                drop.StackMax,
                [])));
        var exactNpcDrops = JournalExactDropCatalog.GetAllNpcDrops()
            .Where(drop => drop.SourceNpcType is { } sourceNpcType
                && includedNpcs.Contains(sourceNpcType)
                && includedItems.Contains(drop.TargetItemId))
            .Select(drop => new SnapshotDrop(
                "npc",
                getNpcReference(drop.SourceNpcType!.Value),
                getItemReference(drop.TargetItemId),
                drop.DropRate,
                drop.StackMin,
                drop.StackMax,
                drop.Conditions
                    .Select(static condition => new SnapshotCondition(
                        condition.Type,
                        condition.Description))
                    .ToList()))
            .ToArray();
        result.AddRange(exactNpcDrops.Where(drop => !ContainsEquivalentDrop(result, drop)));
        result.AddRange(JournalExactDropCatalog.GetAllGlobalDrops()
            .Where(drop => includedItems.Contains(drop.TargetItemId))
            .Select(drop => new SnapshotDrop(
                "global",
                "AAModClassic/GlobalNPCDrops",
                getItemReference(drop.TargetItemId),
                drop.DropRate,
                drop.StackMin,
                drop.StackMax,
                drop.Conditions
                    .Select(static condition => new SnapshotCondition(
                        condition.Type,
                        condition.Description))
                    .ToList())));
        result.AddRange(JournalExactDropCatalog.GetAllWorldDrops()
            .Where(drop => includedItems.Contains(drop.TargetItemId))
            .Select(drop => new SnapshotDrop(
                "world",
                drop.SourceReference!,
                getItemReference(drop.TargetItemId),
                drop.DropRate,
                drop.StackMin,
                drop.StackMax,
                drop.Conditions
                    .Select(static condition => new SnapshotCondition(
                        condition.Type,
                        condition.Description))
                    .ToList(),
                drop.SourceName,
                HideDropRate: !drop.ShowDropRate)));
        return result;
    }

    private static bool ContainsEquivalentDrop(
        IEnumerable<SnapshotDrop> existingDrops,
        SnapshotDrop candidate)
    {
        return existingDrops.Any(existing =>
            existing.SourceType == candidate.SourceType
            && existing.Source == candidate.Source
            && existing.Item == candidate.Item
            && Math.Abs(existing.Rate - candidate.Rate) < 0.000001f
            && existing.StackMin == candidate.StackMin
            && existing.StackMax == candidate.StackMax
            && existing.Conditions
                .Select(static condition => (condition.Type, condition.Description))
                .SequenceEqual(candidate.Conditions.Select(
                    static condition => (condition.Type, condition.Description))));
    }
}
