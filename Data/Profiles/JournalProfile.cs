namespace ProgressionJournal.Data.Profiles;

public sealed class JournalProfile(
    JournalProfileDocument document,
    IReadOnlyList<JournalEntry> entries,
    string sourcePath,
    bool isBuiltIn,
    bool hasVersionMismatch)
{
    private readonly Dictionary<string, JournalProfileClassDocument> _classes =
        document.Classes.ToDictionary(static value => value.Id, StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, JournalProfileStageDocument> _stages =
        document.Stages.ToDictionary(static value => value.Id, StringComparer.OrdinalIgnoreCase);

    public JournalProfileDocument Document { get; } = document;

    public string Id => Document.Id;

    public string Name => Document.Name;

    public string Author => Document.Author;

    public bool IsReadOnly => IsBuiltIn || Document.ReadOnly;

    public bool IsBuiltIn { get; } = isBuiltIn;

    public bool HasVersionMismatch { get; } = hasVersionMismatch;

    public string SourcePath { get; } = sourcePath;

    public IReadOnlyList<JournalProfileClassDocument> Classes => Document.Classes;

    public IReadOnlyList<JournalProfileStageDocument> Stages => Document.Stages;

    public IReadOnlyList<JournalEntry> Entries { get; } = entries;

    public JournalProfileClassDocument GetClass(string classId)
    {
        return _classes.TryGetValue(classId, out var definition)
            ? definition
            : Classes.First();
    }

    public JournalProfileStageDocument GetStage(string stageId)
    {
        return _stages.TryGetValue(stageId, out var definition)
            ? definition
            : Stages.First();
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
}
