import fs from "node:fs";
import path from "node:path";
import { createHash } from "node:crypto";
import { createSnapshotView } from "./KnowledgeBase.mjs";
import { applyVanillaSourceCatalog } from "./VanillaSourceCatalog.mjs";
import { resolveSnapshotStageIndex } from "./SnapshotStageResolver.mjs";

const STANDARD_COMBAT_CLASS_IDS = new Set(["melee", "ranged", "magic", "summoner"]);

export function readJson(file) {
  return JSON.parse(fs.readFileSync(file, "utf8"));
}

export function writeJson(file, value) {
  fs.mkdirSync(path.dirname(file), { recursive: true });
  fs.writeFileSync(file, `${JSON.stringify(value, null, 2)}\n`, "utf8");
}

export function generateProfile(
  source,
  manifest,
  wikiProfile = null,
  manualAssignments = null) {
  const automaticEventPriority = prioritizeObservedEventAvailability(source, manifest);
  manifest = automaticEventPriority.manifest;
  if (!hasManualAvailabilityAssignments(manualAssignments)) {
    const result = generateProfileCore(source, manifest, wikiProfile, manualAssignments);
    result.report.automaticEventPriority = automaticEventPriority.report;
    return result;
  }

  const automaticAssignments = withoutManualAvailabilityAssignments(manualAssignments);
  const automaticResult = generateProfileCore(
    source,
    manifest,
    wikiProfile,
    automaticAssignments);
  const priority = prioritizeAutomaticAvailability(
    source,
    manifest,
    manualAssignments,
    automaticResult.report);
  const result = generateProfileCore(
    source,
    priority.manifest,
    wikiProfile,
    priority.manualAssignments);
  result.report.manualAvailabilityPriority = priority.report;
  result.report.automaticEventPriority = automaticEventPriority.report;
  return result;
}

function prioritizeObservedEventAvailability(source, sourceManifest) {
  const snapshot = source?.format === "ProgressionJournalKnowledge"
    ? createSnapshotView(source)
    : source;
  const manifest = structuredClone(sourceManifest);
  const stageIndexes = new Map(
    (manifest.stages ?? []).map((stage, index) => [stage.id, index]));
  const observedAvailability = new Map();
  for (const record of snapshot?.npcAvailability ?? []) {
    if (!record.observed || !record.npc) continue;
    const stageIndex = resolveObservedStageIndex(
      record,
      manifest,
      stageIndexes,
      `event source ${record.npc}`);
    if (stageIndex < 0 || !manifest.stages[stageIndex]) continue;
    const existing = observedAvailability.get(record.npc);
    if (!existing || stageIndex < existing.stageIndex) {
      observedAvailability.set(record.npc, { record, stageIndex });
    }
  }

  manifest.sourceStageFloors ??= {};
  const corrections = [];
  for (const event of manifest.events ?? []) {
    const declaredStageIndex = stageIndexes.get(event.stageId);
    if (declaredStageIndex === undefined) continue;
    const spawnSources = [
      ...(event.dropSources ?? []),
      ...(event.enemies ?? [])
    ];
    const observedSpawnStages = spawnSources
      .map(sourceId => observedAvailability.get(sourceId))
      .filter(value => value?.record.kind === "spawn")
      .map(value => value.stageIndex);
    if (observedSpawnStages.length === 0) continue;
    const observedEventStageIndex = Math.min(...observedSpawnStages);
    if (observedEventStageIndex >= declaredStageIndex) continue;

    const declaredStageId = event.stageId;
    event.stageId = manifest.stages[observedEventStageIndex].id;
    const deferredSources = [];
    for (const property of ["dropSources", "enemies", "containers", "shops"]) {
      for (const sourceId of event[property] ?? []) {
        const observed = observedAvailability.get(sourceId);
        const existingFloorIndex = stageIndexes.get(manifest.sourceStageFloors[sourceId]);
        const sourceStageIndex = observed?.stageIndex
          ?? existingFloorIndex
          ?? declaredStageIndex;
        const sourceStageId = manifest.stages[sourceStageIndex]?.id;
        if (!sourceStageId) continue;
        manifest.sourceStageFloors[sourceId] = sourceStageId;
        if (sourceStageIndex <= observedEventStageIndex) continue;
        const stage = manifest.stages[sourceStageIndex];
        stage[property] = [...new Set([...(stage[property] ?? []), sourceId])];
        deferredSources.push({ source: sourceId, stageId: sourceStageId });
      }
    }
    corrections.push({
      eventId: event.id ?? "",
      eventCategory: event.eventCategory ?? null,
      declaredStageId,
      automaticStageId: event.stageId,
      deferredSources
    });
  }

  return {
    manifest,
    report: {
      corrections
    }
  };
}

