import { createHash } from "node:crypto";

export function accumulatePositiveProbeEvidence(snapshot, previousKnowledge, stages = []) {
  const current = structuredClone(snapshot);
  const report = {
    compatible: false,
    npcAvailability: 0,
    shops: 0,
    fishing: 0
  };
  if (!previousKnowledge || !hasCompatibleProbeIdentity(current, previousKnowledge)) {
    return { snapshot: current, report };
  }

  const previous = createSnapshotView(previousKnowledge);
  report.compatible = true;
  const stageIndexes = new Map(
    stages.map((stage, index) => [stage.id, index]));
  const currentNpcIds = new Set((current.npcs ?? []).map(npc => npc.id));
  const currentItemIds = new Set((current.items ?? []).map(item => item.id));

  const npcAvailability = new Map(
    (current.npcAvailability ?? []).map(record => [
      `${record.kind}\0${record.npc}`,
      record
    ]));
  for (const previousRecord of previous.npcAvailability ?? []) {
    if (!previousRecord.observed
        || !currentNpcIds.has(previousRecord.npc)
        || !hasKnownStage(previousRecord, stageIndexes)) {
      continue;
    }
    const key = `${previousRecord.kind}\0${previousRecord.npc}`;
    const currentRecord = npcAvailability.get(key);
    if (!currentRecord) {
      npcAvailability.set(key, structuredClone(previousRecord));
      report.npcAvailability++;
      continue;
    }
    const accumulated = earlierPositiveRecord(
      currentRecord,
      previousRecord,
      stageIndexes);
    if (accumulated !== currentRecord) {
      npcAvailability.set(key, accumulated);
      report.npcAvailability++;
    }
  }
  current.npcAvailability = [...npcAvailability.values()];

  const shops = [...(current.shops ?? [])];
  for (const previousRecord of previous.shops ?? []) {
    if (!previousRecord.observed
        || !currentNpcIds.has(previousRecord.npc)
        || !currentItemIds.has(previousRecord.item)
        || !hasKnownStage(previousRecord, stageIndexes)) {
      continue;
    }
    const key = shopKey(previousRecord);
    const currentIndex = shops.findIndex(record => shopKey(record) === key);
    if (currentIndex < 0) {
      shops.push(structuredClone(previousRecord));
      report.shops++;
      continue;
    }
    const currentRecord = shops[currentIndex];
    const accumulated = earlierPositiveRecord(
      currentRecord,
      previousRecord,
      stageIndexes);
    if (accumulated !== currentRecord) {
      shops[currentIndex] = {
        ...accumulated,
        conditions: structuredClone(currentRecord.conditions ?? previousRecord.conditions ?? [])
      };
      report.shops++;
    }
  }
  current.shops = shops;

  const fishing = new Map(
    (current.fishing ?? []).map(record => [fishingKey(record), record]));
  for (const previousRecord of previous.fishing ?? []) {
    const targetExists = previousRecord.targetType === "npc"
      ? currentNpcIds.has(previousRecord.target)
      : currentItemIds.has(previousRecord.target);
    if (!targetExists || !hasKnownStage(previousRecord, stageIndexes)) continue;
    const key = fishingKey(previousRecord);
    const currentRecord = fishing.get(key);
    if (!currentRecord) {
      fishing.set(key, structuredClone(previousRecord));
      report.fishing++;
      continue;
    }
    const accumulated = earlierPositiveRecord(
      currentRecord,
      previousRecord,
      stageIndexes);
    if (accumulated !== currentRecord) {
      fishing.set(key, accumulated);
      report.fishing++;
    }
  }
  current.fishing = [...fishing.values()];

  return { snapshot: current, report };
}

export function buildKnowledgeBase(snapshot) {
  assert(snapshot?.format === "ProgressionJournalSnapshot", "Invalid snapshot format.");
  assert([4, 5, 6, 7, 8].includes(snapshot.version), `Unsupported snapshot version '${snapshot.version}'.`);

  const acquisitions = {
    recipes: structuredClone(snapshot.recipes ?? []),
    drops: structuredClone(snapshot.drops ?? []),
    shops: structuredClone(snapshot.shops ?? []),
    fishing: structuredClone(snapshot.fishing ?? [])
  };
  if (snapshot.shimmerTransforms !== undefined) {
    acquisitions.shimmerTransforms = structuredClone(snapshot.shimmerTransforms);
  }
  if (snapshot.knownSourceItems !== undefined) {
    acquisitions.knownSourceItems = structuredClone(snapshot.knownSourceItems);
  }
  const classifications = {
    vanillaItems: structuredClone(snapshot.vanillaItemClassifications ?? [])
  };
  if (snapshot.vanillaBuffClassifications !== undefined) {
    classifications.vanillaBuffs = structuredClone(snapshot.vanillaBuffClassifications);
  }

  const knowledge = {
    format: "ProgressionJournalKnowledge",
    version: 1,
    source: {
      snapshotFormat: snapshot.format,
      snapshotVersion: snapshot.version,
      snapshotGeneratedAtUtc: snapshot.generatedAtUtc ?? "",
      snapshotSha256: hashSnapshot(snapshot),
      targetMod: snapshot.targetMod ?? "",
      profileId: snapshot.profileId ?? "",
      contentMods: structuredClone(snapshot.contentMods ?? []),
      mods: structuredClone(snapshot.mods ?? []),
      environmentMods: structuredClone(snapshot.environmentMods ?? [])
    },
    entities: {
      items: structuredClone(snapshot.items ?? []),
      npcs: structuredClone(snapshot.npcs ?? [])
    },
    acquisitions,
    availability: {
      npcs: structuredClone(snapshot.npcAvailability ?? [])
    },
    classifications,
    diagnostics: {
      npcSpawnProbe: snapshot.npcSpawnProbe
        ? structuredClone(snapshot.npcSpawnProbe)
        : null
    }
  };

  knowledge.summary = {
    items: knowledge.entities.items.length,
    npcs: knowledge.entities.npcs.length,
    recipes: knowledge.acquisitions.recipes.length,
    drops: knowledge.acquisitions.drops.length,
    shops: knowledge.acquisitions.shops.length,
    fishing: knowledge.acquisitions.fishing.length,
    npcAvailability: knowledge.availability.npcs.length,
    vanillaItemClassifications: knowledge.classifications.vanillaItems.length,
    hasNpcSpawnProbe: knowledge.diagnostics.npcSpawnProbe !== null
  };

  if (knowledge.acquisitions.shimmerTransforms !== undefined) {
    knowledge.summary.shimmerTransforms = knowledge.acquisitions.shimmerTransforms.length;
  }

  if (knowledge.classifications.vanillaBuffs !== undefined) {
    knowledge.summary.vanillaBuffClassifications = knowledge.classifications.vanillaBuffs.length;
  }

  return knowledge;
}

