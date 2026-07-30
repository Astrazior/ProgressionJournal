import fs from "node:fs";
import path from "node:path";

export const GENERATED_PROFILE_FILES = [
  "profile.json",
  "knowledge.json",
  "review.json",
  "report.json",
  "item-audit.json",
  "source-gaps.json",
  "excluded-noncombat-items.json"
];

const SOURCE_RANK = new Map([
  ["missing", 0],
  ["uncovered", 1],
  ["declared", 2],
  ["observed", 3]
]);
const LOWER_IS_BAD_METRICS = new Set([
  "snapshotItems",
  "contentItems",
  "profileItemReferences",
  "knowledgeItems",
  "recipes",
  "drops",
  "shops",
  "fishing",
  "npcAvailability",
  "observedSources",
  "observedSpawnSources",
  "observedTownSources"
]);
const HIGHER_IS_BAD_METRICS = new Set([
  "unresolvedAvailability",
  "unavailableCombat",
  "profileSourceGaps",
  "noAcquisitionPath",
  "reviewIssues",
  "auditErrors",
  "auditWarnings",
  "uncoveredSources"
]);
const NON_BLOCKING_ACQUISITION_TARGETS = new Set([
  "Terraria/CopperCoin",
  "Terraria/SilverCoin",
  "Terraria/GoldCoin",
  "Terraria/PlatinumCoin"
]);

function readJson(file) {
  return JSON.parse(fs.readFileSync(file, "utf8"));
}

function readOptionalJson(file, fallback) {
  return fs.existsSync(file) ? readJson(file) : fallback;
}

function canonicalValue(value) {
  if (Array.isArray(value)) return value.map(canonicalValue);
  if (!value || typeof value !== "object") return value;
  return Object.fromEntries(
    Object.keys(value)
      .sort()
      .map(key => [key, canonicalValue(value[key])]));
}

function stableJson(value) {
  return JSON.stringify(canonicalValue(value));
}

function isConditionRecord(value) {
  return value
    && typeof value === "object"
    && !Array.isArray(value)
    && ("type" in value || "key" in value || "facts" in value)
    && "description" in value;
}

function acquisitionCoreValue(value) {
  if (Array.isArray(value)) return value.map(acquisitionCoreValue);
  if (!value || typeof value !== "object") return value;
  const excluded = new Set();
  if (isConditionRecord(value)) {
    excluded.add("description");
    excluded.add("key");
    excluded.add("facts");
  }
  if ("sourceType" in value && "source" in value && "item" in value) {
    excluded.add("rate");
    excluded.add("stackMin");
    excluded.add("stackMax");
  }
  if ("npc" in value && "shop" in value && "item" in value) {
    excluded.add("observed");
    excluded.add("earliestStageIndex");
    excluded.add("earliestStageId");
    excluded.add("earliestStageName");
  }
  return Object.fromEntries(
    Object.keys(value)
      .filter(key => !excluded.has(key))
      .sort()
      .map(key => [key, acquisitionCoreValue(value[key])]));
}

function acquisitionMachineEvidence(value, result = []) {
  if (Array.isArray(value)) {
    for (const entry of value) acquisitionMachineEvidence(entry, result);
    return result;
  }
  if (!value || typeof value !== "object") return result;
  if (isConditionRecord(value)) {
    if (value.key) result.push(`key:${value.key}`);
    for (const fact of value.facts ?? []) {
      result.push(`fact:${stableJson(fact)}`);
    }
  }
  for (const entry of Object.values(value)) {
    acquisitionMachineEvidence(entry, result);
  }
  return sortedUnique(result);
}

function sortedUnique(values) {
  return [...new Set(values.filter(Boolean))].sort();
}

function localizedName(value) {
  if (typeof value === "string") return value;
  return value?.["ru-RU"] ?? value?.["en-US"] ?? "";
}

function itemId(reference) {
  if (!reference?.mod || !reference?.item) return "";
  return `${reference.mod}/${reference.item}`;
}

function collectStageData(profile) {
  const order = new Map();
  const labels = new Map();
  for (const [index, stage] of (profile?.stages ?? []).entries()) {
    order.set(stage.id, index);
    labels.set(stage.id, localizedName(stage.name) || stage.id);
  }
  return { order, labels };
}

function collectProfileItems(profile) {
  const items = new Map();
  const add = (reference, stageIds, metadata) => {
    const id = itemId(reference);
    if (!id) return;
    const existing = items.get(id) ?? {
      id,
      name: reference.displayName ?? id,
      stageIds: [],
      categories: [],
      classes: [],
      kinds: [],
      entryKeys: []
    };
    existing.stageIds = sortedUnique([...existing.stageIds, ...stageIds]);
    existing.categories = sortedUnique([...existing.categories, metadata.category]);
    existing.classes = sortedUnique([...existing.classes, ...(metadata.classes ?? [])]);
    existing.kinds = sortedUnique([...existing.kinds, metadata.kind]);
    existing.entryKeys = sortedUnique([...existing.entryKeys, metadata.entryKey]);
    items.set(id, existing);
  };

  for (const entry of profile?.entries ?? []) {
    const stageIds = sortedUnique([
      ...(entry.evaluations ?? []).map(evaluation => evaluation?.stageId),
      entry.stageId
    ]);
    for (const reference of (entry.itemGroups ?? []).flat()) {
      add(reference, stageIds, {
        category: entry.category,
        classes: entry.classes,
        kind: "equipment",
        entryKey: entry.key
      });
    }
  }

  for (const entry of profile?.combatBuffs ?? []) {
    const stageIds = sortedUnique([
      entry.stageId,
      ...(entry.evaluations ?? []).map(evaluation => evaluation?.stageId)
    ]);
    for (const reference of (entry.itemGroups ?? []).flat()) {
      add(reference, stageIds, {
        category: entry.category,
        classes: entry.classes,
        kind: "buff",
        entryKey: entry.key
      });
    }
  }

  return items;
}