function generateProfileCore(
  source,
  manifest,
  wikiProfile = null,
  manualAssignments = null) {
  const snapshot = source?.format === "ProgressionJournalKnowledge"
    ? createSnapshotView(source)
    : source;
  assert(snapshot.format === "ProgressionJournalSnapshot", "Invalid snapshot format.");
  assert(
    [4, 5, 6].includes(snapshot.version),
    `Unsupported snapshot version '${snapshot.version}'.`);

  manifest = applyVanillaSourceCatalog(manifest, snapshot);
  const manualResult = applyManualAssignments(manifest, manualAssignments);
  manifest = manualResult.manifest;
  const contentMods = new Set(
    manifest.contentMods
    ?? (manifest.requiredMods ?? []).map(requiredMod => requiredMod.name));
  const stageIndexes = new Map(manifest.stages.map((stage, index) => [stage.id, index]));
  const snapshotStageCorrections = new Map();
  const normalizeObservedStage = (record, label) => {
    const earliestStageIndex = resolveObservedStageIndex(
      record,
      manifest,
      stageIndexes,
      label);
    if (earliestStageIndex !== record.earliestStageIndex) {
      const correctionKey = `${record.earliestStageIndex}:${earliestStageIndex}`;
      const correction = snapshotStageCorrections.get(correctionKey) ?? {
        snapshotStageIndex: record.earliestStageIndex,
        resolvedStageId: manifest.stages[earliestStageIndex]?.id ?? "",
        resolvedStageIndex: earliestStageIndex,
        records: 0,
        examples: []
      };
      correction.records++;
      if (correction.examples.length < 3) correction.examples.push(label);
      snapshotStageCorrections.set(correctionKey, correction);
    }
    return { ...record, earliestStageIndex };
  };
  const snapshotShops = (snapshot.shops ?? []).map((record, index) =>
    normalizeObservedStage(record, `shop[${index}] ${record.npc}/${record.item}`));
  const snapshotFishing = (snapshot.fishing ?? []).map((record, index) =>
    normalizeObservedStage(record, `fishing[${index}] ${record.target}`));
  const snapshotShimmerTransforms = (snapshot.shimmerTransforms ?? [])
    .filter(transform =>
      isAllowedProfileItem(transform.input, contentMods)
      && isAllowedProfileItem(transform.output, contentMods));
  const snapshotNpcAvailability = (snapshot.npcAvailability ?? []).map((record, index) =>
    normalizeObservedStage(record, `npcAvailability[${index}] ${record.npc}`));
  const allowedItems = snapshot.items.filter(item => isAllowedProfileItem(item.id, contentMods));
  const itemById = new Map(snapshot.items.map(item => [item.id, item]));
  const recipesByResult = groupBy(
    snapshot.recipes.filter(recipe =>
      isAllowedProfileItem(recipe.result, contentMods)
      && recipe.ingredients.every(ingredient =>
        isAllowedProfileItem(ingredient.item, contentMods))
      && recipe.stations.every(station =>
        isAllowedProfileItem(station, contentMods))),
    recipe => recipe.result);
  const dropsBySource = groupBy(
    snapshot.drops.filter(drop =>
      drop.sourceType !== "global"
      &&
      (drop.rate ?? 1) > 0
      &&
      isAllowedProfileItem(drop.item, contentMods)
      && isAllowedProfileItem(drop.source, contentMods)),
    drop => drop.source);
  const globalDrops = snapshot.drops.filter(drop =>
    drop.sourceType === "global"
    && (drop.rate ?? 1) > 0
    && isAllowedProfileItem(drop.item, contentMods));
  const containerDropsBySource = groupBy(
    snapshot.drops.filter(drop =>
      drop.sourceType === "container"
      && (drop.rate ?? 1) > 0
      && isAllowedProfileItem(drop.item, contentMods)
      && isAllowedProfileItem(drop.source, contentMods)),
    drop => drop.source);
  const shopsByNpc = groupBy(
    snapshotShops.filter(shop =>
      isAllowedProfileItem(shop.item, contentMods)
      && isAllowedProfileItem(shop.npc, contentMods)),
    shop => shop.npc);
  const fishingByItem = groupBy(
    snapshotFishing.filter(record =>
      record.targetType === "item"
      && isAllowedProfileItem(record.target, contentMods)),
    record => record.target);
  const acquiredBy = new Map();
  const available = new Set(manifest.initialItems ?? []);
  const availableStations = new Set(manifest.initialStations ?? []);
  const availableDropSources = new Set();
  const availableShops = new Set();
  const legacyAvailableShops = new Set();
  const sourceStageFloors = collectSourceStageFloors(manifest, stageIndexes);
  const eventsByCategory = groupBy(
    (manifest.events ?? []).filter(event =>
      event.eventCategory && stageIndexes.has(event.stageId)),
    event => event.eventCategory);
  const sourceAvailabilityCorrections = [];
  const observedNpcAvailability = snapshotNpcAvailability
    .filter(record =>
      record.observed
      && stageIndexes.has(manifest.stages[record.earliestStageIndex]?.id)
      && isAllowedProfileItem(record.npc, contentMods))
    .map(record => {
      const sourceFloorIndex = sourceStageFloors.get(record.npc) ?? -1;
      const eventFloorIndexes = (record.eventCategories ?? [])
        .flatMap(category => (eventsByCategory.get(category) ?? [])
          .map(event => stageIndexes.get(event.stageId)));
      const eventFloorIndex = eventFloorIndexes.length > 0
        ? Math.min(...eventFloorIndexes)
        : -1;
      const floorIndex = Math.max(sourceFloorIndex, eventFloorIndex);
      const effectiveStageIndex = Math.max(record.earliestStageIndex, floorIndex);
      if (effectiveStageIndex > record.earliestStageIndex) {
        sourceAvailabilityCorrections.push({
          source: record.npc,
          observedStageId: manifest.stages[record.earliestStageIndex]?.id ?? "",
          effectiveStageId: manifest.stages[effectiveStageIndex]?.id ?? ""
        });
      }

      const matchedEvent = (record.eventCategories ?? [])
        .flatMap(category => eventsByCategory.get(category) ?? [])
        .filter(event => (stageIndexes.get(event.stageId) ?? Number.MAX_SAFE_INTEGER)
          <= effectiveStageIndex)
        .sort((left, right) =>
          (stageIndexes.get(right.stageId) ?? -1) - (stageIndexes.get(left.stageId) ?? -1))[0];
      return {
        ...record,
        earliestStageIndex: effectiveStageIndex,
        earliestStageName: manifest.stages[effectiveStageIndex]?.name?.["en-US"] ?? "",
        eventMetadata: matchedEvent ? toEventMetadata(matchedEvent) : null
      };
    });
  const observedSourceEventMetadata = new Map(observedNpcAvailability
    .filter(record => record.kind === "spawn" && record.eventMetadata)
    .map(record => [record.npc, record.eventMetadata]));
  const manifestEventsBySource = new Map();
  for (const event of manifest.events ?? []) {
    for (const source of [
      ...(event.dropSources ?? []),
      ...(event.enemies ?? []),
      ...(event.containers ?? []),
      ...(event.shops ?? [])
    ]) {
      const events = manifestEventsBySource.get(source) ?? [];
      events.push(event);
      manifestEventsBySource.set(source, events);
    }
  }
  const eventMetadataForSource = (source, stageIndex) => {
    const observedMetadata = observedSourceEventMetadata.get(source);
    if (observedMetadata) return observedMetadata;
    const event = (manifestEventsBySource.get(source) ?? [])
      .filter(value => (stageIndexes.get(value.stageId) ?? Number.MAX_SAFE_INTEGER) <= stageIndex)
      .sort((left, right) =>
        (stageIndexes.get(right.stageId) ?? -1) - (stageIndexes.get(left.stageId) ?? -1))[0];
    return event ? toEventMetadata(event) : {};
  };
  const itemStageFloors = new Map(
    Object.entries(manifest.itemStageFloors ?? {})
      .filter(([, stageId]) => stageIndexes.has(stageId))
      .map(([id, stageId]) => [id, stageIndexes.get(stageId)]));
  const stationStageFloors = new Map(
    Object.entries(manifest.stationStageFloors ?? {})
      .filter(([, stageId]) => stageIndexes.has(stageId))
      .map(([id, stageId]) => [id, stageIndexes.get(stageId)]));
  const stationAllowedAtStage = (id, stage) => {
    const floorIndex = stationStageFloors.get(id);
    return floorIndex === undefined || (stageIndexes.get(stage) ?? -1) >= floorIndex;
  };
  const sourceAllowedAtStage = (id, stage) => {
    const floorIndex = sourceStageFloors.get(id);
    return floorIndex === undefined || (stageIndexes.get(stage) ?? -1) >= floorIndex;
  };
  const unlockAtStage = (id, stage, via, metadata = {}) => {
    const floorIndex = itemStageFloors.get(id);
    if (floorIndex !== undefined && (stageIndexes.get(stage) ?? -1) < floorIndex) {
      return false;
    }
    return unlock(id, stage, via, available, acquiredBy, metadata);
  };
  const report = {
    profileId: manifest.id,
    generatedAtUtc: new Date().toISOString(),
    unknownReferences: [],
    unresolvedConditions: [],
    ambiguousClasses: [],
    excludedItems: [],
    emptyStages: [],
    paths: {},
    wikiMissingItems: [],
    wikiResolvedItems: [],
    wikiAmbiguousItems: [],
    wikiUnresolvedItems: [],
    wikiAvailabilityCorrections: [],
    snapshotStageCorrections: [...snapshotStageCorrections.values()],
    sourceAvailabilityCorrections,
    staleRules: [],
    unassignedVanillaNpcSources: [],
    unavailableCombatItems: [],
    unresolvedAvailabilityItems: [],
    manualAssignmentProblems: manualResult.problems
  };
  Object.defineProperty(manifest, "_stageIndexes", { value: stageIndexes });
  Object.defineProperty(manifest, "_itemNameIndex", { value: createItemNameIndex(allowedItems) });
  Object.defineProperty(report, "_unresolvedConditionSignatures", { value: new Set() });
  const usedStations = new Set(
    snapshot.recipes
      .filter(recipe => isAllowedProfileItem(recipe.result, contentMods))
      .flatMap(recipe => recipe.stations));
  const wikiResolver = createWikiReferenceResolver(allowedItems, report);
  const classificationContext = {
    usedStations,
    items: allowedItems,
    wikiItems: createWikiClassificationMap(wikiProfile, wikiResolver),
    vanillaItems: new Map(
      (snapshot.vanillaItemClassifications ?? [])
        .map(value => [value.item, value])),
    vanillaBuffs: new Map(
      (snapshot.vanillaBuffClassifications ?? [])
        .map(value => [value.item, value])),
    shimmerOutputs: new Set(snapshotShimmerTransforms.map(staticTransform => staticTransform.output))
  };
  const wikiCompatibility = sourceCompatibility(wikiProfile, snapshot);
  report.officialSourceCompatibility = {
    recommendations: wikiCompatibility
  };

  for (const id of available) acquiredBy.set(id, { stage: "start", via: "initial" });
  const profileEntries = [];
  const profileBuffs = [];
  const entryByItem = new Map();
  const buffItems = new Set();
  unlockPlacedStations(
    available,
    itemById,
    availableStations,
    station => stationAllowedAtStage(station, "start"));

  for (const stage of manifest.stages) {
    const stageIndex = stageIndexes.get(stage.id);
    const before = new Set(available);
    if (stageIndex === 0) {
      for (const id of manifest.initialVisibleItems ?? []) before.delete(id);
    }
    for (const availability of observedNpcAvailability) {
      if (availability.earliestStageIndex !== stageIndex) continue;
      if (availability.kind === "town") {
        availableShops.add(availability.npc);
        availableDropSources.add(availability.npc);
      } else if (availability.kind === "spawn") {
        availableDropSources.add(availability.npc);
      }
    }
    const stageSources = [
      ...(stage.dropSources ?? []),
      ...(stage.enemies ?? []),
      ...(stage.containers ?? [])
    ];
    for (const source of stageSources) {
      if (sourceAllowedAtStage(source, stage.id)) availableDropSources.add(source);
    }
    for (const npc of stage.shops ?? []) {
      if (!sourceAllowedAtStage(npc, stage.id)) continue;
      availableShops.add(npc);
      availableDropSources.add(npc);
      legacyAvailableShops.add(npc);
    }

    for (const source of availableDropSources) {
      if (!sourceAllowedAtStage(source, stage.id)) continue;
      for (const drop of dropsBySource.get(source) ?? []) {
        if (conditionsAllowed(drop.conditions, stage, manifest, report, {
          sourceKind: "drop",
          source,
          item: drop.item,
          available
        })) {
          unlockAtStage(
            drop.item,
            stage.id,
            `${drop.sourceType}:${source}`,
            eventMetadataForSource(source, stageIndex));
        }
      }
    }
    for (const event of (manifest.events ?? []).filter(event => event.stageId === stage.id)) {
      const eventSources = [
        ...(event.dropSources ?? []),
        ...(event.enemies ?? []),
        ...(event.containers ?? [])
      ];
      for (const source of eventSources) {
        if (!sourceAllowedAtStage(source, stage.id)) continue;
        availableDropSources.add(source);
        for (const drop of dropsBySource.get(source) ?? []) {
          if (conditionsAllowed(drop.conditions, stage, manifest, report, {
            sourceKind: "drop",
            source,
            item: drop.item,
            available
          })) {
            unlockAtStage(
              drop.item,
              stage.id,
              `${drop.sourceType}:${source}`,
              {
                eventCategory: event.eventCategory ?? null,
                customEventName: event.customEventName ?? "",
                eventIcon: event.eventIcon ?? ""
              });
          }
        }
      }
    }
    for (const npc of availableShops) {
      if (!sourceAllowedAtStage(npc, stage.id)) continue;
      for (const shop of shopsByNpc.get(npc) ?? []) {
        if ((!shop.observed && !legacyAvailableShops.has(npc))
            || (shop.observed && shop.earliestStageIndex > stageIndex)) {
          continue;
        }
        if (conditionsAllowed(shop.conditions, stage, manifest, report, {
          sourceKind: "shop",
          source: npc,
          item: shop.item,
          available
        })) {
          unlockAtStage(
            shop.item,
            stage.id,
            `shop:${npc}`,
            eventMetadataForSource(npc, stageIndex));
        }
      }
    }
    for (const drop of globalDrops) {
      if (!sourceAllowedAtStage(drop.source, stage.id)) continue;
      if (conditionsAllowed(drop.conditions, stage, manifest, report, {
        sourceKind: "drop",
        source: drop.source,
        item: drop.item,
        available
      })) {
        unlockAtStage(drop.item, stage.id, `global:${drop.source}`);
      }
    }
    for (const [itemId, catches] of fishingByItem) {
      const observedCatch = catches.find(catchRecord => {
        const catchStageIndex = fishingCatchStageIndex(catchRecord, manifest, stageIndexes);
        return catchStageIndex >= 0 && catchStageIndex <= stageIndex;
      });
      if (observedCatch) {
        unlockAtStage(itemId, stage.id, "fishing");
      }
    }
    for (const id of stage.materials ?? []) {
      unlockAtStage(id, stage.id, "manifest");
    }
    for (const station of stage.stations ?? []) {
      if (stationAllowedAtStage(station, stage.id)) availableStations.add(station);
    }

    const unlockTransitiveSources = () => {
      let changed = true;
      while (changed) {
        changed = false;
        if (unlockPlacedStations(
          available,
          itemById,
          availableStations,
          station => stationAllowedAtStage(station, stage.id))) {
          changed = true;
        }
        for (const container of [...available]) {
          if (!sourceAllowedAtStage(container, stage.id)) continue;
          for (const drop of containerDropsBySource.get(container) ?? []) {
            if (!conditionsAllowed(drop.conditions, stage, manifest, report, {
              sourceKind: "drop",
              source: container,
              item: drop.item,
              available
            })) {
              continue;
            }
            if (!available.has(drop.item)) {
              if (unlockAtStage(
                drop.item,
                stage.id,
                `container:${container}`,
                inheritedContainerEventMetadata(container, stage.id, acquiredBy))) {
                changed = true;
              }
            }
          }
        }
        for (const [result, recipes] of recipesByResult) {
          if (available.has(result)) continue;
          const openRecipe = recipes.find(recipe =>
            recipe.ingredients.every(ingredient => available.has(ingredient.item))
            && recipe.stations.every(station => availableStations.has(station))
            && conditionsAllowed(recipe.conditions, stage, manifest, report, {
              sourceKind: "recipe",
              source: result,
              item: result,
              available
            }));
          if (!openRecipe) continue;

          if (!unlockAtStage(
            result,
            stage.id,
            `recipe:${openRecipe.ingredients.map(value => value.item).join("+")}`)) {
            continue;
          }
          const item = itemById.get(result);
          if (item?.placedTile && stationAllowedAtStage(item.placedTile, stage.id)) {
            availableStations.add(item.placedTile);
          }
          changed = true;
        }
        if (available.has("Terraria/ShimmerBlock")) {
          for (const transform of snapshotShimmerTransforms) {
            if (available.has(transform.output) || !available.has(transform.input)) continue;
            if (unlockAtStage(
              transform.output,
              stage.id,
              `shimmer:${transform.input}`)) {
              changed = true;
            }
          }
        }
      }
    };

    unlockTransitiveSources();
    for (const id of stage.include ?? []) {
      unlockAtStage(id, stage.id, "manifest");
    }
    unlockTransitiveSources();

    for (const id of stage.exclude ?? []) available.delete(id);
    const delta = [...available].filter(id => !before.has(id));
    let visibleCount = 0;
    for (const id of delta) {
      const item = itemById.get(id);
      if (!item) {
        report.unknownReferences.push({ stage: stage.id, id });
        continue;
      }
      const classification = classifyItem(item, manifest, report, classificationContext);
      if (!classification) {
        report.excludedItems.push({ stage: stage.id, id, reason: "not combat equipment" });
        continue;
      }

      const classes = classification.classes;
      const itemReference = toItemReference(item);
      if (classification.buffCategory) {
        profileBuffs.push({
          key: `new.${slug(id)}`,
          category: classification.buffCategory,
          classes,
          stageId: stage.id,
          itemGroups: [[itemReference]],
          fishingSources: [
            ...toFishingSources(fishingByItem.get(id) ?? []),
            ...(manifest.fishingSources?.[id] ?? [])
          ]
        });
        buffItems.add(id);
      } else {
        const entry = {
          key: `new.${slug(id)}`,
          category: classification.category,
          classes,
          itemGroups: [[itemReference]],
          evaluations: [{ stageId: stage.id, tier: "FromGuide", scope: "StageOnly" }],
          wiki: [],
          fishingSources: [
            ...toFishingSources(fishingByItem.get(id) ?? []),
            ...(manifest.fishingSources?.[id] ?? [])
          ],
          isSupportWeapon: false,
          eventCategory: acquiredBy.get(id)?.eventCategory ?? null,
          customEventName: acquiredBy.get(id)?.customEventName ?? "",
          eventIcon: acquiredBy.get(id)?.eventIcon ?? ""
        };
        profileEntries.push(entry);
        entryByItem.set(id, entry);
      }
      visibleCount++;
      report.paths[id] = acquiredBy.get(id);
    }

    if (visibleCount === 0) report.emptyStages.push(stage.id);
  }

  for (const [id, acquisition] of acquiredBy) {
    report.paths[id] = acquisition;
  }

  applyWikiRecommendations(
    wikiProfile,
    manifest,
    entryByItem,
    profileEntries,
    profileBuffs,
    buffItems,
    report,
    wikiResolver,
    itemById);
  mergeEquivalentVariantEntries(profileEntries, entryByItem, itemById, manifest);
  narrowUnclassifiedEquipmentClasses(profileEntries, manifest);
  validateManualRules(manifest, itemById, report);
  const review = buildManualReview({
    snapshot,
    manifest,
    manualAssignments,
    report,
    available,
    entryByItem,
    contentMods,
    itemById,
    recipesByResult,
    snapshotShimmerTransforms,
    dropsBySource,
    shopsByNpc
  });

  const profile = {
    format: "ProgressionJournalProfile",
    version: 1,
    id: manifest.id,
    name: manifest.name,
    author: manifest.author ?? "Progression Journal",
    profileVersion: manifest.profileVersion ?? "1",
    readOnly: true,
    sourceUrl: manifest.sourceUrl ?? "",
    sourceRevision: manifest.sourceRevision ?? "",
    generatedAtUtc: new Date().toISOString(),
    requiredMods: manifest.requiredMods ?? [],
    classes: manifest.classes,
    stages: manifest.stages.map(stage => ({
      id: stage.id,
      name: stage.name,
      iconMod: stage.iconMod ?? "",
      iconNpc: stage.iconNpc ?? "",
      accessorySlots: stage.accessorySlots ?? 5,
      unlock: stage.unlock ?? { type: "always" }
    })),
    entries: sortEntries(profileEntries, itemById),
    combatBuffs: profileBuffs
  };

  return { profile, report, review };
}

