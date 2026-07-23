using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Utilities;

namespace ProgressionJournal.Data.Resolvers;

internal static class JournalStaticNpcSpawnConditionResolver
{
    private static readonly FieldInfo? MainHardModeField = typeof(Main).GetField(
        nameof(Main.hardMode),
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    private static readonly FieldInfo? SpawnConditionSkyField = typeof(SpawnCondition).GetField(
        nameof(SpawnCondition.Sky),
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

    public static IEnumerable<string> GetConditions(int npcType)
    {
        if (!IsSimpleHardmodeSky(npcType))
        {
            return [];
        }

        var stageName = ResolveHardmodeStageName();
        return
        [
            Language.GetTextValue(
                "Mods.ProgressionJournal.UI.FishingProgressionCondition",
                stageName),
            Language.GetTextValue(
                "Mods.ProgressionJournal.UI.FishingDepthCondition",
                Language.GetTextValue("Bestiary_Biomes.Sky"))
        ];
    }

    internal static bool IsSimpleHardmodeSky(int npcType)
    {
        if (MainHardModeField is null
            || SpawnConditionSkyField is null
            || !ContentSamples.NpcsByNetId.TryGetValue(npcType, out var sample)
            || sample.ModNPC is not { } modNpc
            || modNpc.GetType().GetMethod(
                nameof(ModNPC.SpawnChance),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is not { } spawnChanceMethod)
        {
            return false;
        }

        var referencedMembers = JournalLegacyDirectDropAnalyzer.GetReferencedMembers(spawnChanceMethod);
        return referencedMembers.Contains(MainHardModeField)
            && referencedMembers.Contains(SpawnConditionSkyField)
            && !referencedMembers
                .OfType<FieldInfo>()
                .Any(field => field != MainHardModeField
                    && field.IsStatic
                    && field.FieldType == typeof(bool)
                    && IsProgressionFlagReferenceName(field.Name));
    }

    private static string ResolveHardmodeStageName()
    {
        var profile = JournalRuntimeProgressionScenarios.CurrentProfile;
        var stage = profile?.Stages.FirstOrDefault(stage => UnlockReferencesHardmode(stage.Unlock));
        return stage?.Name.Resolve() is { Length: > 0 } stageName ? stageName : "Hardmode";
    }

    private static bool UnlockReferencesHardmode(JournalUnlockConditionDocument condition)
    {
        return string.Equals(condition.Key, "hardMode", StringComparison.OrdinalIgnoreCase)
            || condition.Conditions.Any(UnlockReferencesHardmode);
    }

    private static bool IsProgressionFlagReferenceName(string name)
    {
        var normalized = name.Trim('<', '>', '_').ToLowerInvariant();
        return normalized == "hardmode"
            || normalized.Contains("downed", StringComparison.Ordinal)
            || normalized.Contains("defeated", StringComparison.Ordinal)
            || normalized.Contains("killed", StringComparison.Ordinal)
            || normalized.Contains("unlocked", StringComparison.Ordinal)
            || normalized.Contains("completed", StringComparison.Ordinal);
    }
}
