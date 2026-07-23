using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ProgressionJournal.Data.Repositories;

internal static class JournalArmorSetOverviewResolver
{
    private static readonly Type[] ArmorSetParameterTypes = [typeof(Item), typeof(Item), typeof(Item)];
    private static readonly Lazy<GlobalItem[]> GlobalArmorSetHooks = new(() => ModContent
        .GetContent<GlobalItem>()
        .Where(static globalItem => OverridesArmorSetHook(globalItem.GetType()))
        .ToArray());
    private static readonly Lazy<JournalArmorSetFamily[]> VanillaArmorSets = new(CreateVanillaArmorSets);
    private static readonly Dictionary<ArmorSetKey, bool> ModArmorSetCache = [];
    private static readonly HashSet<string> LoggedHookFailures = new(StringComparer.Ordinal);

    public static IReadOnlyList<JournalStageEntry> Resolve(IReadOnlyList<JournalStageEntry> entries)
    {
        if (entries.Count == 0)
        {
            return entries;
        }

        var indexedEntries = entries
            .Select((entry, index) => new IndexedEntry(entry, index))
            .ToArray();
        var matches = new List<ArmorSetMatch>();

        foreach (var candidateGroup in indexedEntries
                     .Where(static value => value.Entry.Entry.Category == JournalItemCategory.Armor)
                     .GroupBy(static value => CreateGroupKey(value.Entry)))
        {
            var groupEntries = candidateGroup.ToArray();
            matches.AddRange(FindKnownArmorSets(groupEntries, VanillaArmorSets.Value));
            matches.AddRange(FindModArmorSets(groupEntries));
        }

        return matches.Count == 0 ? entries : ComposeOverview(indexedEntries, matches);
    }

    public static void ClearCaches()
    {
        ModArmorSetCache.Clear();
        LoggedHookFailures.Clear();
    }

    private static JournalArmorSetFamily[] CreateVanillaArmorSets()
    {
        return JournalRepository.GetAllVanillaEntries()
            .Where(static entry => entry is
            {
                Category: JournalItemCategory.Armor,
                IsArmorSet: true
            })
            .SelectMany(CreateFamilies)
            .ToArray();
    }

    private static IEnumerable<JournalArmorSetFamily> CreateFamilies(JournalEntry entry)
    {
        var definitions = CreateDefinitions(entry.ItemGroups)
            .DistinctBy(static definition => definition.Key)
            .ToArray();
        if (definitions.Length == 0)
        {
            yield break;
        }

        if (string.Equals(entry.Key, JournalRepository.WizardRobeEntryKey, StringComparison.Ordinal))
        {
            yield return new JournalArmorSetFamily(definitions);
            yield break;
        }

        foreach (var definition in definitions)
        {
            yield return new JournalArmorSetFamily([definition]);
        }
    }

    private static IEnumerable<JournalArmorSetDefinition> CreateDefinitions(
        IReadOnlyList<JournalItemGroup> groups)
    {
        if (groups.Count is not (2 or 3))
        {
            yield break;
        }

        var variantCount = groups.Max(static group => group.ItemIds.Count);
        if (groups.Any(group => group.ItemIds.Count != 1 && group.ItemIds.Count != variantCount))
        {
            yield break;
        }

        for (var variantIndex = 0; variantIndex < variantCount; variantIndex++)
        {
            var itemIds = groups
                .Select(group => GetVariantItemId(group, variantIndex))
                .ToArray();
            if (TryCreateArmorSetDefinition(itemIds, out var definition))
            {
                yield return definition;
            }
        }
    }

    private static int GetVariantItemId(JournalItemGroup group, int variantIndex)
    {
        return group.ItemIds.Count == 1 ? group.ItemIds[0] : group.ItemIds[variantIndex];
    }