function collectSourceCoverage(report) {
  const sources = new Map();
  const coverage = report?.audit?.sourceCoverage ?? {};
  for (const status of ["uncovered", "declared", "observed"]) {
    for (const record of coverage[status] ?? []) {
      if (!record?.source) continue;
      const current = sources.get(record.source);
      if (current && SOURCE_RANK.get(current.status) > SOURCE_RANK.get(status)) continue;
      sources.set(record.source, {
        source: record.source,
        status,
        kinds: sortedUnique(record.kinds ?? []),
        availabilityKind: record.availabilityKind ?? "",
        earliestStageIndex: Number.isInteger(record.earliestStageIndex)
          ? record.earliestStageIndex
          : -1
      });
    }
  }
  return sources;
}

function acquisitionEntry(kind, target, identity, description, details) {
  return {
    key: `${kind}|${target}|${identity}`,
    kind,
    target,
    description,
    details: canonicalValue(details)
  };
}

function collectAcquisitionEvidence(knowledge) {
  const result = new Map();
  const add = entry => {
    const existing = result.get(entry.key) ?? {
      key: entry.key,
      kind: entry.kind,
      target: entry.target,
      description: entry.description,
      records: []
    };
    existing.records.push(entry.details);
    result.set(entry.key, existing);
  };
  const acquisitions = knowledge?.acquisitions ?? {};

  for (const recipe of acquisitions.recipes ?? []) {
    const ingredients = (recipe.ingredients ?? [])
      .map(value => `${value.item}x${value.stack ?? 1}`)
      .sort();
    const stations = sortedUnique(recipe.stations ?? []);
    add(acquisitionEntry(
      "recipe",
      recipe.result ?? "",
      stableJson({ ingredients, stations }),
      `recipe ${recipe.result ?? "<unknown>"} <= ${ingredients.join(" + ") || "<none>"}`
        + `${stations.length > 0 ? ` @ ${stations.join(", ")}` : ""}`,
      recipe));
  }

  for (const drop of acquisitions.drops ?? []) {
    add(acquisitionEntry(
      "drop",
      drop.item ?? "",
      `${drop.sourceType ?? ""}|${drop.source ?? ""}`,
      `${drop.sourceType || "source"} ${drop.source || "<unknown>"} -> ${drop.item || "<unknown>"}`,
      drop));
  }

  for (const shop of acquisitions.shops ?? []) {
    add(acquisitionEntry(
      "shop",
      shop.item ?? "",
      `${shop.npc ?? ""}|${shop.shop ?? ""}`,
      `shop ${shop.npc || "<unknown>"}/${shop.shop || "<unknown>"} -> ${shop.item || "<unknown>"}`,
      shop));
  }

  for (const fishing of acquisitions.fishing ?? []) {
    const target = fishing.targetType === "item" ? fishing.target ?? "" : "";
    if (!target) continue;
    add(acquisitionEntry(
      "fishing",
      target,
      "catch",
      `fishing -> ${target}`,
      fishing));
  }

  for (const shimmer of acquisitions.shimmerTransforms ?? []) {
    add(acquisitionEntry(
      "shimmer",
      shimmer.output ?? "",
      shimmer.input ?? "",
      `shimmer ${shimmer.input || "<unknown>"} -> ${shimmer.output || "<unknown>"}`,
      shimmer));
  }

  for (const entry of result.values()) {
    entry.records.sort((left, right) => stableJson(left).localeCompare(stableJson(right)));
  }
  return result;
}

function collectReviewIssues(review) {
  const result = new Map();
  for (const issue of review?.issues ?? []) {
    const key = stableJson(reviewIssueCoreValue(issue));
    result.set(key, canonicalValue(issue));
  }
  return result;
}

function reviewIssueCoreValue(value) {
  if (Array.isArray(value)) return value.map(reviewIssueCoreValue);
  if (!value || typeof value !== "object") return value;
  const excluded = new Set(["id"]);
  if (isConditionRecord(value)) {
    excluded.add("key");
    excluded.add("facts");
  }
  return Object.fromEntries(
    Object.keys(value)
      .filter(key => !excluded.has(key) && key !== "conditionKeys")
      .sort()
      .map(key => [key, reviewIssueCoreValue(value[key])]));
}

