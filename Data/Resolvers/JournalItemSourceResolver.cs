using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ProgressionJournal.Data.Resolvers;

public static class JournalItemSourceResolver
{
    private static readonly Dictionary<(string ProfileId, int ItemId), JournalItemAcquisitionInfo> Cache = new();
    private static readonly Dictionary<int, Item?> StationItemCache = new();
    private static readonly Dictionary<int, bool> RevengeanceExclusiveCache = new();
    private static readonly Dictionary<int, string[]> SourceItemAcquisitionConditionCache = new();
    private static Dictionary<int, JournalRecipeSource[]>? RecipeIndex;
    private static Dictionary<int, JournalDropSource[]>? DropIndex;
    private static Dictionary<int, JournalShopSource[]>? ShopIndex;
    private static readonly FieldInfo? GlobalNpcDropRulesField = typeof(ItemDropDatabase).GetField(
        "_globalEntries",
        BindingFlags.Instance | BindingFlags.NonPublic);
#pragma warning disable SYSLIB1045 // tModLoader's in-game compiler does not run the GeneratedRegex source generator.
    private static readonly Regex InternalNameWordBoundaryRegex = new("(?<=[a-z])(?=[A-Z])", RegexOptions.Compiled);
#pragma warning restore SYSLIB1045

    public static JournalItemAcquisitionInfo GetInfo(int itemId)
    {
        var profileId = JournalProfileRegistry.IsLoaded ? JournalProfileRegistry.Active.Id : string.Empty;
        var cacheKey = (profileId, itemId);
        if (Cache.TryGetValue(cacheKey, out var info))
        {
            return info;
        }

        var stopwatch = Stopwatch.StartNew();
        info = new JournalItemAcquisitionInfo(
            itemId,
            BuildRecipes(itemId),
            BuildDrops(itemId),
            BuildShops(itemId),
            FindProfileFishingSources(itemId));
        Cache[cacheKey] = info;
        LogSlowResolve(itemId, profileId, stopwatch.Elapsed);
        return info;
    }

    private static IEnumerable<JournalFishingSource> FindProfileFishingSources(int itemId)
    {
        if (!JournalProfileRegistry.IsLoaded)
        {
            return [];
        }

        var activeSources = FindProfileFishingSources(JournalProfileRegistry.Active, itemId)
            .ToArray();
        if (activeSources.Length > 0)
        {
            return activeSources;
        }

        var runtimeSources = JournalFishingSourceResolver.FindSources(itemId);
        if (runtimeSources.Count > 0)
        {
            return runtimeSources;
        }

        return JournalProfileRegistry.All
            .Where(profile => !string.Equals(
                profile.Id,
                JournalProfileRegistry.Active.Id,
                StringComparison.OrdinalIgnoreCase))
            .SelectMany(profile => FindProfileFishingSources(profile, itemId))
            .GroupBy(static source => string.Join('\n', source.Conditions), StringComparer.Ordinal)
            .Select(static group => group.First());
    }

    private static IEnumerable<JournalFishingSource> FindProfileFishingSources(JournalProfile profile, int itemId)
    {
        return profile.Entries
            .Where(entry => entry.ItemIds.Contains(itemId))
            .SelectMany(static entry => entry.FishingSources);
    }

    private static JournalRecipeSource[] BuildRecipes(int itemId)
    {
        return GetRecipeIndex().TryGetValue(itemId, out var recipes) ? recipes : [];
    }

    private static Dictionary<int, JournalRecipeSource[]> GetRecipeIndex()
    {
        return RecipeIndex ??= BuildRecipeIndex();
    }

    private static Dictionary<int, JournalRecipeSource[]> BuildRecipeIndex()
    {
        var recipesByItem = new Dictionary<int, List<JournalRecipeSource>>();

        for (var recipeIndex = 0; recipeIndex < Recipe.numRecipes; recipeIndex++)
        {
            var recipe = Main.recipe[recipeIndex];
            if (recipe is null || recipe.Disabled || recipe.createItem.type <= ItemID.None)
            {
                continue;
            }

            var ingredients = recipe.requiredItem
                .Where(static ingredient => ingredient is not null && !ingredient.IsAir)
                .Select(static ingredient => ingredient.Clone())
                .ToArray();
            var stations = recipe.requiredTile
                .Distinct()
                .Select(tileId => new JournalTileStationSource(tileId, GetStationName(tileId)))
                .ToArray();
            var conditions = recipe.Conditions
                .Select(GetConditionDescription)
                .Where(static description => !string.IsNullOrWhiteSpace(description))
                .ToArray();

            AddToIndex(
                recipesByItem,
                recipe.createItem.type,
                new JournalRecipeSource(ingredients, stations, conditions));
        }

        return recipesByItem.ToDictionary(static pair => pair.Key, static pair => pair.Value.ToArray());
    }

