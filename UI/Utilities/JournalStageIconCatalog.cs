using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ProgressionJournal.UI.Utilities;

public static class JournalStageIconCatalog
{
    private static IReadOnlyList<JournalStageIconCandidate>? _cachedCandidates;

    private static readonly Dictionary<string, int> VanillaStageFallbacks =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["pre-boss"] = NPCID.Guide,
            ["pre-evil1"] = NPCID.EyeofCthulhu,
            ["pre-evil2"] = NPCID.EaterofWorldsHead,
            ["pre-skeletron"] = NPCID.SkeletronHead,
            ["pre-wof"] = NPCID.WallofFlesh,
            ["pre-mech"] = NPCID.WallofFlesh,
            ["post-mech1"] = NPCID.TheDestroyer,
            ["post-mech2"] = NPCID.Retinazer,
            ["pre-plantera"] = NPCID.Plantera,
            ["pre-golem"] = NPCID.GolemHead,
            ["post-golem"] = NPCID.GolemHead,
            ["pre-lunar"] = NPCID.CultistBoss,
            ["pre-moonlord"] = NPCID.MoonLordHead,
            ["pre-provi"] = NPCID.MoonLordHead
        };

    private static IReadOnlyList<JournalStageIconCandidate> GetCandidates(string search)
    {
        _cachedCandidates ??= Enumerable.Range(1, NPCLoader.NPCCount - 1)
            .Select(CreateCandidate)
            .Where(static candidate => candidate is not null)
            .Select(static candidate => candidate!)
            .OrderBy(static candidate => candidate.ModDisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(static candidate => candidate.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        return _cachedCandidates
            .Where(candidate => string.IsNullOrWhiteSpace(search)
                || candidate.DisplayName.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                || candidate.InternalName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || candidate.ModDisplayName.Contains(search, StringComparison.CurrentCultureIgnoreCase))
            .ToArray();
    }

    public static bool TryResolve(
        JournalProfile profile,
        JournalProfileStageDocument stage,
        out int npcType)
    {
        if (TryResolveConfigured(stage, out npcType))
        {
            return true;
        }

        var stageName = JournalProfileText.GetStageName(profile, stage.Id);
        var candidates = GetCandidates(string.Empty);
        var target = NormalizeStageName(stageName);
        var best = candidates
            .Select(candidate => new
            {
                Candidate = candidate,
                Score = ScoreCandidate(target, candidate)
            })
            .Where(static value => value.Score > 0)
            .OrderByDescending(static value => value.Score)
            .ThenBy(static value => value.Candidate.DisplayName.Length)
            .FirstOrDefault();
        if (best is not null)
        {
            npcType = best.Candidate.NpcType;
            return true;
        }

        if (VanillaStageFallbacks.TryGetValue(stage.Id, out npcType))
        {
            if (stage.Id == "pre-evil2" && WorldGen.crimson)
            {
                npcType = NPCID.BrainofCthulhu;
            }

            return true;
        }

        npcType = -1;
        return false;
    }

    public static int GetBossHeadSlot(int npcType)
    {
        var headSlot = npcType >= 0 && npcType < NPCID.Sets.BossHeadTextures.Length
            ? NPCID.Sets.BossHeadTextures[npcType]
            : -1;
        if (npcType < 0 || headSlot >= 0 || NPCLoader.GetNPC(npcType) is not { } modNpc) return headSlot;
        headSlot = ModContent.GetModBossHeadSlot($"{modNpc.Texture}_Head_Boss");
        if (headSlot < 0)
        {
            modNpc.BossHeadSlot(ref headSlot);
        }

        return headSlot;
    }

    private static JournalStageIconCandidate? CreateCandidate(int npcType)
    {
        var headSlot = GetBossHeadSlot(npcType);
        if (headSlot < 0)
        {
            return null;
        }

        var modNpc = NPCLoader.GetNPC(npcType);
        return new JournalStageIconCandidate(
            npcType,
            headSlot,
            modNpc?.Mod.Name ?? "Terraria",
            modNpc?.Name ?? npcType.ToString(),
            Lang.GetNPCNameValue(npcType),
            modNpc?.Mod.DisplayNameClean ?? "Terraria");
    }

    private static bool TryResolveConfigured(JournalProfileStageDocument stage, out int npcType)
    {
        npcType = -1;
        var modName = string.IsNullOrWhiteSpace(stage.IconMod) ? stage.Unlock.Mod : stage.IconMod;
        var npcName = string.IsNullOrWhiteSpace(stage.IconNpc) ? stage.Unlock.Npc : stage.IconNpc;

        if (string.Equals(modName, "Terraria", StringComparison.OrdinalIgnoreCase)
            && (int.TryParse(npcName, out npcType) || NPCID.Search.TryGetId(npcName, out npcType)))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(modName)
            && !string.IsNullOrWhiteSpace(npcName)
            && ModContent.TryFind(modName, npcName, out ModNPC modNpc))
        {
            npcType = modNpc.Type;
            return true;
        }

        return false;
    }

    private static int ScoreCandidate(string target, JournalStageIconCandidate candidate)
    {
        var display = Normalize(candidate.DisplayName);
        var internalName = Normalize(candidate.InternalName);
        if (string.IsNullOrWhiteSpace(target))
        {
            return 0;
        }

        if (display == target || internalName == target)
        {
            return 1000;
        }

        if (display.Contains(target, StringComparison.Ordinal)
            || target.Contains(display, StringComparison.Ordinal)
            || internalName.Contains(target, StringComparison.Ordinal)
            || target.Contains(internalName, StringComparison.Ordinal))
        {
            return 700 + Math.Min(target.Length, Math.Max(display.Length, internalName.Length));
        }

        var targetTokens = Tokenize(target);
        var candidateTokens = Tokenize($"{display} {internalName}");
        var matchingTokens = targetTokens.Count(candidateTokens.Contains);
        return matchingTokens == 0 ? 0 : matchingTokens * 100;
    }

    private static string NormalizeStageName(string value)
    {
        var normalized = Normalize(value);
        foreach (var prefix in new[] { "pre", "post" })
        {
            if (normalized.StartsWith(prefix, StringComparison.Ordinal))
            {
                normalized = normalized[prefix.Length..];
                break;
            }
        }

        return normalized
            .Replace("bosses", string.Empty, StringComparison.Ordinal)
            .Replace("boss", string.Empty, StringComparison.Ordinal)
            .Replace("events", string.Empty, StringComparison.Ordinal);
    }

    private static string Normalize(string value)
    {
        return new string(value
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    private static HashSet<string> Tokenize(string value)
    {
        return value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Normalize)
            .Where(static token => token.Length >= 3)
            .ToHashSet(StringComparer.Ordinal);
    }
}

public sealed record JournalStageIconCandidate(
    int NpcType,
    int HeadSlot,
    string ModName,
    string InternalName,
    string DisplayName,
    string ModDisplayName);
