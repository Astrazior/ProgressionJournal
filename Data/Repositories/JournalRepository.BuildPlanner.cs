using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;

namespace ProgressionJournal.Data.Repositories;

public static partial class JournalRepository
{
    private sealed class BuildCandidateAccumulator(
        int itemId,
        ProgressionStageId availableFromStageId,
        ProgressionStageId lastRelevantStageId,
        RecommendationTier? tier,
        int sortStrength,
        bool isClassSpecific)
    {
        public int ItemId { get; } = itemId;

        public ProgressionStageId AvailableFromStageId { get; } = availableFromStageId;

        public ProgressionStageId LastRelevantStageId { get; set; } = lastRelevantStageId;

        public RecommendationTier? Tier { get; set; } = tier;

        public int SortStrength { get; set; } = sortStrength;

        public bool IsClassSpecific { get; } = isClassSpecific;
    }

    public static IReadOnlyList<JournalBuildCandidate> GetBuildCandidates(
        ProgressionStageId stageId,
        CombatClass combatClass,
        string slotKey)
    {
        if (!JournalBuildPlannerCatalog.TryGetSlotKind(slotKey, out var slotKind))
        {
            return [];
        }

        return slotKind switch
        {
            JournalBuildSlotKind.Potion or JournalBuildSlotKind.Food or JournalBuildSlotKind.PermanentBonus
                => BuildBuffCandidates(stageId, combatClass, slotKind),
            _ => BuildEquipmentCandidates(stageId, combatClass, slotKind)
        };
    }

    public static bool IsBuildSelectionValid(
        ProgressionStageId stageId,
        CombatClass combatClass,
        string slotKey,
        int itemId)
    {
        return GetBuildCandidates(stageId, combatClass, slotKey).Any(candidate => candidate.ItemId == itemId);
    }

    private static IReadOnlyList<JournalBuildCandidate> BuildEquipmentCandidates(
        ProgressionStageId stageId,
        CombatClass combatClass,
        JournalBuildSlotKind slotKind)
    {
        var targetIndex = ProgressionStageCatalog.GetStageOrderIndex(stageId);
        Dictionary<int, BuildCandidateAccumulator> candidates = new();

        foreach (var progressionStage in ProgressionStageCatalog.All)
        {
            var currentStageId = progressionStage.Id;
            if (ProgressionStageCatalog.GetStageOrderIndex(currentStageId) > targetIndex)
            {
                break;
            }

            foreach (var stageEntry in GetEntries(currentStageId, combatClass))
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
            .ThenByDescending(candidate => ProgressionStageCatalog.GetStageOrderIndex(candidate.LastRelevantStageId))
            .ThenByDescending(candidate => candidate.SortStrength)
            .ThenBy(candidate => Lang.GetItemNameValue(candidate.ItemId), StringComparer.CurrentCultureIgnoreCase)
            .Select(static candidate => new JournalBuildCandidate(candidate.ItemId))
            .ToArray();
    }

    private static IReadOnlyList<JournalBuildCandidate> BuildBuffCandidates(
        ProgressionStageId stageId,
        CombatClass combatClass,
        JournalBuildSlotKind slotKind)
    {
        Dictionary<int, BuildCandidateAccumulator> candidates = new();

        foreach (var buffEntry in GetCombatBuffEntries(stageId, combatClass))
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
                    buffEntry.AvailableFrom,
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

    private static IEnumerable<int> GetBuildEquipmentItemIds(JournalEntry entry, JournalBuildSlotKind slotKind)
    {
        return slotKind switch
        {
            JournalBuildSlotKind.PrimaryWeapon when entry.Category == JournalItemCategory.Weapon && !entry.IsSupportWeapon
                => entry.ItemGroups.SelectMany(static group => group.ItemIds),
            JournalBuildSlotKind.SupportWeapon when entry.IsSupportWeapon
                => entry.ItemGroups.SelectMany(static group => group.ItemIds),
            JournalBuildSlotKind.ClassSpecific when entry.Category == JournalItemCategory.ClassSpecific && !entry.IsSupportWeapon
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
            JournalBuildSlotKind.ArmorBody => item.bodySlot >= 0 && item.headSlot < 0,
            JournalBuildSlotKind.ArmorLegs => item.legSlot >= 0 && item.headSlot < 0 && item.bodySlot < 0,
            _ => false
        };
    }

    private static bool MatchesBuildBuffSlot(JournalCombatBuffEntry buffEntry, JournalBuildSlotKind slotKind)
    {
        return slotKind switch
        {
            JournalBuildSlotKind.Potion => buffEntry.Category is JournalBuffCategory.Basic or JournalBuffCategory.Potion or JournalBuffCategory.Flask,
            JournalBuildSlotKind.Food => buffEntry.Category == JournalBuffCategory.Food,
            JournalBuildSlotKind.PermanentBonus => buffEntry.Category is JournalBuffCategory.Station or JournalBuffCategory.Passive or JournalBuffCategory.Eternal,
            _ => false
        };
    }
}