    private static bool TryCreateArmorSetDefinition(
        int[] itemIds,
        out JournalArmorSetDefinition definition)
    {
        int headItemId = ItemID.None;
        int bodyItemId = ItemID.None;
        int legItemId = ItemID.None;

        foreach (var itemId in itemIds)
        {
            if (!ContentSamples.ItemsByType.TryGetValue(itemId, out var item)
                || item is null
                || item.vanity)
            {
                definition = null!;
                return false;
            }

            if (item.headSlot >= 0 && headItemId == ItemID.None)
            {
                headItemId = itemId;
            }
            else if (item.bodySlot >= 0 && bodyItemId == ItemID.None)
            {
                bodyItemId = itemId;
            }
            else if (item.legSlot >= 0 && legItemId == ItemID.None)
            {
                legItemId = itemId;
            }
            else
            {
                definition = null!;
                return false;
            }
        }

        definition = new JournalArmorSetDefinition(headItemId, bodyItemId, legItemId);
        return definition.ItemIds.Count >= 2;
    }

    private static IEnumerable<ArmorSetMatch> FindKnownArmorSets(
        IndexedEntry[] entries,
        IReadOnlyList<JournalArmorSetFamily> families)
    {
        var itemEntries = CreateItemEntryLookup(entries);

        foreach (var family in families)
        {
            var availableVariants = family.Variants
                .Where(definition => definition.ItemIds.All(itemEntries.ContainsKey))
                .ToArray();
            if (availableVariants.Length == 0)
            {
                continue;
            }

            var availableFamily = new JournalArmorSetFamily(availableVariants);
            var components = availableFamily.ItemIds
                .SelectMany(itemId => itemEntries[itemId])
                .DistinctBy(static value => value.Index)
                .ToArray();
            var anchor = components.MinBy(static value => value.Index)!;
            availableVariants[0].PrimeBonus();

            yield return new ArmorSetMatch(availableFamily, anchor, components);
        }
    }

