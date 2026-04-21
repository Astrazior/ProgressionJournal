using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ProgressionJournal.Data.Resolvers;

public static class JournalItemSourceResolver
{
    private static readonly Dictionary<int, JournalItemAcquisitionInfo> Cache = new();
    private static readonly Dictionary<int, Item?> StationItemCache = new();

    public static JournalItemAcquisitionInfo GetInfo(int itemId)
    {
        if (Cache.TryGetValue(itemId, out var info))
        {
            return info;
        }

        info = new JournalItemAcquisitionInfo(
            itemId,
            BuildRecipes(itemId),
            BuildDrops(itemId),
            BuildShops(itemId));
        Cache[itemId] = info;
        return info;
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
                .SelectMany(ResolveStationItems)
                .GroupBy(static station => station.type)
                .Select(static group => group.First().Clone())
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
                Main.ItemDropsDB.GetRulesForNPCID(npcType),
                itemId,
                GetNpcName(npcType),
                sourceNpcType: npcType,
                sourceItemId: null);
        }

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

    private static JournalShopSource[] BuildShops(int itemId)
    {
        return NPCShopDatabase.AllShops
            .SelectMany(
                static shop => shop.ActiveEntries.Select(entry => new { shop, entry }))
            .Where(pair => pair.entry.Item is not null && !pair.entry.Item.IsAir && pair.entry.Item.type == itemId)
            .Select(pair => new JournalShopSource(
                pair.shop.NpcType,
                GetNpcName(pair.shop.NpcType),
                pair.shop.Name,
                EnumerateConditions(pair.entry.Conditions).Select(GetConditionDescription)))
            .GroupBy(static shop => new
            {
                shop.NpcType,
                shop.NpcName,
                shop.ShopName,
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
            catch
            {
                // Skip malformed rules from other content sources instead of breaking the journal UI.
            }
        }

        // LoopCanBeConvertedToQuery
        foreach (var drop in reportedDrops)
        {
            if (drop.itemId != targetItemId)
            {
                continue;
            }

            drops.Add(new JournalDropSource(
                sourceName,
                sourceNpcType,
                sourceItemId,
                drop.dropRate,
                drop.stackMin,
                drop.stackMax,
                EnumerateConditions(drop.conditions).Select(GetConditionDescription)));
        }
    }

    private static Item? ResolveStationItem(int tileId)
    {
        if (StationItemCache.TryGetValue(tileId, out var cached))
        {
            return cached?.Clone();
        }

        var requiredStyle = Recipe.GetRequiredTileStyle(tileId);
        Item? exactMatch = null;
        Item? fallbackMatch = null;

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var sample in ContentSamples.ItemsByType.Values)
        {
            if (sample is null || sample.IsAir || sample.createTile != tileId)
            {
                continue;
            }

            if (sample.placeStyle == requiredStyle)
            {
                exactMatch = sample.Clone();
                break;
            }

            fallbackMatch ??= sample.Clone();
        }

        var resolved = exactMatch ?? fallbackMatch;
        StationItemCache[tileId] = resolved?.Clone();
        return resolved;
    }

    private static IEnumerable<Item> ResolveStationItems(int tileId)
    {
        if (tileId == TileID.Bottles)
        {
            yield return JournalItemUtilities.CreateItem(ItemID.Bottle);
            yield return JournalItemUtilities.CreateItem(ItemID.AlchemyTable);
            yield break;
        }

        var stationItem = ResolveStationItem(tileId);
        if (stationItem is not null)
        {
            yield return stationItem;
        }
    }

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
}
