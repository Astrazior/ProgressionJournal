namespace ProgressionJournal.Data.Models;

internal sealed class JournalArmorSetFamily
{
    public JournalArmorSetFamily(IEnumerable<JournalArmorSetDefinition> variants)
    {
        Variants = variants
            .DistinctBy(static variant => variant.Key)
            .ToArray();
        if (Variants.Count == 0)
        {
            throw new ArgumentException("An armor set family must contain at least one variant.", nameof(variants));
        }

        ItemIds = Variants
            .SelectMany(static variant => variant.ItemIds)
            .Distinct()
            .ToArray();
    }

    public IReadOnlyList<JournalArmorSetDefinition> Variants { get; }

    public IReadOnlyList<int> ItemIds { get; }
}
