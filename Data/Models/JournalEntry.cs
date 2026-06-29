using Terraria.ID;

namespace ProgressionJournal.Data.Models;

public sealed class JournalEntry
{
	private readonly Dictionary<string, StageEvaluation> _evaluations;
	private readonly HashSet<string> _classIds;

	public JournalEntry(
		string key,
		JournalItemCategory category,
		CombatClass classes,
		IEnumerable<int> itemIds,
		IEnumerable<StageEvaluation> evaluations,
		JournalEventCategory? eventCategory = null,
		bool isSupportWeapon = false)
		: this(
			key,
			category,
			JournalClassIds.FromLegacyFlags(classes),
			itemIds.Select(itemId => new JournalItemGroup([itemId])),
			evaluations,
			eventCategory,
			isSupportWeapon)
	{
	}

	public JournalEntry(
		string key,
		JournalItemCategory category,
		CombatClass classes,
		IEnumerable<JournalItemGroup> itemGroups,
		IEnumerable<StageEvaluation> evaluations,
		JournalEventCategory? eventCategory = null,
		bool isSupportWeapon = false)
		: this(
			key,
			category,
			JournalClassIds.FromLegacyFlags(classes),
			itemGroups,
			evaluations,
			eventCategory,
			isSupportWeapon)
	{
	}

	public JournalEntry(
		string key,
		JournalItemCategory category,
		IEnumerable<string> classIds,
		IEnumerable<JournalItemGroup> itemGroups,
		IEnumerable<StageEvaluation> evaluations,
		JournalEventCategory? eventCategory = null,
		bool isSupportWeapon = false,
		string? customEventName = null,
		string? eventIcon = null,
		IEnumerable<JournalWikiRecommendation>? wikiRecommendations = null,
		IEnumerable<JournalFishingSource>? fishingSources = null)
	{
		Key = key;
		Category = category;
		_classIds = classIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
		Classes = GetLegacyClasses(_classIds);
		EventCategory = eventCategory;
		CustomEventName = customEventName?.Trim() ?? string.Empty;
		EventIcon = eventIcon?.Trim() ?? string.Empty;
		IsSupportWeapon = isSupportWeapon;
		ItemGroups = itemGroups.ToArray();

		if (ItemGroups.Count == 0) {
			throw new ArgumentException("A journal entry must contain at least one item group.", nameof(itemGroups));
		}

		ItemIds = ItemGroups.SelectMany(group => group.ItemIds).ToArray();
		RepresentativeItemId = ItemGroups[0].RepresentativeItemId;
		CategoryStrength = ComputeCategoryStrength(category, ItemGroups, ItemIds);
		_evaluations = evaluations.ToDictionary(evaluation => evaluation.StageId, StringComparer.OrdinalIgnoreCase);
		WikiRecommendations = wikiRecommendations?.ToArray() ?? [];
		FishingSources = fishingSources?.ToArray() ?? [];
	}

	public string Key { get; }

	public JournalItemCategory Category { get; }

	private CombatClass Classes { get; }

	public IReadOnlySet<string> ClassIds => _classIds;

	public JournalEventCategory? EventCategory { get; }

	public string CustomEventName { get; }

	public string EventIcon { get; }

	public bool IsSupportWeapon { get; }

	public IReadOnlyList<JournalItemGroup> ItemGroups { get; }

	public IReadOnlyList<int> ItemIds { get; }

	public int RepresentativeItemId { get; }

	public int CategoryStrength { get; }

	public IReadOnlyList<JournalWikiRecommendation> WikiRecommendations { get; }

	public IReadOnlyList<JournalFishingSource> FishingSources { get; }

	public IReadOnlyCollection<StageEvaluation> Evaluations => _evaluations.Values;

	public bool AppliesToClass(CombatClass combatClass) => (Classes & combatClass) != 0;

	public bool AppliesToClass(string classId) => _classIds.Contains(classId);

