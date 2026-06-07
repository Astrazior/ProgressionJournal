using Terraria;
using Terraria.ModLoader;

namespace ProgressionJournal.Data.Profiles;

public static class JournalProfileUnlockRegistry
{
    private static readonly Dictionary<string, Func<bool>> ExternalConditions =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Func<bool>?> ModFlagResolvers =
        new(StringComparer.OrdinalIgnoreCase);

    public static void Register(string key, Func<bool> condition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(condition);
        ExternalConditions[key] = condition;
    }

    public static bool IsUnlocked(JournalProfileStageDocument stage, out bool conditionResolved)
    {
        var condition = stage.Unlock;
        var type = condition.Type.Trim().ToLowerInvariant();

        switch (type)
        {
            case "":
            case "always":
                conditionResolved = true;
                return true;
            case "vanilla-stage":
                conditionResolved = JournalStageIds.TryToLegacy(condition.Key, out var vanillaStage);
                return !conditionResolved || ProgressionStageCatalog.Get(vanillaStage).IsUnlocked();
            case "external":
                conditionResolved = ExternalConditions.TryGetValue(condition.Key, out var callback);
                return !conditionResolved || callback!();
            case "mod-flag":
                return TryResolveModFlag(condition, out conditionResolved);
            case "npc":
                return TryResolveNpcCondition(condition, out conditionResolved);
            default:
                conditionResolved = false;
                return true;
        }
    }

    public static void Clear()
    {
        ExternalConditions.Clear();
        ModFlagResolvers.Clear();
    }

    private static bool TryResolveNpcCondition(JournalUnlockConditionDocument condition, out bool conditionResolved)
    {
        if (string.Equals(condition.Mod, "Terraria", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(condition.Npc, out var vanillaNpcType))
        {
            var downed = vanillaNpcType switch
            {
                Terraria.ID.NPCID.KingSlime => NPC.downedSlimeKing,
                Terraria.ID.NPCID.EyeofCthulhu => NPC.downedBoss1,
                Terraria.ID.NPCID.EaterofWorldsHead or Terraria.ID.NPCID.BrainofCthulhu => NPC.downedBoss2,
                Terraria.ID.NPCID.QueenBee => NPC.downedQueenBee,
                Terraria.ID.NPCID.SkeletronHead => NPC.downedBoss3,
                Terraria.ID.NPCID.Deerclops => NPC.downedDeerclops,
                Terraria.ID.NPCID.WallofFlesh => Main.hardMode,
                Terraria.ID.NPCID.QueenSlimeBoss => NPC.downedQueenSlime,
                Terraria.ID.NPCID.TheDestroyer => NPC.downedMechBoss1,
                Terraria.ID.NPCID.Retinazer or Terraria.ID.NPCID.Spazmatism => NPC.downedMechBoss2,
                Terraria.ID.NPCID.SkeletronPrime => NPC.downedMechBoss3,
                Terraria.ID.NPCID.Plantera => NPC.downedPlantBoss,
                Terraria.ID.NPCID.Golem or Terraria.ID.NPCID.GolemHead => NPC.downedGolemBoss,
                Terraria.ID.NPCID.DukeFishron => NPC.downedFishron,
                Terraria.ID.NPCID.HallowBoss => NPC.downedEmpressOfLight,
                Terraria.ID.NPCID.CultistBoss => NPC.downedAncientCultist,
                Terraria.ID.NPCID.MoonLordCore or Terraria.ID.NPCID.MoonLordHead => NPC.downedMoonlord,
                _ => (bool?)null
            };
            conditionResolved = downed.HasValue;
            return downed ?? true;
        }

        if (!string.IsNullOrWhiteSpace(condition.Mod)
            && !string.IsNullOrWhiteSpace(condition.Npc)
            && ModContent.TryFind(condition.Mod, condition.Npc, out ModNPC modNpc))
        {
            var key = $"{condition.Mod}/{condition.Npc}";
            if (ExternalConditions.TryGetValue(key, out var callback))
            {
                conditionResolved = true;
                return callback();
            }

            conditionResolved = true;
            return JournalNpcUnlockTracker.IsDefeated(modNpc.Mod.Name, modNpc.Name);
        }

        conditionResolved = false;
        return true;
    }

    private static bool TryResolveModFlag(
        JournalUnlockConditionDocument condition,
        out bool conditionResolved)
    {
        conditionResolved = false;
        if (string.IsNullOrWhiteSpace(condition.Mod)
            || string.IsNullOrWhiteSpace(condition.Key)
            || !ModLoader.TryGetMod(condition.Mod, out var mod))
        {
            return true;
        }

        try
        {
            var keys = condition.Key.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var resolvedKeys = 0;
            var allTrue = true;
            foreach (var key in keys)
            {
                var resolverKey = $"{condition.Mod}/{key}";
                if (!ModFlagResolvers.TryGetValue(resolverKey, out var resolver))
                {
                    resolver = FindModFlagResolver(mod, key);
                    ModFlagResolvers[resolverKey] = resolver;
                }

                if (resolver is null)
                {
                    return true;
                }

                resolvedKeys++;
                allTrue &= resolver();
            }

            conditionResolved = resolvedKeys == keys.Length;
            return conditionResolved && allTrue;
        }
        catch
        {
            // Unknown or changed mod internals leave the stage available with a warning.
        }

        return true;
    }

    private static Func<bool>? FindModFlagResolver(Mod mod, string key)
    {
        const System.Reflection.BindingFlags flags =
            System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.Static;

        foreach (var type in mod.Code.GetTypes())
        {
            var property = type.GetProperty(key, flags);
            if (property?.PropertyType == typeof(bool)
                && property.GetMethod is not null
                && property.GetMethod.IsStatic)
            {
                return () => (bool)(property.GetValue(null) ?? false);
            }

            var field = type.GetField(key, flags);
            if (field?.FieldType == typeof(bool) && field.IsStatic)
            {
                return () => (bool)(field.GetValue(null) ?? false);
            }
        }

        return null;
    }
}
