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
    private static readonly FieldInfo? GlobalNpcDropRulesField = typeof(ItemDropDatabase).GetField(
        "_globalEntries",
        BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly Dictionary<(string ProfileId, int ItemId), JournalItemAcquisitionInfo> Cache = new();
    private static readonly Dictionary<int, Item?> StationItemCache = new();
    private static readonly Dictionary<int, bool> RevengeanceExclusiveCache = new();
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

        info = new JournalItemAcquisitionInfo(
            itemId,
            BuildRecipes(itemId),
            BuildDrops(itemId),
            BuildShops(itemId),
            FindProfileFishingSources(itemId));
        Cache[cacheKey] = info;
        return info;
    }

    private static IEnumerable<JournalFishingSource> FindProfileFishingSources(int itemId)
    {
        if (!JournalProfileRegistry.IsLoaded)
        {
            return [];
        }

        var profile = JournalProfileRegistry.Active;
        return profile.Entries
            .Where(entry => entry.ItemIds.Contains(itemId))
            .SelectMany(static entry => entry.FishingSources)
            .Concat(profile.CombatBuffEntries
                .Where(entry => entry.ItemGroups.Any(group => group.ItemIds.Contains(itemId)))
                .SelectMany(static entry => entry.FishingSources));
    }

    private static List<JournalRecipeSource> BuildRecipes(int itemId)
    {
        var recipes = new List<JournalRecipeSource>();

        for (var recipeIndex = 0; recipeIndex < Recipe.numRecipes; recipeIndex++)
        {
            var recipe = Main.recipe[recipeIndex];
            if (recipe is null || recipe.Disabled || recipe.createItem.type != itemId)
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

            recipes.Add(new JournalRecipeSource(ingredients, stations, conditions));
        }

        return recipes;
    }

    private static JournalDropSource[] BuildDrops(int itemId)
    {
        var drops = new List<JournalDropSource>();

        for (var npcType = 0; npcType < NPCLoader.NPCCount; npcType++)
        {
            AppendDropSources(
                drops,
                Main.ItemDropsDB.GetRulesForNPCID(npcType, includeGlobalDrops: false),
                itemId,
                GetNpcName(npcType),
                sourceNpcType: npcType,
                sourceItemId: null);
        }

        AppendDropSources(
            drops,
            GlobalNpcDropRulesField?.GetValue(Main.ItemDropsDB) as List<IItemDropRule>,
            itemId,
            Language.GetTextValue("Mods.ProgressionJournal.UI.SelectedItemFromAnyEnemy"),
            sourceNpcType: null,
            sourceItemId: null);

        for (var sourceItemType = 1; sourceItemType < ItemLoader.ItemCount; sourceItemType++)
        {
            if (sourceItemType == itemId)
            {
                continue;
            }

            AppendDropSources(
                drops,
                Main.ItemDropsDB.GetRulesForItemID(sourceItemType),
                itemId,
                Lang.GetItemNameValue(sourceItemType),
                sourceNpcType: null,
                sourceItemId: sourceItemType);
        }

        AppendLegacyDirectNpcSources(drops, itemId);
        AppendLegacyDirectItemSources(drops, itemId);
        AppendExactSources(drops, itemId);
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

    public static void ClearCache()
    {
        Cache.Clear();
    }

    private static void AppendLegacyDirectNpcSources(List<JournalDropSource> drops, int targetItemId)
    {
        drops.AddRange(JournalLegacyDirectDropAnalyzer.GetNpcDrops(targetItemId)
            .Select(source => new JournalDropSource(
                GetNpcName(source.SourceNpcType),
                source.SourceNpcType,
                sourceItemId: null,
                source.DropRate,
                source.StackMin,
                source.StackMax,
                JournalStaticNpcSpawnConditionResolver.GetConditions(source.SourceNpcType))));
    }

    private static void AppendLegacyDirectItemSources(List<JournalDropSource> drops, int targetItemId)
    {
        drops.AddRange(JournalLegacyDirectDropAnalyzer.GetItemDrops(targetItemId)
            .Select(source => new JournalDropSource(
                Lang.GetItemNameValue(source.SourceItemId),
                sourceNpcType: null,
                source.SourceItemId,
                source.DropRate,
                source.StackMin,
                source.StackMax,
                [])));
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

    private static void AppendExactSources(List<JournalDropSource> drops, int targetItemId)
    {
        drops.AddRange(JournalExactDropCatalog.GetSources(targetItemId)
            .Select(source => new JournalDropSource(
                source.SourceName,
                source.SourceNpcType,
                source.SourceItemId,
                source.DropRate,
                source.StackMin,
                source.StackMax,
                AppendNpcSpawnConditions(
                    source.SourceNpcType,
                    source.Conditions.Select(static condition => condition.Description)))));
    }

    private static JournalShopSource[] BuildShops(int itemId)
    {
        var sources = NPCShopDatabase.AllShops
            .SelectMany(
                static shop => shop.ActiveEntries.Select(entry => new { shop, entry }))
            .Where(pair => pair.entry.Item is not null && !pair.entry.Item.IsAir && pair.entry.Item.type == itemId)
            .Select(pair => new JournalShopSource(
                pair.shop.NpcType,
                GetNpcName(pair.shop.NpcType),
                EnumerateConditions(pair.entry.Conditions)
                    .Select(GetConditionDescription)))
            .Concat(JournalExactShopCatalog.GetSources(itemId)
                .Select(static source => new JournalShopSource(
                    source.NpcType,
                    source.NpcName,
                    source.Conditions.Select(static condition => condition.Description))));
        return sources
            .GroupBy(static shop => new
            {
                shop.NpcType,
                shop.NpcName,
                Conditions = string.Join('\n', shop.Conditions)
            })
            .Select(static group => group.First())
            .OrderBy(static shop => shop.NpcName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static void AppendDropSources(
        List<JournalDropSource> drops,
        List<IItemDropRule>? rules,
        int targetItemId,
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
                    $"Failed to inspect drop rates for '{sourceName}' while resolving item {targetItemId}.",
                    exception);
            }
        }

        drops.AddRange(reportedDrops
            .Where(drop => drop.itemId == targetItemId)
            .Select(drop => new JournalDropSource(
                sourceName,
                sourceNpcType,
                sourceItemId,
                drop.dropRate,
                drop.stackMin,
                drop.stackMax,
                AppendNpcSpawnConditions(
                    sourceNpcType,
                    EnumerateConditions(drop.conditions)
                        .Select(condition => GetDropConditionDescription(condition, targetItemId))))));
    }

    private static IEnumerable<string> AppendNpcSpawnConditions(
        int? sourceNpcType,
        IEnumerable<string> dropConditions)
    {
        return sourceNpcType is { } npcType
            ? dropConditions.Concat(JournalStaticNpcSpawnConditionResolver.GetConditions(npcType))
            : dropConditions;
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