	private bool TryGetEvaluation(ProgressionStageId stageId, out StageEvaluation evaluation)
	{
		return TryGetEvaluation(
			JournalProfileIds.Vanilla,
			JournalStageIds.FromLegacy(stageId),
			out evaluation);
	}

	public bool TryGetEvaluation(string profileId, string stageId, out StageEvaluation evaluation)
	{
		if (_evaluations.TryGetValue(stageId, out evaluation!)) {
			return true;
		}

		var hasProfile = JournalProfileRegistry.TryGet(profileId, out var profile);
		var targetIndex = hasProfile
			? profile.GetStageIndex(stageId)
			: GetLegacyStageIndex(stageId);
		StageEvaluation? nearestPreviousEvaluation = null;
		var nearestPreviousIndex = -1;
		var hasLaterEvaluation = false;

		foreach (var pair in _evaluations) {
			var evaluationIndex = hasProfile
				? profile.GetStageIndex(pair.Key)
				: GetLegacyStageIndex(pair.Key);

			if (evaluationIndex < targetIndex && evaluationIndex > nearestPreviousIndex) {
				nearestPreviousEvaluation = pair.Value;
				nearestPreviousIndex = evaluationIndex;
				continue;
			}

			if (evaluationIndex > targetIndex) {
				hasLaterEvaluation = true;
			}
		}

		if (nearestPreviousEvaluation is not null
			&& nearestPreviousEvaluation.Scope == JournalEvaluationScope.UntilNext
			&& hasLaterEvaluation) {
			evaluation = nearestPreviousEvaluation;
			return true;
		}

		evaluation = null!;
		return false;
	}

	public StageEvaluation GetEvaluation(ProgressionStageId stageId)
	{
		return TryGetEvaluation(stageId, out var evaluation) ? evaluation : throw new KeyNotFoundException($"No evaluation exists for stage '{stageId}' in entry '{Key}'.");
	}

	public StageEvaluation GetEvaluation(string profileId, string stageId)
	{
		return TryGetEvaluation(profileId, stageId, out var evaluation)
			? evaluation
			: throw new KeyNotFoundException($"No evaluation exists for stage '{stageId}' in entry '{Key}'.");
	}

	public string GetDisplayName() => string.Join(" + ", ItemGroups.Select(group => group.GetDisplayName()));

	private static int ComputeCategoryStrength(
		JournalItemCategory category,
		IReadOnlyList<JournalItemGroup> itemGroups,
		IReadOnlyList<int> itemIds) => category switch
	{
		JournalItemCategory.Weapon or JournalItemCategory.Ammunition => GetWeaponStrength(itemIds),
		JournalItemCategory.Armor => GetArmorStrength(itemGroups),
		_ => 0
	};

	private static int GetWeaponStrength(IReadOnlyList<int> itemIds)
	{
		var bestDamage = 0;

		foreach (var itemId in itemIds) {
			if (ContentSamples.ItemsByType.TryGetValue(itemId, out var item) && item is not null) {
				bestDamage = Math.Max(bestDamage, item.damage);
			}
		}

		return bestDamage;
	}

	private static int GetArmorStrength(IReadOnlyList<JournalItemGroup> itemGroups)
	{
		var totalDefense = 0;

		foreach (var group in itemGroups) {
			var bestDefenseInGroup = 0;

			foreach (var itemId in group.ItemIds) {
				if (ContentSamples.ItemsByType.TryGetValue(itemId, out var item) && item is not null) {
					bestDefenseInGroup = Math.Max(bestDefenseInGroup, item.defense);
				}
			}

			totalDefense += bestDefenseInGroup;
		}

		return totalDefense;
	}

	private static CombatClass GetLegacyClasses(IEnumerable<string> classIds)
	{
		return classIds.Aggregate(CombatClass.None, (current, classId) => current | JournalClassIds.ToLegacy(classId));
	}

	private static int GetLegacyStageIndex(string stageId)
	{
		return JournalStageIds.TryToLegacy(stageId, out var legacyStage)
			? ProgressionStageCatalog.GetStageOrderIndex(legacyStage)
			: 0;
	}
}