function hasManualAvailabilityAssignments(manualAssignments) {
  return [
    manualAssignments?.itemStages,
    manualAssignments?.itemStageFloors,
    manualAssignments?.sourceStages,
    manualAssignments?.stationStages
  ].some(values => Object.keys(values ?? {}).length > 0);
}

function resolveObservedStageIndex(record, manifest, stageIndexes, label) {
  const resolvedStageIndex = resolveSnapshotStageIndex(record, manifest.stages, label);
  const conditionFloorIndexes = (record.conditions ?? [])
    .map(condition => {
      const normalizedCondition = typeof condition === "string"
        ? { description: condition }
        : condition;
      const description = normalizeConditionText(normalizedCondition.description);
      if (!/^(?:observed biome|наблюдаемый биом):/u.test(description)
          || description.includes(",")
          || !isCelestialPillarCondition(description)) {
        return -1;
      }
      return inferredConditionStageIndex(normalizedCondition, manifest, stageIndexes);
    })
    .filter(index => index >= 0);
  return Math.max(resolvedStageIndex, ...conditionFloorIndexes);
}

function withoutManualAvailabilityAssignments(manualAssignments) {
  return {
    ...structuredClone(manualAssignments),
    itemStages: {},
    sourceStages: {},
    stationStages: {}
  };
}

function prioritizeAutomaticAvailability(source, manifest, manualAssignments, automaticReport) {
  const snapshot = source?.format === "ProgressionJournalKnowledge"
    ? createSnapshotView(source)
    : source;
  const stageIds = new Set((manifest.stages ?? []).map(stage => stage.id));
  const automaticItemStages = new Map(
    Object.entries(automaticReport.paths ?? {})
      .filter(([, acquisition]) =>
        stageIds.has(acquisition?.stage) && isAutomaticAcquisition(acquisition?.via))
      .map(([item, acquisition]) => [item, acquisition.stage]));
  const stageIndexes = new Map(
    (manifest.stages ?? []).map((stage, index) => [stage.id, index]));
  const eventsByCategory = groupBy(
    (manifest.events ?? []).filter(event => event.eventCategory),
    event => event.eventCategory);
  const eventFloorBySource = new Map();
  for (const event of manifest.events ?? []) {
    const stageIndex = stageIndexes.get(event.stageId);
    if (stageIndex === undefined) continue;
    for (const sourceId of [
      ...(event.dropSources ?? []),
      ...(event.enemies ?? []),
      ...(event.containers ?? []),
      ...(event.shops ?? [])
    ]) {
      const existing = eventFloorBySource.get(sourceId);
      if (existing === undefined || stageIndex < existing) {
        eventFloorBySource.set(sourceId, stageIndex);
      }
    }
  }
  const automaticSourceStages = new Map();
  const sourcesWithUnobservedShops = new Set(
    (snapshot.shops ?? [])
      .filter(shop => shop.observed !== true)
      .map(shop => shop.npc));
  for (const record of snapshot.npcAvailability ?? []) {
    if (!record.observed || sourcesWithUnobservedShops.has(record.npc)) continue;
    const observedStageIndex = resolveObservedStageIndex(
      record,
      manifest,
      stageIndexes,
      `automatic source ${record.npc}`);
    const categoryFloorIndexes = (record.eventCategories ?? [])
      .flatMap(category => eventsByCategory.get(category) ?? [])
      .map(event => stageIndexes.get(event.stageId))
      .filter(index => index !== undefined);
    const categoryFloorIndex = categoryFloorIndexes.length > 0
      ? Math.min(...categoryFloorIndexes)
      : -1;
    const stageIndex = Math.max(
      observedStageIndex,
      eventFloorBySource.get(record.npc) ?? -1,
      categoryFloorIndex);
    const stageId = manifest.stages[stageIndex]?.id;
    if (stageId) automaticSourceStages.set(record.npc, stageId);
  }
  const automaticStationStages = new Map();
  for (const item of snapshot.items ?? []) {
    const stageId = automaticItemStages.get(item.id);
    if (item.placedTile && stageId) automaticStationStages.set(item.placedTile, stageId);
  }

  const prioritizedAssignments = structuredClone(manualAssignments);
  const suppressed = {
    itemStages: suppressAutomaticAssignments(
      prioritizedAssignments.itemStages,
      automaticItemStages,
      stageIndexes,
      automaticReport.paths),
    sourceStages: suppressAutomaticAssignments(
      prioritizedAssignments.sourceStages,
      automaticSourceStages,
      stageIndexes,
      null,
      true),
    stationStages: suppressAutomaticAssignments(
      prioritizedAssignments.stationStages,
      automaticStationStages,
      stageIndexes)
  };
  const automaticItemFloors = Object.fromEntries(
    suppressed.itemStages.map(entry => [entry.value, entry.automaticStageId]));
  const automaticSourceFloors = Object.fromEntries(
    suppressed.sourceStages.map(entry => [entry.value, entry.automaticStageId]));
  const automaticStationFloors = Object.fromEntries(
    suppressed.stationStages.map(entry => [entry.value, entry.automaticStageId]));
  const prioritizedManifest = {
    ...manifest,
    itemStageFloors: {
      ...(manifest.itemStageFloors ?? {}),
      ...automaticItemFloors
    },
    sourceStageFloors: {
      ...(manifest.sourceStageFloors ?? {}),
      ...automaticSourceFloors
    },
    stationStageFloors: {
      ...(manifest.stationStageFloors ?? {}),
      ...automaticStationFloors
    }
  };
  return {
    manifest: prioritizedManifest,
    manualAssignments: prioritizedAssignments,
    report: {
      automaticItems: automaticItemStages.size,
      automaticSources: automaticSourceStages.size,
      automaticStations: automaticStationStages.size,
      suppressed,
      retained: {
        itemStages: Object.keys(prioritizedAssignments.itemStages ?? {}).length,
        itemStageFloors: Object.keys(prioritizedAssignments.itemStageFloors ?? {}).length,
        sourceStages: Object.keys(prioritizedAssignments.sourceStages ?? {}).length,
        stationStages: Object.keys(prioritizedAssignments.stationStages ?? {}).length
      }
    }
  };
}

function suppressAutomaticAssignments(
  assignments,
  automaticStages,
  stageIndexes,
  paths = null,
  alwaysPreferAutomatic = false) {
  const suppressed = [];
  for (const [value, manualStageId] of Object.entries(assignments ?? {})) {
    const automaticStageId = automaticStages.get(value);
    if (!automaticStageId) continue;
    const manualStageIndex = stageIndexes.get(manualStageId);
    const automaticStageIndex = stageIndexes.get(automaticStageId);
    if (!alwaysPreferAutomatic
        && (manualStageIndex === undefined
            || automaticStageIndex === undefined
            || automaticStageIndex > manualStageIndex)) {
      continue;
    }
    suppressed.push({
      value,
      manualStageId,
      automaticStageId,
      via: paths?.[value]?.via ?? "runtime"
    });
    delete assignments[value];
  }
  return suppressed.sort((left, right) => left.value.localeCompare(right.value));
}

function isAutomaticAcquisition(via) {
  return /^(?:container|fishing|global|npc|recipe|shop):?/u.test(via ?? "");
}

function applyManualAssignments(sourceManifest, manualAssignments) {
  const manifest = structuredClone(sourceManifest);
  const problems = [];
  if (!manualAssignments) return { manifest, problems };

  assert(
    manualAssignments.format === "ProgressionJournalManualAssignments",
    "Invalid manual assignments format.");
  assert(
    manualAssignments.version === 1,
    `Unsupported manual assignments version '${manualAssignments.version}'.`);
  assert(
    !manualAssignments.profileId || manualAssignments.profileId === manifest.id,
    `Manual assignments profile '${manualAssignments.profileId}' does not match '${manifest.id}'.`);

  const stages = new Map(manifest.stages.map(stage => [stage.id, stage]));
  const applyStageMap = (values, property, kind) => {
    for (const [value, stageId] of Object.entries(values ?? {})) {
      const stage = stages.get(stageId);
      if (!stage) {
        problems.push({ kind, value, stageId, reason: "unknown stage" });
        continue;
      }

      stage[property] = [...new Set([...(stage[property] ?? []), value])];
    }
  };

  applyStageMap(manualAssignments.itemStages, "include", "item-stage");
  applyStageMap(manualAssignments.sourceStages, "dropSources", "source-stage");
  applyStageMap(manualAssignments.sourceStages, "shops", "source-stage");
  applyStageMap(manualAssignments.stationStages, "stations", "station-stage");
  manifest.itemStageFloors = {
    ...(manifest.itemStageFloors ?? {}),
    ...(manualAssignments.itemStageFloors ?? {})
  };
  manifest.stationStageFloors = {
    ...(manifest.stationStageFloors ?? {}),
    ...(manualAssignments.stationStages ?? {})
  };
  manifest.sourceStageFloors = {
    ...(manifest.sourceStageFloors ?? {}),
    ...(manualAssignments.sourceStages ?? {})
  };
  manifest.itemOverrides = {
    ...(manifest.itemOverrides ?? {}),
    ...(manualAssignments.itemOverrides ?? {})
  };
  manifest.fishingSources = {
    ...(manifest.fishingSources ?? {}),
    ...(manualAssignments.fishingSources ?? {})
  };

  manifest.conditionUnlocks = [...(manifest.conditionUnlocks ?? [])];
  for (const assignment of manualAssignments.conditionStages ?? []) {
    if (!stages.has(assignment.stageId)) {
      problems.push({
        kind: "condition-stage",
        value: assignment,
        stageId: assignment.stageId,
        reason: "unknown stage"
      });
      continue;
    }

    manifest.conditionUnlocks.push({
      stageId: assignment.stageId,
      sources: assignment.sources ?? ["drop", "shop", "recipe"],
      sourceIds: assignment.sourceIds ?? [],
      conditionTypes: assignment.conditionTypes ?? [],
      conditionKeys: assignment.conditionKeys ?? [],
      conditionDescriptions: assignment.conditionDescriptions ?? []
    });
  }

  return { manifest, problems };
}

function conditionMatchesUnlockRule(condition, rule) {
  if ((rule.conditionTypes ?? []).includes(condition.type)) return true;
  const expectedLocalizationKeys = [...new Set(rule.conditionKeys ?? [])].sort();
  if (expectedLocalizationKeys.length > 0) {
    const localizationKeys = [...new Set(collectLocalizationLeafKeys(condition))].sort();
    if (localizationKeys.length === expectedLocalizationKeys.length
        && localizationKeys.every((key, index) => key === expectedLocalizationKeys[index])) {
      return true;
    }
  }
  const description = normalizeConditionText(condition.description);
  return (rule.conditionDescriptions ?? [])
    .some(value => normalizeConditionText(value) === description);
}

function collectLocalizationLeafKeys(value, result = []) {
  if (!value || typeof value !== "object") return result;
  const children = [...(value.args ?? []), ...(value.join ?? [])];
  if (children.length === 0 && typeof value.key === "string" && value.key) {
    result.push(value.key);
  }
  for (const child of children) {
    collectLocalizationLeafKeys(child, result);
  }
  return result;
}

function normalizeConditionText(value) {
  return (value ?? "")
    .trim()
    .replace(/^(?:items|предметы|drops|дроп)\s*:\s*/iu, "")
    .toLocaleLowerCase();
}

