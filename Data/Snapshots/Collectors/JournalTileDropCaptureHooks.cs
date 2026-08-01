#nullable enable

using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ProgressionJournal.Data.Snapshots.Collectors;

internal static class JournalTileDropCapture
{
    [ThreadStatic]
    private static List<CapturedItem>? _items;

    private static readonly CapturedItem[] NoItems = [];

    public static bool IsActive => _items is not null;

    public static IDisposable Begin()
    {
        if (_items is not null)
        {
            throw new InvalidOperationException("A tile drop capture is already active.");
        }

        _items = [];
        return new CaptureScope();
    }

    public static IReadOnlyList<CapturedItem> Items => _items is null ? NoItems : _items;

    public static void Clear() => _items?.Clear();

    public static void Record(Item item)
    {
        if (_items is null || item.IsAir)
        {
            return;
        }

        _items.Add(new CapturedItem(item.type, Math.Max(1, item.stack)));
        item.active = false;
    }

    private sealed class CaptureScope : IDisposable
    {
        public void Dispose()
        {
            _items = null;
        }
    }
}

internal sealed class JournalTileDropCaptureGlobalItem : GlobalItem
{
    public override void OnSpawn(Item item, IEntitySource source)
    {
        JournalTileDropCapture.Record(item);
    }
}

internal sealed class JournalTileDropCaptureGlobalNpc : GlobalNPC
{
    public override void OnSpawn(NPC npc, IEntitySource source)
    {
        if (JournalTileDropCapture.IsActive)
        {
            npc.active = false;
        }
    }
}

internal sealed class JournalTileDropCaptureGlobalProjectile : GlobalProjectile
{
    public override void OnSpawn(Projectile projectile, IEntitySource source)
    {
        if (JournalTileDropCapture.IsActive)
        {
            projectile.active = false;
        }
    }
}

internal readonly record struct CapturedItem(int ItemType, int Stack);