function collectMetrics(itemAudit, knowledge, review, report) {
  const auditSummary = itemAudit?.summary ?? {};
  const knowledgeSummary = knowledge?.summary ?? {};
  const coverage = report?.audit?.sourceCoverage ?? {};
  return {
    snapshotItems: auditSummary.snapshotItems ?? null,
    contentItems: auditSummary.contentItems ?? null,
    profileItemReferences: auditSummary.profileItemReferences ?? null,
    unresolvedAvailability: auditSummary.unresolvedAvailability ?? null,
    unavailableCombat: auditSummary.unavailableCombat ?? null,
    profileSourceGaps: auditSummary.profileSourceGaps ?? null,
    noAcquisitionPath: auditSummary.noAcquisitionPath ?? null,
    knowledgeItems: knowledgeSummary.items ?? null,
    recipes: knowledgeSummary.recipes ?? null,
    drops: knowledgeSummary.drops ?? null,
    shops: knowledgeSummary.shops ?? null,
    fishing: knowledgeSummary.fishing ?? null,
    npcAvailability: knowledgeSummary.npcAvailability ?? null,
    reviewIssues: review?.summary?.total ?? review?.issues?.length ?? 0,
    auditErrors: report?.audit?.errors?.length ?? 0,
    auditWarnings: report?.audit?.warnings?.length ?? 0,
    observedSources: coverage.observed?.length ?? 0,
    observedSpawnSources: coverage.observedSpawnCount ?? 0,
    observedTownSources: coverage.observedTownCount ?? 0,
    uncoveredSources: coverage.uncovered?.length ?? 0
  };
}

function collectGenerationRecords(report) {
  const generation = report?.generation ?? {};
  return [
    ...(generation.unresolvedAvailabilityItems ?? []).map(record => ({
      kind: "unresolved-availability",
      ...canonicalValue(record)
    })),
    ...(generation.unavailableCombatItems ?? []).map(record => ({
      kind: "unavailable-combat",
      ...canonicalValue(record)
    })),
    ...(generation.profileItemSourceGaps ?? []).map(record => ({
      kind: "profile-item-source-gap",
      ...canonicalValue(record)
    })),
    ...(generation.excludedItems ?? []).map(record => ({
      kind: "excluded",
      item: record.id,
      ...canonicalValue(record)
    }))
  ];
}

export function captureProfileState(directory) {
  const profile = readOptionalJson(path.join(directory, "profile.json"), {
    stages: [],
    entries: [],
    combatBuffs: []
  });
  const itemAudit = readOptionalJson(path.join(directory, "item-audit.json"), { items: [] });
  const report = readOptionalJson(path.join(directory, "report.json"), {});
  const review = readOptionalJson(path.join(directory, "review.json"), { issues: [], summary: {} });
  const knowledge = readOptionalJson(path.join(directory, "knowledge.json"), {
    acquisitions: {},
    summary: {}
  });
  return {
    directory,
    exists: fs.existsSync(path.join(directory, "profile.json")),
    metadata: {
      profileId: profile.id ?? itemAudit.profileId ?? "",
      targetMod: itemAudit.targetMod ?? report.targetMod ?? "",
      generatedAtUtc: itemAudit.generatedAtUtc ?? report.generatedAtUtc ?? "",
      snapshotGeneratedAtUtc: itemAudit.snapshotGeneratedAtUtc
        ?? report.snapshot?.generatedAtUtc
        ?? ""
    },
    stageData: collectStageData(profile),
    profileItems: collectProfileItems(profile),
    auditItems: new Map((itemAudit.items ?? []).map(item => [item.id, canonicalValue(item)])),
    paths: report?.generation?.paths ?? {},
    sourceCoverage: collectSourceCoverage(report),
    acquisitions: collectAcquisitionEvidence(knowledge),
    reviewIssues: collectReviewIssues(review),
    generationRecords: collectGenerationRecords(report),
    auditErrors: canonicalValue(report?.audit?.errors ?? []),
    auditWarnings: canonicalValue(report?.audit?.warnings ?? []),
    metrics: collectMetrics(itemAudit, knowledge, review, report)
  };
}

function stageLabels(state, stageIds) {
  return stageIds.map(stageId => state.stageData.labels.get(stageId) ?? stageId);
}

function normalizeItemState(state, id) {
  const profileItem = state.profileItems.get(id);
  const auditItem = state.auditItems.get(id);
  const inProfile = Boolean(profileItem);
  const stageIds = profileItem?.stageIds?.length > 0
    ? profileItem.stageIds
    : sortedUnique([auditItem?.stage]);
  return {
    id,
    name: profileItem?.name ?? auditItem?.name ?? id,
    inProfile,
    status: auditItem?.status
      ?? (profileItem?.kinds?.includes("buff") ? "buff" : inProfile ? "equipment" : "missing"),
    stageIds,
    stageLabels: stageLabels(state, stageIds),
    via: auditItem?.via ?? state.paths[id]?.via ?? "",
    reason: auditItem?.reason ?? "",
    contentItem: auditItem?.contentItem ?? false,
    categories: profileItem?.categories ?? [],
    classes: profileItem?.classes ?? [],
    kinds: profileItem?.kinds ?? [],
    entryKeys: profileItem?.entryKeys ?? []
  };
}

