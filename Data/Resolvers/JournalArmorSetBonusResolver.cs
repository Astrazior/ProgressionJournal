using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace ProgressionJournal.Data.Resolvers;

internal readonly record struct JournalArmorSetBonusResult(
    string Text,
    int DefenseBonus,
    bool Failed);

internal static class JournalArmorSetBonusResolver
{
    private static readonly Dictionary<string, JournalArmorSetBonusResult> Cache =
        new(StringComparer.Ordinal);

    private static readonly MethodInfo? PlayerLoaderSetupPlayerMethod =
        typeof(PlayerLoader).GetMethod(
            "SetupPlayer",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

    public static JournalArmorSetBonusResult Resolve(JournalArmorSetDefinition armorSet)
    {
        var cacheKey = $"{armorSet.Key}|{Terraria.Localization.Language.ActiveCulture.Name}";
        if (Cache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var resolved = TryProbe(armorSet);
        Cache[cacheKey] = resolved;
        return resolved;
    }

    public static void Clear() => Cache.Clear();

    private static JournalArmorSetBonusResult TryProbe(JournalArmorSetDefinition armorSet)
    {
        if (Main.dedServ
            || !TryCreateArmorSlotItem(armorSet.HeadItemId, out var head)
            || !TryCreateArmorSlotItem(armorSet.BodyItemId, out var body)
            || !TryCreateArmorSlotItem(armorSet.LegItemId, out var legs))
        {
            return new JournalArmorSetBonusResult(string.Empty, 0, false);
        }

        var probeIndex = Math.Min(Main.maxPlayers, Main.player.Length - 1);
        var previousPlayer = Main.player[probeIndex];
        var previousRandom = Main.rand;

        try
        {
            var probe = new Player
            {
                whoAmI = probeIndex,
                active = true,
                dead = false
            };
            PlayerLoaderSetupPlayerMethod?.Invoke(null, [probe]);

            Main.player[probeIndex] = probe;
            Main.rand = new UnifiedRandom(0);
            probe.ResetEffects();

            probe.armor[0] = head;
            probe.armor[1] = body;
            probe.armor[2] = legs;
            probe.head = head.headSlot;
            probe.body = body.bodySlot;
            probe.legs = legs.legSlot;
            probe.setBonus = string.Empty;
            var defenseBeforeSetBonus = probe.statDefense;

            if (head.ModItem is null && body.ModItem is null && legs.ModItem is null)
            {
                probe.UpdateArmorSets(probeIndex);
            }
            else
            {
                ItemLoader.UpdateArmorSet(probe, head, body, legs);
            }

            return new JournalArmorSetBonusResult(
                probe.setBonus?.Trim() ?? string.Empty,
                probe.statDefense - defenseBeforeSetBonus,
                false);
        }
        catch (Exception exception)
        {
            ProgressionJournal.Instance?.Logger.Debug(
                $"Failed to resolve armor set bonus for '{armorSet.Key}'.{Environment.NewLine}{exception}");
            return new JournalArmorSetBonusResult(string.Empty, 0, true);
        }
        finally
        {
            Main.rand = previousRandom;
            Main.player[probeIndex] = previousPlayer;
        }
    }

    private static bool TryCreateArmorSlotItem(int itemId, out Item item)
    {
        if (itemId <= ItemID.None)
        {
            item = new Item();
            item.SetDefaults();
            return true;
        }

        if (!ContentSamples.ItemsByType.TryGetValue(itemId, out var sample))
        {
            item = new Item();
            return false;
        }

        item = sample.Clone();
        return item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0;
    }
}
