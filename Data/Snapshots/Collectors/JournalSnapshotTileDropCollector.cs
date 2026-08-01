using System.Reflection;
using System.Text.RegularExpressions;
using ProgressionJournal.Commands;
using ProgressionJournal.Data.Resolvers;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace ProgressionJournal.Data.Snapshots.Collectors;

internal static class JournalSnapshotTileDropCollector
{
    private const int MaximumFrameSamplesPerTile = 24;
    private const int RandomProbeAttempts = 24;
    private const int SpecialVanillaProbeAttempts = 64;
    private const int PotProbeAttempts = 2048;
    private const int VanillaPotStyleCount = 37;

    private static readonly MethodInfo? VanillaTileDropMethod = typeof(WorldGen).GetMethod(
        "KillTile_GetItemDrops",
        BindingFlags.Static | BindingFlags.NonPublic);

    private static readonly FieldInfo? WorldRandomField = typeof(WorldGen).GetField(
        "_genRand",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

    private static readonly SpawnThingsFromPotDelegate? SpawnThingsFromPot = CreatePotDropDelegate();

#pragma warning disable SYSLIB1045 // tModLoader's in-game compiler does not run the GeneratedRegex source generator.
    private static readonly Regex InternalNameWordBoundaryRegex = new(
        "(?<=[a-z])(?=[A-Z])",
        RegexOptions.Compiled);
#pragma warning restore SYSLIB1045

    public static List<SnapshotDrop> Collect(
        HashSet<int> includedItems,
        Func<int, string> getItemReference,
        Func<int, string> getTileReference,
        Action<string, Exception> logDebug)
    {
        if (Main.maxTilesX <= 0 || Main.maxTilesY <= 0)
        {
            return [];
        }

        Dictionary<(int TileType, int ItemType, string SourceName), SnapshotDrop> result = [];
        using var worldIsolation = new JournalWorldStateIsolation();
        var originalRandom = Main.rand;
        var originalWorldRandom = WorldGen.genRand;
        var originalDestroyObject = WorldGen.destroyObject;
        var originalNoTileActions = WorldGen.noTileActions;
        var originalShadowOrbSmashed = WorldGen.shadowOrbSmashed;
        var originalShadowOrbCount = WorldGen.shadowOrbCount;

        try
        {
            foreach (var sample in CollectWorldSamples())
            {
                ProbeSample(
                    sample,
                    includedItems,
                    getItemReference,
                    getTileReference,
                    result,
                    logDebug);
            }

            ProbeUnobservedModTiles(
                includedItems,
                getItemReference,
                getTileReference,
                result,
                logDebug);
            ProbeSpecialVanillaTiles(
                includedItems,
                getItemReference,
                getTileReference,
                result,
                logDebug);
        }
        finally
        {
            Main.rand = originalRandom;
            WorldRandomField?.SetValue(null, originalWorldRandom);
            WorldGen.destroyObject = originalDestroyObject;
            WorldGen.noTileActions = originalNoTileActions;
            WorldGen.shadowOrbSmashed = originalShadowOrbSmashed;
            WorldGen.shadowOrbCount = originalShadowOrbCount;
        }

        return result.Values
            .OrderBy(static drop => drop.Source, StringComparer.Ordinal)
            .ThenBy(static drop => drop.Item, StringComparer.Ordinal)
            .ToList();
    }

    private static TileProbeSample[] CollectWorldSamples()
    {
        var frameSamplesByTile = new Dictionary<int, HashSet<(short FrameX, short FrameY)>>();
        List<TileProbeSample> samples = [];

        for (var x = 1; x < Main.maxTilesX - 1; x++)
        {
            for (var y = 1; y < Main.maxTilesY - 1; y++)
            {
                var tile = Main.tile[x, y];
                if (!tile.HasTile)
                {
                    continue;
                }

                var tileType = tile.TileType;
                if (!frameSamplesByTile.TryGetValue(tileType, out var frames))
                {
                    frames = [];
                    frameSamplesByTile[tileType] = frames;
                }

                var frame = (tile.TileFrameX, tile.TileFrameY);
                if (frames.Count >= MaximumFrameSamplesPerTile || !frames.Add(frame))
                {
                    continue;
                }

                samples.Add(new TileProbeSample(x, y, tileType));
            }
        }

        return samples.ToArray();
    }

    private static void ProbeSample(
        TileProbeSample sample,
        HashSet<int> includedItems,
        Func<int, string> getItemReference,
        Func<int, string> getTileReference,
        IDictionary<(int TileType, int ItemType, string SourceName), SnapshotDrop> result,
        Action<string, Exception> logDebug)
    {
        var tile = Main.tile[sample.X, sample.Y];
        if (!tile.HasTile || tile.TileType != sample.TileType)
        {
            return;
        }

        for (var attempt = 0; attempt < RandomProbeAttempts; attempt++)
        {
            Main.rand = new UnifiedRandom(HashCode.Combine(sample.TileType, tile.TileFrameX, tile.TileFrameY, attempt));
            if (sample.TileType < TileID.Count)
            {
                ProbeVanillaTile(
                    sample,
                    tile,
                    includedItems,
                    getItemReference,
                    getTileReference,
                    result,
                    logDebug);
            }
            else if (TileLoader.GetTile(sample.TileType) is { } modTile)
            {
                ProbeModTile(
                    sample,
                    modTile,
                    includedItems,
                    getItemReference,
                    getTileReference,
                    result,
                    logDebug);
            }
        }
    }

    private static void ProbeVanillaTile(
        TileProbeSample sample,
        Tile tile,
        HashSet<int> includedItems,
        Func<int, string> getItemReference,
        Func<int, string> getTileReference,
        IDictionary<(int TileType, int ItemType, string SourceName), SnapshotDrop> result,
        Action<string, Exception> logDebug)
    {
        if (VanillaTileDropMethod is null)
        {
            return;
        }

        object?[] arguments = [sample.X, sample.Y, tile, 0, 1, 0, 1, true];
        try
        {
            VanillaTileDropMethod.Invoke(null, arguments);
            AddResult((int)arguments[3]!, (int)arguments[4]!);
            AddResult((int)arguments[5]!, (int)arguments[6]!);
        }
        catch (Exception exception)
        {
            logDebug($"Could not inspect vanilla tile drop {sample.TileType}.", exception);
        }

        return;

        void AddResult(int itemType, int stack)
        {
            if (itemType <= ItemID.None || !includedItems.Contains(itemType))
            {
                return;
            }

            AddDrop(sample.TileType, itemType, stack, getItemReference, getTileReference, result);
        }
    }

    private static void ProbeModTile(
        TileProbeSample sample,
        ModTile modTile,
        HashSet<int> includedItems,
        Func<int, string> getItemReference,
        Func<int, string> getTileReference,
        IDictionary<(int TileType, int ItemType, string SourceName), SnapshotDrop> result,
        Action<string, Exception> logDebug)
    {
        try
        {
            foreach (var item in modTile.GetItemDrops(sample.X, sample.Y) ?? [])
            {
                if (item is null || item.IsAir || !includedItems.Contains(item.type))
                {
                    continue;
                }

                AddDrop(
                    sample.TileType,
                    item.type,
                    item.stack,
                    getItemReference,
                    getTileReference,
                    result);
            }
        }
        catch (Exception exception)
        {
            logDebug($"Could not inspect mod tile drop {modTile.FullName}.", exception);
        }
    }

    private static void ProbeUnobservedModTiles(
        HashSet<int> includedItems,
        Func<int, string> getItemReference,
        Func<int, string> getTileReference,
        IDictionary<(int TileType, int ItemType, string SourceName), SnapshotDrop> result,
        Action<string, Exception> logDebug)
    {
        var x = Math.Clamp(Main.spawnTileX, 1, Main.maxTilesX - 2);
        var y = Math.Clamp(Main.spawnTileY, 1, Main.maxTilesY - 2);
        var originalTile = (Tile)Main.tile[x, y].Clone();

        try
        {
            for (var tileType = TileID.Count; tileType < TileLoader.TileCount; tileType++)
            {
                if (TileLoader.GetTile(tileType) is not { } modTile)
                {
                    continue;
                }

                for (var style = 0; style < 32; style++)
                {
                    var itemType = TileLoader.GetItemDropFromTypeAndStyle(tileType, style);
                    if (itemType > ItemID.None && includedItems.Contains(itemType))
                    {
                        AddDrop(tileType, itemType, 1, getItemReference, getTileReference, result);
                    }
                }

                if (modTile.GetType().GetMethod(nameof(ModTile.GetItemDrops))?.DeclaringType == typeof(ModTile))
                {
                    continue;
                }

                for (short frameX = 0; frameX <= 72; frameX += 18)
                {
                    for (short frameY = 0; frameY <= 72; frameY += 18)
                    {
                        var tile = Main.tile[x, y];
                        tile.ClearEverything();
                        tile.HasTile = true;
                        tile.TileType = (ushort)tileType;
                        tile.TileFrameX = frameX;
                        tile.TileFrameY = frameY;

                        ProbeSample(
                            new TileProbeSample(x, y, tileType),
                            includedItems,
                            getItemReference,
                            getTileReference,
                            result,
                            logDebug);
                    }
                }
            }
        }
        finally
        {
            Main.tile[x, y].CopyFrom(originalTile);
        }
    }

    private static void ProbeSpecialVanillaTiles(
        HashSet<int> includedItems,
        Func<int, string> getItemReference,
        Func<int, string> getTileReference,
        IDictionary<(int TileType, int ItemType, string SourceName), SnapshotDrop> result,
        Action<string, Exception> logDebug)
    {
        var x = Math.Clamp(Main.spawnTileX, 10, Main.maxTilesX - 12);
        var y = Math.Clamp(Main.spawnTileY, 10, Main.maxTilesY - 12);
        var originalTiles = CaptureTiles(x - 3, y - 3, width: 8, height: 8);

        try
        {
            JournalWorldStateIsolation.ApplyNeutralBaseline();
            WorldGen.noTileActions = false;
            using var capture = JournalTileDropCapture.Begin();

            ProbeOrbMechanic(
                x,
                y,
                TileID.Heart,
                frameOffsetX: 0,
                Lang.GetItemNameValue(ItemID.LifeCrystal),
                includedItems,
                getItemReference,
                getTileReference,
                result);
            ProbeOrbMechanic(
                x,
                y,
                TileID.ShadowOrbs,
                frameOffsetX: 0,
                Lang.GetItemNameValue(ItemID.ShadowOrb),
                includedItems,
                getItemReference,
                getTileReference,
                result);
            ProbeOrbMechanic(
                x,
                y,
                TileID.ShadowOrbs,
                frameOffsetX: 36,
                Lang.GetItemNameValue(ItemID.CrimsonHeart),
                includedItems,
                getItemReference,
                getTileReference,
                result);
            ProbePots(
                x,
                includedItems,
                getItemReference,
                getTileReference,
                result);
        }
        catch (Exception exception)
        {
            logDebug("Could not inspect special vanilla tile drops.", exception);
        }
        finally
        {
            RestoreTiles(originalTiles);
        }
    }

    private static void ProbeOrbMechanic(
        int x,
        int y,
        int tileType,
        short frameOffsetX,
        string sourceName,
        HashSet<int> includedItems,
        Func<int, string> getItemReference,
        Func<int, string> getTileReference,
        IDictionary<(int TileType, int ItemType, string SourceName), SnapshotDrop> result)
    {
        Main.rand = new UnifiedRandom(HashCode.Combine(tileType, frameOffsetX));
        SetWorldRandom(new UnifiedRandom(HashCode.Combine(frameOffsetX, tileType, 1)));

        for (var attempt = 0; attempt < SpecialVanillaProbeAttempts; attempt++)
        {
            PrepareOrbTiles(x, y, tileType, frameOffsetX);
            WorldGen.destroyObject = false;
            WorldGen.shadowOrbSmashed = true;
            WorldGen.shadowOrbCount = 2;
            WorldGen.CheckOrb(x, y, tileType);
            AppendCapturedDrops(
                tileType,
                sourceName,
                includedItems,
                getItemReference,
                getTileReference,
                result);

            if (tileType == TileID.Heart)
            {
                break;
            }
        }
    }

    private static void PrepareOrbTiles(int x, int y, int tileType, short frameOffsetX)
    {
        for (var offsetX = 0; offsetX < 2; offsetX++)
        {
            for (var offsetY = 0; offsetY < 2; offsetY++)
            {
                var tile = Main.tile[x + offsetX, y + offsetY];
                tile.ClearEverything();
                tile.HasTile = true;
                tile.TileType = (ushort)tileType;
                tile.TileFrameX = (short)(frameOffsetX + offsetX * 18);
                tile.TileFrameY = (short)(offsetY * 18);
            }
        }

        Main.tile[x + 1, y + 1].ClearEverything();
        Main.tile[x, y + 2].ClearEverything();
        Main.tile[x + 1, y + 2].ClearEverything();
    }

    private static void ProbePots(
        int x,
        HashSet<int> includedItems,
        Func<int, string> getItemReference,
        Func<int, string> getTileReference,
        IDictionary<(int TileType, int ItemType, string SourceName), SnapshotDrop> result)
    {
        if (SpawnThingsFromPot is null)
        {
            throw new MissingMethodException(typeof(WorldGen).FullName, "SpawnThingsFromPot");
        }

        var sourceName = ResolveTileName(TileID.Pots, ItemID.None);
        int[] depths =
        [
            Math.Clamp((int)Main.worldSurface - 50, 10, Main.maxTilesY - 10),
            Math.Clamp((int)((Main.worldSurface + Main.rockLayer) / 2d), 10, Main.maxTilesY - 10),
            Math.Clamp((int)((Main.rockLayer + Main.UnderworldLayer) / 2d), 10, Main.maxTilesY - 10),
            Math.Clamp(Main.UnderworldLayer + 20, 10, Main.maxTilesY - 10)
        ];
        Main.rand = new UnifiedRandom(0x504F5453);
        SetWorldRandom(new UnifiedRandom(0x44524F50));

        foreach (var hardmode in new[] { false, true })
        {
            Main.hardMode = hardmode;
            foreach (var depth in depths)
            {
                for (var style = 0; style < VanillaPotStyleCount; style++)
                {
                    for (var attempt = 0; attempt < PotProbeAttempts; attempt++)
                    {
                        SpawnThingsFromPot(x, depth, x, depth, style);
                        AppendCapturedDrops(
                            TileID.Pots,
                            sourceName,
                            includedItems,
                            getItemReference,
                            getTileReference,
                            result);
                    }
                }
            }
        }
    }

    private static void AppendCapturedDrops(
        int tileType,
        string sourceName,
        HashSet<int> includedItems,
        Func<int, string> getItemReference,
        Func<int, string> getTileReference,
        IDictionary<(int TileType, int ItemType, string SourceName), SnapshotDrop> result)
    {
        try
        {
            foreach (var item in JournalTileDropCapture.Items)
            {
                if (!includedItems.Contains(item.ItemType))
                {
                    continue;
                }

                AddDrop(
                    tileType,
                    item.ItemType,
                    item.Stack,
                    getItemReference,
                    getTileReference,
                    result,
                    sourceName);
            }
        }
        finally
        {
            JournalTileDropCapture.Clear();
        }
    }

    private static Dictionary<(int X, int Y), Tile> CaptureTiles(int startX, int startY, int width, int height)
    {
        Dictionary<(int X, int Y), Tile> result = [];
        for (var x = startX; x < startX + width; x++)
        {
            for (var y = startY; y < startY + height; y++)
            {
                result[(x, y)] = (Tile)Main.tile[x, y].Clone();
                Main.tile[x, y].ClearEverything();
            }
        }

        return result;
    }

    private static void RestoreTiles(Dictionary<(int X, int Y), Tile> tiles)
    {
        foreach (var ((x, y), tile) in tiles)
        {
            Main.tile[x, y].CopyFrom(tile);
        }
    }

    private static void SetWorldRandom(UnifiedRandom random)
    {
        WorldRandomField?.SetValue(null, random);
    }

    private static SpawnThingsFromPotDelegate? CreatePotDropDelegate()
    {
        var method = typeof(WorldGen).GetMethod(
            "SpawnThingsFromPot",
            BindingFlags.Static | BindingFlags.NonPublic);
        return method is null
            ? null
            : (SpawnThingsFromPotDelegate)Delegate.CreateDelegate(
                typeof(SpawnThingsFromPotDelegate),
                method);
    }

    private static void AddDrop(
        int tileType,
        int itemType,
        int stack,
        Func<int, string> getItemReference,
        Func<int, string> getTileReference,
        IDictionary<(int TileType, int ItemType, string SourceName), SnapshotDrop> result,
        string? sourceName = null)
    {
        sourceName ??= ResolveTileName(tileType, itemType);
        var key = (tileType, itemType, sourceName);
        var stackSize = Math.Max(1, stack);
        if (result.TryGetValue(key, out var existing))
        {
            result[key] = existing with
            {
                StackMin = Math.Min(existing.StackMin, stackSize),
                StackMax = Math.Max(existing.StackMax, stackSize)
            };
            return;
        }

        result[key] = new SnapshotDrop(
            "tile",
            getTileReference(tileType),
            getItemReference(itemType),
            1f,
            stackSize,
            stackSize,
            [],
            sourceName,
            HideDropRate: true);
    }

    private static string ResolveTileName(int tileType, int itemType)
    {
        var placedItemName = ContentSamples.ItemsByType.Values
            .Where(item => item.createTile == tileType && item.type == itemType)
            .Select(static item => item.Name)
            .FirstOrDefault(static name => !string.IsNullOrWhiteSpace(name));
        if (!string.IsNullOrWhiteSpace(placedItemName))
        {
            return Lang.GetItemNameValue(itemType);
        }

        var internalName = TileLoader.GetTile(tileType)?.Name ?? TileID.Search.GetName(tileType);
        return string.IsNullOrWhiteSpace(internalName)
            ? $"Tile {tileType}"
            : InternalNameWordBoundaryRegex.Replace(internalName, " ");
    }

    private readonly record struct TileProbeSample(int X, int Y, int TileType);

    private delegate void SpawnThingsFromPotDelegate(int i, int j, int x2, int y2, int style);
}