function dependenciesFromVia(via) {
  if (via?.startsWith("recipe:")) {
    return via.slice("recipe:".length).split("+").filter(Boolean);
  }
  if (via?.startsWith("shimmer:")) {
    return [via.slice("shimmer:".length)].filter(Boolean);
  }
  return [];
}

function traceAcquisitionPath(paths, id, visited = new Set(), depth = 0) {
  if (depth >= 10 || visited.has(id)) {
    return { item: id, cycleOrLimit: true };
  }
  const record = paths[id];
  if (!record) return { item: id, missing: true };
  const nextVisited = new Set(visited);
  nextVisited.add(id);
  return {
    item: id,
    stage: record.stage ?? "",
    via: record.via ?? "",
    dependencies: dependenciesFromVia(record.via)
      .map(dependency => traceAcquisitionPath(paths, dependency, nextVisited, depth + 1))
  };
}

function issueAffectsItem(issue, id) {
  return issue?.item === id
    || (issue?.affected ?? []).some(value => value?.item === id);
}

function diagnosisForItem(state, item) {
  return {
    auditReason: item.reason,
    reviewIssues: [...state.reviewIssues.values()]
      .filter(issue => issueAffectsItem(issue, item.id)),
    generationRecords: state.generationRecords
      .filter(record => record.item === item.id || record.id === item.id),
    path: traceAcquisitionPath(state.paths, item.id)
  };
}

function firstStageIndex(state, item) {
  const indices = item.stageIds
    .map(stageId => state.stageData.order.get(stageId))
    .filter(Number.isInteger);
  return indices.length > 0 ? Math.min(...indices) : null;
}

function arraysEqual(left, right) {
  return stableJson(left) === stableJson(right);
}

function itemChangeKind(before, after) {
  if (!before.inProfile && after.inProfile) return "added-to-profile";
  if (before.inProfile && !after.inProfile) return "removed-from-profile";
  if (before.inProfile && after.inProfile && !arraysEqual(before.stageIds, after.stageIds)) {
    return "stage-changed";
  }
  if (before.inProfile && after.inProfile && before.via !== after.via) {
    return "source-path-changed";
  }
  if (before.status !== after.status) return "status-changed";
  if (before.via !== after.via) return "evidence-changed";
  if (!arraysEqual(
    [before.categories, before.classes, before.kinds, before.entryKeys],
    [after.categories, after.classes, after.kinds, after.entryKeys])) {
    return "metadata-changed";
  }
  return "";
}

function explainItemChange(kind, before, after) {
  const previous = `${before.status}${before.stageIds.length > 0
    ? ` @ ${before.stageIds.join(", ")}`
    : ""}${before.via ? ` via ${before.via}` : ""}`;
  const current = `${after.status}${after.stageIds.length > 0
    ? ` @ ${after.stageIds.join(", ")}`
    : ""}${after.via ? ` via ${after.via}` : ""}`;
  if (kind === "removed-from-profile") {
    if (after.status === "missing") {
      return `Предмет отсутствует в новом item-audit/snapshot. Ранее: ${previous}.`;
    }
    return `Предмет больше не входит в профиль: ${previous} -> ${current}. ${after.reason}`;
  }
  if (kind === "stage-changed") {
    return `Этап изменился: ${before.stageIds.join(", ") || "<none>"} -> `
      + `${after.stageIds.join(", ") || "<none>"}. Путь: `
      + `${before.via || "<none>"} -> ${after.via || "<none>"}.`;
  }
  if (kind === "source-path-changed") {
    return `Выбранный путь получения изменился: ${before.via || "<none>"} -> `
      + `${after.via || "<none>"}.`;
  }
  if (kind === "added-to-profile") {
    return `Предмет добавлен в профиль: ${current}.`;
  }
  return `Состояние изменилось: ${previous} -> ${current}. ${after.reason}`;
}

function compareItems(beforeState, afterState) {
  const ids = new Set([
    ...beforeState.profileItems.keys(),
    ...afterState.profileItems.keys(),
    ...beforeState.auditItems.keys(),
    ...afterState.auditItems.keys()
  ]);
  const changes = [];
  for (const id of [...ids].sort()) {
    const before = normalizeItemState(beforeState, id);
    const after = normalizeItemState(afterState, id);
    const kind = itemChangeKind(before, after);
    if (!kind) continue;
    const beforeIndex = firstStageIndex(beforeState, before);
    const afterIndex = firstStageIndex(afterState, after);
    const blocking = kind === "removed-from-profile"
      || kind === "stage-changed"
      || kind === "metadata-changed"
      || (kind === "evidence-changed" && Boolean(before.via) && !after.via);
    changes.push({
      id,
      name: after.name || before.name || id,
      kind,
      blocking,
      stageDirection: kind === "stage-changed" && beforeIndex !== null && afterIndex !== null
        ? (afterIndex > beforeIndex ? "later" : afterIndex < beforeIndex ? "earlier" : "changed")
        : "",
      before,
      after,
      explanation: explainItemChange(kind, before, after),
      beforeDiagnosis: diagnosisForItem(beforeState, before),
      afterDiagnosis: diagnosisForItem(afterState, after)
    });
  }
  return changes;
}

