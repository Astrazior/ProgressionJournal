using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ProgressionJournal.Data.Repositories;

public static partial class JournalRepository
{
    private sealed class BuildCandidateAccumulator(
        int itemId,
        string availableFromStageId,
        string lastRelevantStageId,
        RecommendationTier? tier,
        int sortStrength,
        bool isClassSpecific)
    {
        public int ItemId { get; } = itemId;

        public string AvailableFromStageId { get; } = availableFromStageId;

        public string LastRelevantStageId { get; set; } = lastRelevantStageId;

        public RecommendationTier? Tier { get; set; } = tier;

        public int SortStrength { get; set; } = sortStrength;

        public bool IsClassSpecific { get; } = isClassSpecific;
    }

    public static IReadOnlyList<JournalBuildCandidate> GetBuildCandidates(
        ProgressionStageId stageId,
        CombatClass combatClass,
        string slotKey)
    {
        return GetBuildCandidates(
            JournalProfileIds.Vanilla,
            JournalStageIds.FromLegacy(stageId),
            JournalClassIds.FromLegacy(combatClass),
            slotKey);
    }

    public static IReadOnlyList<JournalBuildCandidate> GetBuildCandidates(
        string profileId,
        string stageId,
        string classId,
        string slotKey)
    {
        if (!JournalBuildPlannerCatalog.TryGetSlotKind(slotKey, out var slotKind))
        {
            return [];
        }

        return slotKind switch
        {
            JournalBuildSlotKind.Food => BuildFoodCandidates(),
            JournalBuildSlotKind.Potion => BuildBuffCandidates(profileId, stageId, classId, slotKind),
            _ => BuildEquipmentCandidates(profileId, stageId, classId, slotKind)
        };
    }

    public static bool IsBuildSelectionValid(
        ProgressionStageId stageId,
        CombatClass combatClass,
        string slotKey,
        int itemId)
    {
        return IsBuildSelectionValid(
            JournalProfileIds.Vanilla,
            JournalStageIds.FromLegacy(stageId),
            JournalClassIds.FromLegacy(combatClass),
            slotKey,
            itemId);
    }

    public static bool IsBuildSelectionValid(
        string profileId,
        string stageId,
        string classId,
        string slotKey,
        int itemId)
    {
        if (!JournalBuildPlannerCatalog.TryGetSlotKind(slotKey, out var slotKind))
        {
            return false;
        }

        return GetBuildCandidates(profileId, stageId, classId, slotKey).Any(candidate => candidate.ItemId == itemId)
            || IsValidModBuildCandidate(profileId, classId, slotKind, itemId);
    }

    public static IReadOnlyList<JournalBuildCandidateGroup> GetModBuildCandidateGroups(
        CombatClass combatClass,
        string slotKey)
    {
        return GetModBuildCandidateGroups(
            JournalProfileIds.Vanilla,
            JournalClassIds.FromLegacy(combatClass),
            slotKey);
    }

    public static IReadOnlyList<JournalBuildCandidateGroup> GetModBuildCandidateGroups(
        string profileId,
        string classId,
        string slotKey)
    {
        if (!JournalBuildPlannerCatalog.TryGetSlotKind(slotKey, out var slotKind))
        {
            return [];
        }

        return ContentSamples.ItemsByType.Values
            .Where(item => IsModBuildCandidate(profileId, classId, slotKind, item))
            .GroupBy(static item => item.ModItem!.Mod.DisplayNameClean)
            .OrderBy(static group => group.Key, StringComparer.CurrentCultureIgnoreCase)
            .Select(static group =>
            {
                var uniqueItems = group
                    .GroupBy(static item => item.type)
                    .Select(static itemGroup => itemGroup.First())
                    .OrderBy(static item => Lang.GetItemNameValue(item.type), StringComparer.CurrentCultureIgnoreCase)
                    .ToArray();

                return new JournalBuildCandidateGroup(
                    group.Key,
                    uniqueItems
                        .Select(static item => new JournalBuildCandidate(item.type))
                        .ToArray(),
                    uniqueItems.Length > 0 ? uniqueItems[0].type : 0);
            })
            .Where(static group => group.Candidates.Count > 0)
            .ToArray();
    }