    private static IEnumerable<ArmorSetMatch> FindModArmorSets(IndexedEntry[] entries)
    {
        var itemEntries = CreateItemEntryLookup(entries);
        var items = itemEntries.Keys
            .Select(itemId => TryGetArmorItem(itemId, out var item) ? item : null)
            .OfType<Item>()
            .ToArray();
        var air = CreateAirItem();
        var heads = items.Where(static item => item.headSlot >= 0).Prepend(air).ToArray();
        var bodies = items.Where(static item => item.bodySlot >= 0).Prepend(air).ToArray();
        var legs = items.Where(static item => item.legSlot >= 0).Prepend(air).ToArray();
        var emittedDefinitionKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var head in heads)
        foreach (var body in bodies)
        foreach (var leg in legs)
        {
            if (CountPresentItems(head, body, leg) < 2)
            {
                continue;
            }

            foreach (var definition in ResolveModArmorSetDefinitions(head, body, leg))
            {
                if (!emittedDefinitionKeys.Add(definition.Key))
                {
                    continue;
                }

                var components = definition.ItemIds
                    .SelectMany(itemId => itemEntries[itemId])
                    .DistinctBy(static value => value.Index)
                    .ToArray();
                var anchor = components.MinBy(static value => value.Index)!;
                definition.PrimeBonus();
                yield return new ArmorSetMatch(
                    new JournalArmorSetFamily([definition]),
                    anchor,
                    components);
            }
        }
    }

    private static IEnumerable<JournalArmorSetDefinition> ResolveModArmorSetDefinitions(
        Item head,
        Item body,
        Item legs)
    {
        if (!IsModArmorSet(head, body, legs))
        {
            yield break;
        }

        var definition = new JournalArmorSetDefinition(head.type, body.type, legs.type);
        if (definition.ItemIds.Count == 2)
        {
            yield return definition;
            yield break;
        }

        var air = CreateAirItem();
        var twoPieceDefinitions = new[]
            {
                (Head: air, Body: body, Legs: legs),
                (Head: head, Body: air, Legs: legs),
                (Head: head, Body: body, Legs: air)
            }
            .Where(candidate => IsModArmorSet(candidate.Head, candidate.Body, candidate.Legs))
            .Select(static candidate => new JournalArmorSetDefinition(
                candidate.Head.type,
                candidate.Body.type,
                candidate.Legs.type))
            .DistinctBy(static definition => definition.Key)
            .ToArray();

        if (twoPieceDefinitions.Length == 0)
        {
            yield return definition;
            yield break;
        }

        foreach (var twoPieceDefinition in twoPieceDefinitions)
        {
            yield return twoPieceDefinition;
        }
    }

    private static int CountPresentItems(Item head, Item body, Item legs)
    {
        return (head.type > ItemID.None ? 1 : 0)
               + (body.type > ItemID.None ? 1 : 0)
               + (legs.type > ItemID.None ? 1 : 0);
    }

    private static Item CreateAirItem()
    {
        var item = new Item();
        item.SetDefaults(ItemID.None);
        return item;
    }

    private static Dictionary<int, List<IndexedEntry>> CreateItemEntryLookup(
        IEnumerable<IndexedEntry> entries)
    {
        var result = new Dictionary<int, List<IndexedEntry>>();
        foreach (var entry in entries)
        {
            foreach (var itemId in entry.Entry.Entry.ItemIds)
            {
                if (!result.TryGetValue(itemId, out var owners))
                {
                    owners = [];
                    result[itemId] = owners;
                }

                owners.Add(entry);
            }
        }

        return result;
    }

    private static bool TryGetArmorItem(int itemId, out Item item)
    {
        if (ContentSamples.ItemsByType.TryGetValue(itemId, out var sample)
            && sample is not null
            && !sample.vanity
            && (sample.headSlot >= 0 || sample.bodySlot >= 0 || sample.legSlot >= 0))
        {
            item = sample.Clone();
            return true;
        }

        item = null!;
        return false;
    }

    private static bool IsModArmorSet(Item head, Item body, Item legs)
    {
        var key = new ArmorSetKey(head.type, body.type, legs.type);
        if (ModArmorSetCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var result = IsModItemArmorSet(head, body, legs) || IsGlobalItemArmorSet(head, body, legs);
        ModArmorSetCache[key] = result;
        return result;
    }

    private static bool IsModItemArmorSet(Item head, Item body, Item legs)
    {
        var modItems = new[] { head.ModItem, body.ModItem, legs.ModItem }
            .Where(static modItem => modItem is not null)
            .Select(static modItem => modItem!)
            .Distinct();

        foreach (var modItem in modItems)
        {
            try
            {
                if (modItem.IsArmorSet(head, body, legs))
                {
                    return true;
                }
            }
            catch (Exception exception)
            {
                LogHookFailure(modItem.GetType(), head, body, legs, exception);
            }
        }

        return false;
    }

    private static bool IsGlobalItemArmorSet(Item head, Item body, Item legs)
    {
        foreach (var globalItem in GlobalArmorSetHooks.Value)
        {
            try
            {
                if (!string.IsNullOrEmpty(globalItem.IsArmorSet(head, body, legs)))
                {
                    return true;
                }
            }
            catch (Exception exception)
            {
                LogHookFailure(globalItem.GetType(), head, body, legs, exception);
            }
        }

        return false;
    }

    private static bool OverridesArmorSetHook(Type type)
    {
        var method = type.GetMethod(
            nameof(GlobalItem.IsArmorSet),
            BindingFlags.Instance | BindingFlags.Public,
            null,
            ArmorSetParameterTypes,
            null);
        return method?.DeclaringType != typeof(GlobalItem);
    }

    private static void LogHookFailure(
        Type hookType,
        Item head,
        Item body,
        Item legs,
        Exception exception)
    {
        var context = $"{hookType.FullName}:{head.type}:{body.type}:{legs.type}";
        if (!LoggedHookFailures.Add(context))
        {
            return;
        }

        ProgressionJournal.Instance?.Logger.Debug(
            $"Armor set detection failed for hook '{hookType.FullName}' and items " +
            $"{head.type}/{body.type}/{legs.type}.{Environment.NewLine}{exception}");
    }

    private static JournalStageEntry[] ComposeOverview(
        IndexedEntry[] entries,
        IReadOnlyList<ArmorSetMatch> matches)
    {
        var uniqueMatches = matches
            .GroupBy(static match => new MatchKey(
                string.Join(
                    ",",
                    match.Components
                        .Select(static component => component.Index)
                        .Order()),
                string.Join(
                    ",",
                    match.Family.Variants
                        .Select(static variant => variant.Key)
                        .Order()),
                CreateGroupKey(match.Anchor.Entry)))
            .Select(static group =>
            {
                var components = group
                    .SelectMany(static match => match.Components)
                    .DistinctBy(static component => component.Index)
                    .ToArray();
                var anchor = components.MinBy(static component => component.Index)!;
                var family = new JournalArmorSetFamily(group
                    .SelectMany(static match => match.Family.Variants));
                return new ArmorSetMatch(family, anchor, components);
            })
            .ToArray();
        var matchesByAnchor = uniqueMatches
            .GroupBy(static match => match.Anchor.Index)
            .ToDictionary(static group => group.Key, static group => group.ToArray());
        var coveredItems = new Dictionary<int, HashSet<int>>();

        foreach (var match in uniqueMatches)
        {
            foreach (var component in match.Components)
            {
                if (!coveredItems.TryGetValue(component.Index, out var itemIds))
                {
                    itemIds = [];
                    coveredItems[component.Index] = itemIds;
                }

                itemIds.UnionWith(match.Family.ItemIds);
            }
        }

        var result = new List<JournalStageEntry>(entries.Length);
        foreach (var indexedEntry in entries)
        {
            if (matchesByAnchor.TryGetValue(indexedEntry.Index, out var anchoredMatches))
            {
                result.AddRange(anchoredMatches.Select(match => new JournalStageEntry(
                    indexedEntry.Entry.Entry,
                    indexedEntry.Entry.Evaluation,
                    indexedEntry.Entry.WikiRecommendation,
                    match.Family)));
            }

            if (!coveredItems.TryGetValue(indexedEntry.Index, out var coveredItemIds))
            {
                result.Add(indexedEntry.Entry);
                continue;
            }

            var remainder = CreateRemainderEntry(indexedEntry.Entry, coveredItemIds);
            if (remainder is not null)
            {
                result.Add(remainder);
            }
        }

        return result.ToArray();
    }

    private static JournalStageEntry? CreateRemainderEntry(
        JournalStageEntry stageEntry,
        HashSet<int> coveredItemIds)
    {
        var itemGroups = stageEntry.Entry.ItemGroups
            .Select(group => group.ItemIds
                .Where(itemId => !coveredItemIds.Contains(itemId))
                .ToArray())
            .Where(static itemIds => itemIds.Length > 0)
            .Select(static itemIds => new JournalItemGroup(itemIds))
            .ToArray();
        if (itemGroups.Length == 0)
        {
            return null;
        }

        var source = stageEntry.Entry;
        var remainder = new JournalEntry(
            $"{source.Key}:armor-remainder",
            source.Category,
            source.ClassIds,
            itemGroups,
            source.Evaluations,
            source.EventCategory,
            source.IsSupportWeapon,
            source.CustomEventName,
            source.EventIcon,
            source.WikiRecommendations,
            source.FishingSources);
        return new JournalStageEntry(remainder, stageEntry.Evaluation, stageEntry.WikiRecommendation);
    }

    private static ArmorSetGroupKey CreateGroupKey(JournalStageEntry entry)
    {
        return new ArmorSetGroupKey(
            entry.Evaluation.Tier,
            entry.Evaluation.Scope,
            entry.IsWikiRecommendation,
            entry.Entry.EventCategory,
            entry.Entry.CustomEventName,
            entry.WikiRecommendation?.SourceName ?? string.Empty,
            entry.WikiRecommendation?.SourceUrl ?? string.Empty);
    }

    private readonly record struct ArmorSetKey(int HeadItemId, int BodyItemId, int LegItemId);

    private readonly record struct ArmorSetGroupKey(
        RecommendationTier Tier,
        JournalEvaluationScope Scope,
        bool IsWikiRecommendation,
        JournalEventCategory? EventCategory,
        string CustomEventName,
        string WikiSourceName,
        string WikiSourceUrl);

    private readonly record struct MatchKey(
        string ComponentKey,
        string FamilyKey,
        ArmorSetGroupKey GroupKey);

    private sealed record IndexedEntry(JournalStageEntry Entry, int Index);

    private sealed record ArmorSetMatch(
        JournalArmorSetFamily Family,
        IndexedEntry Anchor,
        IReadOnlyList<IndexedEntry> Components);
}