function collectSourceStageFloors(manifest, stageIndexes) {
  const floors = new Map(
    Object.entries(manifest.sourceStageFloors ?? {})
      .filter(([, stageId]) => stageIndexes.has(stageId))
      .map(([source, stageId]) => [source, stageIndexes.get(stageId)]));
  const explicitSources = new Set(floors.keys());
  const addSources = (stageId, sources) => {
    const stageIndex = stageIndexes.get(stageId);
    if (stageIndex === undefined) return;
    for (const source of sources ?? []) {
      if (explicitSources.has(source)) continue;
      const existing = floors.get(source);
      if (existing === undefined || stageIndex < existing) {
        floors.set(source, stageIndex);
      }
    }
  };

  for (const stage of manifest.stages ?? []) {
    addSources(stage.id, stage.dropSources);
    addSources(stage.id, stage.enemies);
    addSources(stage.id, stage.containers);
    addSources(stage.id, stage.shops);
  }
  for (const event of manifest.events ?? []) {
    addSources(event.stageId, event.dropSources);
    addSources(event.stageId, event.enemies);
    addSources(event.stageId, event.containers);
    addSources(event.stageId, event.shops);
  }
  return floors;
}

function classifyItem(item, manifest, report, context) {
  const override = manifest.itemOverrides?.[item.id];
  if (override?.exclude) return null;
  if (item.vanity || item.sourceNamespace?.split(".").includes("Vanity")) return null;
  if (override?.buffCategory) {
    return { buffCategory: override.buffCategory, classes: override.classes ?? allClasses(manifest) };
  }
  const vanillaBuff = item.id.startsWith("Terraria/")
    ? context.vanillaBuffs?.get(item.id)
    : null;
  if (vanillaBuff) {
    const classes = vanillaBuff.isClassSpecific
      ? (vanillaBuff.classes ?? []).filter(classId =>
        manifest.classes.some(profileClass => profileClass.id === classId))
      : allClasses(manifest);
    return { buffCategory: vanillaBuff.category, classes };
  }

  if (item.placedTile && context.usedStations.has(item.placedTile)) {
    return null;
  }
  if (item.flask) return { buffCategory: "Flask", classes: override?.classes ?? allClasses(manifest) };
  if (item.food) return { buffCategory: "Food", classes: override?.classes ?? allClasses(manifest) };
  if (item.healLife > 0 || item.healMana > 0) {
    return { buffCategory: "Basic", classes: override?.classes ?? allClasses(manifest) };
  }
  if (isPermanentShimmerUpgrade(item, context)) {
    return { buffCategory: "Eternal", classes: override?.classes ?? allClasses(manifest) };
  }
  if (item.consumable
      && item.maxStack === 1
      && item.sourceNamespace?.split(".").includes("PermanentBoosters")) {
    return { buffCategory: "Eternal", classes: override?.classes ?? allClasses(manifest) };
  }

  if (item.pick > 0 || item.axe > 0 || item.hammer > 0 || item.mountType >= 0
      || item.createWall >= 0 || (item.createTile >= 0 && item.damage <= 0 && item.buffType <= 0)) {
    return null;
  }

  const vanilla = item.id.startsWith("Terraria/")
    ? context.vanillaItems?.get(item.id)
    : null;
  if (vanilla?.category === "Weapon" && item.ammo <= 0) {
    const validClasses = new Set(allClasses(manifest));
    const classes = override?.classes
      ?? (vanilla.classes ?? []).filter(value => validClasses.has(value));
    if (classes.length > 0) {
      return {
        category: override?.category ?? vanilla.category,
        classes
      };
    }
  }

  if (item.id.startsWith("Terraria/")
      && (item.accessory || item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0)) {
    if (!vanilla) return null;
    const validClasses = new Set(allClasses(manifest));
    const isArmor = item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0;
    const runtimeArmorClasses = isArmor
      ? resolveEffectClassEvidence(item, manifest).specificClasses
      : [];
    const vanillaClasses = (vanilla.classes ?? []).filter(value => validClasses.has(value));
    const overlapsVanillaClassification = runtimeArmorClasses.some(classId =>
      vanillaClasses.includes(classId));
    let classes = runtimeArmorClasses.length === 0
      ? vanillaClasses
      : overlapsVanillaClassification
        ? [...new Set([...vanillaClasses, ...runtimeArmorClasses])]
        : runtimeArmorClasses;
    const vanillaCombatClasses = ["melee", "ranged", "magic", "summoner"];
    if (item.accessory
        && vanillaCombatClasses.every(classId => classes.includes(classId))) {
      classes = allClasses(manifest);
    }
    if (classes.length === 0) return null;
    return {
      category: override?.category ?? vanilla.category,
      classes: override?.classes ?? classes
    };
  }

  const wikiClassification = classifyWikiItem(context.wikiItems?.get(item.id));
  const classes = override?.classes ?? resolveClasses(item, manifest, report, context);
  if (classes.length === 0) {
    if (wikiClassification
        && ((wikiClassification.category === "Accessory" && item.accessory)
            || (wikiClassification.category === "Armor"
                && (item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0))
            || (wikiClassification.category === "Weapon" && (item.damage > 0 || item.sentry))
            || wikiClassification.category === "Support")) {
      return wikiClassification;
    }
    if (item.accessory
        && !item.vanity
        && item.createTile < 0
        && !item.id.startsWith("Terraria/")
        && item.sourceNamespace?.split(".").includes("Accessories")) {
      return { category: "Accessory", classes: allClasses(manifest) };
    }
    return null;
  }
  if (override?.category) return { category: override.category, classes };
  if (item.ammo > 0) return { category: "Ammunition", classes };
  if (item.accessory) return { category: "Accessory", classes };
  if (item.damage > 0
      || item.sentry
      || (item.shoot > 0
          && !item.consumable
          && item.damageClass.endsWith("SummonDamageClass"))) {
    return { category: "Weapon", classes };
  }
  if (item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0) return { category: "Armor", classes };
  if (item.buffType > 0 && item.consumable) {
    return { buffCategory: "Potion", classes: allClasses(manifest) };
  }
  if (wikiClassification?.category === "Support") return wikiClassification;
  if (classes.length < allClasses(manifest).length
      && item.damageClass
      && (!item.consumable
          || classes.some(classId => !STANDARD_COMBAT_CLASS_IDS.has(classId)))) {
    return { category: "Support", classes };
  }
  return null;
}

function classifyWikiItem(metadata) {
  if (!metadata) return null;
  if (metadata.category === "Buff") {
    return { buffCategory: "Potion", classes: metadata.classes };
  }

  const category = metadata.category === "Support"
    ? "Support"
    : metadata.category;
  return { category, classes: metadata.classes };
}

function resolveClasses(item, manifest, report, context) {
  if (item.ammo > 0) {
    const ammoClasses = new Set();
    for (const weapon of context.items.filter(value => value.useAmmo === item.ammo && value.damage > 0)) {
      for (const cls of resolveDamageClasses(weapon.damageClass, manifest)) ammoClasses.add(cls);
    }
    if (ammoClasses.size > 0) return [...ammoClasses];
  }

  const { specificClasses, hasGenericEffect } = resolveEffectClassEvidence(item, manifest);
  const effectClasses = new Set(specificClasses);
  const isArmor = item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0;
  if (isArmor && effectClasses.size > 0) {
    return [...effectClasses];
  }
  if (!hasGenericEffect && effectClasses.size > 0) {
    return [...effectClasses];
  }
  if (hasGenericEffect) {
    return allClasses(manifest);
  }

  const matches = resolveDamageClasses(item.damageClass, manifest);
  if (matches.length > 0) return matches;
  if (item.defense > 0 || item.buffType > 0) return allClasses(manifest);
  return [];
}

function resolveEffectClassEvidence(item, manifest) {
  const effectClasses = new Set();
  let hasGenericEffect = false;
  for (const effect of item.classEffects ?? []) {
    if (effect.damageClass.toLowerCase().includes("genericdamageclass")) {
      hasGenericEffect = true;
      continue;
    }
    for (const cls of resolveDamageClasses(effect.damageClass, manifest)) effectClasses.add(cls);
  }
  return { specificClasses: [...effectClasses], hasGenericEffect };
}

function createWikiClassificationMap(wikiProfile, wikiResolver) {
  const result = new Map();
  for (const entry of wikiProfile?.entries ?? []) {
    if (!entry.category || (entry.classes ?? []).length === 0) continue;
    for (const id of resolveWikiEntryIds(entry, wikiResolver)) {
      const existing = result.get(id);
      if (!existing) {
        result.set(id, {
          category: entry.category,
          classes: [...new Set(entry.classes)]
        });
      } else if (existing.category === entry.category) {
        existing.classes = [...new Set([...existing.classes, ...entry.classes])];
      }
    }
  }
  return result;
}

function resolveDamageClasses(damageClass, manifest) {
  if (damageClass.toLowerCase().includes("summonmeleespeeddamageclass")) {
    return manifest.classes
      .filter(cls => (cls.damageClassNames ?? []).some(name =>
        name.toLowerCase().includes("summon")))
      .map(cls => cls.id);
  }
  const matches = manifest.classes
    .filter(cls => (cls.damageClassNames ?? []).some(name =>
      damageClass.toLowerCase().includes(name.toLowerCase())))
    .map(cls => cls.id);
  return matches;
}

function conditionsAllowed(conditions, stage, manifest, report, context) {
  const stageIndexes = manifest._stageIndexes
    ?? new Map(manifest.stages.map((value, index) => [value.id, index]));
  const currentStageIndex = stageIndexes.get(stage.id) ?? -1;
  for (const condition of conditions ?? []) {
    if (!condition.type && !condition.description) continue;
    if (isAlternativeEarlyContainerCondition(condition, context)) continue;
    if (isUnavailableCondition(condition) || isDefaultExcludedVariantCondition(condition)) return false;
    const dependencyIds = conditionDependencyIds(condition, manifest);
    if (dependencyIds.length > 0) {
      if (context.available && dependencyIds.some(id => context.available.has(id))) continue;
      return false;
    }
    const assignedStageIndex = assignedConditionStageIndex(
      condition,
      context.sourceKind,
      context.source,
      manifest,
      stageIndexes);
    if (assignedStageIndex >= 0) {
      if (currentStageIndex < assignedStageIndex) return false;
      continue;
    }
    if (isProgressionNeutralCondition(condition)) continue;
    if (isSafeOpaqueDropCondition(condition, context)) continue;
    const rule = manifest.conditionRules?.[condition.type];
    if (rule === "allow") continue;
    if (rule?.stages?.includes(stage.id)) continue;
    const unresolved = {
      sourceKind: context.sourceKind,
      source: context.source,
      item: context.item,
      condition
    };
    const signature = JSON.stringify(unresolved);
    const signatures = report._unresolvedConditionSignatures;
    if (!signatures || !signatures.has(signature)) {
      report.unresolvedConditions.push(unresolved);
      signatures?.add(signature);
    }
    return false;
  }
  return true;
}

function isAlternativeEarlyContainerCondition(condition, context) {
  const type = condition.type ?? "";
  // Presents can drop during real-world Christmas before hardmode.
  return context.sourceKind === "drop"
    && context.source === "Terraria/Present"
    && type.endsWith("+IsHardmode");
}

function assignedConditionStageIndex(
  condition,
  sourceKind,
  source,
  manifest,
  stageIndexes) {
  const indexes = [
    inferredConditionStageIndex(condition, manifest, stageIndexes),
    configuredConditionStageIndex(
      condition,
      sourceKind,
      source,
      manifest,
      stageIndexes)
  ]
    .filter(index => index >= 0);
  return indexes.length === 0 ? -1 : Math.max(...indexes);
}

function configuredConditionStageIndex(
  condition,
  sourceKind,
  source,
  manifest,
  stageIndexes) {
  const indexes = (manifest.conditionUnlocks ?? [])
    .filter(rule =>
      (rule.sources ?? ["drop", "shop", "recipe"]).includes(sourceKind)
      && ((rule.sourceIds ?? []).length === 0 || rule.sourceIds.includes(source))
      && conditionMatchesUnlockRule(condition, rule))
    .map(rule => stageIndexes.get(rule.stageId) ?? -1)
    .filter(index => index >= 0);
  return indexes.length === 0 ? -1 : Math.max(...indexes);
}

function fishingCatchStageIndex(catchRecord, manifest, stageIndexes) {
  const conditionStageIndexes = (catchRecord.conditions ?? [])
    .map(condition => configuredConditionStageIndex(
      typeof condition === "string" ? { description: condition } : condition,
      "fishing",
      catchRecord.target,
      manifest,
      stageIndexes))
    .filter(index => index >= 0);
  return Math.max(catchRecord.earliestStageIndex ?? -1, ...conditionStageIndexes);
}