    private static IReadOnlyList<JournalBuildCandidate> BuildEquipmentCandidates(
        string profileId,
        string stageId,
        string classId,
        JournalBuildSlotKind slotKind)
    {
        var profile = JournalProfileRegistry.TryGet(profileId, out var registeredProfile)
            ? registeredProfile
            : JournalProfileRegistry.Active;
        var targetIndex = profile.GetStageIndex(stageId);
        Dictionary<int, BuildCandidateAccumulator> candidates = new();

        foreach (var progressionStage in profile.Stages)
        {
            var currentStageId = progressionStage.Id;
            if (profile.GetStageIndex(currentStageId) > targetIndex)
            {
                break;
            }

            foreach (var stageEntry in GetEntries(profile.Id, currentStageId, classId))
            {
                foreach (var itemId in GetBuildEquipmentItemIds(stageEntry.Entry, slotKind))
                {
                    if (!candidates.TryGetValue(itemId, out var candidate))
                    {
                        candidates[itemId] = new BuildCandidateAccumulator(
                            itemId,
                            currentStageId,
                            currentStageId,
                            stageEntry.Evaluation.Tier,
                            stageEntry.Entry.CategoryStrength,
                            stageEntry.Entry.Category == JournalItemCategory.ClassSpecific);
                        continue;
                    }

                    candidate.LastRelevantStageId = currentStageId;
                    candidate.Tier = stageEntry.Evaluation.Tier;
                    candidate.SortStrength = Math.Max(candidate.SortStrength, stageEntry.Entry.CategoryStrength);
                }
            }
        }

        return candidates.Values
            .OrderBy(candidate => candidate.Tier is { } tier ? JournalOrdering.GetTierOrder(tier) : int.MaxValue)
            .ThenByDescending(candidate => profile.GetStageIndex(candidate.LastRelevantStageId))
            .ThenByDescending(candidate => candidate.SortStrength)
            .ThenBy(candidate => Lang.GetItemNameValue(candidate.ItemId), StringComparer.CurrentCultureIgnoreCase)
            .Select(static candidate => new JournalBuildCandidate(candidate.ItemId))
            .ToArray();
    }

    private static IReadOnlyList<JournalBuildCandidate> BuildBuffCandidates(
        string profileId,
        string stageId,
        string classId,
        JournalBuildSlotKind slotKind)
    {
        Dictionary<int, BuildCandidateAccumulator> candidates = new();

        if (!string.Equals(profileId, JournalProfileIds.Vanilla, StringComparison.OrdinalIgnoreCase)
            || !JournalStageIds.TryToLegacy(stageId, out var legacyStage))
        {
            return [];
        }

        var legacyClass = JournalClassIds.ToLegacy(classId);
        foreach (var buffEntry in GetCombatBuffEntries(legacyStage, legacyClass))
        {
            if (!MatchesBuildBuffSlot(buffEntry, slotKind))
            {
                continue;
            }

            foreach (var itemId in buffEntry.ItemGroups.SelectMany(static group => group.ItemIds).Distinct())
            {
                if (candidates.ContainsKey(itemId))
                {
                    continue;
                }

                candidates[itemId] = new BuildCandidateAccumulator(
                    itemId,
                    JournalStageIds.FromLegacy(buffEntry.AvailableFrom),
                    stageId,
                    tier: null,
                    sortStrength: GetCombatBuffCategoryOrder(buffEntry.Category),
                    isClassSpecific: buffEntry.IsClassSpecific);
            }
        }

        return candidates.Values
            .OrderBy(candidate => candidate.SortStrength)
            .ThenByDescending(candidate => candidate.IsClassSpecific)
            .ThenBy(candidate => Lang.GetItemNameValue(candidate.ItemId), StringComparer.CurrentCultureIgnoreCase)
            .Select(static candidate => new JournalBuildCandidate(candidate.ItemId))
            .ToArray();
    }

    private static IReadOnlyList<JournalBuildCandidate> BuildFoodCandidates()
    {
        return ContentSamples.ItemsByType.Values
            .Where(IsFoodBuffItem)
            .GroupBy(static item => item.type)
            .Select(static group => group.First())
            .OrderBy(static item => GetFoodBuffSortOrder(item.buffType))
            .ThenBy(static item => Lang.GetItemNameValue(item.type), StringComparer.CurrentCultureIgnoreCase)
            .Select(static item => new JournalBuildCandidate(item.type))
            .ToArray();
    }

    private static bool IsFoodBuffItem(Item item)
    {
        return item is { IsAir: false, consumable: true, buffType: BuffID.WellFed or BuffID.WellFed2 or BuffID.WellFed3 };
    }

    private static int GetFoodBuffSortOrder(int buffType)
    {
        return buffType switch
        {
            BuffID.WellFed3 => 0,
            BuffID.WellFed2 => 1,
            BuffID.WellFed => 2,
            _ => 3
        };
    }

    private static IEnumerable<int> GetBuildEquipmentItemIds(JournalEntry entry, JournalBuildSlotKind slotKind)
    {
        return slotKind switch
        {
            JournalBuildSlotKind.PrimaryWeapon when entry is { Category: JournalItemCategory.Weapon, IsSupportWeapon: false }
                => entry.ItemGroups.SelectMany(static group => group.ItemIds),
            JournalBuildSlotKind.SupportWeapon when entry.IsSupportWeapon
                => entry.ItemGroups.SelectMany(static group => group.ItemIds),
            JournalBuildSlotKind.ClassSpecific when entry is { Category: JournalItemCategory.ClassSpecific, IsSupportWeapon: false }
                => entry.ItemGroups.SelectMany(static group => group.ItemIds),
            JournalBuildSlotKind.Accessory when entry.Category == JournalItemCategory.Accessory
                => entry.ItemGroups.SelectMany(static group => group.ItemIds),
            JournalBuildSlotKind.ArmorHead or JournalBuildSlotKind.ArmorBody or JournalBuildSlotKind.ArmorLegs
                => GetArmorPieceItemIds(entry, slotKind),
            _ => []
        };
    }

