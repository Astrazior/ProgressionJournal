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
    private static readonly Dictionary<Type, MemberInfo?> LocalizedTextMemberCache = new();
    private static readonly Dictionary<string, string> ConditionTypeLocalizationKeys =
        new(StringComparer.Ordinal)
        {
            ["AAModClassic.Utilities.AbstractsLikeDigitalCircus.Items.AAConditions+GoblinsDefated"] = "AAModClassic.GoblinsDefeated",
            ["AAModClassic.Utilities.AbstractsLikeDigitalCircus.Items.AAConditions+GolemDefated"] = "AAModClassic.GolemDefeated",
            ["AAModClassic.Utilities.AbstractsLikeDigitalCircus.Items.AAConditions+OneMechDefated"] = "AAModClassic.OneMechDefeated",
            ["AAModClassic.Utilities.AbstractsLikeDigitalCircus.Items.AAConditions+PostLateAncientsAndRemovedWorld"] = "AAModClassic.PostLateAncients",
            ["AAModClassic.Utilities.AbstractsLikeDigitalCircus.Items.AAConditions+PostLateAncientsAndRemovedWorldAndExpert"] = "AAModClassic.PostLateAncients",
            ["AAModClassic.Utilities.AbstractsLikeDigitalCircus.Items.AAConditions+PostLateAncientsAndRemovedWorldAndNotExpert"] = "AAModClassic.PostLateAncients",
            ["AAModClassic.Utilities.AbstractsLikeDigitalCircus.Items.AAConditions+RevOrMaster"] = "AAModClassic.MasterMode",
            ["AAModClassic.Utilities.AbstractsLikeDigitalCircus.Items.AAConditions+SkeletronDefated"] = "AAModClassic.SkeletronDefeated",
            ["AAModClassic.Utilities.MasterRevDropRule"] = "AAModClassic.MasterModeDropRate",
            ["AAModClassic._Content.Chaos._PostMoonlord.NPCs.__BossSistersOfDiscord.Ashe.Ashe+MissingSisterInMaster"] = "AAModClassic.MasterMode",
            ["AAModClassic._Content.Chaos.___PreHardmode.NPCs.__BossGripsOfChaos.GripOfChaosAbstract+MissingGripMaster"] = "AAModClassic.MasterMode",
            ["AAModClassic._Content.Snow.___PreHardmode.NPCs._Night._SnowSerpent.SnowSerpentHead+SpawnedBySubzeroSerpent"] = "AAModClassic.SubzeroSerpentNotAlive",
            ["AAModClassic._Content.Stars._PostMoonlord.Items._BossEquinoxWorms.BossStandard.EquinoxWormsTreasureBag+Daytime"] = "AAModClassic.OpenedDuringDay",
            ["AAModClassic._Content.Stars._PostMoonlord.Items._BossEquinoxWorms.BossStandard.EquinoxWormsTreasureBag+Nighttime"] = "AAModClassic.OpenedDuringNight",
            ["AAModClassic._Content.Stars._PostMoonlord.NPCs._Day.SunWatcher+EquinoxWormsDefeated"] = "AAModClassic.EquinoxWormsDefeated",
            ["AAModClassic._Content.Stars._PostMoonlord.NPCs.__BossEquinoxWorms.Daybringer.DaybringerHead+LastWormInMaster"] = "AAModClassic.MasterMode",
            ["CalamityMod.DropHelper+LambdaDropRuleCondition2"] = "CalamityMod.ProvidenceChallenge",
            ["FargowiltasSouls.Core.Globals.FirstKillCondition"] = "FargowiltasSouls.FirstKill",
            ["FargowiltasSouls.Core.ItemDropRules.Conditions.DownedEvilBossDropCondition"] = "FargowiltasSouls.DownedEvilBoss",
            ["FargowiltasSouls.Core.ItemDropRules.Conditions.EModeDropCondition"] = "FargowiltasSouls.EMode",
            ["FargowiltasSouls.Core.ItemDropRules.Conditions.EModeEarlyBirdLockDropCondition"] = "FargowiltasSouls.EModeEarlyBirdHM",
            ["FargowiltasSouls.Core.ItemDropRules.Conditions.EModeEarlyBirdRewardDropCondition"] = "FargowiltasSouls.EModeEarlyBirdPHM",
            ["FargowiltasSouls.Core.ItemDropRules.Conditions.EModeNotMasterDropCondition"] = "FargowiltasSouls.EModeNotMaster",
            ["FargowiltasSouls.Core.ItemDropRules.Conditions.NotEModeDropCondition"] = "FargowiltasSouls.NotEMode",
            ["FargowiltasSouls.Core.ItemDropRules.Conditions.PatreonPlantDropCondition"] = "FargowiltasSouls.PatreonPlant",
            ["FargowiltasSouls.Core.ItemDropRules.Conditions.TimsConcoctionDropCondition"] = "FargowiltasSouls.TimsConcoction",
            ["ThoriumMod.Core.ItemDropRules.DropConditions.AbyssalWhistleCondition"] = "ThoriumMod.Underworld",
            ["ThoriumMod.Core.ItemDropRules.DropConditions.AquaticDepthsKeyCondition"] = "ThoriumMod.AquaticDepthsHardmode",
            ["ThoriumMod.Core.ItemDropRules.DropConditions.CookIsNotSellingEtherianGrogCondition"] = "ThoriumMod.CookNotSellingEtherianGrog",
            ["ThoriumMod.Core.ItemDropRules.DropConditions.DownedFallenBeholderCondition"] = "ThoriumMod.DownedFallenBeholder",
            ["ThoriumMod.Core.ItemDropRules.DropConditions.DownedSkeletronCondition"] = "ThoriumMod.DownedSkeletron",
            ["ThoriumMod.Core.ItemDropRules.DropConditions.PharaohsBreathCondition"] = "ThoriumMod.Desert",
            ["ThoriumMod.Core.ItemDropRules.DropConditions.SoulofPlightCondition"] = "ThoriumMod.HardmodeUnderworld",
            ["ThoriumMod.Core.ItemDropRules.DropConditions.UnderworldKeyCondition"] = "ThoriumMod.HardmodeUnderworld",
            ["ThoriumMod.Core.ItemDropRules.DropConditions.UnholyShardsCondition"] = "ThoriumMod.BloodMoon"
        };
    private static readonly Dictionary<(string TypeName, string Description), string>
        KeylessConditionLocalizationKeys = new()
        {
            [("CalamityMod.DropHelper+LambdaDropRuleCondition", "After defeating both Draedon and Calamitas")] =
                "ExternalConditions.CalamityMod.Condition.Drops.Cynosure",
            [("CalamityMod.DropHelper+LambdaDropRuleCondition", "Drops if Providence has been enraged")] =
                "ExternalConditions.CalamityMod.Condition.Drops.ProvidenceEnraged",
            [("CalamityMod.DropHelper+LambdaDropRuleCondition", "Drops if defeated after Cataclysmic Construct")] =
                "ExternalConditions.CalamityMod.Condition.Drops.CatastropheKilledLast",
            [("CalamityMod.DropHelper+LambdaDropRuleCondition", "Drops if defeated after Catastrophic Construct")] =
                "ExternalConditions.CalamityMod.Condition.Drops.CataclysmKilledLast",
            [("CalamityMod.DropHelper+LambdaDropRuleCondition", "Drops on the first kill of the final Mechanical Boss")] =
                "ExternalConditions.CalamityMod.Condition.Drops.MechBoss",
            [("CalamityMod.DropHelper+LambdaDropRuleCondition", "Drops only on the first kill")] =
                "ExternalConditions.CalamityMod.Condition.Drops.FirstKill",
            [("FargowiltasSouls.Core.ItemDropRules.Conditions.RuntimeDropCondition",
                "[i:FargowiltasSouls/RoombaPet] Patreon Drop")] =
                "ExternalConditionTypes.FargowiltasSouls.Patreon",
            [("FargowiltasSouls.Core.ItemDropRules.Conditions.RuntimeDropCondition",
                "[i:FargowiltasSouls/RoombaPet] Patreon Drop in Eternity Mode")] =
                "ExternalConditionTypes.FargowiltasSouls.PatreonEMode"
        };
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
            BuildShimmerSources(itemId),
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

    private static JournalShimmerSource[] BuildShimmerSources(int itemId)
    {
        var inputItems = Enumerable.Range(1, ItemLoader.ItemCount - 1)
            .Where(inputItemId => ItemID.Sets.ShimmerTransformToItem[inputItemId] == itemId)
            .Select(inputItemId => ContentSamples.ItemsByType[inputItemId])
            .Where(static item => item is not null && !item.IsAir)
            .ToArray();
        return inputItems.Length == 0 ? [] : [new JournalShimmerSource(inputItems)];
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
            source.ConditionLocalizationKeys.Select(static key => Language.GetTextValue(key)))));
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
            return Language.GetTextValue(
                "Mods.ProgressionJournal.ExternalConditionTypes.CalamityMod.Revengeance");
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
        if (condition is null)
        {
            return string.Empty;
        }

        var conditionType = condition.GetType();
        if (conditionType.FullName is { } conditionTypeName
            && ConditionTypeLocalizationKeys.TryGetValue(conditionTypeName, out var conditionLocalizationKey))
        {
            return Language.GetTextValue(
                $"Mods.ProgressionJournal.ExternalConditionTypes.{conditionLocalizationKey}");
        }

        var descriptionProperty = conditionType.GetProperty(
            "Description",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (descriptionProperty?.GetValue(condition) is { } descriptionValue)
        {
            if (descriptionValue is LocalizedText localizedText)
            {
                return ResolveConditionDescription(localizedText);
            }

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

        if (GetLocalizedTextMember(conditionType)?.GetValue(condition) is LocalizedText embeddedLocalizedText)
        {
            return ResolveConditionDescription(embeddedLocalizedText);
        }

        if (condition is IProvideItemConditionDescription itemConditionDescription)
        {
            return ResolveKeylessConditionDescription(
                conditionType.FullName,
                itemConditionDescription.GetConditionDescription());
        }

        var getterMethod = conditionType.GetMethod(
            "GetConditionDescription",
            BindingFlags.Public | BindingFlags.Instance);
        if (getterMethod?.Invoke(condition, null) is string reflectedDescription)
        {
            return ResolveKeylessConditionDescription(conditionType.FullName, reflectedDescription);
        }

        return string.Empty;
    }

    private static string ResolveConditionDescription(LocalizedText description)
    {
        const string modKeyPrefix = "Mods.";
        if (description.Key.StartsWith(modKeyPrefix, StringComparison.Ordinal))
        {
            var overrideKey =
                $"Mods.ProgressionJournal.ExternalConditions.{description.Key[modKeyPrefix.Length..]}";
            if (Language.Exists(overrideKey))
            {
                return Language.GetTextValue(overrideKey);
            }
        }

        return description.Value;
    }

    private static string ResolveKeylessConditionDescription(string? typeName, string description)
    {
        return typeName is not null
               && KeylessConditionLocalizationKeys.TryGetValue((typeName, description), out var localizationKey)
            ? Language.GetTextValue($"Mods.ProgressionJournal.{localizationKey}")
            : description;
    }

    private static MemberInfo? GetLocalizedTextMember(Type conditionType)
    {
        if (LocalizedTextMemberCache.TryGetValue(conditionType, out var cached))
        {
            return cached;
        }

        var member = conditionType
                         .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                         .FirstOrDefault(static property =>
                             property.PropertyType == typeof(LocalizedText)
                             && property.GetIndexParameters().Length == 0)
                     ?? (MemberInfo?)conditionType
                         .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                         .FirstOrDefault(static field => field.FieldType == typeof(LocalizedText));
        LocalizedTextMemberCache[conditionType] = member;
        return member;
    }

    private static object? GetValue(this MemberInfo member, object instance)
    {
        return member switch
        {
            PropertyInfo property => property.GetValue(instance),
            FieldInfo field => field.GetValue(instance),
            _ => null
        };
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
