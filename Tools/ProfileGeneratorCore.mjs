import fs from "node:fs";
import path from "node:path";
import { createHash } from "node:crypto";
import { applyVanillaSourceCatalog } from "./VanillaSourceCatalog.mjs";

export function readJson(file) {
  return JSON.parse(fs.readFileSync(file, "utf8"));
}

export function writeJson(file, value) {
  fs.mkdirSync(path.dirname(file), { recursive: true });
  fs.writeFileSync(file, `${JSON.stringify(value, null, 2)}\n`, "utf8");
}

export function generateProfile(
  snapshot,
  manifest,
  wikiProfile = null,
  manualAssignments = null) {
  assert(snapshot.format === "ProgressionJournalSnapshot", "Invalid snapshot format.");
  assert(
    snapshot.version === 1 || snapshot.version === 2,
    `Unsupported snapshot version '${snapshot.version}'.`);

  manifest = applyVanillaSourceCatalog(manifest);
  const manualResult = applyManualAssignments(manifest, manualAssignments);
  manifest = manualResult.manifest;
  const contentMods = new Set(
    manifest.contentMods
    ?? (manifest.requiredMods ?? []).map(requiredMod => requiredMod.name));
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
      (drop.rate ?? 1) > 0
      &&
      isAllowedProfileItem(drop.item, contentMods)
      && isAllowedProfileItem(drop.source, contentMods)),
    drop => drop.source);
  const shopsByNpc = groupBy(
    snapshot.shops.filter(shop =>
      isAllowedProfileItem(shop.item, contentMods)
      && isAllowedProfileItem(shop.npc, contentMods)),
    shop => shop.npc);
  const acquiredBy = new Map();
  const available = new Set(manifest.initialItems ?? []);
  const availableStations = new Set(manifest.initialStations ?? []);
  const availableDropSources = new Set();
  const availableShops = new Set();
  const stageIndexes = new Map(manifest.stages.map((stage, index) => [stage.id, index]));
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
    staleRules: [],
    manualAssignmentProblems: manualResult.problems
  };
  Object.defineProperty(manifest, "_stageIndexes", { value: stageIndexes });
  Object.defineProperty(report, "_unresolvedConditionSignatures", { value: new Set() });
  const usedStations = new Set(
    snapshot.recipes
      .filter(recipe => isAllowedProfileItem(recipe.result, contentMods))
      .flatMap(recipe => recipe.stations));
  const classificationContext = { usedStations, items: allowedItems };
  const wikiResolver = createWikiReferenceResolver(allowedItems, report);
  const wikiItemIds = getWikiItemIds(wikiProfile, manifest, wikiResolver);
  const modifiedVanillaItems = new Set(manifest.modifiedVanillaItems ?? []);
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
    const before = new Set(available);
    const stageSources = [
      ...(stage.dropSources ?? []),
      ...(stage.enemies ?? []),
      ...(stage.containers ?? [])
    ];
    for (const source of stageSources) availableDropSources.add(source);
    for (const npc of stage.shops ?? []) availableShops.add(npc);

    for (const source of availableDropSources) {
      for (const drop of dropsBySource.get(source) ?? []) {
        if (conditionsAllowed(drop.conditions, stage, manifest, report, {
          sourceKind: "drop",
          source,
          item: drop.item
        })) {
          unlockAtStage(drop.item, stage.id, `${drop.sourceType}:${source}`);
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
        availableDropSources.add(source);
        for (const drop of dropsBySource.get(source) ?? []) {
          if (conditionsAllowed(drop.conditions, stage, manifest, report, {
            sourceKind: "drop",
            source,
            item: drop.item
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
      for (const shop of shopsByNpc.get(npc) ?? []) {
        if (conditionsAllowed(shop.conditions, stage, manifest, report, {
          sourceKind: "shop",
          source: npc,
          item: shop.item
        })) {
          unlockAtStage(shop.item, stage.id, `shop:${npc}`);
        }
      }
    }
    for (const id of [...(stage.materials ?? []), ...(stage.include ?? [])]) {
      unlockAtStage(id, stage.id, "manifest");
    }
    for (const station of stage.stations ?? []) {
      if (stationAllowedAtStage(station, stage.id)) availableStations.add(station);
    }

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
        for (const drop of dropsBySource.get(container) ?? []) {
          if (!conditionsAllowed(drop.conditions, stage, manifest, report, {
            sourceKind: "drop",
            source: container,
            item: drop.item
          })) {
            continue;
          }
          if (!available.has(drop.item)) {
            if (unlockAtStage(
              drop.item,
              stage.id,
              `container:${container}`,
              inheritedEventMetadata([{ item: container }], stage.id, acquiredBy))) {
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
            item: result
          }));
        if (!openRecipe) continue;

        if (!unlockAtStage(
          result,
          stage.id,
          `recipe:${openRecipe.ingredients.map(value => value.item).join("+")}`,
          inheritedEventMetadata(openRecipe.ingredients, stage.id, acquiredBy))) {
          continue;
        }
        const item = itemById.get(result);
        if (item?.placedTile) availableStations.add(item.placedTile);
        changed = true;
      }
    }

    for (const id of stage.exclude ?? []) available.delete(id);
    const delta = [...available].filter(id => !before.has(id));
    let visibleCount = 0;
    for (const id of delta) {
      const item = itemById.get(id);
      if (!item) {
        report.unknownReferences.push({ stage: stage.id, id });
        continue;
      }
      if (shouldExcludeVanillaItem(id, manifest, wikiItemIds, modifiedVanillaItems)) {
        report.excludedItems.push({
          stage: stage.id,
          id,
          reason: "unchanged vanilla item in mod profile"
        });
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
          itemGroups: [[itemReference]]
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
          fishingSources: manifest.fishingSources?.[id] ?? [],
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
    ...(manualAssignments.itemStages ?? {})
  };
  manifest.stationStageFloors = {
    ...(manifest.stationStageFloors ?? {}),
    ...(manualAssignments.stationStages ?? {})
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
      conditionDescriptions: assignment.conditionDescriptions ?? []
    });
  }

  return { manifest, problems };
}

function conditionMatchesUnlockRule(condition, rule) {
  if ((rule.conditionTypes ?? []).includes(condition.type)) return true;
  const description = normalizeConditionText(condition.description);
  return (rule.conditionDescriptions ?? [])
    .some(value => normalizeConditionText(value) === description);
}

function normalizeConditionText(value) {
  return (value ?? "").trim().toLocaleLowerCase();
}

function classifyItem(item, manifest, report, context) {
  const override = manifest.itemOverrides?.[item.id];
  if (override?.exclude) return null;
  if (item.vanity || item.sourceNamespace?.split(".").includes("Vanity")) return null;
  if (override?.buffCategory) {
    return { buffCategory: override.buffCategory, classes: override.classes ?? allClasses(manifest) };
  }

  if (item.placedTile && context.usedStations.has(item.placedTile)) {
    return null;
  }
  if (item.flask) return { buffCategory: "Flask", classes: override?.classes ?? allClasses(manifest) };
  if (item.food) return { buffCategory: "Food", classes: override?.classes ?? allClasses(manifest) };
  if (item.healLife > 0 || item.healMana > 0) {
    return { buffCategory: "Basic", classes: override?.classes ?? allClasses(manifest) };
  }

  if (item.pick > 0 || item.axe > 0 || item.hammer > 0 || item.mountType >= 0
      || item.createWall >= 0 || (item.createTile >= 0 && item.damage <= 0 && item.buffType <= 0)) {
    return null;
  }

  const classes = override?.classes ?? resolveClasses(item, manifest, report, context);
  if (classes.length === 0) return null;
  if (override?.category) return { category: override.category, classes };
  if (item.ammo > 0) return { category: "Ammunition", classes };
  if (item.damage > 0 || item.sentry) return { category: "Weapon", classes };
  if (item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0) return { category: "Armor", classes };
  if (item.accessory) return { category: "Accessory", classes };
  if (item.buffType > 0 && item.consumable) {
    return { buffCategory: "Potion", classes: allClasses(manifest) };
  }
  if (classes.length < allClasses(manifest).length && item.damageClass) {
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

  const effectClasses = new Set();
  let hasGenericEffect = false;
  for (const effect of item.classEffects ?? []) {
    if (effect.damageClass.toLowerCase().includes("genericdamageclass")) {
      hasGenericEffect = true;
      continue;
    }
    for (const cls of resolveDamageClasses(effect.damageClass, manifest)) effectClasses.add(cls);
  }
  if (!hasGenericEffect && effectClasses.size === 1) {
    return [...effectClasses];
  }
  if (hasGenericEffect || effectClasses.size > 1) {
    return allClasses(manifest);
  }

  const matches = resolveDamageClasses(item.damageClass, manifest);
  if (matches.length > 1) report.ambiguousClasses.push({ item: item.id, classes: matches });
  if (matches.length > 0) return matches;
  if (item.accessory || item.defense > 0 || item.buffType > 0) return allClasses(manifest);
  return [];
}

function resolveDamageClasses(damageClass, manifest) {
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
    if (!condition.type) continue;
    if (isProgressionNeutralCondition(condition)) continue;
    const rule = manifest.conditionRules?.[condition.type];
    if (rule === "allow") continue;
    if (rule?.stages?.includes(stage.id)) continue;
    const assignedStageIndex = assignedConditionStageIndex(
      condition,
      context.sourceKind,
      context.source,
      manifest,
      stageIndexes);
    if (assignedStageIndex < 0) {
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
    if (currentStageIndex < assignedStageIndex) return false;
  }
  return true;
}

function assignedConditionStageIndex(
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

function conditionHasAssignment(condition, sourceKind, source, manifest) {
  if (isProgressionNeutralCondition(condition)) return true;
  if (manifest.conditionRules?.[condition.type]) return true;
  return (manifest.conditionUnlocks ?? []).some(rule =>
    (rule.sources ?? ["drop", "shop", "recipe"]).includes(sourceKind)
    && ((rule.sourceIds ?? []).length === 0 || rule.sourceIds.includes(source))
    && conditionMatchesUnlockRule(condition, rule));
}

function isProgressionNeutralCondition(condition) {
  return new Set([
    "Terraria.GameContent.ItemDropRules.Conditions+IsExpert",
    "Terraria.GameContent.ItemDropRules.Conditions+NotExpert",
    "Terraria.GameContent.ItemDropRules.Conditions+IsMasterMode",
    "Terraria.GameContent.ItemDropRules.Conditions+NotMasterMode",
    "Terraria.GameContent.ItemDropRules.Conditions+LegacyHack_IsBossAndNotExpert"
  ]).has(condition.type);
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

function getWikiItemIds(wikiProfile, manifest, wikiResolver) {
  if (!wikiProfile) return new Set();

  const ids = new Set();
  for (const sourceEntry of wikiProfile.entries ?? []) {
    const hasMappedStage = (sourceEntry.evaluations ?? [])
      .some(evaluation => manifest.wikiStageMap?.[evaluation.stageId]);
    if (!hasMappedStage) continue;

    for (const id of resolveWikiEntryIds(sourceEntry, wikiResolver)) ids.add(id);
  }
  return ids;
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
      && normalizeWikiText(item.name).startsWith(stem)));
    const heads = candidates.filter(item => item.headSlot >= 0);
    const bodies = candidates.filter(item => item.bodySlot >= 0);
    const legs = candidates.filter(item => item.legSlot >= 0);
    const isSingleSet = heads.length > 0 && bodies.length === 1 && legs.length === 1;
    const isParallelSet = heads.length >= 2
      && heads.length === bodies.length
      && bodies.length === legs.length
      && new Set(heads.map(item => normalizeWikiText(item.name))).size === 1
      && new Set(bodies.map(item => normalizeWikiText(item.name))).size === 1
      && new Set(legs.map(item => normalizeWikiText(item.name))).size === 1;
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

    const candidates = uniqueItems(modItems.filter(item => {
      const name = normalizeWikiText(item.name);
      return itemMatchesWikiCategory(item, category)
        && name !== target
        && (name.startsWith(target) || name.endsWith(target));
    }));
    if (candidates.length < 2) return null;
    return { ids: candidates.map(item => item.id), method: "variant-family" };
  }
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

function shouldExcludeVanillaItem(id, manifest, wikiItemIds, modifiedVanillaItems) {
  return (manifest.requiredMods?.length ?? 0) > 0
    && id.startsWith("Terraria/")
    && !wikiItemIds.has(id)
    && !modifiedVanillaItems.has(id);
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
  recipesByResult
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
    items: snapshot.items.filter(item => isAllowedProfileItem(item.id, contentMods))
  };
  const conditionClassificationReport = { ambiguousClasses: [] };

  const unresolvedConditions = uniqueBy(
    [
      ...report.unresolvedConditions,
      ...collectUnknownConditionRecords(snapshot, manifest, contentMods)
    ],
    value => JSON.stringify(value))
    .filter(record =>
      (isOwnedByContentMod(record.item, contentMods)
        || (manifest.modifiedVanillaItems ?? []).includes(record.item))
      && !ignoredItems.has(record.item)
      && classifyItem(
        itemById.get(record.item),
        manifest,
        conditionClassificationReport,
        classificationContext));
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

  for (const item of classificationContext.items) {
    if (!isOwnedByContentMod(item.id, contentMods)
        || available.has(item.id)
        || (entryByItem.get(item.id)?.evaluations.length ?? 0) > 0
        || ignoredItems.has(item.id)) {
      continue;
    }

    const classification = classifyItem(
      item,
      manifest,
      classificationReport,
      classificationContext);
    if (!classification) continue;

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

    issues.push(createReviewIssue("unassigned-combat-item", {
      item: item.id,
      displayName: item.name,
      classification,
      evidence: { drops, shops, recipes },
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

function createReviewIssue(kind, values, identity = values) {
  const signature = JSON.stringify({ kind, ...identity, resolution: undefined });
  return {
    id: `${kind}.${createHash("sha1").update(signature).digest("hex").slice(0, 12)}`,
    kind,
    ...values
  };
}

function isOwnedByContentMod(id, contentMods) {
  const slash = id.indexOf("/");
  return slash > 0 && contentMods.has(id.slice(0, slash));
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

function inheritedEventMetadata(ingredients, stageId, acquiredBy) {
  const eventSources = ingredients
    .map(ingredient => acquiredBy.get(ingredient.item))
    .filter(source =>
      source?.stage === stageId
      && (source.eventCategory || source.customEventName));
  if (eventSources.length === 0) return {};

  const first = eventSources[0];
  const sameEvent = eventSources.every(source =>
    source.eventCategory === first.eventCategory
    && source.customEventName === first.customEventName
    && source.eventIcon === first.eventIcon);
  return sameEvent
    ? {
        eventCategory: first.eventCategory ?? null,
        customEventName: first.customEventName ?? "",
        eventIcon: first.eventIcon ?? ""
      }
    : {};
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