function missingSource(source) {
  return {
    source,
    status: "missing",
    kinds: [],
    availabilityKind: "",
    earliestStageIndex: -1
  };
}

function compareSources(beforeState, afterState) {
  const ids = new Set([
    ...beforeState.sourceCoverage.keys(),
    ...afterState.sourceCoverage.keys()
  ]);
  const changes = [];
  for (const id of [...ids].sort()) {
    const before = beforeState.sourceCoverage.get(id) ?? missingSource(id);
    const after = afterState.sourceCoverage.get(id) ?? missingSource(id);
    if (stableJson(before) === stableJson(after)) continue;
    const statusDowngraded = SOURCE_RANK.get(after.status) < SOURCE_RANK.get(before.status);
    const stageRegressed = before.earliestStageIndex >= 0
      && (after.earliestStageIndex < 0
        || after.earliestStageIndex > before.earliestStageIndex);
    const kindsLost = before.kinds.some(kind => !after.kinds.includes(kind));
    changes.push({
      source: id,
      kind: before.status === "missing"
        ? "source-added"
        : after.status === "missing"
          ? "source-removed"
          : "source-changed",
      blocking: statusDowngraded || stageRegressed || kindsLost,
      reasons: [
        statusDowngraded ? `coverage ${before.status} -> ${after.status}` : "",
        stageRegressed
          ? `earliestStageIndex ${before.earliestStageIndex} -> ${after.earliestStageIndex}`
          : "",
        kindsLost
          ? `lost kinds: ${before.kinds.filter(kind => !after.kinds.includes(kind)).join(", ")}`
          : ""
      ].filter(Boolean),
      before,
      after
    });
  }
  return changes;
}

function missingAcquisition(key) {
  return {
    key,
    kind: "missing",
    target: "",
    description: "<missing>",
    records: []
  };
}

function compareAcquisitions(beforeState, afterState) {
  const keys = new Set([
    ...beforeState.acquisitions.keys(),
    ...afterState.acquisitions.keys()
  ]);
  const changes = [];
  for (const key of [...keys].sort()) {
    const before = beforeState.acquisitions.get(key) ?? missingAcquisition(key);
    const after = afterState.acquisitions.get(key) ?? missingAcquisition(key);
    if (stableJson(before.records) === stableJson(after.records)) continue;
    const unmatchedAfter = new Set(after.records.map((_, index) => index));
    const matched = [];
    const removedRecords = [];
    for (const beforeRecord of before.records) {
      const core = stableJson(acquisitionCoreValue(beforeRecord));
      const candidates = [...unmatchedAfter]
        .filter(index =>
          stableJson(acquisitionCoreValue(after.records[index])) === core)
        .map(index => {
          const beforeEvidence = acquisitionMachineEvidence(beforeRecord);
          const afterEvidence = acquisitionMachineEvidence(after.records[index]);
          return {
            index,
            lostEvidence: beforeEvidence.filter(value => !afterEvidence.includes(value)),
            addedEvidence: afterEvidence.filter(value => !beforeEvidence.includes(value))
          };
        })
        .sort((left, right) =>
          left.lostEvidence.length - right.lostEvidence.length
          || left.addedEvidence.length - right.addedEvidence.length);
      const best = candidates[0];
      if (!best) {
        removedRecords.push(beforeRecord);
        continue;
      }
      unmatchedAfter.delete(best.index);
      matched.push({
        before: beforeRecord,
        after: after.records[best.index],
        lostEvidence: best.lostEvidence,
        addedEvidence: best.addedEvidence
      });
    }
    const addedRecords = [...unmatchedAfter].map(index => after.records[index]);
    const redundantRemovedRecords = [];
    const substantiveRemovedRecords = removedRecords.filter(beforeRecord => {
      const core = stableJson(acquisitionCoreValue(beforeRecord));
      const beforeEvidence = acquisitionMachineEvidence(beforeRecord);
      const replacement = after.records.find(afterRecord =>
        stableJson(acquisitionCoreValue(afterRecord)) === core
        && beforeEvidence.every(value =>
          acquisitionMachineEvidence(afterRecord).includes(value)));
      if (!replacement) return true;
      redundantRemovedRecords.push(beforeRecord);
      return false;
    });
    const evidenceRegressions = matched.filter(record => record.lostEvidence.length > 0);
    const evidenceEnrichments = matched.filter(record => record.addedEvidence.length > 0);
    const coreChanged = substantiveRemovedRecords.length > 0 || addedRecords.length > 0;
    const target = after.target || before.target;
    const blocking = !NON_BLOCKING_ACQUISITION_TARGETS.has(target)
      && (substantiveRemovedRecords.length > 0 || evidenceRegressions.length > 0);
    const evidenceOnly = !coreChanged;
    changes.push({
      key,
      kind: before.records.length === 0
        ? "acquisition-added"
        : after.records.length === 0
          ? "acquisition-removed"
          : evidenceOnly && evidenceRegressions.length > 0
            ? "acquisition-evidence-regressed"
            : evidenceOnly && evidenceEnrichments.length > 0
              ? "acquisition-evidence-enriched"
              : evidenceOnly && redundantRemovedRecords.length > 0
                ? "acquisition-duplicates-removed"
              : evidenceOnly
                ? "acquisition-metadata-changed"
                : "acquisition-changed",
      target,
      description: after.description !== "<missing>" ? after.description : before.description,
      blocking,
      removedRecords: [
        ...substantiveRemovedRecords,
        ...evidenceRegressions.map(record => record.before)
      ],
      redundantRemovedRecords,
      addedRecords: [
        ...addedRecords,
        ...evidenceRegressions.map(record => record.after),
        ...evidenceEnrichments.map(record => record.after)
      ],
      lostEvidence: evidenceRegressions.flatMap(record => record.lostEvidence),
      addedEvidence: evidenceEnrichments.flatMap(record => record.addedEvidence),
      beforeCount: before.records.length,
      afterCount: after.records.length
    });
  }
  return changes;
}