export function createSnapshotView(knowledge) {
  assert(knowledge?.format === "ProgressionJournalKnowledge", "Invalid knowledge format.");
  assert(knowledge.version === 1, `Unsupported knowledge version '${knowledge.version}'.`);

  const snapshot = {
    format: knowledge.source.snapshotFormat,
    version: knowledge.source.snapshotVersion,
    generatedAtUtc: knowledge.source.snapshotGeneratedAtUtc,
    targetMod: knowledge.source.targetMod,
    profileId: knowledge.source.profileId,
    contentMods: knowledge.source.contentMods,
    mods: knowledge.source.mods,
    environmentMods: knowledge.source.environmentMods,
    items: knowledge.entities.items,
    npcs: knowledge.entities.npcs,
    recipes: knowledge.acquisitions.recipes
  };
  if (knowledge.acquisitions.shimmerTransforms !== undefined) {
    snapshot.shimmerTransforms = knowledge.acquisitions.shimmerTransforms;
  }
  if (knowledge.acquisitions.knownSourceItems !== undefined) {
    snapshot.knownSourceItems = knowledge.acquisitions.knownSourceItems;
  }
  snapshot.drops = knowledge.acquisitions.drops;
  snapshot.shops = knowledge.acquisitions.shops;
  snapshot.fishing = knowledge.acquisitions.fishing;
  snapshot.npcAvailability = knowledge.availability.npcs;
  if (knowledge.diagnostics.npcSpawnProbe !== null) {
    snapshot.npcSpawnProbe = knowledge.diagnostics.npcSpawnProbe;
  }
  snapshot.vanillaItemClassifications = knowledge.classifications.vanillaItems;
  if (knowledge.classifications.vanillaBuffs !== undefined) {
    snapshot.vanillaBuffClassifications = knowledge.classifications.vanillaBuffs;
  }

  assert(
    hashSnapshot(snapshot) === knowledge.source.snapshotSha256,
    "Knowledge facts do not match the source snapshot hash.");
  return snapshot;
}

function hasCompatibleProbeIdentity(snapshot, knowledge) {
  const source = knowledge?.source;
  return knowledge?.format === "ProgressionJournalKnowledge"
    && knowledge.version === 1
    && source?.snapshotFormat === snapshot.format
    && [4, 5, 6, 7, 8].includes(source.snapshotVersion)
    && [4, 5, 6, 7, 8].includes(snapshot.version)
    && source.targetMod === (snapshot.targetMod ?? "")
    && source.profileId === (snapshot.profileId ?? "")
    && sameStringSet(source.contentMods ?? [], snapshot.contentMods ?? [])
    && sameModVersions(source.mods ?? [], snapshot.mods ?? []);
}

function sameStringSet(left, right) {
  return [...left].sort().join("\0") === [...right].sort().join("\0");
}

function sameModVersions(left, right) {
  const normalize = values => values
    .map(value => `${value.name}\0${value.version ?? ""}`)
    .sort();
  return normalize(left).join("\n") === normalize(right).join("\n");
}

function earlierPositiveRecord(current, previous, stageIndexes) {
  if ("observed" in current && current.observed !== true) {
    return structuredClone(previous);
  }
  return recordStageIndex(previous, stageIndexes) < recordStageIndex(current, stageIndexes)
    ? structuredClone(previous)
    : current;
}

function recordStageIndex(record, stageIndexes) {
  if (record.earliestStageId && stageIndexes.has(record.earliestStageId)) {
    return stageIndexes.get(record.earliestStageId);
  }
  return Number.isInteger(record.earliestStageIndex)
    ? record.earliestStageIndex
    : Number.MAX_SAFE_INTEGER;
}

function hasKnownStage(record, stageIndexes) {
  return stageIndexes.size === 0
    || !record.earliestStageId
    || stageIndexes.has(record.earliestStageId);
}

function shopKey(record) {
  return `${record.npc}\0${record.shop}\0${record.item}\0`
    + JSON.stringify((record.conditions ?? []).map(condition => ({
      type: condition.type ?? "",
      description: condition.description ?? ""
    })));
}

function fishingKey(record) {
  return `${record.targetType}\0${record.target}`;
}

function hashSnapshot(snapshot) {
  return createHash("sha256")
    .update(JSON.stringify(snapshot))
    .digest("hex");
}

function assert(condition, message) {
  if (!condition) throw new Error(message);
}
