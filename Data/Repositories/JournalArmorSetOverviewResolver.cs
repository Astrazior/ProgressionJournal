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
    private static readonly Dictionary<ArmorSetKey, string[]> ModArmorSetClaimCache = [];
    private static readonly HashSet<string> LoggedHookFailures = new(StringComparer.Ordinal);

    public static IReadOnlyList<JournalStageEntry> Resolve(
        IReadOnlyList<JournalStageEntry> entries,
        string classId)
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
            var modMatches = FindModArmorSets(groupEntries).ToArray();
            matches.AddRange(FindKnownArmorSets(groupEntries, VanillaArmorSets.Value));
            var canonicalModMatches = RemoveShimmerCompatibilityPermutations(modMatches);
            matches.AddRange(GroupModArmorSetFamilies(canonicalModMatches));
        }

        return matches.Count == 0
            ? entries.Where(entry => AppliesToClass(entry, classId)).ToArray()
            : ComposeOverview(indexedEntries, matches, classId);
    }

    public static void ClearCaches()
    {
        ModArmorSetClaimCache.Clear();
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
            var classIds = ResolveArmorSetClassIds(availableFamily, itemEntries);
            if (classIds.Count == 0)
            {
                continue;
            }

            var anchor = components.MinBy(static value => value.Index)!;
            availableVariants[0].PrimeBonus();

            yield return new ArmorSetMatch(availableFamily, anchor, components, classIds, []);
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

            foreach (var resolved in ResolveModArmorSetDefinitions(head, body, leg))
            {
                var definition = resolved.Definition;
                if (!emittedDefinitionKeys.Add(definition.Key))
                {
                    continue;
                }

                var components = definition.ItemIds
                    .SelectMany(itemId => itemEntries[itemId])
                    .DistinctBy(static value => value.Index)
                    .ToArray();
                var family = new JournalArmorSetFamily([definition]);
                var classIds = ResolveArmorSetClassIds(family, itemEntries);
                if (classIds.Count == 0)
                {
                    continue;
                }

                var anchor = components.MinBy(static value => value.Index)!;
                definition.PrimeBonus();
                yield return new ArmorSetMatch(
                    family,
                    anchor,
                    components,
                    classIds,
                    resolved.ClaimKeys);
            }
        }
    }

    private static ArmorSetMatch[] GroupModArmorSetFamilies(ArmorSetMatch[] matches)
    {
        var remainingMatches = matches.ToList();
        var result = new List<ArmorSetMatch>(matches.Length);

        while (remainingMatches.Count > 0)
        {
            var component = TakeCompatibleFamilyComponent(remainingMatches);
            if (component.Length == 1)
            {
                result.Add(component[0]);
                continue;
            }

            var components = component
                .SelectMany(static match => match.Components)
                .DistinctBy(static value => value.Index)
                .ToArray();
            result.Add(new ArmorSetMatch(
                new JournalArmorSetFamily(component.SelectMany(static match => match.Family.Variants)),
                components.MinBy(static value => value.Index)!,
                components,
                component.SelectMany(static match => match.ClassIds)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase),
                component.SelectMany(static match => match.ClaimKeys)
                    .ToHashSet(StringComparer.Ordinal)));
        }

        return result.ToArray();
    }

    private static ArmorSetMatch[] TakeCompatibleFamilyComponent(List<ArmorSetMatch> remainingMatches)
    {
        var component = new List<ArmorSetMatch> { remainingMatches[0] };
        remainingMatches.RemoveAt(0);

        for (var index = 0; index < remainingMatches.Count; index++)
        {
            var candidate = remainingMatches[index];
            if (!component.Any(match => CanCombineModArmorSetMatches(match, candidate)))
            {
                continue;
            }

            component.Add(candidate);
            remainingMatches.RemoveAt(index);
            index = -1;
        }

        return component.ToArray();
    }

    private static bool CanCombineModArmorSetMatches(ArmorSetMatch left, ArmorSetMatch right) =>
        left.ClaimKeys.Overlaps(right.ClaimKeys)
        && left.Family.ItemIds.Any(right.Family.ItemIds.Contains)
        && HaveMatchingArmorSetBonus(
            left.Family.Variants[0],
            right.Family.Variants[0]);

    private static bool HaveMatchingArmorSetBonus(
        JournalArmorSetDefinition left,
        JournalArmorSetDefinition right)
    {
        var leftBonus = JournalArmorSetBonusResolver.Resolve(left);
        var rightBonus = JournalArmorSetBonusResolver.Resolve(right);
        return !leftBonus.Failed
               && !rightBonus.Failed
               && leftBonus.DefenseBonus == rightBonus.DefenseBonus
               && string.Equals(leftBonus.Text, rightBonus.Text, StringComparison.Ordinal);
    }

    private static ArmorSetMatch[] RemoveShimmerCompatibilityPermutations(ArmorSetMatch[] matches)
    {
        var remainingCompleteMatches = matches
            .Where(static match => match.Family.Variants.Count == 1
                                   && match.Family.Variants[0].ItemIds.Count == 3)
            .ToList();
        if (remainingCompleteMatches.Count < 2)
        {
            return matches;
        }

        var suppressedKeys = new HashSet<string>(StringComparer.Ordinal);
        while (remainingCompleteMatches.Count > 0)
        {
            var component = TakeOverlappingComponent(remainingCompleteMatches);
            if (!TryResolveCartesianShimmerVariants(component, out var canonicalKeys))
            {
                continue;
            }

            suppressedKeys.UnionWith(component
                .Select(static match => match.Family.Variants[0].Key)
                .Where(key => !canonicalKeys.Contains(key)));
        }

        return matches
            .Where(match => match.Family.Variants.Count != 1
                            || !suppressedKeys.Contains(match.Family.Variants[0].Key))
            .ToArray();
    }

    private static ArmorSetMatch[] TakeOverlappingComponent(List<ArmorSetMatch> remainingMatches)
    {
        var component = new List<ArmorSetMatch> { remainingMatches[^1] };
        remainingMatches.RemoveAt(remainingMatches.Count - 1);
        var componentItemIds = component[0].Family.ItemIds.ToHashSet();

        for (var index = remainingMatches.Count - 1; index >= 0; index--)
        {
            var candidate = remainingMatches[index];
            if (!candidate.Family.ItemIds.Any(componentItemIds.Contains))
            {
                continue;
            }

            component.Add(candidate);
            componentItemIds.UnionWith(candidate.Family.ItemIds);
            remainingMatches.RemoveAt(index);
            index = remainingMatches.Count;
        }

        return component.ToArray();
    }

    private static bool TryResolveCartesianShimmerVariants(
        ArmorSetMatch[] component,
        out HashSet<string> canonicalKeys)
    {
        canonicalKeys = [];
        var definitions = component
            .Select(static match => match.Family.Variants[0])
            .DistinctBy(static definition => definition.Key)
            .ToArray();
        var headItemIds = OrderShimmerVariants(definitions
            .Select(static definition => definition.HeadItemId)
            .Distinct()
            .ToArray());
        var bodyItemIds = OrderShimmerVariants(definitions
            .Select(static definition => definition.BodyItemId)
            .Distinct()
            .ToArray());
        var legItemIds = OrderShimmerVariants(definitions
            .Select(static definition => definition.LegItemId)
            .Distinct()
            .ToArray());
        var variantCount = Math.Max(headItemIds.Length, Math.Max(bodyItemIds.Length, legItemIds.Length));
        if (variantCount < 2
            || !HasCompatibleVariantCount(headItemIds, variantCount)
            || !HasCompatibleVariantCount(bodyItemIds, variantCount)
            || !HasCompatibleVariantCount(legItemIds, variantCount)
            || definitions.Length != headItemIds.Length * bodyItemIds.Length * legItemIds.Length
            || !AreShimmerVariants(headItemIds)
            || !AreShimmerVariants(bodyItemIds)
            || !AreShimmerVariants(legItemIds))
        {
            return false;
        }

        var definitionKeys = definitions
            .Select(static definition => definition.Key)
            .ToHashSet(StringComparer.Ordinal);
        for (var variantIndex = 0; variantIndex < variantCount; variantIndex++)
        {
            var definition = new JournalArmorSetDefinition(
                GetVariantItemId(headItemIds, variantIndex),
                GetVariantItemId(bodyItemIds, variantIndex),
                GetVariantItemId(legItemIds, variantIndex));
            if (!definitionKeys.Contains(definition.Key))
            {
                canonicalKeys.Clear();
                return false;
            }

            canonicalKeys.Add(definition.Key);
        }

        return canonicalKeys.Count < definitions.Length;
    }

    private static bool HasCompatibleVariantCount(int[] itemIds, int variantCount) =>
        itemIds.Length == 1 || itemIds.Length == variantCount;

    private static int GetVariantItemId(int[] itemIds, int variantIndex) =>
        itemIds.Length == 1 ? itemIds[0] : itemIds[variantIndex];

    private static int[] OrderShimmerVariants(int[] itemIds)
    {
        return itemIds
            .OrderBy(itemId => IsShimmerTargetOnly(itemId, itemIds) ? 1 : 0)
            .ThenBy(static itemId => itemId)
            .ToArray();
    }

    private static bool IsShimmerTargetOnly(int itemId, int[] variants)
    {
        var hasIncomingTransform = variants.Any(variant => GetShimmerTransform(variant) == itemId);
        var hasOutgoingTransform = variants.Contains(GetShimmerTransform(itemId));
        return hasIncomingTransform && !hasOutgoingTransform;
    }

    private static bool AreShimmerVariants(int[] itemIds)
    {
        if (itemIds.Length <= 1)
        {
            return true;
        }

        var reached = new HashSet<int> { itemIds[0] };
        var remaining = itemIds.Skip(1).ToHashSet();
        while (remaining.RemoveWhere(candidate => reached.Any(itemId => AreShimmerLinked(itemId, candidate))) > 0)
        {
            reached.UnionWith(itemIds.Where(itemId => !remaining.Contains(itemId)));
        }

        return remaining.Count == 0;
    }

    private static bool AreShimmerLinked(int leftItemId, int rightItemId) =>
        GetShimmerTransform(leftItemId) == rightItemId
        || GetShimmerTransform(rightItemId) == leftItemId;

    private static int GetShimmerTransform(int itemId)
    {
        return itemId >= ItemID.None && itemId < ItemID.Sets.ShimmerTransformToItem.Length
            ? ItemID.Sets.ShimmerTransformToItem[itemId]
            : ItemID.None;
    }

    private static HashSet<string> ResolveArmorSetClassIds(
        JournalArmorSetFamily family,
        Dictionary<int, List<IndexedEntry>> itemEntries)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var variant in family.Variants)
        {
            HashSet<string>? variantClassIds = null;
            foreach (var itemId in variant.ItemIds)
            {
                var itemClassIds = itemEntries[itemId]
                    .SelectMany(static entry => GetClassIds(entry.Entry))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (variantClassIds is null)
                {
                    variantClassIds = itemClassIds;
                }
                else
                {
                    variantClassIds.IntersectWith(itemClassIds);
                }
            }

            if (variantClassIds is not null)
            {
                result.UnionWith(variantClassIds);
            }
        }

        return result;
    }

    private static IReadOnlySet<string> GetClassIds(JournalStageEntry entry)
    {
        return entry.WikiRecommendation?.ClassIds ?? entry.Entry.ClassIds;
    }

    private static IEnumerable<ModArmorSetDefinition> ResolveModArmorSetDefinitions(
        Item head,
        Item body,
        Item legs)
    {
        var claimKeys = ResolveModArmorSetClaims(head, body, legs);
        if (claimKeys.Count == 0)
        {
            yield break;
        }

        var definition = new JournalArmorSetDefinition(head.type, body.type, legs.type);
        if (definition.ItemIds.Count == 2)
        {
            yield return new ModArmorSetDefinition(definition, claimKeys);
            yield break;
        }

        var air = CreateAirItem();
        var twoPieceDefinitions = new[]
            {
                (Head: air, Body: body, Legs: legs),
                (Head: head, Body: air, Legs: legs),
                (Head: head, Body: body, Legs: air)
            }
            .Select(candidate => new
            {
                Definition = new JournalArmorSetDefinition(
                    candidate.Head.type,
                    candidate.Body.type,
                    candidate.Legs.type),
                ClaimKeys = ResolveModArmorSetClaims(candidate.Head, candidate.Body, candidate.Legs)
            })
            .Where(static candidate => candidate.ClaimKeys.Count > 0)
            .DistinctBy(static candidate => candidate.Definition.Key)
            .ToArray();

        if (twoPieceDefinitions.Length == 0)
        {
            yield return new ModArmorSetDefinition(definition, claimKeys);
            yield break;
        }

        foreach (var twoPieceDefinition in twoPieceDefinitions)
        {
            yield return new ModArmorSetDefinition(
                twoPieceDefinition.Definition,
                twoPieceDefinition.ClaimKeys);
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

    private static HashSet<string> ResolveModArmorSetClaims(Item head, Item body, Item legs)
    {
        var key = new ArmorSetKey(head.type, body.type, legs.type);
        if (ModArmorSetClaimCache.TryGetValue(key, out var cached))
        {
            return cached.ToHashSet(StringComparer.Ordinal);
        }

        var result = new HashSet<string>(StringComparer.Ordinal);
        AppendModItemArmorSetClaims(result, head, body, legs);
        AppendGlobalItemArmorSetClaims(result, head, body, legs);
        ModArmorSetClaimCache[key] = result.ToArray();
        return result;
    }

    private static void AppendModItemArmorSetClaims(
        HashSet<string> result,
        Item head,
        Item body,
        Item legs)
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
                    result.Add($"item:{modItem.GetType().AssemblyQualifiedName}");
                }
            }
            catch (Exception exception)
            {
                LogHookFailure(modItem.GetType(), head, body, legs, exception);
            }
        }
    }

    private static void AppendGlobalItemArmorSetClaims(
        HashSet<string> result,
        Item head,
        Item body,
        Item legs)
    {
        foreach (var globalItem in GlobalArmorSetHooks.Value)
        {
            try
            {
                var setName = globalItem.IsArmorSet(head, body, legs);
                if (!string.IsNullOrEmpty(setName))
                {
                    result.Add($"global:{globalItem.GetType().AssemblyQualifiedName}:{setName}");
                }
            }
            catch (Exception exception)
            {
                LogHookFailure(globalItem.GetType(), head, body, legs, exception);
            }
        }
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
        IReadOnlyList<ArmorSetMatch> matches,
        string classId)
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
                var classIds = group
                    .SelectMany(static match => match.ClassIds)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var claimKeys = group
                    .SelectMany(static match => match.ClaimKeys)
                    .ToHashSet(StringComparer.Ordinal);
                return new ArmorSetMatch(family, anchor, components, classIds, claimKeys);
            })
            .ToArray();
        var matchesByAnchor = uniqueMatches
            .Where(match => match.ClassIds.Contains(classId))
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

            if (!AppliesToClass(indexedEntry.Entry, classId))
            {
                continue;
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

    private static bool AppliesToClass(JournalStageEntry entry, string classId)
    {
        return GetClassIds(entry).Contains(classId);
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

    private sealed record ModArmorSetDefinition(
        JournalArmorSetDefinition Definition,
        HashSet<string> ClaimKeys);

    private sealed record ArmorSetMatch(
        JournalArmorSetFamily Family,
        IndexedEntry Anchor,
        IReadOnlyList<IndexedEntry> Components,
        HashSet<string> ClassIds,
        HashSet<string> ClaimKeys);
}
