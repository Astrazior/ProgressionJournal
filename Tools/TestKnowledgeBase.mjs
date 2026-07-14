import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { isDeepStrictEqual } from "node:util";
import {
  buildKnowledgeBase,
  createSnapshotView
} from "./KnowledgeBase.mjs";
import { generateProfile } from "./ProfileGeneratorCore.mjs";

const root = path.resolve(import.meta.dirname, "..");
const modsRoot = path.join(root, "Profiles", "Mods");
const fixture = {
  format: "ProgressionJournalSnapshot",
  version: 5,
  generatedAtUtc: "2026-07-13T00:00:00Z",
  targetMod: "ExampleMod",
  profileId: "builtin.example",
  contentMods: ["ExampleMod"],
  mods: [{ name: "ExampleMod", version: "1.0" }],
  environmentMods: [{ name: "ExampleLibrary", version: "2.0" }],
  items: [
    { id: "ExampleMod/Result", name: "Result" },
    { id: "ExampleMod/Ingredient", name: "Ingredient" },
    { id: "ExampleMod/ShimmerResult", name: "Shimmer Result" }
  ],
  npcs: [{ id: "ExampleMod/Enemy", name: "Enemy", boss: false }],
  recipes: [
    {
      result: "ExampleMod/Result",
      resultStack: 1,
      ingredients: [{ item: "ExampleMod/Ingredient", stack: 2 }],
      stations: ["ExampleMod/ExampleStation"],
      conditions: [{ type: "Terraria.Condition", description: "At night" }]
    }
  ],
  shimmerTransforms: [
    { input: "ExampleMod/Ingredient", output: "ExampleMod/ShimmerResult" }
  ],
  drops: [
    {
      sourceType: "npc",
      source: "ExampleMod/Enemy",
      item: "ExampleMod/Result",
      rate: 0.5,
      stackMin: 1,
      stackMax: 1,
      conditions: []
    },
    {
      sourceType: "container",
      source: "ExampleMod/WorldChest",
      item: "ExampleMod/Result",
      rate: 1,
      stackMin: 1,
      stackMax: 1,
      conditions: []
    },
    {
      sourceType: "global",
      source: "ExampleMod/GlobalDrops",
      item: "ExampleMod/Result",
      rate: 0.01,
      stackMin: 1,
      stackMax: 1,
      conditions: [{ type: "ExampleCondition", description: null }]
    }
  ],
  shops: [
    {
      npc: "ExampleMod/Enemy",
      shop: "Shop",
      item: "ExampleMod/Result",
      conditions: [],
      observed: false,
      earliestStageIndex: -1,
      earliestStageName: ""
    }
  ],
  fishing: [
    {
      targetType: "item",
      target: "ExampleMod/Result",
      earliestStageIndex: 2,
      earliestStageName: "Example stage",
      conditions: ["Exact localized condition"]
    }
  ],
  npcAvailability: [
    {
      npc: "ExampleMod/Enemy",
      kind: "spawn",
      observed: false,
      earliestStageIndex: -1,
      earliestStageName: "",
      conditions: []
    }
  ],
  vanillaItemClassifications: [
    { item: "Terraria/Example", category: "Weapon", classes: ["melee"] }
  ],
  vanillaBuffClassifications: [
    {
      item: "Terraria/HeartLantern",
      category: "Passive",
      classes: ["melee", "ranged", "magic", "summoner"],
      isClassSpecific: false
    }
  ]
};

fixture.drops.push(structuredClone(fixture.drops[0]));
const originalFixture = structuredClone(fixture);
const knowledge = buildKnowledgeBase(fixture);
assert.deepEqual(fixture, originalFixture, "Knowledge generation mutated the snapshot");
assert.deepEqual(buildKnowledgeBase(fixture), knowledge, "Knowledge output is not deterministic");
assert.deepEqual(createSnapshotView(knowledge), fixture, "Knowledge is not a lossless snapshot view");
assert.equal(knowledge.format, "ProgressionJournalKnowledge");
assert.equal(knowledge.version, 1);
assert.equal(knowledge.source.snapshotGeneratedAtUtc, fixture.generatedAtUtc);
assert.equal(knowledge.entities.items.length, fixture.items.length);
assert.equal(knowledge.entities.npcs.length, fixture.npcs.length);
assert.deepEqual(knowledge.acquisitions.recipes, fixture.recipes);
assert.deepEqual(knowledge.acquisitions.drops, fixture.drops);
assert.equal(
  knowledge.acquisitions.drops.filter(drop => drop.sourceType === "npc").length,
  2,
  "Duplicate drop facts must be preserved");