function compareMetrics(beforeState, afterState) {
  const changes = [];
  for (const key of Object.keys(afterState.metrics)) {
    const before = beforeState.metrics[key];
    const after = afterState.metrics[key];
    if (typeof before !== "number" || typeof after !== "number" || before === after) continue;
    const blocking = (LOWER_IS_BAD_METRICS.has(key) && after < before)
      || (HIGHER_IS_BAD_METRICS.has(key) && after > before);
    changes.push({ metric: key, before, after, blocking });
  }
  return changes;
}

function compareNamedRecords(beforeMap, afterMap) {
  const added = [];
  const resolved = [];
  for (const [key, value] of afterMap) {
    if (!beforeMap.has(key)) added.push(value);
  }
  for (const [key, value] of beforeMap) {
    if (!afterMap.has(key)) resolved.push(value);
  }
  return {
    added: added.sort((left, right) => stableJson(left).localeCompare(stableJson(right))),
    resolved: resolved.sort((left, right) => stableJson(left).localeCompare(stableJson(right)))
  };
}

function addedArrayRecords(before, after) {
  const beforeValues = new Set(before.map(stableJson));
  return after.filter(value => !beforeValues.has(stableJson(value)));
}

export function compareProfileStates(modName, beforeState, afterState) {
  const itemChanges = compareItems(beforeState, afterState);
  const sourceChanges = compareSources(beforeState, afterState);
  const acquisitionChanges = compareAcquisitions(beforeState, afterState);
  const metricChanges = compareMetrics(beforeState, afterState);
  const acquisitionMetricPrefixes = new Map([
    ["recipes", "recipe|"],
    ["drops", "drop|"],
    ["shops", "shop|"],
    ["fishing", "fishing|"]
  ]);
  for (const change of metricChanges) {
    const prefix = acquisitionMetricPrefixes.get(change.metric);
    if (prefix && change.blocking && !acquisitionChanges.some(acquisition =>
      acquisition.blocking && acquisition.key.startsWith(prefix))) {
      change.blocking = false;
      change.explainedByNonBlockingAcquisitionChanges = true;
    }
  }
  const reviewChanges = compareNamedRecords(beforeState.reviewIssues, afterState.reviewIssues);
  const newAuditErrors = addedArrayRecords(beforeState.auditErrors, afterState.auditErrors);
  const newAuditWarnings = addedArrayRecords(beforeState.auditWarnings, afterState.auditWarnings);
  const blockingReasons = [
    ...itemChanges.filter(change => change.blocking).map(change => ({
      kind: change.kind,
      subject: change.id,
      reason: change.explanation
    })),
    ...sourceChanges.filter(change => change.blocking).map(change => ({
      kind: change.kind,
      subject: change.source,
      reason: change.reasons.join("; ")
    })),
    ...acquisitionChanges.filter(change => change.blocking).map(change => ({
      kind: change.kind,
      subject: change.target || change.key,
      reason: `${change.description}: ${change.beforeCount} -> ${change.afterCount}`
    })),
    ...metricChanges.filter(change => change.blocking).map(change => ({
      kind: "metric-regression",
      subject: change.metric,
      reason: `${change.before} -> ${change.after}`
    })),
    ...reviewChanges.added.map(issue => ({
      kind: "review-issue-added",
      subject: issue.item ?? issue.id ?? issue.kind ?? "<unknown>",
      reason: issue.kind ?? "new review issue"
    })),
    ...newAuditErrors.map(error => ({
      kind: "audit-error-added",
      subject: modName,
      reason: typeof error === "string" ? error : stableJson(error)
    })),
    ...(afterState.auditWarnings.length > beforeState.auditWarnings.length
      ? newAuditWarnings.map(warning => ({
        kind: "audit-warning-added",
        subject: modName,
        reason: typeof warning === "string" ? warning : stableJson(warning)
      }))
      : [])
  ];

  return {
    mod: modName,
    before: beforeState.metadata,
    after: afterState.metadata,
    summary: {
      beforeProfileItems: beforeState.profileItems.size,
      afterProfileItems: afterState.profileItems.size,
      itemChanges: itemChanges.length,
      addedItems: itemChanges.filter(change => change.kind === "added-to-profile").length,
      removedItems: itemChanges.filter(change => change.kind === "removed-from-profile").length,
      movedItems: itemChanges.filter(change => change.kind === "stage-changed").length,
      pathChanges: itemChanges.filter(change => change.kind === "source-path-changed").length,
      sourceChanges: sourceChanges.length,
      acquisitionChanges: acquisitionChanges.length,
      addedReviewIssues: reviewChanges.added.length,
      resolvedReviewIssues: reviewChanges.resolved.length,
      blockingReasons: blockingReasons.length
    },
    blocked: blockingReasons.length > 0,
    blockingReasons,
    itemChanges,
    sourceChanges,
    acquisitionChanges,
    metricChanges,
    reviewChanges,
    newAuditErrors,
    newAuditWarnings
  };
}

