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
        return result;
    }
}
