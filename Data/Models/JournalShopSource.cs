namespace ProgressionJournal.Data.Models;

public sealed class JournalShopSource(
    int npcType,
    string npcName,
    IEnumerable<string> conditions)
{
    public int NpcType { get; } = npcType;

    public string NpcName { get; } = npcName;

    public IReadOnlyList<string> Conditions { get; } = conditions
        .Where(static condition => !string.IsNullOrWhiteSpace(condition))
        .Distinct()
        .ToArray();
}
