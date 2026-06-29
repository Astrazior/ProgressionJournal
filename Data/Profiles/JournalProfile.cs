using Terraria.ModLoader;

namespace ProgressionJournal.Data.Profiles;

public sealed class JournalProfile(
    JournalProfileDocument document,
    IReadOnlyList<JournalEntry> entries,
    IReadOnlyList<JournalCombatBuffEntry> combatBuffEntries,
    bool hasVersionMismatch)
{
    private readonly Dictionary<string, JournalProfileClassDocument> _classes =
        document.Classes.ToDictionary(static value => value.Id, StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, JournalProfileStageDocument> _stages =
        document.Stages.ToDictionary(static value => value.Id, StringComparer.OrdinalIgnoreCase);

    public JournalProfileDocument Document { get; } = document;

    public string Id => Document.Id;

    private string Name => Document.Name.Resolve();

    public string DisplayName
    {
        get
        {
            var requiredMod = Document.RequiredMods.FirstOrDefault();
            return requiredMod is null ? Name : NormalizeModProfileName(ModLoader.TryGetMod(requiredMod.Name, out var mod) ? mod.DisplayNameClean : Name);
        }
    }

    public bool HasVersionMismatch { get; } = hasVersionMismatch;

    public IReadOnlyList<JournalProfileClassDocument> Classes => Document.Classes;

    public IReadOnlyList<JournalProfileStageDocument> Stages => Document.Stages;

    public IReadOnlyList<JournalEntry> Entries { get; } = entries;

    public IReadOnlyList<JournalCombatBuffEntry> CombatBuffEntries { get; } = combatBuffEntries;

    public JournalProfileClassDocument GetClass(string classId)
    {
        return _classes.TryGetValue(classId, out var definition)
            ? definition
            : Classes[0];
    }

    public JournalProfileStageDocument GetStage(string stageId)
    {
        return _stages.TryGetValue(stageId, out var definition)
            ? definition
            : Stages[0];
    }

    public int GetStageIndex(string stageId)
    {
        for (var index = 0; index < Stages.Count; index++)
        {
            if (string.Equals(Stages[index].Id, stageId, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return 0;
    }

    private static string NormalizeModProfileName(string name)
    {
        var result = name.Trim();
        ReadOnlySpan<string> prefixes = ["Прогрессия ", "Progression "];

        foreach (var prefix in prefixes)
        {
            if (!result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            result = result[prefix.Length..].TrimStart();
            break;
        }

        if (result.EndsWith(" Mod", StringComparison.OrdinalIgnoreCase))
        {
            result = result[..^4].TrimEnd();
        }

        return result;
    }
}
