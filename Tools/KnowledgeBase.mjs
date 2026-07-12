import { createHash } from "node:crypto";

export function buildKnowledgeBase(snapshot) {
  assert(snapshot?.format === "ProgressionJournalSnapshot", "Invalid snapshot format.");
  assert(snapshot.version === 4, `Unsupported snapshot version '${snapshot.version}'.`);

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
    acquisitions: {
      recipes: structuredClone(snapshot.recipes ?? []),
      drops: structuredClone(snapshot.drops ?? []),
      shops: structuredClone(snapshot.shops ?? []),
      fishing: structuredClone(snapshot.fishing ?? [])
    },
    availability: {
      npcs: structuredClone(snapshot.npcAvailability ?? [])
    },
    classifications: {
      vanillaItems: structuredClone(snapshot.vanillaItemClassifications ?? [])
    },
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
    recipes: knowledge.acquisitions.recipes,
    drops: knowledge.acquisitions.drops,
    shops: knowledge.acquisitions.shops,
    fishing: knowledge.acquisitions.fishing,
    npcAvailability: knowledge.availability.npcs
  };
  if (knowledge.diagnostics.npcSpawnProbe !== null) {
    snapshot.npcSpawnProbe = knowledge.diagnostics.npcSpawnProbe;
  }
  snapshot.vanillaItemClassifications = knowledge.classifications.vanillaItems;

  assert(
    hashSnapshot(snapshot) === knowledge.source.snapshotSha256,
    "Knowledge facts do not match the source snapshot hash.");
  return snapshot;
}

function hashSnapshot(snapshot) {
  return createHash("sha256")
    .update(JSON.stringify(snapshot))
    .digest("hex");
}

function assert(condition, message) {
  if (!condition) throw new Error(message);
}
