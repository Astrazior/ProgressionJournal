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
        result.AddRange(JournalExactDropCatalog.GetAllNpcDrops()
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
                    .ToList())));
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
        return result;
    }
}