function conditionHasAssignment(condition, sourceKind, source, manifest) {
  if (isUnavailableCondition(condition)
      || isDefaultExcludedVariantCondition(condition)
      || isProgressionNeutralCondition(condition)
      || isSafeOpaqueDropCondition(condition, { source })) return true;
  if (conditionDependencyIds(condition, manifest).length > 0) return true;
  if (manifest.conditionRules?.[condition.type]) return true;
  const stageIndexes = manifest._stageIndexes
    ?? new Map(manifest.stages.map((stage, index) => [stage.id, index]));
  if (inferredConditionStageIndex(condition, manifest, stageIndexes) >= 0) return true;
  return (manifest.conditionUnlocks ?? []).some(rule =>
    (rule.sources ?? ["drop", "shop", "recipe"]).includes(sourceKind)
    && ((rule.sourceIds ?? []).length === 0 || rule.sourceIds.includes(source))
    && conditionMatchesUnlockRule(condition, rule));
}

function inferredConditionStageIndex(condition, manifest, stageIndexes) {
  const type = condition.type ?? "";
  const description = normalizeConditionText(condition.description);
  const typeIndex = inferConditionTypeStageIndex(type, manifest, stageIndexes);
  if (typeIndex >= 0) return typeIndex;

  if (type === "ProgressionJournal.AfterProgression") {
    const match = /^(?:available after(?: stage)?|доступно после этапа):?\s*(.+)$/u.exec(description);
    if (match) {
      const target = match[1];
      const targetStage = manifest.stages.find(stage =>
        stage.id.replaceAll("-", " ") === target
        || Object.values(stage.name ?? {})
          .some(name => normalizeConditionText(name) === target));
      if (targetStage) return stageIndexes.get(targetStage.id) ?? -1;
    }
  }

  const hardmodeIndex = stageIndexByFlagOrId(manifest, stageIndexes, "hardMode", "wall-of-flesh");
  const allMechsIndex = stageIndexByFlagOrId(manifest, stageIndexes, "downedMechBoss3", "skeletron-prime");
  const anyMechIndex = stageIndexByFlagOrId(manifest, stageIndexes, "downedMechBoss1", "destroyer");
  const planteraIndex = stageIndexByFlagOrId(manifest, stageIndexes, "downedPlantBoss", "plantera");
  const golemIndex = stageIndexByFlagOrId(manifest, stageIndexes, "downedGolemBoss", "golem");
  const skeletronIndex = stageIndexByFlagOrId(manifest, stageIndexes, "downedBoss3", "skeletron");
  const eyeIndex = stageIndexByFlagOrId(manifest, stageIndexes, "downedBoss1", "eye-of-cthulhu");
  const worldEvilIndex = stageIndexById(manifest, stageIndexes, "world-evil");
  const queenBeeIndex = stageIndexById(manifest, stageIndexes, "queen-bee");
  const twinsIndex = stageIndexByFlagOrId(manifest, stageIndexes, "downedMechBoss2", "twins");
  const destroyerIndex = stageIndexByFlagOrId(manifest, stageIndexes, "downedMechBoss1", "destroyer");
  const skeletronPrimeIndex = stageIndexByFlagOrId(manifest, stageIndexes, "downedMechBoss3", "skeletron-prime");
  const lunaticCultistIndex = stageIndexByFlagOrId(
    manifest,
    stageIndexes,
    "downedAncientCultist",
    "lunatic-cultist");

  if (type.endsWith("+IsBloodMoonAndNotFromStatue")
      || /blood moon|кровав(?:ой|ая) лун/u.test(description)) {
    return stageIndexByEvent(manifest, stageIndexes, "BloodMoon");
  }
  if (/(?:hardmode|hard mode|хардмод|сложн(?:ом|ого) режим)/u.test(description)) {
    return hardmodeIndex;
  }
  if (/all mech|all mechanical|всем[и]? механическ/u.test(description)) {
    return allMechsIndex;
  }
  if (/mechdusa/u.test(description)) {
    return allMechsIndex;
  }
  if (/any mech|any mechanical|люб(?:ым|ого) механическ/u.test(description)) {
    return anyMechIndex;
  }
  if (/shadow orb|crimson heart|тенев(?:ой|ую) сфер|багрян(?:ого|ое) серд/u.test(description)) {
    return worldEvilIndex;
  }
  if (/pirate invasion|after defeating (?:the )?pirates|вторжен(?:ия|ием) пират/u.test(description)) {
    return hardmodeIndex;
  }
  if (/martian madness|after defeating (?:the )?martians|марсианск(?:им|ого) безуми/u.test(description)) {
    return golemIndex;
  }
  if (isCelestialPillarCondition(description)) {
    return lunaticCultistIndex;
  }
  if (/wave|волны/u.test(description)) {
    const pumpkinMoonIndex = stageIndexByEvent(manifest, stageIndexes, "PumpkinMoon");
    const frostMoonIndex = stageIndexByEvent(manifest, stageIndexes, "FrostMoon");
    const indexes = [pumpkinMoonIndex, frostMoonIndex, planteraIndex].filter(index => index >= 0);
    if (indexes.length > 0) return Math.min(...indexes);
  }

  const candidates = [];
  const add = index => {
    if (index >= 0) candidates.push(index);
  };
  if (/eye of cthulhu|глаз(?:ом)? ктулху/u.test(description)) add(eyeIndex);
  if (/eater of worlds|brain of cthulhu|пожирател(?:я|ем) миров|мозг(?:а|ом) ктулху/u.test(description)) add(worldEvilIndex);
  if (/queen bee|королев(?:ы|ой) пч/u.test(description)) add(queenBeeIndex);
  if (/skeletron prime|скелетрон(?:ом)? прайм/u.test(description)) add(skeletronPrimeIndex);
  if (/skeletron|скелетрон/u.test(description)) add(skeletronIndex);
  if (/destroyer|уничтожител/u.test(description)) add(destroyerIndex);
  if (/twins|близнец/u.test(description)) add(twinsIndex);
  if (/plantera|плантер/u.test(description)) add(planteraIndex);
  if (/golem|голем/u.test(description)) add(golemIndex);
  if (candidates.length > 0) {
    return /\bor\b| или /u.test(description)
      ? Math.min(...candidates)
      : Math.max(...candidates);
  }

  const match = /^after defeating (?:the )?(.+)$/u.exec(description);
  if (match) {
    const target = match[1];
    const stage = manifest.stages.find(value =>
      Object.values(value.name ?? {})
        .some(name => normalizeConditionText(name) === target)
      || value.id.replaceAll("-", " ") === target);
    return stage ? stageIndexes.get(stage.id) ?? -1 : -1;
  }
  return -1;
}

function isCelestialPillarCondition(description) {
  return /(?:solar|vortex|nebula|stardust).*(?:pillar|tower)|(?:pillar|tower).*(?:solar|vortex|nebula|stardust)|башн.*(?:солнеч|вихр|туман|зв[её]здн)|(?:солнеч|вихр|туман|зв[её]здн).*башн/u.test(description);
}

function inferConditionTypeStageIndex(type, manifest, stageIndexes) {
  if (!type) return -1;
  if (type.endsWith("+IsHardmode") || type.endsWith("+RemixSeedHardmode")) {
    return stageIndexByFlagOrId(manifest, stageIndexes, "hardMode", "wall-of-flesh");
  }
  if (type.endsWith("+DownedPlantera")) {
    return stageIndexByFlagOrId(manifest, stageIndexes, "downedPlantBoss", "plantera");
  }
  if (type.endsWith("+FirstTimeKillingPlantera")) {
    return stageIndexByFlagOrId(manifest, stageIndexes, "downedPlantBoss", "plantera");
  }
  if (type.endsWith("+DownedAllMechBosses") || type.endsWith("+MechdusaKill")) {
    return stageIndexByFlagOrId(manifest, stageIndexes, "downedMechBoss3", "skeletron-prime");
  }
  if (type.endsWith("+OneMechDefated")) {
    return stageIndexByFlagOrId(manifest, stageIndexes, "downedMechBoss1", "destroyer");
  }
  return -1;
}

function stageIndexByFlagOrId(manifest, stageIndexes, flag, id) {
  const byFlag = manifest.stages.find(stage => unlockContainsVanillaFlag(stage.unlock, flag));
  if (byFlag) return stageIndexes.get(byFlag.id) ?? -1;
  return stageIndexById(manifest, stageIndexes, id);
}

function stageIndexById(manifest, stageIndexes, id) {
  const stage = manifest.stages.find(value => value.id === id);
  return stage ? stageIndexes.get(stage.id) ?? -1 : -1;
}

function stageIndexByEvent(manifest, stageIndexes, eventCategory) {
  const event = (manifest.events ?? []).find(value => value.eventCategory === eventCategory);
  return event ? stageIndexes.get(event.stageId) ?? -1 : -1;
}

function unlockContainsVanillaFlag(unlock, key) {
  if (!unlock) return false;
  if (unlock.type === "vanilla-flag" && unlock.key === key) return true;
  return (unlock.conditions ?? []).some(condition =>
    unlockContainsVanillaFlag(condition, key));
}

function isProgressionNeutralCondition(condition) {
  const description = normalizeConditionText(condition.description);
  const type = condition.type ?? "";
  if (isOneTimeUseEligibilityCondition(condition)) return true;
  if (/^not in (?:a remix world|world generation )/u.test(description)
      || /^не в генерации мира /u.test(description)) {
    return true;
  }
  if (/blood moon|solar eclipse|halloween|during daytime|at night|not currently alive|\b(?:full|new|waxing|waning|quarter|gibbous|crescent) moon\b|ночью|дн[её]м|кровав(?:ой|ая) лун|солнечн(?:ого|ое) затмени|луны|лун[аеы]|полнолуни|четверт|новолуни|graveyard|кладбищ|honey|water|lava|м[её]д|вод[ауы]|лав[аы]|snow|снег|biome|бестиар|bestiary|happy|pylon|пилон|достаточно счастлив|wave|волны|хэллоуин|императриц[аы] света атакована в дневное время|выпадает в порче/u.test(description)) {
    return true;
  }
  if (type === "Terraria.Condition"
      && (/^between \d/u.test(description)
          || /^player is in /u.test(description)
          || /^in a world with /u.test(description)
          || /^not in a remix world$/u.test(description)
          || /^world (?:with|has) /u.test(description)
          || /^мир с /u.test(description)
          || /^enabled in .* configuration$/u.test(description))) {
    return true;
  }
  return new Set([
    "ProgressionJournal.BelowSurface",
    "ProgressionJournal.Biome",
    "ProgressionJournal.Event",
    "ProgressionJournal.ZenithWorld",
    "Terraria.GameContent.ItemDropRules.Conditions+IsExpert",
    "Terraria.GameContent.ItemDropRules.Conditions+NotExpert",
    "Terraria.GameContent.ItemDropRules.Conditions+IsMasterMode",
    "Terraria.GameContent.ItemDropRules.Conditions+NotMasterMode",
    "Terraria.GameContent.ItemDropRules.Conditions+LegacyHack_IsBossAndNotExpert",
    "Terraria.GameContent.ItemDropRules.Conditions+NotRemixSeed",
    "Terraria.GameContent.ItemDropRules.Conditions+DontStarveIsNotUp",
    "Terraria.GameContent.ItemDropRules.Conditions+HalloweenWeapons",
    "Terraria.GameContent.ItemDropRules.Conditions+EmpressOfLightIsGenuinelyEnraged",
    "Terraria.GameContent.ItemDropRules.Conditions+IsCorruption",
    "Terraria.GameContent.ItemDropRules.Conditions+IsCorruptionAndNotExpert",
    "Terraria.GameContent.ItemDropRules.Conditions+IsBloodMoonAndNotFromStatue",
    "Terraria.GameContent.ItemDropRules.Conditions+NotFromStatue",
    "FargowiltasSouls.Core.ItemDropRules.Conditions.EModeDropCondition"
  ]).has(type);
}

