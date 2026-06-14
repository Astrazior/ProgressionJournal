import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { normalizeAgentRules } from "./BuildModProfiles.mjs";
import { readJson } from "./ProfileGeneratorCore.mjs";
import { applyVanillaSourceCatalog } from "./VanillaSourceCatalog.mjs";

const root = path.resolve(import.meta.dirname, "..");
const modsRoot = path.join(root, "Profiles", "Mods");
const expected = ["CalamityMod", "FargowiltasSouls", "ThoriumMod"];
const requiredFiles = [
  "support.json",
  "snapshot.json",
  "agent-rules.json",
  "recommendations.json",
  "review.json",
  "report.json",
  "profile.json"
];

for (const modName of expected) {
  const directory = path.join(modsRoot, modName);
  for (const name of requiredFiles) {
    assert(fs.existsSync(path.join(directory, name)), `${modName}/${name} is missing`);
  }
  const support = readJson(path.join(directory, "support.json"));
  const snapshot = readJson(path.join(directory, "snapshot.json"));
  const profile = readJson(path.join(directory, "profile.json"));
  assert.equal(support.targetMod, modName);
  assert.equal(snapshot.targetMod, modName);
  assert.equal(profile.format, "ProgressionJournalProfile");
  const snapshotVersions = new Map(snapshot.mods.map(mod => [mod.name, mod.version]));
  for (const required of support.requiredMods ?? []) {
    assert(required.version, `${modName}: required mod '${required.name}' has no supported version`);
    assert(snapshotVersions.has(required.name), `${modName}: '${required.name}' is missing from snapshot`);
  }
  const allowed = new Set(["Terraria", ...(support.contentMods ?? [modName])]);
  for (const item of snapshot.items) assert(allowed.has(modOf(item.id)), item.id);
  for (const npc of snapshot.npcs) assert(allowed.has(modOf(npc.id)), npc.id);
  for (const recipe of snapshot.recipes) {
    assert(allowed.has(modOf(recipe.result)), recipe.result);
    assert(recipe.ingredients.every(value => allowed.has(modOf(value.item))));
    assert(recipe.stations.every(value => allowed.has(modOf(value))));
  }
  for (const drop of snapshot.drops) {
    assert(allowed.has(modOf(drop.item)), drop.item);
    assert(allowed.has(modOf(drop.source)), drop.source);
  }
  for (const shop of snapshot.shops) {
    assert(allowed.has(modOf(shop.item)), shop.item);
    assert(allowed.has(modOf(shop.npc)), shop.npc);
  }
}

const fargoSupport = readJson(path.join(modsRoot, "FargowiltasSouls", "support.json"));
assert(fargoSupport.requiredMods.some(mod => mod.name === "FargowiltasSouls"));
assert(fargoSupport.requiredMods.some(mod => mod.name === "Fargowiltas"));

const testSupport = {
  id: "test",
  stages: [{ id: "start" }, { id: "boss" }]
};
const validRule = {
  format: "ProgressionJournalAgentRules",
  version: 1,
  profileId: "test",
  rules: [
    {
      id: "verified-item",
      kind: "item-stage",
      item: "Test/Sword",
      stageId: "boss",
      sourceUrl: "https://example.invalid/wiki/Sword",
      sourceVersion: "revision-1",
      checkedAt: "2026-06-13",
      reason: "The official source states that the sword is unlocked after the boss."
    },
    {
      id: "verified-sources",
      kind: "source-stage",
      sources: ["Test/EnemyA", "Test/EnemyB"],
      stageId: "boss",
      sourceUrl: "https://example.invalid/source/Enemies",
      sourceVersion: "revision-1",
      checkedAt: "2026-06-13",
      reason: "The official source gives both enemies the same progression gate."
    }
  ],
  ignoredItems: [],
  ignoredIssues: []
};
const normalized = normalizeAgentRules(validRule, testSupport);
assert.deepEqual(normalized.problems, []);
assert.equal(normalized.assignments.itemStages["Test/Sword"], "boss");
assert.equal(normalized.assignments.sourceStages["Test/EnemyA"], "boss");
assert.equal(normalized.assignments.sourceStages["Test/EnemyB"], "boss");
const invalid = normalizeAgentRules({
  ...validRule,
  rules: [{ kind: "item-stage", item: "Test/Sword", stageId: "boss" }]
}, testSupport);
assert(invalid.problems.some(problem => problem.includes("sourceUrl")));
assert(invalid.problems.some(problem => problem.includes("checkedAt")));

const vanillaSources = applyVanillaSourceCatalog({
  initialStations: [],
  events: [{ stageId: "goblin-stage", eventCategory: "GoblinArmy" }],
  stages: [
    { id: "start", unlock: { type: "always" } },
    { id: "goblin-stage", unlock: { type: "vanilla-flag", key: "downedBoss1" } },
    { id: "dungeon", unlock: { type: "vanilla-flag", key: "downedBoss3" } },
    { id: "hardmode", unlock: { type: "vanilla-flag", key: "hardMode" } }
  ]
});
assert(vanillaSources.initialStations.includes("Terraria/DemonAltar"));
assert(vanillaSources.initialStations.includes("Terraria/Hellforge"));
assert(vanillaSources.initialItems.includes("Terraria/PinkGel"));
assert(vanillaSources.stages.find(stage => stage.id === "goblin-stage")
  .include.includes("Terraria/TinkerersWorkshop"));
assert(vanillaSources.stages.find(stage => stage.id === "dungeon")
  .stations.includes("Terraria/AlchemyTable"));
assert(vanillaSources.stages.find(stage => stage.id === "hardmode")
  .include.includes("Terraria/AdamantiteForge"));

const ignore = fs.readFileSync(path.join(root, "build.txt"), "utf8");
for (const name of requiredFiles.filter(name => name !== "profile.json")) {
  assert(ignore.includes(`Profiles/Mods/*/${name}`), `${name} is not excluded from .tmod`);
}
assert(!ignore.includes("Profiles/Mods/*/profile.json"));
assert(ignore.includes("Tools/*"));
assert(ignore.includes("*.user"));
assert(ignore.includes("*.md"));
const registry = fs.readFileSync(
  path.join(root, "Data", "Profiles", "JournalProfileRegistry.cs"),
  "utf8");
assert(registry.includes('path.StartsWith("Profiles/Mods/"'));
assert(registry.includes('path.EndsWith("/profile.json"'));
assert(!registry.includes("Profiles/Builtin/"));
const exporter = fs.readFileSync(
  path.join(root, "Commands", "ExportProgressionSnapshotCommand.cs"),
  "utf8");
const builder = fs.readFileSync(path.join(root, "Tools", "BuildModProfiles.mjs"), "utf8");
assert(exporter.includes('public override string Usage => "/pjexport <InternalModName>"'));
assert(exporter.includes('Path.Combine(directory, "snapshot.json")'));
assert(exporter.includes("ResolveTransitiveDependencies(targetMod)"));
assert(builder.includes("rule.sources ?? (rule.source ? [rule.source] : [])"));
assert(builder.includes("rule.items ?? (rule.item ? [rule.item] : [])"));
assert(exporter.includes("File.Move(temporaryPath, path, overwrite: true)"));
assert(exporter.includes("EnvironmentMods = ModLoader.Mods"));

console.log("Mod profile pipeline tests: OK");

function modOf(reference) {
  return reference.split("/", 1)[0];
}