assert.deepEqual(knowledge.acquisitions.shops, fixture.shops);
assert.deepEqual(knowledge.acquisitions.fishing, fixture.fishing);
assert.deepEqual(knowledge.acquisitions.shimmerTransforms, fixture.shimmerTransforms);
assert.deepEqual(knowledge.availability.npcs, fixture.npcAvailability);
assert.deepEqual(
  knowledge.classifications.vanillaItems,
  fixture.vanillaItemClassifications);
assert.deepEqual(
  knowledge.classifications.vanillaBuffs,
  fixture.vanillaBuffClassifications);
assert.deepEqual(
  [...new Set(knowledge.acquisitions.drops.map(drop => drop.sourceType))].sort(),
  ["container", "global", "npc"]);
assert.equal(knowledge.acquisitions.shops[0].observed, false);
assert.equal(knowledge.availability.npcs[0].observed, false);
assert.equal(knowledge.acquisitions.fishing[0].conditions[0], "Exact localized condition");
assert.equal(knowledge.diagnostics.npcSpawnProbe, null);
assert.equal(knowledge.summary.hasNpcSpawnProbe, false);

for (const modName of fs.readdirSync(modsRoot)) {
  const snapshotPath = path.join(modsRoot, modName, "snapshot.json");
  if (!fs.existsSync(snapshotPath)) continue;
  const snapshot = JSON.parse(fs.readFileSync(snapshotPath, "utf8"));
  const actual = buildKnowledgeBase(snapshot);
  assert.deepEqual(
    createSnapshotView(actual),
    snapshot,
    `${modName}: knowledge facts are not lossless`);
  const knowledgePath = path.join(modsRoot, modName, "knowledge.json");
  if (fs.existsSync(knowledgePath)) {
    assert.ok(
      isDeepStrictEqual(
        JSON.parse(fs.readFileSync(knowledgePath, "utf8")),
        actual),
      `${modName}: knowledge.json is stale`);
  }
  assert.equal(actual.summary.items, snapshot.items.length, `${modName}: item loss`);
  assert.equal(actual.summary.npcs, snapshot.npcs.length, `${modName}: NPC loss`);
  assert.equal(actual.summary.recipes, snapshot.recipes.length, `${modName}: recipe loss`);
  assert.equal(actual.summary.drops, snapshot.drops.length, `${modName}: drop loss`);
  assert.equal(actual.summary.shops, snapshot.shops.length, `${modName}: shop loss`);
  assert.equal(actual.summary.fishing, (snapshot.fishing ?? []).length, `${modName}: fishing loss`);
  assert.equal(
    actual.summary.shimmerTransforms,
    snapshot.shimmerTransforms?.length,
    `${modName}: shimmer transform loss`);
  assert.equal(
    actual.summary.npcAvailability,
    (snapshot.npcAvailability ?? []).length,
    `${modName}: NPC availability loss`);
  assert.equal(
    actual.summary.vanillaItemClassifications,
    (snapshot.vanillaItemClassifications ?? []).length,
    `${modName}: classification loss`);
  assert.equal(
    actual.summary.vanillaBuffClassifications,
    snapshot.vanillaBuffClassifications?.length,
    `${modName}: buff classification loss`);
  assert.deepEqual(actual.acquisitions.recipes, snapshot.recipes, `${modName}: recipes changed`);
  assert.deepEqual(actual.acquisitions.drops, snapshot.drops, `${modName}: drops changed`);
  assert.deepEqual(actual.acquisitions.shops, snapshot.shops, `${modName}: shops changed`);
  assert.deepEqual(actual.acquisitions.fishing, snapshot.fishing ?? [], `${modName}: fishing changed`);
  assert.deepEqual(
    actual.acquisitions.shimmerTransforms,
    snapshot.shimmerTransforms,
    `${modName}: shimmer transforms changed`);

  if (modName === "AAModClassic") {
    const support = JSON.parse(fs.readFileSync(
      path.join(modsRoot, modName, "support.json"),
      "utf8"));
    const recommendations = JSON.parse(fs.readFileSync(
      path.join(modsRoot, modName, "recommendations.json"),
      "utf8"));
    const fromSnapshot = generateProfile(
      snapshot,
      structuredClone(support),
      structuredClone(recommendations));
    const fromKnowledge = generateProfile(
      actual,
      structuredClone(support),
      structuredClone(recommendations));
    assert.deepEqual(
      removeGeneratedTimes(fromKnowledge),
      removeGeneratedTimes(fromSnapshot),
      "Knowledge-based stage generation changed the profile result");
  }
}

console.log("Knowledge base tests: OK");

function removeGeneratedTimes(value) {
  if (Array.isArray(value)) return value.map(removeGeneratedTimes);
  if (!value || typeof value !== "object") return value;
  return Object.fromEntries(
    Object.entries(value)
      .filter(([key]) => key !== "generatedAtUtc")
      .map(([key, nested]) => [key, removeGeneratedTimes(nested)]));
}
