using System.Reflection;
using System.Text.Json;
using ProgressionJournal.Data.Snapshots.Collectors;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace ProgressionJournal.Commands;

public sealed class ExportProgressionSnapshotCommand : ModCommand
{
    private static readonly MethodInfo? PlayerLoaderSetupPlayerMethod = typeof(PlayerLoader).GetMethod(
        "SetupPlayer",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    public override CommandType Type => CommandType.Chat;

    public override string Command => "pjexport";

    public override string Usage => "/pjexport <InternalModName>";

    public override string Description => "Exports loaded content for the Progression Journal profile generator.";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (args.Length != 1)
        {
            caller.Reply($"Usage: {Usage}");
            return;
        }

        var targetModName = args[0].Trim();
        if (!ModLoader.TryGetMod(targetModName, out var targetMod))
        {
            caller.Reply($"Loaded mod '{targetModName}' was not found. Use its internal mod name.");
            return;
        }

        var matchingProfiles = JournalProfileRegistry.All
            .Where(profile => profile.Document.RequiredMods.Any(requirement =>
                string.Equals(requirement.Name, targetMod.Name, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        if (matchingProfiles.Length != 1)
        {
            caller.Reply(
                matchingProfiles.Length == 0
                    ? $"No loaded Progression Journal profile targets '{targetMod.Name}'."
                    : $"More than one loaded profile targets '{targetMod.Name}': "
                      + string.Join(", ", matchingProfiles.Select(static profile => profile.Id)));
            return;
        }

        var targetProfile = matchingProfiles[0];
        using var profileScope = JournalRuntimeProgressionScenarios.UseProfile(targetProfile);
        var dependencyMods = ResolveTransitiveDependencies(targetMod);
        var contentMods = dependencyMods
            .Select(static mod => mod.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var includedMods = new HashSet<string>(contentMods, StringComparer.OrdinalIgnoreCase)
        {
            "Terraria"
        };

        var itemIds = Enumerable.Range(1, ItemLoader.ItemCount - 1)
            .Where(itemId => includedMods.Contains(GetItemModName(itemId)))
            .ToHashSet();
        var npcIds = Enumerable.Range(1, NPCLoader.NPCCount - 1)
            .Where(npcId => includedMods.Contains(GetNpcModName(npcId)))
            .ToHashSet();

        var npcAvailability = JournalSnapshotNpcAvailabilityCollector.Collect(npcIds, GetNpcReference);
        var npcSpawnProbe = JournalNpcSpawnAvailabilityResolver.GetDiagnostics();
        var items = itemIds.Select(CreateItem).ToList();
        var npcs = npcIds.Select(CreateNpc).ToList();
        var recipes = CreateRecipes(itemIds, includedMods);
        List<SnapshotDrop> drops = [];
        drops.AddRange(JournalSnapshotNpcDropCollector.Collect(
            itemIds,
            npcIds,
            GetItemReference,
            GetNpcReference,
            CreateCondition,
            LogDebug));
        drops.AddRange(JournalSnapshotItemContainerCollector.Collect(
            itemIds,
            GetItemReference,
            CreateCondition,
            LogDebug));
        drops.AddRange(JournalSnapshotWorldContainerCollector.Collect(itemIds));
        var snapshot = new ProgressionSnapshot
        {
            GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
            TargetMod = targetMod.Name,
            ProfileId = targetProfile.Id,
            ContentMods = contentMods
                .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Mods = dependencyMods
                .Select(static mod => new SnapshotMod(mod.Name, mod.Version.ToString()))
                .OrderBy(static mod => mod.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            EnvironmentMods = ModLoader.Mods
                .Select(static mod => new SnapshotMod(mod.Name, mod.Version.ToString()))
                .OrderBy(static mod => mod.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Items = items,
            Npcs = npcs,
            Recipes = recipes,
            Drops = drops,
            Shops = JournalSnapshotShopCollector.Collect(
                itemIds,
                npcIds,
                GetItemReference,
                GetNpcReference,
                CreateCondition),
            Fishing = JournalSnapshotFishingCollector.Collect(
                itemIds,
                npcIds,
                GetItemReference,
                GetNpcReference),
            NpcAvailability = npcAvailability,
            NpcSpawnProbe = new SnapshotNpcSpawnProbe(
                npcAvailability.Count(static record => record is { Kind: "spawn", Observed: true }),
                npcAvailability.Count(static record => record.Kind == "spawn"),
                npcSpawnProbe.ObservedNpcCount,
                npcSpawnProbe.CandidateNpcCount,
                npcSpawnProbe.ModNpcTemplateCount,
                npcSpawnProbe.ContextCount,
                npcSpawnProbe.SpawnRateBlockedContextCount,
                npcSpawnProbe.PositiveSpawnChanceCount,
                npcSpawnProbe.ChosenSpawnCount,
                npcSpawnProbe.FullSpawnCount,
                npcSpawnProbe.FullSpawnContextCount,
                npcSpawnProbe.FullSpawnAttemptCount,
                npcSpawnProbe.FullSpawnSuccessfulAttemptCount,
                npcSpawnProbe.FullSpawnedNpcInstanceCount,
                npcSpawnProbe.FullSpawnContextDetails
                    .Select(detail => new SnapshotNpcFullSpawnContext(
                        detail.StageIndex,
                        detail.Environment,
                        detail.Depth,
                        detail.Event,
                        detail.Water,
                        detail.PlayerSafe,
                        detail.PlayerInTown,
                        detail.Attempts,
                        detail.SuccessfulAttempts,
                        detail.SpawnedNpcInstances,
                        detail.NearbyActiveNpcs,
                        detail.TownNpcs,
                        detail.MinimumSpawnRate,
                        detail.MaximumSpawnRate,
                        detail.MinimumMaxSpawns,
                        detail.MaximumMaxSpawns,
                        detail.JourneyMode,
                        detail.JourneySpawnsDisabled,
                        detail.JourneySpawnRateMultiplier,
                        detail.SpawnRateHookTrace.ToList(),
                        detail.SpawnedNpcTypes.Select(GetNpcReference).ToList()))
                    .ToList(),
                npcSpawnProbe.Failures.ToList()),
            VanillaItemClassifications = CreateVanillaItemClassifications()
        };

        var sourceFolder = ProgressionJournal.Instance?.SourceFolder;
        var developmentExport = !string.IsNullOrWhiteSpace(sourceFolder)
            && Directory.Exists(sourceFolder);
        var directory = developmentExport
            ? Path.Combine(sourceFolder!, "Profiles", "Mods", targetMod.Name)
            : Path.Combine(
                Main.SavePath,
                "Mods",
                nameof(ProgressionJournal),
                "Snapshots",
                targetMod.Name);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "snapshot.json");
        WriteAtomically(path, JsonSerializer.Serialize(snapshot, SnapshotJsonOptions));
        CreateSupportTemplateIfMissing(directory, targetMod, dependencyMods);
        caller.Reply(
            $"Progression Journal snapshot exported: {path}. "
            + $"Profile: {snapshot.ProfileId}. "
            + $"Content mods: {string.Join(", ", snapshot.ContentMods)}. "
            + $"Ordinary NPCs observed: {snapshot.NpcSpawnProbe.Observed}/{snapshot.NpcSpawnProbe.Total}. "
            + $"Raw: {snapshot.NpcSpawnProbe.RawObserved}; candidates: {snapshot.NpcSpawnProbe.Candidates}; "
            + $"contexts/rate-blocked: {snapshot.NpcSpawnProbe.Contexts}/{snapshot.NpcSpawnProbe.RateBlocked}; "
            + $"positive/chosen/full: {snapshot.NpcSpawnProbe.PositiveSpawnChance}/"
            + $"{snapshot.NpcSpawnProbe.ChosenSpawn}/{snapshot.NpcSpawnProbe.FullSpawn}. "
            + $"Probe failures: {snapshot.NpcSpawnProbe.Failures.Count}.");
    }

    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static Mod[] ResolveTransitiveDependencies(Mod targetMod)
    {
        var loadedByAssembly = ModLoader.Mods
            .Where(static mod => mod.Code is not null)
            .GroupBy(static mod => mod.Code.GetName().Name ?? mod.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, Mod>(StringComparer.OrdinalIgnoreCase);
        var pending = new Queue<Mod>();
        pending.Enqueue(targetMod);
        while (pending.TryDequeue(out var mod))
        {
            if (!result.TryAdd(mod.Name, mod) || mod.Code is null)
            {
                continue;
            }

            foreach (var reference in mod.Code.GetReferencedAssemblies())
            {
                if (reference.Name is not null
                    && loadedByAssembly.TryGetValue(reference.Name, out var dependency)
                    && !result.ContainsKey(dependency.Name))
                {
                    pending.Enqueue(dependency);
                }
            }
        }

        return result.Values
            .Where(static mod => !mod.Name.Equals("ModLoader", StringComparison.OrdinalIgnoreCase))
            .OrderBy(static mod => mod.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void WriteAtomically(string path, string contents)
    {
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, contents);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void CreateSupportTemplateIfMissing(
        string directory,
        Mod targetMod,
        Mod[] dependencyMods)
    {
        var path = Path.Combine(directory, "support.json");
        if (File.Exists(path))
        {
            return;
        }

        var template = new
        {
            format = "ProgressionJournalModSupport",
            version = 1,
            targetMod = targetMod.Name,
            id = $"mod.{targetMod.Name.ToLowerInvariant()}",
            name = new Dictionary<string, string>
            {
                ["en-US"] = $"{targetMod.DisplayName} progression",
                ["ru-RU"] = $"Прогрессия {targetMod.DisplayName}"
            },
            profileVersion = targetMod.Version.ToString(),
            requiredMods = dependencyMods.Select(static mod => new SnapshotMod(
                mod.Name,
                mod.Version.ToString())),
            contentMods = dependencyMods.Select(static mod => mod.Name).ToArray(),
            classes = Array.Empty<object>(),
            stages = Array.Empty<object>()
        };
        WriteAtomically(path, JsonSerializer.Serialize(template, SnapshotJsonOptions));
    }

    private static SnapshotItem CreateItem(int itemId)
    {
        var item = ContentSamples.ItemsByType[itemId];
        return new SnapshotItem(
            GetItemReference(itemId),
            item.HoverName,
            GetEnglishItemName(itemId),
            item.DamageType?.FullName ?? string.Empty,
            item.damage,
            item.defense,
            item.headSlot,
            item.bodySlot,
            item.legSlot,
            item.accessory,
            item.vanity,
            item.ammo,
            item.useAmmo,
            item.buffType,
            item.buffTime,
            item.consumable,
            item.potion,
            item.healLife,
            item.healMana,
            ItemID.Sets.IsFood[itemId],
            item.buffType > 0 && BuffID.Sets.IsAFlaskBuff[item.buffType],
            item.maxStack,
            item.createTile,
            GetPlacedTileReference(item.createTile),
            item.createWall,
            item.pick,
            item.axe,
            item.hammer,
            item.mountType,
            item.shoot,
            item.sentry,
            item.ModItem?.GetType().Namespace ?? string.Empty,
            GetClassEffects(item));
    }

    private static string GetEnglishItemName(int itemId)
    {
        if (ItemLoader.GetItem(itemId) is not null)
        {
            return string.Empty;
        }

        var internalName = ItemID.Search.GetName(itemId);
        return internalName is not null
            && EnglishVanillaItemNames.Value.TryGetValue(internalName, out var name)
            ? name
            : string.Empty;
    }

    private static readonly Lazy<Dictionary<string, string>> EnglishVanillaItemNames =
        new(LoadEnglishVanillaItemNames);

    private static Dictionary<string, string> LoadEnglishVanillaItemNames()
    {
        const string resourceName = "Terraria.Localization.Content.en_US.Items.json";
        using var stream = typeof(Main).Assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return new Dictionary<string, string>();
        }

        using var document = JsonDocument.Parse(stream, new JsonDocumentOptions
        {
            AllowTrailingCommas = true
        });
        if (!document.RootElement.TryGetProperty("ItemName", out var itemNames))
        {
            return new Dictionary<string, string>();
        }

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        // ReSharper disable once LoopCanBePartlyConvertedToQuery
        foreach (var property in itemNames.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            result[property.Name] = property.Value.GetString() ?? string.Empty;
        }

        return result;
    }

    private static List<SnapshotClassEffect> GetClassEffects(Item sourceItem)
    {
        if (sourceItem is { accessory: false, headSlot: < 0, bodySlot: < 0, legSlot: < 0 })
        {
            return [];
        }

        try
        {
            var baseline = CreateEffectProbePlayer();
            var equipped = CreateEffectProbePlayer();
            var item = sourceItem.Clone();
            var previousRandom = Main.rand;

            try
            {
                RunEffectProbe(baseline, item: null);
                RunEffectProbe(equipped, item);
            }
            finally
            {
                Main.rand = previousRandom;
            }

            return EnumerateDamageClasses()
                .Select(damageClass => CreateClassEffect(damageClass, baseline, equipped))
                .OfType<SnapshotClassEffect>()
                .ToList();
        }
        catch (Exception exception)
        {
            LogDebug($"Failed to inspect class effects for item {sourceItem.type}.", exception);
            return [];
        }
    }

    private static SnapshotClassEffect? CreateClassEffect(DamageClass damageClass, Player baseline, Player equipped)
    {
        var damage = !StatModifierEquals(
            baseline.GetDamage(damageClass),
            equipped.GetDamage(damageClass));
        var crit = !NearlyEqual(
            baseline.GetCritChance(damageClass),
            equipped.GetCritChance(damageClass));
        var attackSpeed = !NearlyEqual(
            baseline.GetAttackSpeed(damageClass),
            equipped.GetAttackSpeed(damageClass));
        var armorPenetration = !NearlyEqual(
            baseline.GetArmorPenetration(damageClass),
            equipped.GetArmorPenetration(damageClass));
        var knockback = !StatModifierEquals(
            baseline.GetKnockback(damageClass),
            equipped.GetKnockback(damageClass));

        return damage || crit || attackSpeed || armorPenetration || knockback
            ? new SnapshotClassEffect(
                damageClass.FullName,
                damage,
                crit,
                attackSpeed,
                armorPenetration,
                knockback)
            : null;
    }

    private static Player CreateEffectProbePlayer()
    {
        var player = new Player
        {
            whoAmI = Main.myPlayer,
            active = true,
            dead = false
        };
        PlayerLoaderSetupPlayerMethod?.Invoke(null, [player]);
        player.ResetEffects();
        return player;
    }

    private static void RunEffectProbe(Player player, Item? item)
    {
        var playerIndex = Main.myPlayer;
        var previousPlayer = Main.player[playerIndex];
        player.whoAmI = playerIndex;
        Main.player[playerIndex] = player;
        try
        {
            if (item is not null)
            {
                player.armor[GetProbeEquipmentSlot(item)] = item;
            }

            Main.rand = new UnifiedRandom(0);
            for (var tick = 0; tick < 2; tick++)
            {
                player.ResetEffects();
                player.UpdateEquips(playerIndex);
                PlayerLoader.PostUpdateEquips(player);
                PlayerLoader.PostUpdateMiscEffects(player);
            }

            player.active = true;
            player.dead = false;
        }
        finally
        {
            Main.player[playerIndex] = previousPlayer;
        }
    }

    private static int GetProbeEquipmentSlot(Item item) => item switch
    {
        { headSlot: >= 0 } => 0,
        { bodySlot: >= 0 } => 1,
        { legSlot: >= 0 } => 2,
        _ => 3
    };

    private static IEnumerable<DamageClass> EnumerateDamageClasses()
    {
        for (var type = 0; type < DamageClassLoader.DamageClassCount; type++)
        {
            var damageClass = DamageClassLoader.GetDamageClass(type);
            if (damageClass is null)
            {
                continue;
            }

            yield return damageClass;
        }
    }

    private static List<SnapshotVanillaItemClassification> CreateVanillaItemClassifications()
    {
        return JournalRepository.GetAllVanillaEntries()
            .SelectMany(entry => entry.ItemIds
                .Where(static itemId => itemId > 0 && itemId < ItemID.Count)
                .Select(itemId => new
                {
                    ItemId = itemId,
                    entry.Category,
                    Classes = entry.ClassIds
                }))
            .GroupBy(static value => value.ItemId)
            .Select(group => new SnapshotVanillaItemClassification(
                GetItemReference(group.Key),
                group.Select(static value => value.Category.ToString()).First(),
                group.SelectMany(static value => value.Classes)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .ToList()))
            .OrderBy(static value => value.Item, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool StatModifierEquals(StatModifier left, StatModifier right)
    {
        return NearlyEqual(left.Base, right.Base)
            && NearlyEqual(left.Additive, right.Additive)
            && NearlyEqual(left.Multiplicative, right.Multiplicative)
            && NearlyEqual(left.Flat, right.Flat);
    }

    private static bool NearlyEqual(float left, float right) => MathF.Abs(left - right) < 0.0001f;

    private static SnapshotNpc CreateNpc(int npcId)
    {
        var npc = ContentSamples.NpcsByNetId[npcId];
        var headSlot = npcId < NPCID.Sets.BossHeadTextures.Length
            ? NPCID.Sets.BossHeadTextures[npcId]
            : -1;
        return new SnapshotNpc(
            GetNpcReference(npcId),
            npc.FullName,
            npc.boss,
            headSlot);
    }

    private static List<SnapshotRecipe> CreateRecipes(
        HashSet<int> includedItems,
        HashSet<string> includedMods)
    {
        List<SnapshotRecipe> recipes = [];
        for (var recipeIndex = 0; recipeIndex < Recipe.numRecipes; recipeIndex++)
        {
            var recipe = Main.recipe[recipeIndex];
            if (recipe is null || recipe.Disabled || !includedItems.Contains(recipe.createItem.type))
            {
                continue;
            }

            var result = new SnapshotRecipe(
                GetItemReference(recipe.createItem.type),
                recipe.createItem.stack,
                recipe.requiredItem
                    .Where(static item => item is not null && !item.IsAir)
                    .Select(static item => new SnapshotStack(GetItemReference(item.type), item.stack))
                    .ToList(),
                recipe.requiredTile.Select(GetTileReference).ToList(),
                recipe.Conditions.Select(CreateCondition).ToList());
            if (result.Ingredients.All(stack => ReferenceIsIncluded(stack.Item, includedMods))
                && result.Stations.All(station => ReferenceIsIncluded(station, includedMods)))
            {
                recipes.Add(result);
            }
        }

        return recipes;
    }

    private static SnapshotCondition CreateCondition(object? condition)
    {
        if (condition is null)
        {
            return new SnapshotCondition(string.Empty, string.Empty);
        }

        var description = condition is IProvideItemConditionDescription provider
            ? provider.GetConditionDescription()
            : GetReflectedConditionDescription(condition);
        return new SnapshotCondition(condition.GetType().FullName ?? condition.GetType().Name, description);
    }

    private static string GetReflectedConditionDescription(object condition)
    {
        var property = condition.GetType().GetProperty("Description", BindingFlags.Public | BindingFlags.Instance);
        var value = property?.GetValue(condition);
        if (value is string text)
        {
            return text;
        }

        var localizedValue = value?.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(value) as string;
        return localizedValue ?? value?.ToString() ?? string.Empty;
    }

    private static IEnumerable<object?> EnumerateObjects<T>(IEnumerable<T>? values)
    {
        if (values is null)
        {
            yield break;
        }

        foreach (var value in values)
        {
            yield return value;
        }
    }

    private static string GetItemReference(int itemId)
    {
        var modItem = ItemLoader.GetItem(itemId);
        return modItem is null
            ? $"Terraria/{ItemID.Search.GetName(itemId)}"
            : $"{modItem.Mod.Name}/{modItem.Name}";
    }

    private static string GetNpcReference(int npcId)
    {
        var modNpc = NPCLoader.GetNPC(npcId);
        return modNpc is null
            ? $"Terraria/{NPCID.Search.GetName(npcId)}"
            : $"{modNpc.Mod.Name}/{modNpc.Name}";
    }

    private static string GetTileReference(int tileId)
    {
        var modTile = TileLoader.GetTile(tileId);
        return modTile is null
            ? $"Terraria/{TileID.Search.GetName(tileId)}"
            : $"{modTile.Mod.Name}/{modTile.Name}";
    }

    private static string GetPlacedTileReference(int tileId)
    {
        return tileId < 0 ? string.Empty : GetTileReference(tileId);
    }

    private static string GetItemModName(int itemId) => ItemLoader.GetItem(itemId)?.Mod.Name ?? "Terraria";

    private static string GetNpcModName(int npcId) => NPCLoader.GetNPC(npcId)?.Mod.Name ?? "Terraria";

    private static bool ReferenceIsIncluded(string reference, HashSet<string> includedMods)
    {
        var separator = reference.IndexOf('/');
        return separator > 0 && includedMods.Contains(reference[..separator]);
    }

    private static void LogDebug(string message, Exception exception)
    {
        ProgressionJournal.Instance?.Logger.Debug($"{message}{Environment.NewLine}{exception}");
    }

}

public sealed class ProgressionSnapshot
{
    public string Format { get; set; } = "ProgressionJournalSnapshot";
    public int Version { get; set; } = 4;
    public string GeneratedAtUtc { get; set; } = string.Empty;
    public string TargetMod { get; set; } = string.Empty;
    public string ProfileId { get; set; } = string.Empty;
    public List<string> ContentMods { get; init; } = [];
    public List<SnapshotMod> Mods { get; set; } = [];
    public List<SnapshotMod> EnvironmentMods { get; set; } = [];
    public List<SnapshotItem> Items { get; set; } = [];
    public List<SnapshotNpc> Npcs { get; set; } = [];
    public List<SnapshotRecipe> Recipes { get; set; } = [];
    public List<SnapshotDrop> Drops { get; set; } = [];
    public List<SnapshotShop> Shops { get; set; } = [];
    public List<SnapshotFishingCatch> Fishing { get; set; } = [];
    public List<SnapshotNpcAvailability> NpcAvailability { get; set; } = [];
    public SnapshotNpcSpawnProbe NpcSpawnProbe { get; set; } =
        new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, [], []);
    public List<SnapshotVanillaItemClassification> VanillaItemClassifications { get; set; } = [];
}

public sealed record SnapshotMod(string Name, string Version);
public sealed record SnapshotVanillaItemClassification(
    string Item,
    string Category,
    List<string> Classes);
public sealed record SnapshotItem(
    string Id,
    string Name,
    string EnglishName,
    string DamageClass,
    int Damage,
    int Defense,
    int HeadSlot,
    int BodySlot,
    int LegSlot,
    bool Accessory,
    bool Vanity,
    int Ammo,
    int UseAmmo,
    int BuffType,
    int BuffTime,
    bool Consumable,
    bool Potion,
    int HealLife,
    int HealMana,
    bool Food,
    bool Flask,
    int MaxStack,
    int CreateTile,
    string PlacedTile,
    int CreateWall,
    int Pick,
    int Axe,
    int Hammer,
    int MountType,
    int Shoot,
    bool Sentry,
    string SourceNamespace,
    List<SnapshotClassEffect> ClassEffects);
public sealed record SnapshotClassEffect(
    string DamageClass,
    bool Damage,
    bool Crit,
    bool AttackSpeed,
    bool ArmorPenetration,
    bool Knockback);
public sealed record SnapshotNpc(string Id, string Name, bool Boss, int BossHeadSlot);
public sealed record SnapshotStack(string Item, int Stack);
public sealed record SnapshotCondition(string Type, string Description);
public sealed record SnapshotRecipe(
    string Result,
    int ResultStack,
    List<SnapshotStack> Ingredients,
    List<string> Stations,
    List<SnapshotCondition> Conditions);
public sealed record SnapshotDrop(
    string SourceType,
    string Source,
    string Item,
    float Rate,
    int StackMin,
    int StackMax,
    List<SnapshotCondition> Conditions);
public sealed record SnapshotShop(
    string Npc,
    string Shop,
    string Item,
    List<SnapshotCondition> Conditions,
    bool Observed,
    int EarliestStageIndex,
    string EarliestStageName);
public sealed record SnapshotFishingCatch(
    string TargetType,
    string Target,
    int EarliestStageIndex,
    string EarliestStageName,
    List<string> Conditions);
public sealed record SnapshotNpcAvailability(
    string Npc,
    string Kind,
    bool Observed,
    int EarliestStageIndex,
    string EarliestStageName,
    List<string> Conditions,
    List<string> EventCategories);
public sealed record SnapshotNpcSpawnProbe(
    int Observed,
    int Total,
    int RawObserved,
    int Candidates,
    int ModNpcTemplates,
    int Contexts,
    int RateBlocked,
    int PositiveSpawnChance,
    int ChosenSpawn,
    int FullSpawn,
    int FullSpawnContexts,
    int FullSpawnAttempts,
    int FullSpawnSuccessfulAttempts,
    int FullSpawnedNpcInstances,
    List<SnapshotNpcFullSpawnContext> FullSpawnContextDetails,
    List<string> Failures);
public sealed record SnapshotNpcFullSpawnContext(
    int StageIndex,
    string Environment,
    int Depth,
    string Event,
    bool Water,
    bool PlayerSafe,
    bool PlayerInTown,
    int Attempts,
    int SuccessfulAttempts,
    int SpawnedNpcInstances,
    float NearbyActiveNpcs,
    float TownNpcs,
    int MinimumSpawnRate,
    int MaximumSpawnRate,
    int MinimumMaxSpawns,
    int MaximumMaxSpawns,
    bool JourneyMode,
    bool JourneySpawnsDisabled,
    float JourneySpawnRateMultiplier,
    List<string> SpawnRateHookTrace,
    List<string> SpawnedNpcs);