export function createRegressionReport(comparisons, options = {}) {
  return {
    format: "ProgressionJournalProfileRegressionReport",
    version: 1,
    generatedAtUtc: new Date().toISOString(),
    command: options.command ?? [],
    candidateRoot: options.candidateRoot ?? "",
    acceptedWithOverride: options.acceptedWithOverride ?? false,
    blocked: comparisons.some(comparison => comparison.blocked),
    summary: {
      mods: comparisons.length,
      blockingMods: comparisons.filter(comparison => comparison.blocked).length,
      blockingReasons: comparisons.reduce(
        (sum, comparison) => sum + comparison.blockingReasons.length,
        0)
    },
    mods: comparisons
  };
}

function stateText(item) {
  const stage = item.stageIds.length > 0 ? ` @ ${item.stageIds.join(", ")}` : "";
  const via = item.via ? ` via ${item.via}` : "";
  return `${item.status}${stage}${via}`;
}

function conditionText(condition) {
  if (condition?.key) return condition.key;
  if (condition?.type && condition?.description) {
    return `${condition.type}: ${condition.description}`;
  }
  return condition?.type ?? condition?.description ?? stableJson(condition);
}

function evidenceLines(issue) {
  const lines = [];
  for (const [kind, records] of Object.entries(issue?.evidence ?? {})) {
    for (const record of records ?? []) {
      const source = record.source ?? record.npc ?? record.result ?? "";
      const conditions = (record.conditions ?? []).map(conditionText).filter(Boolean);
      lines.push(`${kind}${source ? ` ${source}` : ""}`
        + `${conditions.length > 0 ? ` [${conditions.join("; ")}]` : ""}`);
    }
  }
  return lines;
}

function traceLines(trace, prefix = "") {
  const marker = trace.missing
    ? "<path missing>"
    : trace.cycleOrLimit
      ? "<cycle/depth limit>"
      : `${trace.stage || "<no stage>"} via ${trace.via || "<no source>"}`;
  const lines = [`${prefix}${trace.item}: ${marker}`];
  for (const dependency of trace.dependencies ?? []) {
    lines.push(...traceLines(dependency, `${prefix}  `));
  }
  return lines;
}

function markdownItemChange(change) {
  const lines = [
    `### ${change.name} (\`${change.id}\`)`,
    "",
    `- Тип: \`${change.kind}\`${change.blocking ? " — **БЛОКИРУЕТ ЗАПИСЬ**" : ""}`,
    `- БЫЛО: ${stateText(change.before)}`,
    `- СТАЛО: ${stateText(change.after)}`,
    `- Причина: ${change.explanation}`
  ];
  const reviewIssues = change.afterDiagnosis.reviewIssues ?? [];
  for (const issue of reviewIssues) {
    lines.push(`- Новый review: \`${issue.kind ?? "unknown"}\` — ${issue.item ?? issue.id ?? ""}`);
    for (const evidence of evidenceLines(issue)) lines.push(`  - evidence: ${evidence}`);
  }
  for (const record of change.afterDiagnosis.generationRecords ?? []) {
    lines.push(`- Диагностика генератора: \`${record.kind}\` — `
      + `${record.reason ?? record.displayName ?? record.item ?? ""}`);
  }
  lines.push("- Прежняя доказанная цепочка:");
  lines.push(...traceLines(change.beforeDiagnosis.path).map(value => `  - ${value}`));
  lines.push("- Новая доказанная цепочка:");
  lines.push(...traceLines(change.afterDiagnosis.path).map(value => `  - ${value}`));
  lines.push("");
  return lines;
}

function recordText(record) {
  if (!record) return "<none>";
  const conditions = (record.conditions ?? []).map(conditionText).filter(Boolean);
  const suffix = conditions.length > 0 ? `; conditions: ${conditions.join("; ")}` : "";
  if (record.npc) {
    return `${record.npc}/${record.shop ?? "Shop"}; observed=${record.observed ?? false}; `
      + `stage=${record.earliestStageId ?? record.earliestStageIndex ?? "?"}${suffix}`;
  }
  if (record.source) return `${record.sourceType ?? "source"} ${record.source}${suffix}`;
  if (record.result) {
    return `recipe ${record.result} <= ${(record.ingredients ?? [])
      .map(value => value.item)
      .join(" + ")}${suffix}`;
  }
  if (record.target) {
    return `fishing ${record.target} @ ${record.earliestStageId ?? "?"}${suffix}`;
  }
  if (record.input || record.output) return `shimmer ${record.input} -> ${record.output}`;
  return stableJson(record);
}

