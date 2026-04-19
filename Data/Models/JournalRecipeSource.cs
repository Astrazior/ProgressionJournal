using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace ProgressionJournal.Data.Models;

public sealed class JournalRecipeSource(
    IEnumerable<Item> ingredients,
    IEnumerable<Item> stations,
    IEnumerable<string> conditions)
{
    public IReadOnlyList<Item> Ingredients { get; } = ingredients
        .Select(static ingredient => ingredient.Clone())
        .ToArray();

    public IReadOnlyList<Item> Stations { get; } = stations
        .Select(static station => station.Clone())
        .ToArray();

    public IReadOnlyList<string> Conditions { get; } = conditions
        .Where(static condition => !string.IsNullOrWhiteSpace(condition))
        .Distinct()
        .ToArray();
}
