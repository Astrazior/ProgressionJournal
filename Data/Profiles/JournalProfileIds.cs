namespace ProgressionJournal.Data.Profiles;

public static class JournalProfileIds
{
    public const string Vanilla = "builtin.vanilla";
    public const string CalamityWiki = "builtin.calamity-wiki";
}

public static class JournalClassIds
{
    public const string Melee = "melee";
    public const string Ranged = "ranged";
    public const string Magic = "magic";
    public const string Summoner = "summoner";
    public const string Rogue = "rogue";

    public static string FromLegacy(CombatClass combatClass) => combatClass switch
    {
        CombatClass.Melee => Melee,
        CombatClass.Ranged => Ranged,
        CombatClass.Magic => Magic,
        CombatClass.Summoner => Summoner,
        _ => Melee
    };

    public static CombatClass ToLegacy(string classId)
    {
        return TryToLegacy(classId, out var combatClass)
            ? combatClass
            : CombatClass.Melee;
    }

    public static bool TryToLegacy(string classId, out CombatClass combatClass)
    {
        combatClass = classId.ToLowerInvariant() switch
        {
            Melee => CombatClass.Melee,
            Ranged => CombatClass.Ranged,
            Magic => CombatClass.Magic,
            Summoner => CombatClass.Summoner,
            _ => CombatClass.None
        };
        return combatClass != CombatClass.None;
    }

    public static IReadOnlyList<string> FromLegacyFlags(CombatClass classes)
    {
        List<string> result = [];

        if ((classes & CombatClass.Melee) != 0)
        {
            result.Add(Melee);
        }

        if ((classes & CombatClass.Ranged) != 0)
        {
            result.Add(Ranged);
        }

        if ((classes & CombatClass.Magic) != 0)
        {
            result.Add(Magic);
        }

        if ((classes & CombatClass.Summoner) != 0)
        {
            result.Add(Summoner);
        }

        return result;
    }
}

public static class JournalStageIds
{
    public static string FromLegacy(ProgressionStageId stageId) => stageId.ToString();

    public static bool TryToLegacy(string stageId, out ProgressionStageId result)
    {
        return Enum.TryParse(stageId, ignoreCase: true, out result);
    }
}