export function renderRegressionMarkdown(report) {
  const result = report.committed && report.blocked
    ? "**ПРИНЯТО С ЯВНЫМ ПОДТВЕРЖДЕНИЕМ**"
    : report.blocked
      ? "**ЗАБЛОКИРОВАНО**"
      : report.committed
        ? "**ПРИНЯТО**"
        : "**ПРОВЕРЕНО: РЕГРЕССИЙ НЕТ**";
  const lines = [
    "# Проверка регрессий профилей",
    "",
    `- Результат: ${result}`,
    `- Модов: ${report.summary.mods}`,
    `- Блокирующих причин: ${report.summary.blockingReasons}`,
    `- Кандидаты: \`${report.candidateRoot || "<not recorded>"}\``,
    ""
  ];

  for (const comparison of report.mods) {
    const summary = comparison.summary;
    lines.push(
      `## ${comparison.mod}`,
      "",
      `Предметов: **${summary.beforeProfileItems} -> ${summary.afterProfileItems}**; `
        + `добавлено ${summary.addedItems}, удалено ${summary.removedItems}, `
        + `перемещено ${summary.movedItems}, изменён путь ${summary.pathChanges}.`,
      "",
      `Источников изменено: ${summary.sourceChanges}; acquisition-записей изменено: `
        + `${summary.acquisitionChanges}; новых review: ${summary.addedReviewIssues}.`,
      ""
    );

    if (comparison.blockingReasons.length > 0) {
      lines.push("### Почему запись заблокирована", "");
      for (const reason of comparison.blockingReasons) {
        lines.push(`- **${reason.kind}** \`${reason.subject}\`: ${reason.reason}`);
      }
      lines.push("");
    }

    if (comparison.itemChanges.length > 0) {
      lines.push("## Предметы: БЫЛО -> СТАЛО", "");
      for (const change of comparison.itemChanges) {
        lines.push(...markdownItemChange(change));
      }
    }

    if (comparison.sourceChanges.length > 0) {
      lines.push("## Источники NPC/магазинов", "");
      for (const change of comparison.sourceChanges) {
        lines.push(
          `- ${change.blocking ? "**БЛОКИРУЕТ** " : ""}\`${change.source}\`: `
          + `${change.before.status}@${change.before.earliestStageIndex} -> `
          + `${change.after.status}@${change.after.earliestStageIndex}`
          + `${change.reasons.length > 0 ? `; ${change.reasons.join("; ")}` : ""}`);
      }
      lines.push("");
    }

    if (comparison.acquisitionChanges.length > 0) {
      lines.push("## Рецепты, дропы, магазины, рыбалка и shimmer", "");
      for (const change of comparison.acquisitionChanges) {
        lines.push(
          `### ${change.blocking ? "БЛОКИРУЕТ: " : ""}${change.description}`,
          "",
          `- Записей: ${change.beforeCount} -> ${change.afterCount}`
        );
        for (const record of change.removedRecords) {
          lines.push(`- БЫЛО, теперь отсутствует: ${recordText(record)}`);
        }
        for (const record of change.addedRecords) {
          lines.push(`- СТАЛО, новая запись: ${recordText(record)}`);
        }
        lines.push("");
      }
    }

    if (comparison.metricChanges.length > 0) {
      lines.push("## Сводные показатели", "");
      for (const change of comparison.metricChanges) {
        lines.push(`- ${change.blocking ? "**БЛОКИРУЕТ** " : ""}`
          + `\`${change.metric}\`: ${change.before} -> ${change.after}`);
      }
      lines.push("");
    }

    if (comparison.reviewChanges.added.length > 0) {
      lines.push("## Новые вопросы review", "");
      for (const issue of comparison.reviewChanges.added) {
        lines.push(`- **БЛОКИРУЕТ** \`${issue.kind ?? "unknown"}\`: `
          + `${issue.item ?? issue.id ?? stableJson(issue)}`);
        for (const evidence of evidenceLines(issue)) lines.push(`  - ${evidence}`);
      }
      lines.push("");
    }
  }

  return `${lines.join("\n")}\n`;
}

export function formatConsoleSummary(report) {
  const lines = [
    "",
    report.blocked
      ? "SAFE BUILD: ОБНАРУЖЕНА РЕГРЕССИЯ, рабочие профили не изменены."
      : "SAFE BUILD: блокирующих регрессий нет."
  ];
  for (const comparison of report.mods) {
    const value = comparison.summary;
    lines.push(
      `${comparison.mod}: items ${value.beforeProfileItems} -> ${value.afterProfileItems}; `
      + `added ${value.addedItems}, removed ${value.removedItems}, `
      + `moved ${value.movedItems}, paths ${value.pathChanges}; `
      + `blocking ${value.blockingReasons}`);
    for (const reason of comparison.blockingReasons.slice(0, 20)) {
      lines.push(`  BLOCK ${reason.kind} ${reason.subject}: ${reason.reason}`);
    }
    if (comparison.blockingReasons.length > 20) {
      lines.push(`  ... ещё ${comparison.blockingReasons.length - 20}; см. полный отчёт.`);
    }
  }
  return lines.join("\n");
}
