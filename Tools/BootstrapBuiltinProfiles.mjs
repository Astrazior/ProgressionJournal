import path from "node:path";
import { loadManifest, readJson, writeJson } from "./ProfileGeneratorCore.mjs";

const root = path.resolve(import.meta.dirname, "..");
for (const name of ["calamity", "thorium", "fargosouls"]) {
  const manifestPath = path.join(root, "Profiles", "Manifests", `${name}.json`);
  const manifest = loadManifest(manifestPath);
  const profile = createEmptyProfile(manifest);

  if (manifest.wikiInput) {
    const source = readJson(path.resolve(path.dirname(manifestPath), manifest.wikiInput));
    profile.entries = convertWikiEntries(source, manifest);
    profile.sourceRevision = source.sourceRevision ?? "";
  }
  if (manifest.availabilityInput) {
    const source = readJson(path.resolve(path.dirname(manifestPath), manifest.availabilityInput));
    profile.entries.push(...convertAvailabilityEntries(source, manifest));
    profile.sourceRevision = source.sourceRevision ?? profile.sourceRevision;
  }

  writeJson(path.join(root, "Profiles", "Builtin", `${manifest.outputName}.json`), profile);
}

function convertAvailabilityEntries(source, manifest) {
  const result = [];
  const allClasses = manifest.classes.map(cls => cls.id);
  const modifiedVanillaItems = new Set(manifest.modifiedVanillaItems ?? []);
  for (const sourceEntry of source.entries ?? []) {
    const stages = sourceEntry.evaluations
      .map(evaluation => manifest.availabilityStageMap?.[evaluation.stageId])
      .filter(Boolean);
    if (stages.length === 0) continue;
    const itemGroups = (sourceEntry.itemGroups ?? [])
      .map(group => group.filter(reference =>
        reference.mod !== "Terraria"
        || modifiedVanillaItems.has(`${reference.mod}/${reference.item}`)))
      .filter(group => group.length > 0);
    if (itemGroups.length === 0) continue;

    result.push({
      key: `availability.${sourceEntry.key}`,
      category: sourceEntry.category,
      classes: sourceEntry.classes.includes("all") ? allClasses : sourceEntry.classes,
      itemGroups,
      evaluations: [...new Set(stages)].map(stageId => ({
        stageId,
        tier: "FromGuide",
        scope: "StageOnly"
      })),
      wiki: [],
      isSupportWeapon: sourceEntry.isSupportWeapon ?? false,
      eventCategory: sourceEntry.eventCategory ?? null,
      customEventName: sourceEntry.customEventName ?? ""
    });
  }
  return result;
}

function createEmptyProfile(manifest) {
  return {
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
    entries: [],
    combatBuffs: []
  };
}

function convertWikiEntries(source, manifest) {
  const result = [];
  for (const sourceEntry of source.entries ?? []) {
    const mappings = sourceEntry.evaluations
      .map(evaluation => manifest.wikiStageMap?.[evaluation.stageId])
      .filter(Boolean);
    if (mappings.length === 0) continue;

    const uniqueStages = [...new Map(mappings.map(mapping => [mapping.stageId, mapping])).values()];
    result.push({
      key: `wiki.${sourceEntry.key}`,
      category: sourceEntry.category,
      classes: sourceEntry.classes,
      itemGroups: sourceEntry.itemGroups,
      evaluations: uniqueStages.map(mapping => ({
        stageId: mapping.stageId,
        tier: "FromGuide",
        scope: "StageOnly"
      })),
      wiki: uniqueStages.map(mapping => ({
        stageId: mapping.stageId,
        classes: sourceEntry.classes,
        sourceName: manifest.wikiSource.name,
        sourceUrl: manifest.wikiSource.url,
        target: mapping.target
      })),
      isSupportWeapon: sourceEntry.isSupportWeapon ?? false,
      eventCategory: sourceEntry.eventCategory ?? null,
      customEventName: sourceEntry.customEventName ?? ""
    });
  }
  return result;
}
