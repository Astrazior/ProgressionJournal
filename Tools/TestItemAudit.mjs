import assert from "node:assert/strict";
import { createItemAudit } from "./BuildModProfiles.mjs";

const snapshot = {
  targetMod: "TestMod",
  profileId: "test.profile",
  generatedAtUtc: "2026-07-12T00:00:00.000Z",
  contentMods: ["TestMod"],
  items: [
    { id: "TestMod/Sword", name: "Sword" },
    { id: "TestMod/Potion", name: "Potion" },
    { id: "TestMod/Furniture", name: "Furniture" },
    { id: "TestMod/Missing", name: "Missing" },
    { id: "TestMod/Unavailable", name: "Unavailable" },
    { id: "Terraria/StoneBlock", name: "Stone Block" }
  ]
};
const profile = {
  id: "test.profile",
  entries: [{
    evaluations: [{ stageId: "start" }],
    itemGroups: [[{ mod: "TestMod", item: "Sword" }]]
  }],
  combatBuffs: [{
    stageId: "boss",
    itemGroups: [[{ mod: "TestMod", item: "Potion" }]]
  }]
};
const generationReport = {
  paths: {
    "TestMod/Sword": { stage: "start", via: "npc:TestMod/Enemy" },
    "TestMod/Potion": { stage: "boss", via: "recipe:TestMod/Herb" },
    "TestMod/Furniture": { stage: "start", via: "recipe:Terraria/Wood" },
    "Terraria/StoneBlock": { stage: "start", via: "manifest" }
  },
  excludedItems: [{ stage: "start", id: "TestMod/Furniture", reason: "not combat equipment" }],
  unresolvedAvailabilityItems: [{ item: "TestMod/Missing" }],
  unavailableCombatItems: [{ item: "TestMod/Unavailable" }]
};

const audit = createItemAudit(snapshot, profile, generationReport, {
  targetMod: "TestMod",
  id: "test.profile",
  contentMods: ["TestMod"]
});
const statuses = Object.fromEntries(audit.items.map(item => [item.id, item.status]));

assert.equal(audit.summary.snapshotItems, 6);
assert.equal(audit.summary.contentItems, 5);
assert.equal(audit.summary.profileItemReferences, 2);
assert.equal(statuses["TestMod/Sword"], "equipment");
assert.equal(statuses["TestMod/Potion"], "buff");
assert.equal(statuses["TestMod/Furniture"], "excluded");
assert.equal(statuses["TestMod/Missing"], "unresolved-availability");
assert.equal(statuses["TestMod/Unavailable"], "unavailable-combat");
assert.equal(statuses["Terraria/StoneBlock"], "acquired-non-profile");

console.log("Item audit tests: OK");
