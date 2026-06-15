using System.Reflection;
using System.Text.Json;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ProgressionJournal.Commands;

public sealed class ExportProgressionSnapshotCommand : ModCommand
{
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

        var snapshot = new ProgressionSnapshot
        {
            GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
            TargetMod = targetMod.Name,
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
            Items = itemIds.Select(CreateItem).ToList(),
            Npcs = npcIds.Select(CreateNpc).ToList(),
            Recipes = CreateRecipes(itemIds, includedMods),
            Drops = CreateDrops(itemIds, npcIds),
            Shops = CreateShops(itemIds, npcIds)
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
            + $"Content mods: {string.Join(", ", snapshot.ContentMods)}");
    }

    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static IReadOnlyList<Mod> ResolveTransitiveDependencies(Mod targetMod)
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
        IReadOnlyList<Mod> dependencyMods)
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

    private static readonly Lazy<IReadOnlyDictionary<string, string>> EnglishVanillaItemNames =
        new(LoadEnglishVanillaItemNames);

    private static IReadOnlyDictionary<string, string> LoadEnglishVanillaItemNames()
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

        return itemNames.EnumerateObject()
            .Where(static property => property.Value.ValueKind == JsonValueKind.String)
            .ToDictionary(
                static property => property.Name,
                static property => property.Value.GetString() ?? string.Empty,
                StringComparer.Ordinal);
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

            equipped.ApplyEquipFunctional(item, hideVisual: false);

            return EnumerateDamageClasses()
                .Select(damageClass => CreateClassEffect(damageClass, baseline, equipped))
                .OfType<SnapshotClassEffect>()
                .ToList();
        }
        catch
        {
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
        return new Player();
    }

    private static IEnumerable<DamageClass> EnumerateDamageClasses()
    {
        var emptyRun = 0;
        for (var type = 0; type < 4096 && emptyRun < 32; type++)
        {
            var damageClass = DamageClassLoader.GetDamageClass(type);
            if (damageClass is null)
            {
                emptyRun++;
                continue;
            }

            emptyRun = 0;
            yield return damageClass;
        }
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
        IReadOnlySet<int> includedItems,
        IReadOnlySet<string> includedMods)
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

    private static List<SnapshotDrop> CreateDrops(IReadOnlySet<int> includedItems, IReadOnlySet<int> includedNpcs)
    {
        List<SnapshotDrop> drops = [];
        foreach (var npcId in includedNpcs)
        {
            AppendDrops(
                drops,
                Main.ItemDropsDB.GetRulesForNPCID(npcId),
                "npc",
                GetNpcReference(npcId),
                includedItems);
        }

        foreach (var itemId in includedItems)
        {
            AppendDrops(
                drops,
                Main.ItemDropsDB.GetRulesForItemID(itemId),
                "container",
                GetItemReference(itemId),
                includedItems);
        }

        return drops;
    }

    private static void AppendDrops(
        ICollection<SnapshotDrop> result,
        List<IItemDropRule>? rules,
        string sourceType,
        string source,
        IReadOnlySet<int> includedItems)
    {
        if (rules is null)
        {
            return;
        }

        List<DropRateInfo> reported = [];
        foreach (var rule in rules)
        {
            try
            {
                rule.ReportDroprates(reported, new DropRateInfoChainFeed(1f));
            }
            catch
            {
                // A malformed third-party drop rule is reported by its absence, not allowed to abort the export.
            }
        }

        foreach (var drop in reported.Where(drop => includedItems.Contains(drop.itemId)))
        {
            result.Add(new SnapshotDrop(
                sourceType,
                source,
                GetItemReference(drop.itemId),
                drop.dropRate,
                drop.stackMin,
                drop.stackMax,
                EnumerateObjects(drop.conditions).Select(CreateCondition).ToList()));
        }
    }

    private static List<SnapshotShop> CreateShops(
        IReadOnlySet<int> includedItems,
        IReadOnlySet<int> includedNpcs)
    {
        return NPCShopDatabase.AllShops
            .SelectMany(static shop => shop.ActiveEntries.Select(entry => new { shop, entry }))
            .Where(pair => pair.entry.Item is not null
                && !pair.entry.Item.IsAir
                && includedNpcs.Contains(pair.shop.NpcType)
                && includedItems.Contains(pair.entry.Item.type))
            .Select(pair => new SnapshotShop(
                GetNpcReference(pair.shop.NpcType),
                pair.shop.Name,
                GetItemReference(pair.entry.Item.type),
                EnumerateObjects(pair.entry.Conditions).Select(CreateCondition).ToList()))
            .ToList();
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

    private static bool ReferenceIsIncluded(string reference, IReadOnlySet<string> includedMods)
    {
        var separator = reference.IndexOf('/');
        return separator > 0 && includedMods.Contains(reference[..separator]);
    }
}

public sealed class ProgressionSnapshot
{
    public string Format { get; set; } = "ProgressionJournalSnapshot";
    public int Version { get; set; } = 2;
    public string GeneratedAtUtc { get; set; } = string.Empty;
    public string TargetMod { get; set; } = string.Empty;
    public List<string> ContentMods { get; init; } = [];
    public List<SnapshotMod> Mods { get; set; } = [];
    public List<SnapshotMod> EnvironmentMods { get; set; } = [];
    public List<SnapshotItem> Items { get; set; } = [];
    public List<SnapshotNpc> Npcs { get; set; } = [];
    public List<SnapshotRecipe> Recipes { get; set; } = [];
    public List<SnapshotDrop> Drops { get; set; } = [];
    public List<SnapshotShop> Shops { get; set; } = [];
}

public sealed record SnapshotMod(string Name, string Version);
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
    List<SnapshotCondition> Conditions);