    private static IEnumerable<int> GetArmorPieceItemIds(JournalEntry entry, JournalBuildSlotKind slotKind)
    {
        if (entry.Category != JournalItemCategory.Armor)
        {
            return [];
        }

        foreach (var group in entry.ItemGroups)
        {
            if (group.ItemIds.Any(itemId => MatchesArmorPiece(itemId, slotKind)))
            {
                return group.ItemIds;
            }
        }

        var fallbackIndex = slotKind switch
        {
            JournalBuildSlotKind.ArmorHead => 0,
            JournalBuildSlotKind.ArmorBody => 1,
            JournalBuildSlotKind.ArmorLegs => 2,
            _ => -1
        };

        return fallbackIndex >= 0 && entry.ItemGroups.Count > fallbackIndex
            ? entry.ItemGroups[fallbackIndex].ItemIds
            : [];
    }

    private static bool MatchesArmorPiece(int itemId, JournalBuildSlotKind slotKind)
    {
        if (!ContentSamples.ItemsByType.TryGetValue(itemId, out var item) || item is null)
        {
            return false;
        }

        return slotKind switch
        {
            JournalBuildSlotKind.ArmorHead => item.headSlot >= 0,
            JournalBuildSlotKind.ArmorBody => item is { bodySlot: >= 0, headSlot: < 0 },
            JournalBuildSlotKind.ArmorLegs => item is { legSlot: >= 0, headSlot: < 0, bodySlot: < 0 },
            _ => false
        };
    }

    private static bool MatchesBuildBuffSlot(JournalCombatBuffEntry buffEntry, JournalBuildSlotKind slotKind)
    {
        return slotKind switch
        {
            JournalBuildSlotKind.Potion => buffEntry.Category is JournalBuffCategory.Basic or JournalBuffCategory.Potion or JournalBuffCategory.Flask,
            JournalBuildSlotKind.Food => buffEntry.Category == JournalBuffCategory.Food,
            _ => false
        };
    }

    private static bool IsValidModBuildCandidate(string profileId, string classId, JournalBuildSlotKind slotKind, int itemId)
    {
        return ContentSamples.ItemsByType.TryGetValue(itemId, out var item)
            && IsModBuildCandidate(profileId, classId, slotKind, item);
    }

    private static bool IsModBuildCandidate(string profileId, string classId, JournalBuildSlotKind slotKind, Item item)
    {
        if (item.type <= ItemID.None || item.IsAir || item.ModItem is null)
        {
            return false;
        }

        return slotKind switch
        {
            JournalBuildSlotKind.PrimaryWeapon or JournalBuildSlotKind.SupportWeapon => IsWeaponItem(item),
            JournalBuildSlotKind.ClassSpecific => IsClassSpecificModItem(profileId, classId, item),
            JournalBuildSlotKind.ArmorHead => item.headSlot >= 0,
            JournalBuildSlotKind.ArmorBody => item is { bodySlot: >= 0, headSlot: < 0 },
            JournalBuildSlotKind.ArmorLegs => item is { legSlot: >= 0, headSlot: < 0, bodySlot: < 0 },
            JournalBuildSlotKind.Accessory => item.accessory,
            JournalBuildSlotKind.Potion => IsBuffConsumable(item) && !IsFoodBuffItem(item),
            JournalBuildSlotKind.Food => IsFoodBuffItem(item),
            _ => false
        };
    }

    private static bool IsClassSpecificModItem(string profileId, string classId, Item item)
    {
        if (JournalProfileRegistry.TryGet(profileId, out var profile))
        {
            var classDefinition = profile.GetClass(classId);
            var damageClassType = item.DamageType.GetType();
            if (classDefinition.DamageClassNames.Any(
                name => string.Equals(name, damageClassType.Name, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name, damageClassType.FullName, StringComparison.OrdinalIgnoreCase)))
            {
                return IsWeaponItem(item);
            }
        }

        var combatClass = JournalClassIds.ToLegacy(classId);
        return combatClass switch
        {
            CombatClass.Ranged => item.ammo > AmmoID.None || item.useAmmo > AmmoID.None,
            CombatClass.Summoner => IsWeaponItem(item) && item.CountsAsClass(DamageClass.Summon),
            CombatClass.Magic => IsWeaponItem(item) && (item.CountsAsClass(DamageClass.Magic) || item.mana > 0),
            CombatClass.Melee => IsWeaponItem(item) && item.CountsAsClass(DamageClass.Melee),
            _ => IsWeaponItem(item)
        };
    }

    private static bool IsWeaponItem(Item item)
    {
        return item is { damage: > 0, accessory: false, headSlot: < 0, bodySlot: < 0, legSlot: < 0 };
    }

    private static bool IsBuffConsumable(Item item)
    {
        return item is { consumable: true, buffType: > 0 };
    }
}
