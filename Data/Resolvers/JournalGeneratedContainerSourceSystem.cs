using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ProgressionJournal.Data.Resolvers;

public sealed class JournalGeneratedContainerSourceSystem : ModSystem
{
    private const string CompleteKey = "generatedContainerSourcesComplete";
    private const string SourcesKey = "generatedContainerSources";
    private static readonly List<StoredContainerSource> Sources = [];
    private bool _captureAfterWorldGen;

    public static IReadOnlyList<JournalGeneratedContainerSource> GetSources(int targetItemId)
    {
        return Sources
            .Where(source => TryResolveItemReference(source.TargetItem, out var resolvedTarget)
                && resolvedTarget == targetItemId
                && TryResolveItemReference(source.SourceItem, out _))
            .Select(source =>
            {
                TryResolveItemReference(source.SourceItem, out var sourceItemId);
                return new JournalGeneratedContainerSource(
                    sourceItemId,
                    source.DropRate,
                    source.StackMin,
                    source.StackMax);
            })
            .ToArray();
    }

    public static IReadOnlyList<JournalGeneratedContainerDrop> GetAllSources()
    {
        return Sources
            .Select(source => new JournalGeneratedContainerDrop(
                source.SourceItem,
                source.TargetItem,
                source.DropRate,
                source.StackMin,
                source.StackMax))
            .ToArray();
    }

    public override void OnWorldLoad()
    {
        _captureAfterWorldGen = false;
        Sources.Clear();
        JournalItemSourceResolver.ClearCache();
    }

    public override void PostWorldGen()
    {
        CaptureGeneratedContainers();
        _captureAfterWorldGen = true;
    }

    public override void PostUpdateWorld()
    {
        if (!_captureAfterWorldGen)
        {
            return;
        }

        _captureAfterWorldGen = false;
        CaptureGeneratedContainers();
    }

    public override void SaveWorldData(TagCompound tag)
    {
        if (Sources.Count == 0)
        {
            return;
        }

        tag[CompleteKey] = true;
        tag[SourcesKey] = Sources.Select(static source => new TagCompound
        {
            ["sourceItem"] = source.SourceItem,
            ["targetItem"] = source.TargetItem,
            ["dropRate"] = source.DropRate,
            ["stackMin"] = source.StackMin,
            ["stackMax"] = source.StackMax
        }).ToList();
    }

    public override void LoadWorldData(TagCompound tag)
    {
        Sources.Clear();
        if (!tag.GetBool(CompleteKey))
        {
            JournalItemSourceResolver.ClearCache();
            return;
        }

        foreach (var sourceTag in tag.GetList<TagCompound>(SourcesKey))
        {
            var sourceItem = sourceTag.GetString("sourceItem");
            var targetItem = sourceTag.GetString("targetItem");
            if (string.IsNullOrWhiteSpace(sourceItem) || string.IsNullOrWhiteSpace(targetItem))
            {
                continue;
            }

            Sources.Add(new StoredContainerSource(
                sourceItem,
                targetItem,
                sourceTag.GetFloat("dropRate"),
                Math.Max(1, sourceTag.GetInt("stackMin")),
                Math.Max(1, sourceTag.GetInt("stackMax"))));
        }

        JournalItemSourceResolver.ClearCache();
    }

    public override void OnWorldUnload()
    {
        _captureAfterWorldGen = false;
        Sources.Clear();
        JournalItemSourceResolver.ClearCache();
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write7BitEncodedInt(Sources.Count);
        foreach (var source in Sources)
        {
            writer.Write(source.SourceItem);
            writer.Write(source.TargetItem);
            writer.Write(source.DropRate);
            writer.Write7BitEncodedInt(source.StackMin);
            writer.Write7BitEncodedInt(source.StackMax);
        }
    }

    public override void NetReceive(BinaryReader reader)
    {
        Sources.Clear();
        var count = reader.Read7BitEncodedInt();
        for (var index = 0; index < count; index++)
        {
            Sources.Add(new StoredContainerSource(
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadSingle(),
                reader.Read7BitEncodedInt(),
                reader.Read7BitEncodedInt()));
        }

        JournalItemSourceResolver.ClearCache();
    }

    private static void CaptureGeneratedContainers()
    {
        var observations = Main.chest
            .Where(static chest => chest is not null)
            .Select(chest => new
            {
                Chest = chest!,
                SourceItemId = ResolveWorldContainerItem(chest!)
            })
            .Where(static observation => observation.SourceItemId is not null)
            .ToArray();

        Sources.Clear();
        foreach (var sourceGroup in observations.GroupBy(static observation => observation.SourceItemId!.Value))
        {
            var containerCount = sourceGroup.Count();
            var itemObservations = sourceGroup
                .SelectMany(static observation => observation.Chest.item
                    .Where(static item => item is not null && !item.IsAir)
                    .GroupBy(static item => item.type)
                    .Select(static group => new
                    {
                        ItemId = group.Key,
                        Stack = group.Sum(static item => item.stack)
                    }))
                .GroupBy(static observation => observation.ItemId);

            foreach (var itemGroup in itemObservations)
            {
                Sources.Add(new StoredContainerSource(
                    GetItemReference(sourceGroup.Key),
                    GetItemReference(itemGroup.Key),
                    itemGroup.Count() / (float)containerCount,
                    itemGroup.Min(static observation => observation.Stack),
                    itemGroup.Max(static observation => observation.Stack)));
            }
        }

        JournalItemSourceResolver.ClearCache();
    }

    private static int? ResolveWorldContainerItem(Chest chest)
    {
        var tile = Framing.GetTileSafely(chest.x, chest.y);
        if (!tile.HasTile)
        {
            return null;
        }

        var style = tile.TileFrameX / 36;
        int? fallback = null;
        for (var itemId = ItemID.None + 1; itemId < ItemLoader.ItemCount; itemId++)
        {
            var item = ContentSamples.ItemsByType[itemId];
            if (item.createTile != tile.TileType)
            {
                continue;
            }

            fallback ??= itemId;
            if (item.placeStyle == style)
            {
                return itemId;
            }
        }

        return fallback;
    }

    private static string GetItemReference(int itemId)
    {
        var modItem = ItemLoader.GetItem(itemId);
        return modItem is null
            ? $"Terraria/{ItemID.Search.GetName(itemId)}"
            : $"{modItem.Mod.Name}/{modItem.Name}";
    }

    private static bool TryResolveItemReference(string reference, out int itemId)
    {
        itemId = ItemID.None;
        var separator = reference.IndexOf('/');
        if (separator <= 0 || separator >= reference.Length - 1)
        {
            return false;
        }

        var modName = reference[..separator];
        var itemName = reference[(separator + 1)..];
        if (string.Equals(modName, "Terraria", StringComparison.OrdinalIgnoreCase))
        {
            return ItemID.Search.TryGetId(itemName, out itemId);
        }

        if (!ModContent.TryFind<ModItem>($"{modName}/{itemName}", out var modItem))
        {
            return false;
        }

        itemId = modItem.Type;
        return true;
    }

    private sealed record StoredContainerSource(
        string SourceItem,
        string TargetItem,
        float DropRate,
        int StackMin,
        int StackMax);
}

public sealed record JournalGeneratedContainerSource(
    int SourceItemId,
    float DropRate,
    int StackMin,
    int StackMax);

public sealed record JournalGeneratedContainerDrop(
    string SourceItem,
    string TargetItem,
    float DropRate,
    int StackMin,
    int StackMax);
