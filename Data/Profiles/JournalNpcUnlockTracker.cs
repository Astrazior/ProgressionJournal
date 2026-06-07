namespace ProgressionJournal.Data.Profiles;

public static class JournalNpcUnlockTracker
{
    private static readonly HashSet<string> DefeatedNpcs =
        new(StringComparer.OrdinalIgnoreCase);

    public static void Record(string modName, string npcName)
    {
        if (!string.IsNullOrWhiteSpace(modName) && !string.IsNullOrWhiteSpace(npcName))
        {
            DefeatedNpcs.Add(CreateKey(modName, npcName));
        }
    }

    public static bool IsDefeated(string modName, string npcName)
    {
        return DefeatedNpcs.Contains(CreateKey(modName, npcName));
    }

    public static void Clear()
    {
        DefeatedNpcs.Clear();
    }

    private static string CreateKey(string modName, string npcName)
    {
        return $"{modName.Trim()}/{npcName.Trim()}";
    }
}
