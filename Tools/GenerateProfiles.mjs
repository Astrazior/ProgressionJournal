import fs from "node:fs";
import path from "node:path";
import { generateProfile, loadManifest, readJson, writeJson } from "./ProfileGeneratorCore.mjs";

const args = parseArgs(process.argv.slice(2));
if (!args.snapshot) {
  throw new Error("Usage: node Tools/GenerateProfiles.mjs --snapshot <file> [--manifest <file> | --all]");
}

const root = path.resolve(import.meta.dirname, "..");
const manifestFiles = args.all
  ? ["calamity.json", "thorium.json", "fargosouls.json"].map(name => path.join(root, "Profiles", "Manifests", name))
  : [path.resolve(args.manifest)];
const snapshot = readJson(path.resolve(args.snapshot));
const previousSnapshot = args.previous ? readJson(path.resolve(args.previous)) : null;

for (const manifestFile of manifestFiles) {
  const manifest = loadManifest(manifestFile);
  const loadedMods = new Set((snapshot.mods ?? []).map(mod => mod.name));
  const missingMods = (manifest.requiredMods ?? [])
    .map(mod => mod.name)
    .filter(name => !loadedMods.has(name));
  if (missingMods.length > 0) {
    console.warn(
      `${manifest.id}: skipped because snapshot is missing required mods: ${missingMods.join(", ")}`);
    continue;
  }
  const wikiProfile = manifest.wikiInput
    ? readJson(path.resolve(path.dirname(manifestFile), manifest.wikiInput))
    : null;
  const availabilityProfile = manifest.availabilityInput
    ? readJson(path.resolve(path.dirname(manifestFile), manifest.availabilityInput))
    : null;
  const manualPath = path.join(root, "Profiles", "Manual", `${manifest.outputName}.json`);
  const manualAssignments = fs.existsSync(manualPath)
    ? readJson(manualPath)
    : null;
  const { profile, report, review } = generateProfile(
    snapshot,
    manifest,
    wikiProfile,
    manualAssignments,
    availabilityProfile);
  if (previousSnapshot) report.snapshotChanges = compareSnapshots(previousSnapshot, snapshot);
  const output = path.join(root, "Profiles", "Builtin", `${manifest.outputName}.json`);
  const reportPath = path.join(root, "Profiles", "Reports", `${manifest.outputName}-report.json`);
  const reviewPath = path.join(root, "Profiles", "Review", `${manifest.outputName}-review.json`);
  writeJson(output, profile);
  writeJson(reportPath, report);
  writeJson(reviewPath, review);
  console.log(
    `${manifest.id}: ${profile.entries.length} equipment entries, `
    + `${profile.combatBuffs.length} buff entries, ${review.summary.total} manual review issues`);
}

function compareSnapshots(previous, current) {
  const oldItems = new Map(previous.items.map(item => [item.id, item]));
  const newItems = new Map(current.items.map(item => [item.id, item]));
  const addedItems = [...newItems.keys()].filter(id => !oldItems.has(id));
  const removedItems = [...oldItems.keys()].filter(id => !newItems.has(id));
  const changedItems = [...newItems.keys()].filter(id =>
    oldItems.has(id) && JSON.stringify(oldItems.get(id)) !== JSON.stringify(newItems.get(id)));
  return {
    addedItems,
    removedItems,
    renamedCandidates: removedItems.flatMap(oldId => {
      const oldName = oldItems.get(oldId)?.name;
      return addedItems
        .filter(newId => newItems.get(newId)?.name === oldName)
        .map(newId => ({ from: oldId, to: newId }));
    }),
    changedItems,
    recipesChanged: signature(previous.recipes) !== signature(current.recipes),
    dropsChanged: signature(previous.drops) !== signature(current.drops),
    shopsChanged: signature(previous.shops) !== signature(current.shops)
  };
}

function signature(value) {
  return JSON.stringify(value ?? []);
}

function parseArgs(values) {
  const result = {};
  for (let index = 0; index < values.length; index++) {
    const key = values[index].replace(/^--/, "");
    if (key === "all") result.all = true;
    else result[key] = values[++index];
  }
  return result;
}
