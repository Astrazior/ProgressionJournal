using Terraria;

namespace ProgressionJournal.Data.Models;

public sealed class JournalRecipeSource(
    IEnumerable<Item> ingredients,
    IEnumerable<JournalTileStationSource> stations,
    IEnumerable<string> conditions)
{
    public IReadOnlyList<Item> Ingredients { get; } = ingredients
        .Select(static ingredient => ingredient.Clone())
        .ToArray();

    public IReadOnlyList<JournalTileStationSource> Stations { get; } = stations
        .DistinctBy(static station => station.TileId)
        .ToArray();

    public IReadOnlyList<string> Conditions { get; } = conditions
        .Where(static condition => !string.IsNullOrWhiteSpace(condition))
        .Distinct()
        .ToArray();
}