    private static JournalDropSource[] BuildDrops(int itemId)
    {
        var drops = GetDropIndex().TryGetValue(itemId, out var indexedDrops)
            ? indexedDrops.ToList()
            : [];

        AppendContainerCatalogSources(drops, itemId);
        return drops
            .GroupBy(static drop => new
            {
                drop.SourceName,
                drop.SourceNpcType,
                drop.SourceItemId,
                drop.StackMin,
                drop.StackMax,
                Conditions = string.Join('\n', drop.Conditions)
            })
            .Select(static group => group
                .OrderByDescending(static drop => drop.DropRate)
                .First())
            .OrderByDescending(static drop => drop.DropRate)
            .ThenBy(static drop => drop.SourceName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static Dictionary<int, JournalDropSource[]> GetDropIndex()
    {
        return DropIndex ??= BuildDropIndex();
    }

    private static Dictionary<int, JournalDropSource[]> BuildDropIndex()
    {
        var drops = new Dictionary<int, List<JournalDropSource>>();

        for (var npcType = 0; npcType < NPCLoader.NPCCount; npcType++)
        {
            AppendDropSourcesToIndex(
                drops,
                Main.ItemDropsDB.GetRulesForNPCID(npcType),
                GetNpcName(npcType),
                sourceNpcType: npcType,
                sourceItemId: null);
        }

        for (var sourceItemType = 1; sourceItemType < ItemLoader.ItemCount; sourceItemType++)
        {
            AppendDropSourcesToIndex(
                drops,
                Main.ItemDropsDB.GetRulesForItemID(sourceItemType),
                Lang.GetItemNameValue(sourceItemType),
                sourceNpcType: null,
                sourceItemId: sourceItemType);
        }

        return drops.ToDictionary(static pair => pair.Key, static pair => pair.Value.ToArray());
    }

    public static void ClearProfileCache()
    {
        Cache.Clear();
    }

    public static void ClearCache()
    {
        Cache.Clear();
        SourceItemAcquisitionConditionCache.Clear();
        RecipeIndex = null;
        DropIndex = null;
        ShopIndex = null;
    }

    private static void AppendContainerCatalogSources(List<JournalDropSource> drops, int targetItemId)
    {
        var catalogSources = JournalContainerLootCatalog.GetSources(targetItemId);
        drops.AddRange(catalogSources.Select(source => new JournalDropSource(
            string.IsNullOrWhiteSpace(source.SourceDisplayName)
                ? Lang.GetItemNameValue(source.SourceItemId)
                : source.SourceDisplayName,
            sourceNpcType: null,
            sourceItemId: source.SourceItemId,
            source.DropRate,
            source.StackMin,
            source.StackMax,
            conditions: [])));
    }

    private static JournalShopSource[] BuildShops(int itemId)
    {
        return GetShopIndex().TryGetValue(itemId, out var shops)
            ? shops.ToArray()
            : [];
    }

    private static Dictionary<int, JournalShopSource[]> GetShopIndex()
    {
        return ShopIndex ??= BuildShopIndex();
    }

    private static Dictionary<int, JournalShopSource[]> BuildShopIndex()
    {
        return NPCShopDatabase.AllShops
            .SelectMany(
                static shop => shop.ActiveEntries.Select(entry => new { shop, entry }))
            .Where(static pair => pair.entry.Item is not null && !pair.entry.Item.IsAir && pair.entry.Item.type > ItemID.None)
            .Select(pair => new
            {
                ItemId = pair.entry.Item!.type,
                Source = new JournalShopSource(
                    pair.shop.NpcType,
                    GetNpcName(pair.shop.NpcType),
                    pair.shop.Name,
                    EnumerateConditions(pair.entry.Conditions)
                        .Select(GetConditionDescription))
            })
            .GroupBy(static value => new
            {
                value.ItemId,
                value.Source.NpcType,
                value.Source.NpcName,
                value.Source.ShopName,
                Conditions = string.Join('\n', value.Source.Conditions)
            })
            .GroupBy(static group => group.Key.ItemId)
            .ToDictionary(
                static group => group.Key,
                static group => group
                    .Select(static value => value.First().Source)
                    .OrderBy(static shop => shop.NpcName, StringComparer.CurrentCultureIgnoreCase)
                    .ToArray());
    }

    private static void AppendDropSourcesToIndex(
        Dictionary<int, List<JournalDropSource>> drops,
        List<IItemDropRule>? rules,
        string sourceName,
        int? sourceNpcType,
        int? sourceItemId)
    {
        if (rules is null || rules.Count == 0)
        {
            return;
        }

        var reportedDrops = new List<DropRateInfo>();
        var ratesInfo = new DropRateInfoChainFeed(1f);
        foreach (var rule in rules)
        {
            try
            {
                rule.ReportDroprates(reportedDrops, ratesInfo);
            }
            catch (Exception exception)
            {
                LogDebug(
                    $"Failed to inspect drop rates for '{sourceName}' while building acquisition source index.",
                    exception);
            }
        }

        foreach (var drop in reportedDrops.Where(static drop => drop.itemId > ItemID.None))
        {
            if (sourceItemId == drop.itemId)
            {
                continue;
            }

            AddToIndex(
                drops,
                drop.itemId,
                new JournalDropSource(
                sourceName,
                sourceNpcType,
                sourceItemId,
                drop.dropRate,
                drop.stackMin,
                drop.stackMax,
                    GetDropConditionDescriptions(drop, drop.itemId, sourceItemId)));
        }
    }

    private static void AddToIndex<T>(Dictionary<int, List<T>> index, int itemId, T value)
    {
        if (!index.TryGetValue(itemId, out var values))
        {
            values = [];
            index[itemId] = values;
        }

        values.Add(value);
    }

    [Conditional("DEBUG")]
    private static void LogSlowResolve(int itemId, string profileId, TimeSpan elapsed)
    {
        if (elapsed.TotalMilliseconds < 50d)
        {
            return;
        }

        ProgressionJournal.Instance?.Logger.Info(
            $"[Perf] ResolveItemSources item={itemId} profile={profileId} took {elapsed.TotalMilliseconds:F1} ms.");
    }

    private static IEnumerable<string> GetDropConditionDescriptions(
        DropRateInfo drop,
        int targetItemId,
        int? sourceItemId)
    {
        foreach (var description in EnumerateConditions(drop.conditions)
                     .Select(condition => GetDropConditionDescription(condition, targetItemId)))
        {
            yield return description;
        }

        if (sourceItemId is not { } itemId)
        {
            yield break;
        }

        foreach (var description in GetSourceItemAcquisitionConditions(itemId))
        {
            yield return description;
        }
    }

    private static string[] GetSourceItemAcquisitionConditions(int itemId)
    {
        if (SourceItemAcquisitionConditionCache.TryGetValue(itemId, out var cached))
        {
            return cached;
        }

        var result = ResolveSourceItemAcquisitionConditions(itemId);
        SourceItemAcquisitionConditionCache[itemId] = result;
        return result;
    }

    private static string[] ResolveSourceItemAcquisitionConditions(int itemId)
    {
        if (GlobalNpcDropRulesField?.GetValue(Main.ItemDropsDB) is not List<IItemDropRule> globalRules)
        {
            return [];
        }

        var reportedDrops = new List<DropRateInfo>();
        var ratesInfo = new DropRateInfoChainFeed(1f);
        foreach (var rule in globalRules)
        {
            try
            {
                rule.ReportDroprates(reportedDrops, ratesInfo);
            }
            catch (Exception exception)
            {
                LogDebug(
                    $"Failed to inspect source acquisition conditions for item {itemId}.",
                    exception);
            }
        }

        return reportedDrops
            .Where(drop => drop.itemId == itemId)
            .SelectMany(static drop => EnumerateConditions(drop.conditions))
            .Select(condition => GetDropConditionDescription(condition, itemId))
            .Where(static description => !string.IsNullOrWhiteSpace(description))
            .Distinct()
            .ToArray();
    }

    private static string GetDropConditionDescription(object? condition, int targetItemId)
    {
        var description = GetConditionDescription(condition);
        if (!string.IsNullOrWhiteSpace(description))
        {
            return description;
        }

        if (condition?.GetType().FullName == "CalamityMod.DropHelper+LambdaDropRuleCondition"
            && IsRevengeanceExclusive(targetItemId))
        {
            return Language.GetTextValue("Mods.CalamityMod.UI.Revengeance");
        }

        return string.Empty;
    }

    private static bool IsRevengeanceExclusive(int itemId)
    {
        if (RevengeanceExclusiveCache.TryGetValue(itemId, out var cached))
        {
            return cached;
        }

        var result = false;
        try
        {
            if (ModLoader.TryGetMod("CalamityMod", out var calamity)
                && calamity.Code.GetType("CalamityMod.Items.CalamityGlobalItem") is { } globalItemType)
            {
                var getGlobalItemMethod = typeof(Item)
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(static method =>
                        method is { Name: "GetGlobalItem", IsGenericMethodDefinition: true }
                        && method.GetGenericArguments().Length == 1
                        && method.GetParameters().Length == 0);
                var globalItem = getGlobalItemMethod?
                    .MakeGenericMethod(globalItemType)
                    .Invoke(ContentSamples.ItemsByType[itemId], null);
                result = globalItemType
                    .GetProperty("revengeanceItem", BindingFlags.Public | BindingFlags.Instance)?
                    .GetValue(globalItem) is true;
            }
        }
        catch (Exception exception)
        {
            LogDebug($"Failed to inspect Calamity revengeance exclusivity for item {itemId}.", exception);
        }

        RevengeanceExclusiveCache[itemId] = result;
        return result;
    }

    private static Item? ResolveStationItem(int tileId)
    {
        if (StationItemCache.TryGetValue(tileId, out var cached))
        {
            return cached?.Clone();
        }

        var requiredStyle = Recipe.GetRequiredTileStyle(tileId);
        var stationSamples = ContentSamples.ItemsByType.Values
            .Where(sample => sample is not null && !sample.IsAir && sample.createTile == tileId)
            .ToArray();
        var exactMatch = stationSamples.FirstOrDefault(sample => sample.placeStyle == requiredStyle)?.Clone();
        var fallbackMatch = exactMatch is null
            ? stationSamples.FirstOrDefault()?.Clone()
            : null;

        var resolved = exactMatch ?? fallbackMatch;
        StationItemCache[tileId] = resolved?.Clone();
        return resolved;
    }

    private static string GetStationName(int tileId)
    {
        switch (tileId)
        {
            case TileID.DemonAltar:
                return Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemDemonAltar");
            case TileID.Bottles:
                return Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemBottleStation");
        }

        var stationItemName = ResolveStationItem(tileId)?.HoverName;
        if (!string.IsNullOrWhiteSpace(stationItemName))
        {
            return stationItemName;
        }

        var modTileName = TileLoader.GetTile(tileId)?.Name;
        if (!string.IsNullOrWhiteSpace(modTileName))
        {
            return SplitInternalName(modTileName);
        }

        var internalName = TileID.Search.GetName(tileId);
        return string.IsNullOrWhiteSpace(internalName)
            ? $"Tile {tileId}"
            : SplitInternalName(internalName);
    }

    private static string SplitInternalName(string value) =>
        InternalNameWordBoundaryRegex.Replace(value, " ");

    private static string GetNpcName(int npcType)
    {
        var name = Lang.GetNPCNameValue(npcType);
        return string.IsNullOrWhiteSpace(name) ? $"NPC {npcType}" : name;
    }

    private static string GetConditionDescription(object? condition)
    {
        switch (condition)
        {
            case null:
                return string.Empty;
            case IProvideItemConditionDescription itemConditionDescription:
                return itemConditionDescription.GetConditionDescription();
        }

        var descriptionProperty = condition.GetType().GetProperty("Description", BindingFlags.Public | BindingFlags.Instance);
        if (descriptionProperty?.GetValue(condition) is { } descriptionValue)
        {
            if (descriptionValue is string stringDescription)
            {
                return stringDescription;
            }

            var valueProperty = descriptionValue.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
            if (valueProperty?.GetValue(descriptionValue) is string localizedDescription)
            {
                return localizedDescription;
            }

            return descriptionValue.ToString() ?? string.Empty;
        }

        var getterMethod = condition.GetType().GetMethod("GetConditionDescription", BindingFlags.Public | BindingFlags.Instance);
        if (getterMethod?.Invoke(condition, null) is string reflectedDescription)
        {
            return reflectedDescription;
        }

        return string.Empty;
    }

    private static IEnumerable<object?> EnumerateConditions<T>(IEnumerable<T>? conditions)
    {
        if (conditions is null)
        {
            yield break;
        }

        foreach (var condition in conditions)
        {
            yield return condition;
        }
    }

    private static void LogDebug(string message, Exception exception)
    {
        ProgressionJournal.Instance?.Logger.Debug($"{message}{Environment.NewLine}{exception}");
    }
}