function isOneTimeUseEligibilityCondition(condition) {
  const type = condition.type ?? "";
  const description = normalizeConditionText(condition.description);
  return /(?:^|\+)NotUsed[A-Za-z0-9_]*$/u.test(type)
    || /(?:has not|hasn't|not yet) (?:used (?:the )?item|consumed .+ before)|ещ[её] не (?:успел )?использова|не успел использовать предмет/u.test(description);
}

function isPermanentShimmerUpgrade(item, context) {
  return context.shimmerOutputs?.has(item.id)
    && item.consumable
    && !item.accessory
    && item.headSlot < 0
    && item.bodySlot < 0
    && item.legSlot < 0
    && item.buffType <= 0
    && item.healLife <= 0
    && item.healMana <= 0
    && item.createTile < 0
    && item.createWall < 0
    && item.shoot <= 0
    && !item.sourceNamespace?.split(".").some(namespacePart =>
      /^(?:Treasure|Grab)Bags?$/iu.test(namespacePart))
    && item.damage <= 0;
}

function isUnavailableCondition(condition) {
  return condition.type === "Terraria.GameContent.ItemDropRules.Conditions+NeverTrue";
}

function isDefaultExcludedVariantCondition(condition) {
  const description = normalizeConditionText(condition.description);
  const type = condition.type ?? "";
  if (/^not in world generation /u.test(description)
      || /^не в генерации мира /u.test(description)) {
    return false;
  }
  if (type === "Terraria.GameContent.ItemDropRules.Conditions+RemixSeed"
      || type === "Terraria.GameContent.ItemDropRules.Conditions+RemixSeedEasymode"
      || type === "Terraria.GameContent.ItemDropRules.Conditions+RemixSeedHardmode"
      || type.endsWith("+Unofficial")
      || type === "Terraria.GameContent.ItemDropRules.Conditions+DontStarveIsUp") return true;
  return /^in (?:an? )?(?:remix|unoffifical) worlds?$/u.test(description)
    || /zenith|get fixed boi|gfb|celebration|mk 10|don't starve|dontstarve|^в генерации мира [«"]?(?:zenith|remix|celebration|get fixed boi)|^in world generation [«"]?(?:zenith|remix|celebration|get fixed boi)/u.test(description);
}

function isSafeOpaqueDropCondition(condition, context) {
  if (condition.type !== "CalamityMod.DropHelper+LambdaDropRuleCondition") return false;
  if (normalizeConditionText(condition.description)) return false;
  return new Set([
    "CalamityMod/StarterBag",
    "Terraria/IceMimic",
    "Terraria/Mimic",
    "CalamityMod/FearlessGoldfishWarrior",
    "CalamityMod/Trasher",
    "CalamityMod/AbyssalTreasure",
    "CalamityMod/SulphuricTreasure"
  ]).has(context.source);
}

function conditionDependencyIds(condition, manifest) {
  const names = extractConditionDependencyNames(condition);
  if (names.length === 0) return [];
  const index = manifest._itemNameIndex;
  if (!index) return [];
  const ids = [];
  for (const name of names) {
    const variants = [
      name,
      name.replace(/\s+upgrade$/u, ""),
      name.replace(/^an?\s+/u, ""),
      name.replace(/^the\s+/u, "")
    ];
    for (const variant of variants) {
      const id = index.get(normalizeLookupText(variant));
      if (id && !ids.includes(id)) ids.push(id);
    }
  }
  return ids;
}

function extractConditionDependencyNames(condition) {
  const description = normalizeConditionText(condition.description);
  const patterns = [
    /(?:while holding|when holding|when in inventory|when carried|when the player has|inventory contains|когда в инвентаре находится|если в инвентаре есть|при наличии)\s+(?:a |an |the )?(.+?)(?:\s+in (?:their|the) inventory)?$/u
  ];
  const names = [];
  for (const pattern of patterns) {
    const match = pattern.exec(description);
    if (!match) continue;
    const value = match[1]
      .replace(/[.;]+$/u, "")
      .replace(/^оружи[ея],?\s*/u, "")
      .trim();
    if (value) names.push(value);
  }
  if (/uses seeds as ammo|использует семена как боеприпасы/u.test(description)) {
    names.push("Blowpipe", "Blowgun", "Блуопайп", "Духовая трубка");
  }
  return names;
}

function createItemNameIndex(items) {
  const result = new Map();
  const add = (key, id) => {
    const normalized = normalizeLookupText(key);
    if (normalized && !result.has(normalized)) result.set(normalized, id);
  };
  for (const item of items ?? []) {
    const localId = item.id?.split("/").pop() ?? "";
    add(item.id, item.id);
    add(localId, item.id);
    add(decamelize(localId), item.id);
    for (const value of [item.name, item.displayName, item.localizedName]) add(value, item.id);
  }
  return result;
}

function decamelize(value) {
  return (value ?? "")
    .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
    .replace(/([A-Z]+)([A-Z][a-z])/g, "$1 $2")
    .replace(/_/g, " ");
}

function normalizeLookupText(value) {
  return normalizeConditionText(value)
    .replace(/[«»“”"']/gu, "")
    .replace(/\s+/gu, " ")
    .trim();
}

function applyWikiRecommendations(
  wikiProfile,
  manifest,
  entryByItem,
  profileEntries,
  profileBuffs,
  buffItems,
  report,
  wikiResolver,
  itemById) {
  if (!wikiProfile) return;
  const buffByItem = new Map(
    profileBuffs.flatMap(buff => buff.itemGroups
      .flat()
      .map(reference => [`${reference.mod}/${reference.item}`, buff])));
  const wikiBuffClasses = new Map();

  for (const sourceEntry of wikiProfile.entries ?? []) {
    const ids = resolveWikiEntryIds(sourceEntry, wikiResolver);
    for (const evaluation of sourceEntry.evaluations ?? []) {
      const mapping = manifest.wikiStageMap?.[evaluation.stageId];
      if (!mapping) continue;
      for (const id of ids) {
        if (sourceEntry.category === "Buff") {
          const item = itemById.get(id);
          if (!item) {
            report.wikiMissingItems.push({
              id,
              wikiStage: evaluation.stageId,
              sourceReference: sourceEntry.itemGroups?.flat()
                .map(ref => `${ref.mod}/${ref.item}`)
            });
            continue;
          }

          const buff = buffByItem.get(id);
          if (!buff) {
            report.wikiMissingItems.push({
              id,
              wikiStage: evaluation.stageId,
              reason: "recommendation has no proven availability",
              sourceReference: sourceEntry.itemGroups?.flat()
                .map(ref => `${ref.mod}/${ref.item}`)
            });
            continue;
          }

          const classes = wikiBuffClasses.get(id) ?? new Set();
          for (const classId of sourceEntry.classes ?? []) classes.add(classId);
          wikiBuffClasses.set(id, classes);
          continue;
        }

        if (buffItems.has(id)) continue;
        const entry = entryByItem.get(id);
        if (!entry) {
          report.wikiMissingItems.push({
            id,
            wikiStage: evaluation.stageId,
            reason: "recommendation has no proven availability",
            sourceReference: sourceEntry.itemGroups?.flat()
              .map(ref => `${ref.mod}/${ref.item}`)
          });
          continue;
        }

        const factualStageId = entry.evaluations?.[0]?.stageId;
        const factualStageIndex = manifest._stageIndexes.get(factualStageId) ?? -1;
        const recommendationStageIndex = manifest._stageIndexes.get(mapping.stageId) ?? -1;
        if (factualStageIndex >= 0 && recommendationStageIndex < factualStageIndex) {
          report.wikiAvailabilityCorrections.push({
            id,
            factualStage: factualStageId,
            recommendedStage: mapping.stageId,
            wikiStage: evaluation.stageId,
            reason: "recommendation precedes proven availability"
          });
          continue;
        }

        if (sourceEntry.category === "Support" || sourceEntry.isSupportWeapon === true) {
          entry.isSupportWeapon = true;
        }
        const recommendation = {
          stageId: mapping.stageId,
          classes: sourceEntry.classes,
          sourceName: manifest.wikiSource?.name ?? "Official Wiki",
          sourceUrl: manifest.wikiSource?.url ?? "",
          target: mapping.target
        };
        const signature = wikiRecommendationSignature(recommendation);
        if (!entry.wiki.some(value => wikiRecommendationSignature(value) === signature)) {
          entry.wiki.push(recommendation);
        }
      }
    }
  }

  const profileClasses = allClasses(manifest);
  for (const [id, classes] of wikiBuffClasses) {
    const buff = buffByItem.get(id);
    if (buff) {
      buff.classes = profileClasses.filter(classId => classes.has(classId));
    }
  }
}

function narrowUnclassifiedEquipmentClasses(profileEntries, manifest) {
  const profileClasses = allClasses(manifest);
  const allClassIds = new Set(profileClasses);
  for (const entry of profileEntries) {
    const reference = entry.itemGroups?.[0]?.[0];
    const itemId = reference ? `${reference.mod}/${reference.item}` : "";
    if (manifest.itemOverrides?.[itemId]?.classes) {
      continue;
    }
    if (!["Armor", "Accessory"].includes(entry.category)
        || entry.classes.length !== profileClasses.length
        || !entry.classes.every(classId => allClassIds.has(classId))) {
      continue;
    }

    const recommendedClasses = new Set(
      (entry.wiki ?? []).flatMap(recommendation => recommendation.classes ?? []));
    const narrowedClasses = profileClasses.filter(classId => recommendedClasses.has(classId));
    if (narrowedClasses.length > 0 && narrowedClasses.length < profileClasses.length) {
      entry.classes = narrowedClasses;
    }
  }
}

function sourceCompatibility(sourceProfile, snapshot) {
  if (!sourceProfile) {
    return { present: false, compatible: false, comparisons: [] };
  }

  const snapshotMods = new Map(
    (snapshot.mods ?? []).map(mod => [mod.name, mod.version]));
  const comparisons = (sourceProfile.requiredMods ?? [])
    .filter(requiredMod => requiredMod.version)
    .map(requiredMod => {
      const snapshotVersion = snapshotMods.get(requiredMod.name) ?? "";
      return {
        mod: requiredMod.name,
        sourceVersion: requiredMod.version,
        snapshotVersion,
        compatible: sameVersionFamily(requiredMod.version, snapshotVersion)
      };
    });
  return {
    present: true,
    compatible: comparisons.every(comparison => comparison.compatible),
    comparisons
  };
}

const EquivalentVariantGroups = [
  [
    "Terraria/WoodHelmet",
    "Terraria/BorealWoodHelmet",
    "Terraria/RichMahoganyHelmet",
    "Terraria/PalmWoodHelmet",
    "Terraria/EbonwoodHelmet",
    "Terraria/ShadewoodHelmet",
    "Terraria/AshWoodHelmet"
  ],
  [
    "Terraria/WoodBreastplate",
    "Terraria/BorealWoodBreastplate",
    "Terraria/RichMahoganyBreastplate",
    "Terraria/PalmWoodBreastplate",
    "Terraria/EbonwoodBreastplate",
    "Terraria/ShadewoodBreastplate",
    "Terraria/AshWoodBreastplate"
  ],
  [
    "Terraria/WoodGreaves",
    "Terraria/BorealWoodGreaves",
    "Terraria/RichMahoganyGreaves",
    "Terraria/PalmWoodGreaves",
    "Terraria/EbonwoodGreaves",
    "Terraria/ShadewoodGreaves",
    "Terraria/AshWoodGreaves"
  ],
  ["Terraria/CopperHelmet", "Terraria/TinHelmet"],
  ["Terraria/CopperChainmail", "Terraria/TinChainmail"],
  ["Terraria/CopperGreaves", "Terraria/TinGreaves"],
  ["Terraria/IronHelmet", "Terraria/LeadHelmet"],
  ["Terraria/IronChainmail", "Terraria/LeadChainmail"],
  ["Terraria/IronGreaves", "Terraria/LeadGreaves"],
  ["Terraria/SilverHelmet", "Terraria/TungstenHelmet"],
  ["Terraria/SilverChainmail", "Terraria/TungstenChainmail"],
  ["Terraria/SilverGreaves", "Terraria/TungstenGreaves"],
  ["Terraria/GoldHelmet", "Terraria/PlatinumHelmet"],
  ["Terraria/GoldChainmail", "Terraria/PlatinumChainmail"],
  ["Terraria/GoldGreaves", "Terraria/PlatinumGreaves"],
  [
    "Terraria/WoodenSword",
    "Terraria/BorealWoodSword",
    "Terraria/RichMahoganySword",
    "Terraria/PalmWoodSword",
    "Terraria/EbonwoodSword",
    "Terraria/ShadewoodSword",
    "Terraria/AshWoodSword"
  ],
  [
    "Terraria/WoodenBow",
    "Terraria/BorealWoodBow",
    "Terraria/RichMahoganyBow",
    "Terraria/PalmWoodBow",
    "Terraria/EbonwoodBow",
    "Terraria/ShadewoodBow",
    "Terraria/AshWoodBow"
  ],
  ["Terraria/CopperShortsword", "Terraria/TinShortsword"],
  ["Terraria/CopperBroadsword", "Terraria/TinBroadsword"],
  ["Terraria/CopperBow", "Terraria/TinBow"],
  ["Terraria/IronShortsword", "Terraria/LeadShortsword"],
  ["Terraria/IronBroadsword", "Terraria/LeadBroadsword"],
  ["Terraria/IronBow", "Terraria/LeadBow"],
  ["Terraria/SilverShortsword", "Terraria/TungstenShortsword"],
  ["Terraria/SilverBroadsword", "Terraria/TungstenBroadsword"],
  ["Terraria/SilverBow", "Terraria/TungstenBow"],
  ["Terraria/GoldShortsword", "Terraria/PlatinumShortsword"],
  ["Terraria/GoldBroadsword", "Terraria/PlatinumBroadsword"],
  ["Terraria/GoldBow", "Terraria/PlatinumBow"],
  [
    "Terraria/WhiteString",
    "Terraria/RedString",
    "Terraria/OrangeString",
    "Terraria/YellowString",
    "Terraria/LimeString",
    "Terraria/GreenString",
    "Terraria/TealString",
    "Terraria/CyanString",
    "Terraria/SkyBlueString",
    "Terraria/BlueString",
    "Terraria/PurpleString",
    "Terraria/VioletString",
    "Terraria/PinkString",
    "Terraria/BrownString",
    "Terraria/BlackString",
    "Terraria/RainbowString"
  ]
];

function mergeEquivalentVariantEntries(profileEntries, entryByItem, itemById, manifest) {
  const stageIndexes = manifest._stageIndexes ?? new Map();
  for (const group of EquivalentVariantGroups) {
    const entries = group
      .map((id, groupIndex) => ({ id, groupIndex, entry: entryByItem.get(id) }))
      .filter(value => value.entry);
    const uniqueEntries = uniqueBy(entries, value => value.entry.key);
    if (uniqueEntries.length < 2) continue;

    const canonical = [...uniqueEntries]
      .sort((left, right) => {
        const leftStage = left.entry.evaluations?.[0]?.stageId ?? "";
        const rightStage = right.entry.evaluations?.[0]?.stageId ?? "";
        return (stageIndexes.get(leftStage) ?? Number.MAX_SAFE_INTEGER)
          - (stageIndexes.get(rightStage) ?? Number.MAX_SAFE_INTEGER)
          || left.groupIndex - right.groupIndex;
      })[0].entry;
    const references = group
      .filter(id => entryByItem.has(id))
      .map(id => itemById.get(id))
      .filter(Boolean)
      .map(toItemReference);
    canonical.itemGroups = [references];

    const wiki = uniqueBy(
      uniqueEntries.flatMap(value => value.entry.wiki ?? []),
      wikiRecommendationSignature);
    canonical.wiki = wiki;
    canonical.fishingSources = uniqueBy(
      uniqueEntries.flatMap(value => value.entry.fishingSources ?? []),
      value => JSON.stringify(value));

    for (const { id } of entries) {
      entryByItem.set(id, canonical);
    }
    for (const { entry } of uniqueEntries) {
      if (entry === canonical) continue;
      const index = profileEntries.indexOf(entry);
      if (index >= 0) profileEntries.splice(index, 1);
    }
  }
}

function sameVersionFamily(left, right) {
  if (!left || !right) return false;
  const family = value => value.split(".").slice(0, 2).join(".");
  return family(left) === family(right);
}

function wikiRecommendationSignature(value) {
  return JSON.stringify({
    stageId: value.stageId,
    classes: [...value.classes].sort(),
    sourceName: value.sourceName,
    sourceUrl: value.sourceUrl,
    target: value.target
  });
}

function resolveWikiEntryIds(sourceEntry, wikiResolver) {
  return [...new Set(
    (sourceEntry.itemGroups?.flat() ?? [])
      .flatMap(reference => wikiResolver.resolve(reference, sourceEntry.category)))];
}

function createWikiReferenceResolver(items, report) {
  const itemById = new Map(items.map(item => [item.id, item]));
  const itemsByMod = groupBy(items, item => item.id.slice(0, item.id.indexOf("/")));
  const cache = new Map();
  const reported = new Set();

  return {
    resolve(reference, category) {
      const sourceId = `${reference.mod}/${reference.item}`;
      const cacheKey = `${sourceId}\n${reference.displayName}\n${category}`;
      if (cache.has(cacheKey)) return cache.get(cacheKey);

      let resolution = null;
      if (itemById.has(sourceId)) {
        resolution = { ids: [sourceId], method: "exact-id" };
      } else {
        const modItems = itemsByMod.get(reference.mod) ?? [];
        resolution = resolveUniqueWikiMatch(
          modItems.filter(item => normalizeWikiText(item.id.slice(item.id.indexOf("/") + 1))
            === normalizeWikiText(reference.item)),
          "normalized-internal-name");
        resolution ??= resolveUniqueWikiMatch(
          modItems.filter(item => normalizeWikiText(item.name)
            === normalizeWikiText(reference.displayName)),
          "display-name");
        resolution ??= resolveUniqueWikiMatch(
          modItems.filter(item => normalizeWikiText(item.englishName)
            === normalizeWikiText(reference.displayName)),
          "english-display-name");
        resolution ??= resolveArmorSet(reference, category, modItems);
        resolution ??= resolveVariantFamily(reference, category, modItems);
      }

      const ids = resolution?.ids ?? [];
      cache.set(cacheKey, ids);
      if (!reported.has(cacheKey) && resolution && resolution.method !== "exact-id") {
        report.wikiResolvedItems.push({
          from: sourceId,
          displayName: reference.displayName ?? "",
          to: ids,
          method: resolution.method
        });
        reported.add(cacheKey);
      } else if (!reported.has(cacheKey) && !resolution) {
        report.wikiUnresolvedItems.push({
          id: sourceId,
          displayName: reference.displayName ?? "",
          category
        });
        reported.add(cacheKey);
      }
      return ids;
    }
  };

  function resolveUniqueWikiMatch(candidates, method) {
    const unique = uniqueItems(candidates);
    if (unique.length === 1) return { ids: [unique[0].id], method };
    if (unique.length > 1) {
      report.wikiAmbiguousItems.push({
        method,
        candidates: unique.map(item => item.id)
      });
    }
    return null;
  }

  function resolveArmorSet(reference, category, modItems) {
    if (category !== "Armor" || !/armor\s*$/i.test(reference.displayName ?? "")) return null;
    const stem = normalizeWikiText(reference.displayName.replace(/armor\s*$/i, ""));
    if (!stem) return null;

    const candidates = uniqueItems(modItems.filter(item =>
      item.defense > 0
      && (item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0)
      && [item.englishName, item.name]
        .some(name => wikiNameStartsWith(name, reference.displayName.replace(/armor\s*$/i, "")))));
    const heads = candidates.filter(item => item.headSlot >= 0);
    const bodies = candidates.filter(item => item.bodySlot >= 0);
    const legs = candidates.filter(item => item.legSlot >= 0);
    const isSingleSet = heads.length > 0 && bodies.length > 0 && legs.length > 0
      && [heads, bodies, legs].filter(part => part.length > 1).length <= 1;
    const isParallelSet = heads.length >= 2
      && heads.length === bodies.length
      && bodies.length === legs.length
      && new Set(heads.map(item => normalizeWikiText(item.englishName ?? item.name))).size === 1
      && new Set(bodies.map(item => normalizeWikiText(item.englishName ?? item.name))).size === 1
      && new Set(legs.map(item => normalizeWikiText(item.englishName ?? item.name))).size === 1;
    if (!isSingleSet && !isParallelSet) {
      if (candidates.length > 0) {
        report.wikiAmbiguousItems.push({
          method: "armor-set",
          source: `${reference.mod}/${reference.item}`,
          displayName: reference.displayName ?? "",
          candidates: candidates.map(item => item.id)
        });
      }
      return null;
    }

    return {
      ids: (isParallelSet ? [...heads, ...bodies, ...legs] : [...heads, bodies[0], legs[0]])
        .map(item => item.id),
      method: isParallelSet ? "parallel-armor-sets" : "armor-set"
    };
  }

  function resolveVariantFamily(reference, category, modItems) {
    if (!["Weapon", "Accessory", "Support"].includes(category)) return null;
    const target = normalizeWikiText(reference.displayName);
    if (target.length < 5) return null;
    const targets = target.endsWith("s")
      ? [target, target.slice(0, -1)]
      : [target];

    const candidates = uniqueItems(modItems.filter(item => {
      const names = [item.englishName, item.name].map(normalizeWikiText);
      return itemMatchesWikiCategory(item, category)
        && names.every(name => !targets.includes(name))
        && names.some(name => targets.some(candidate =>
          name.startsWith(candidate) || name.endsWith(candidate)));
    }));
    if (candidates.length === 0) return null;
    return {
      ids: candidates.map(item => item.id),
      method: candidates.length === 1 ? "display-name-affix" : "variant-family"
    };
  }
}

function wikiNameStartsWith(value, stem) {
  const words = wikiNameWords(value);
  const stemWords = wikiNameWords(stem);
  return stemWords.length > 0
    && stemWords.every((word, index) =>
      words[index] === word || words[index] === `${word}s`);
}

function wikiNameWords(value) {
  return (value ?? "")
    .normalize("NFKD")
    .toLowerCase()
    .replace(/[’']/g, "")
    .replace(/[^a-z0-9]+/g, " ")
    .trim()
    .split(/\s+/g)
    .filter(Boolean);
}

function itemMatchesWikiCategory(item, category) {
  if (category === "Accessory") return item.accessory;
  if (category === "Weapon") return item.damage > 0 || item.sentry;
  if (category === "Support") {
    return !item.accessory
      && item.headSlot < 0
      && item.bodySlot < 0
      && item.legSlot < 0;
  }
  return false;
}

function uniqueItems(items) {
  return [...new Map(items.map(item => [item.id, item])).values()];
}

function normalizeWikiText(value) {
  return (value ?? "")
    .normalize("NFKD")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "");
}

function isAllowedProfileItem(id, contentMods) {
  const slash = id.indexOf("/");
  const mod = slash >= 0 ? id.slice(0, slash) : "";
  return mod === "Terraria" || contentMods.has(mod);
}

function sortEntries(entries, itemById) {
  return entries.sort((left, right) => {
    const a = itemById.get(`${left.itemGroups[0][0].mod}/${left.itemGroups[0][0].item}`);
    const b = itemById.get(`${right.itemGroups[0][0].mod}/${right.itemGroups[0][0].item}`);
    const strengthA = left.category === "Armor" ? a?.defense ?? 0 : a?.damage ?? 0;
    const strengthB = right.category === "Armor" ? b?.defense ?? 0 : b?.damage ?? 0;
    return strengthB - strengthA || (a?.name ?? "").localeCompare(b?.name ?? "");
  });
}

function validateManualRules(manifest, itemById, report) {
  const references = [
    ...(manifest.initialItems ?? []),
    ...manifest.stages.flatMap(stage => [...(stage.materials ?? []), ...(stage.include ?? []), ...(stage.exclude ?? [])]),
    ...(manifest.modifiedVanillaItems ?? []),
    ...Object.keys(manifest.itemOverrides ?? {})
  ];
  for (const id of new Set(references)) {
    if (!itemById.has(id)) report.staleRules.push(id);
  }
}

function buildManualReview({
  snapshot,
  manifest,
  manualAssignments,
  report,
  available,
  entryByItem,
  contentMods,
  itemById,
  recipesByResult,
  snapshotShimmerTransforms
}) {
  const ignoredItems = new Set(manualAssignments?.ignoredItems ?? []);
  const ignoredIssues = new Set(manualAssignments?.ignoredIssues ?? []);
  const issues = [];
  const usedStations = new Set(
    snapshot.recipes
      .filter(recipe => isAllowedProfileItem(recipe.result, contentMods))
      .flatMap(recipe => recipe.stations));
  const classificationContext = {
    usedStations,
    items: snapshot.items.filter(item => isAllowedProfileItem(item.id, contentMods)),
    vanillaBuffs: new Map(
      (snapshot.vanillaBuffClassifications ?? [])
        .map(value => [value.item, value])),
    shimmerOutputs: new Set((snapshotShimmerTransforms ?? []).map(transform => transform.output))
  };
  const conditionClassificationReport = { ambiguousClasses: [] };

  const unresolvedConditions = uniqueBy(
    [
      ...report.unresolvedConditions,
      ...collectUnknownConditionRecords(snapshot, manifest, contentMods)
    ],
    value => JSON.stringify(value))
    .filter(record =>
      !ignoredItems.has(record.item)
      && isReviewableClassification(
        itemById.get(record.item),
        classifyItem(
          itemById.get(record.item),
          manifest,
          conditionClassificationReport,
          classificationContext)));
  const unresolvedConditionGroups = groupBy(
    unresolvedConditions,
    record => JSON.stringify({
      sourceKind: record.sourceKind,
      type: record.condition.type,
      description: record.condition.description
    }));
  for (const records of unresolvedConditionGroups.values()) {
    const record = records[0];
    const affected = uniqueBy(
      records.map(value => ({ item: value.item, source: value.source })),
      value => JSON.stringify(value));
    const sourceIds = [...new Set(records.map(value => value.source))];
    issues.push(createReviewIssue("unresolved-condition", {
      sourceKind: record.sourceKind,
      conditions: [record.condition],
      affectedCount: affected.length,
      affected: affected.slice(0, 25),
      resolution: {
        conditionStages: [{
          stageId: "<stage-id>",
          sources: [record.sourceKind],
          sourceIds,
          conditionTypes: !record.condition.description && record.condition.type
            ? [record.condition.type]
            : [],
          conditionKeys: collectLocalizationLeafKeys(record.condition),
          conditionDescriptions: record.condition.description
            ? [record.condition.description]
            : []
        }]
      }
    }, {
      sourceKind: record.sourceKind,
      condition: record.condition
    }));
  }

  const classificationReport = { ambiguousClasses: [] };
  const assignedDropSources = new Set(manifest.stages.flatMap(stage => [
    ...(stage.dropSources ?? []),
    ...(stage.enemies ?? []),
    ...(stage.containers ?? [])
  ]));
  for (const event of manifest.events ?? []) {
    for (const source of [
      ...(event.dropSources ?? []),
      ...(event.enemies ?? []),
      ...(event.containers ?? [])
    ]) {
      assignedDropSources.add(source);
    }
  }
  const assignedShops = new Set(
    manifest.stages.flatMap(stage => stage.shops ?? []));
  for (const shop of assignedShops) assignedDropSources.add(shop);
  const missingVanillaSources = new Map();
  const recordMissingSource = (source, sourceKind, item) => {
    if (!source.startsWith("Terraria/")) return;
    const key = `${sourceKind}:${source}`;
    const record = missingVanillaSources.get(key) ?? {
      source,
      sourceKind,
      items: []
    };
    if (!record.items.includes(item)) record.items.push(item);
    missingVanillaSources.set(key, record);
  };
  const coverageClassificationReport = { ambiguousClasses: [] };
  for (const drop of snapshot.drops) {
    if (drop.sourceType !== "npc"
        || assignedDropSources.has(drop.source)
        || !classifyItem(
          itemById.get(drop.item),
          manifest,
          coverageClassificationReport,
          classificationContext)) {
      continue;
    }
    recordMissingSource(drop.source, "drop", drop.item);
  }
  for (const shop of snapshot.shops) {
    if (assignedShops.has(shop.npc)
        || !classifyItem(
          itemById.get(shop.item),
          manifest,
          coverageClassificationReport,
          classificationContext)) {
      continue;
    }
    recordMissingSource(shop.npc, "shop", shop.item);
  }
  report.unassignedVanillaNpcSources = [...missingVanillaSources.values()]
    .map(record => ({ ...record, items: record.items.sort() }))
    .sort((left, right) =>
      left.sourceKind.localeCompare(right.sourceKind)
      || left.source.localeCompare(right.source));
  if (report.unassignedVanillaNpcSources.length > 0) {
    report.unassignedVanillaNpcSourceCount = report.unassignedVanillaNpcSources.length;
  }

  for (const item of classificationContext.items) {
    if (available.has(item.id)
        || (entryByItem.get(item.id)?.evaluations.length ?? 0) > 0
        || ignoredItems.has(item.id)) {
      continue;
    }

    const classification = classifyItem(
      item,
      manifest,
      classificationReport,
      classificationContext);
    if (!isReviewableClassification(item, classification)) continue;

    const drops = snapshot.drops
      .filter(drop => (drop.rate ?? 1) > 0 && drop.item === item.id)
      .map(drop => ({
        sourceKind: drop.sourceType,
        source: drop.source,
        conditions: drop.conditions
      }));
    const shops = snapshot.shops
      .filter(shop => shop.item === item.id)
      .map(shop => ({
        sourceKind: "shop",
        source: shop.npc,
        shop: shop.shop,
        conditions: shop.conditions
      }));
    const recipes = (recipesByResult.get(item.id) ?? []).map(recipe => ({
      ingredients: recipe.ingredients,
      stations: recipe.stations,
      conditions: recipe.conditions
    }));
    const shimmer = (snapshotShimmerTransforms ?? [])
      .filter(transform => transform.output === item.id)
      .map(transform => ({ sourceKind: "shimmer", source: transform.input, conditions: [] }));
    const evidence = { drops, shops, recipes, shimmer };
    if (availabilityEvidenceIsUnavailable(evidence)) {
      report.unavailableCombatItems.push({
        item: item.id,
        displayName: item.name,
        classification,
        evidence
      });
      continue;
    }
    if (availabilityEvidenceIsAbsent(item, evidence)) {
      report.unresolvedAvailabilityItems.push({
        item: item.id,
        displayName: item.name,
        classification
      });
      continue;
    }

    issues.push(createReviewIssue("unassigned-combat-item", {
      item: item.id,
      displayName: item.name,
      classification,
      evidence,
      resolution: {
        itemStages: { [item.id]: "<stage-id>" },
        sourceStages: Object.fromEntries(
          drops.map(drop => [drop.source, "<stage-id>"]))
      }
    }));
  }

  for (const record of uniqueBy(
    classificationReport.ambiguousClasses,
    value => value.item)) {
    if (ignoredItems.has(record.item)) continue;
    issues.push(createReviewIssue("ambiguous-classes", {
      item: record.item,
      classes: record.classes,
      resolution: {
        itemOverrides: {
          [record.item]: { classes: ["<class-id>"] }
        }
      }
    }));
  }

  for (const record of report.wikiUnresolvedItems) {
    if (ignoredItems.has(record.id)) continue;
    issues.push(createReviewIssue("unresolved-wiki-item", {
      item: record.id,
      displayName: record.displayName,
      category: record.category,
      resolution: {
        ignoredItems: [record.id]
      }
    }));
  }

  for (const record of report.wikiAmbiguousItems) {
    issues.push(createReviewIssue("ambiguous-wiki-item", {
      ...record,
      resolution: {
        ignoredItems: record.source ? [record.source] : []
      }
    }));
  }

  for (const problem of report.manualAssignmentProblems) {
    issues.push(createReviewIssue("invalid-manual-assignment", problem));
  }

  const visibleIssues = issues
    .filter(issue => !ignoredIssues.has(issue.id))
    .sort((left, right) =>
      left.kind.localeCompare(right.kind)
      || (left.item ?? left.source ?? left.id)
        .localeCompare(right.item ?? right.source ?? right.id));

  const byKind = Object.fromEntries(
    [...new Set(visibleIssues.map(issue => issue.kind))]
      .map(kind => [kind, visibleIssues.filter(issue => issue.kind === kind).length]));
  return {
    format: "ProgressionJournalManualReview",
    version: 1,
    profileId: manifest.id,
    generatedAtUtc: new Date().toISOString(),
    summary: {
      total: visibleIssues.length,
      byKind
    },
    issues: visibleIssues
  };
}

function collectUnknownConditionRecords(snapshot, manifest, contentMods) {
  const records = [
    ...snapshot.drops
      .filter(drop => (drop.rate ?? 1) > 0 && isAllowedProfileItem(drop.item, contentMods))
      .flatMap(drop => (drop.conditions ?? []).map(condition => ({
        sourceKind: "drop",
        source: drop.source,
        item: drop.item,
        condition
      }))),
    ...snapshot.shops
      .filter(shop => isAllowedProfileItem(shop.item, contentMods))
      .flatMap(shop => (shop.conditions ?? []).map(condition => ({
        sourceKind: "shop",
        source: shop.npc,
        item: shop.item,
        condition
      }))),
    ...snapshot.recipes
      .filter(recipe => isAllowedProfileItem(recipe.result, contentMods))
      .flatMap(recipe => (recipe.conditions ?? []).map(condition => ({
        sourceKind: "recipe",
        source: recipe.result,
        item: recipe.result,
        condition
      })))
  ];

  return records.filter(record =>
    record.condition.type
    && !conditionHasAssignment(
      record.condition,
      record.sourceKind,
      record.source,
      manifest));
}

function isReviewableClassification(item, classification) {
  return !!classification
    && (!classification.buffCategory || !item?.id?.startsWith("Terraria/"));
}


function availabilityEvidenceIsUnavailable(evidence) {
  const paths = [
    ...(evidence.drops ?? []),
    ...(evidence.shops ?? []),
    ...(evidence.recipes ?? []),
    ...(evidence.shimmer ?? [])
  ];
  return paths.length > 0 && paths.every(path =>
    (path.conditions ?? []).some(condition =>
      isUnavailableCondition(condition) || isDefaultExcludedVariantCondition(condition)));
}

function availabilityEvidenceIsAbsent(item, evidence) {
  return item.id?.startsWith("Terraria/")
    && (evidence.drops ?? []).length === 0
    && (evidence.shops ?? []).length === 0
    && (evidence.recipes ?? []).length === 0
    && (evidence.shimmer ?? []).length === 0;
}

function createReviewIssue(kind, values, identity = values) {
  const signature = JSON.stringify({ kind, ...identity, resolution: undefined });
  return {
    id: `${kind}.${createHash("sha1").update(signature).digest("hex").slice(0, 12)}`,
    kind,
    ...values
  };
}

function appendUnique(values, value, selector) {
  const signature = selector(value);
  if (!values.some(existing => selector(existing) === signature)) values.push(value);
}

function uniqueBy(values, selector) {
  return [...new Map(values.map(value => [selector(value), value])).values()];
}

function toItemReference(item) {
  const slash = item.id.indexOf("/");
  return { mod: item.id.slice(0, slash), item: item.id.slice(slash + 1), displayName: item.name };
}

function allClasses(manifest) {
  return manifest.classes.map(cls => cls.id);
}

function unlock(id, stage, via, available, acquiredBy, metadata = {}) {
  if (available.has(id)) return false;
  acquiredBy.set(id, { stage, via, ...metadata });
  available.add(id);
  return true;
}

function unlockPlacedStations(
  available,
  itemById,
  availableStations,
  stationAllowed = () => true) {
  let changed = false;
  for (const id of available) {
    const station = itemById.get(id)?.placedTile;
    if (station && stationAllowed(station) && !availableStations.has(station)) {
      availableStations.add(station);
      changed = true;
    }
  }
  return changed;
}

function inheritedContainerEventMetadata(container, stageId, acquiredBy) {
  const source = acquiredBy.get(container);
  return source?.stage === stageId
    && (source.eventCategory || source.customEventName)
    ? {
        eventCategory: source.eventCategory ?? null,
        customEventName: source.customEventName ?? "",
        eventIcon: source.eventIcon ?? ""
      }
    : {};
}

function toFishingSources(records) {
  return records
    .filter(record => (record.conditions ?? []).some(Boolean))
    .map(record => ({
      conditions: uniqueBy(
        (record.conditions ?? []).filter(Boolean),
        condition => typeof condition === "string"
          ? `string:${condition}`
          : `json:${JSON.stringify(condition)}`)
    }));
}

function toEventMetadata(event) {
  return {
    eventCategory: event.eventCategory ?? null,
    customEventName: event.customEventName ?? "",
    eventIcon: event.eventIcon ?? ""
  };
}

function groupBy(values, selector) {
  const result = new Map();
  for (const value of values) {
    const key = selector(value);
    if (!result.has(key)) result.set(key, []);
    result.get(key).push(value);
  }
  return result;
}

function slug(value) {
  return value.toLowerCase().replace(/[^a-z0-9]+/g, ".");
}

function assert(value, message) {
  if (!value) throw new Error(message);
}
