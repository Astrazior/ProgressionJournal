import assert from "node:assert/strict";
import { generateProfile } from "./ProfileGeneratorCore.mjs";

const item = (id, values = {}) => ({
  id, name: id, englishName: "", damageClass: "", damage: 0, defense: 0,
  headSlot: -1, bodySlot: -1, legSlot: -1, accessory: false, vanity: false,
  ammo: 0, useAmmo: 0, buffType: 0, buffTime: 0, consumable: false,
  potion: false, healLife: 0, healMana: 0, food: false, flask: false, maxStack: 1,
  createTile: -1, placedTile: "", createWall: -1, pick: 0, axe: 0,
  hammer: 0, mountType: -1, shoot: 0, sentry: false, classEffects: [], ...values
});
const snapshot = {
  format: "ProgressionJournalSnapshot",
  version: 1,
  mods: [{ name: "Test", version: "1.2.3" }],
  items: [
    item("Test/Ore"),
    item("Test/Bar"),
    item("Test/Sword", { damageClass: "Melee", damage: 30 }),
    item("Test/Potion", { buffType: 1, consumable: true }),
    item("Test/EventMaterial"),
    item("Test/EventGun", { damageClass: "Melee", damage: 20 }),
    item("Test/ShopBlade", { damageClass: "Melee", damage: 18 }),
    item("Test/ManualShopBlade", { damageClass: "Melee", damage: 17 }),
    item("Test/UnknownShopBlade", { damageClass: "Melee", damage: 19 }),
    item("Test/ClassTool", { damageClass: "Melee" }),
    item("Test/BossBag"),
    item("Test/BagBlade", { damageClass: "Melee", damage: 21 }),
    item("Test/ConditionalBagBlade", { damageClass: "Melee", damage: 22 }),
    item("Test/ZeroRateBlade", { damageClass: "Melee", damage: 23 }),
    item("Test/FlooredBlade", { damageClass: "Melee", damage: 24 }),
    item("Test/LateDrop", { damageClass: "Melee", damage: 40 }),
    item("Test/ManualLateDrop", { damageClass: "Melee", damage: 39 }),
    item("Test/WikiOnlyBlade", { damageClass: "Melee", damage: 41 }),
    item("Terraria/FilteredSword", { damageClass: "Melee", damage: 25 }),
    item("Terraria/ModifiedSword", { damageClass: "Melee", damage: 26 }),
    item("Terraria/WikiSword", {
      englishName: "Localized Wiki Sword",
      damageClass: "Melee",
      damage: 27
    }),
    item("Test/EarlyWikiSword", { damageClass: "Melee", damage: 28 }),
    item("Test/RenamedBlade", { name: "Renamed Blade", damageClass: "Melee", damage: 29 }),
    item("Test/ExampleHelmet", { name: "Example Helmet", defense: 3, headSlot: 1 }),
    item("Test/ExampleBreastplate", { name: "Example Breastplate", defense: 4, bodySlot: 1 }),
    item("Test/ExampleGreaves", { name: "Example Greaves", defense: 2, legSlot: 1 }),
    item("Test/ExamplePartyHat", { name: "Example Party Hat", headSlot: 2 }),
    item("Test/BuffStation", {
      name: "Buff Station",
      createTile: 100,
      placedTile: "Test/BuffStationTile",
      consumable: true
    }),
    item("Test/BlueChime", { name: "Blue Chime", damageClass: "Magic", damage: 12 }),
    item("Test/RedChime", { name: "Red Chime", damageClass: "Magic", damage: 12 }),
    item("Test/TwinMask", { name: "Twin Mask", defense: 3, headSlot: 3 }),
    item("Test/TwinMask2", { name: "Twin Mask", defense: 3, headSlot: 4 }),
    item("Test/TwinShirt", { name: "Twin Shirt", defense: 4, bodySlot: 3 }),
    item("Test/TwinShirt2", { name: "Twin Shirt", defense: 4, bodySlot: 4 }),
    item("Test/TwinLeggings", { name: "Twin Leggings", defense: 2, legSlot: 3 }),
    item("Test/TwinLeggings2", { name: "Twin Leggings", defense: 2, legSlot: 4 }),
    item("Test/MeleeAccessory", {
      name: "Melee Accessory",
      accessory: true,
      classEffects: [{ damageClass: "Melee", damage: true }]
    }),
    item("Test/UniversalAccessory", {
      name: "Universal Accessory",
      accessory: true,
      classEffects: [{ damageClass: "GenericDamageClass", damage: true }]
    }),
    item("Test/MixedAccessory", {
      name: "Mixed Accessory",
      accessory: true,
      classEffects: [
        { damageClass: "Melee", damage: true },
        { damageClass: "Magic", crit: true }
      ]
    }),
    item("Test/GenericMeleeAccessory", {
      name: "Generic Melee Accessory",
      accessory: true,
      classEffects: [
        { damageClass: "GenericDamageClass", damage: true },
        { damageClass: "Melee", damage: true }
      ]
    }),
    item("Test/SupportTool"),
    item("Other/Material"),
    item("Other/Sword", { damageClass: "Melee", damage: 100 }),
    item("Test/ForeignRecipeSword", { damageClass: "Melee", damage: 90 }),
    item("Test/CycleA", { damageClass: "Melee", damage: 1 }),
    item("Test/CycleB"),
    item("Test/Forge", { createTile: 1, placedTile: "Test/ForgeTile" }),
    item("Test/ForgeSword", { damageClass: "Melee", damage: 35 }),
    item("Test/SeedSword", { damageClass: "Melee", damage: 99 }),
    item("Test/VanityAccessory", { accessory: true, vanity: true })
  ],
  npcs: [],
  recipes: [
    { result: "Test/Bar", ingredients: [{ item: "Test/Ore", stack: 1 }], stations: [], conditions: [] },
    { result: "Test/Sword", ingredients: [{ item: "Test/Bar", stack: 1 }], stations: [], conditions: [] },
    { result: "Test/EarlyWikiSword", ingredients: [{ item: "Test/Ore", stack: 1 }], stations: [], conditions: [] },
    { result: "Test/EventGun", ingredients: [{ item: "Test/EventMaterial", stack: 1 }], stations: [], conditions: [] },
    { result: "Test/ForeignRecipeSword", ingredients: [{ item: "Other/Material", stack: 1 }], stations: [], conditions: [] },
    { result: "Test/CycleA", ingredients: [{ item: "Test/CycleB", stack: 1 }], stations: [], conditions: [] },
    { result: "Test/CycleB", ingredients: [{ item: "Test/CycleA", stack: 1 }], stations: [], conditions: [] },
    { result: "Test/Forge", ingredients: [{ item: "Test/Ore", stack: 1 }], stations: [], conditions: [] },
    {
      result: "Test/ForgeSword",
      ingredients: [{ item: "Test/Ore", stack: 1 }],
      stations: ["Test/ForgeTile"],
      conditions: []
    }
  ],
  drops: [
    {
      source: "Test/Boss",
      sourceType: "npc",
      item: "Test/Ore",
      conditions: [{
        type: "Terraria.GameContent.ItemDropRules.Conditions+NotExpert",
        description: ""
      }]
    },
    { source: "Test/Boss", sourceType: "npc", item: "Test/Potion", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Terraria/FilteredSword", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Terraria/ModifiedSword", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Terraria/WikiSword", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Test/EarlyWikiSword", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Test/ClassTool", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Test/BossBag", conditions: [] },
    { source: "Test/BossBag", sourceType: "container", item: "Test/BagBlade", conditions: [] },
    {
      source: "Test/BossBag",
      sourceType: "container",
      item: "Test/ConditionalBagBlade",
      conditions: [{ type: "Test.Lambda", description: null }]
    },
    {
      source: "Test/Boss",
      sourceType: "npc",
      item: "Test/ZeroRateBlade",
      rate: 0,
      conditions: []
    },
    { source: "Test/Boss", sourceType: "npc", item: "Test/FlooredBlade", conditions: [] },
    { source: "Test/LateBoss", sourceType: "npc", item: "Test/LateDrop", conditions: [] },
    { source: "Test/ManualLateBoss", sourceType: "npc", item: "Test/ManualLateDrop", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Test/MeleeAccessory", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Test/UniversalAccessory", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Test/MixedAccessory", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Test/GenericMeleeAccessory", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Other/Material", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Other/Sword", conditions: [] },
    {
      source: "Test/Boss",
      sourceType: "npc",
      item: "Test/SeedSword",
      conditions: [{ type: "Test.SpecialSeed", description: "Special seed only" }]
    },
    { source: "Test/EventEnemy", sourceType: "npc", item: "Test/EventMaterial", conditions: [] }
  ],
  shops: [
    {
      npc: "Test/Merchant",
      item: "Test/ShopBlade",
      conditions: [{ type: "Test.Condition", description: "After Boss" }]
    },
    {
      npc: "Test/Merchant",
      item: "Test/UnknownShopBlade",
      conditions: [{ type: "Test.UnknownCondition", description: "After unknown event" }]
    },
    {
      npc: "Test/ManualMerchant",
      item: "Test/ManualShopBlade",
      conditions: []
    }
  ]
};
const manifest = {
  id: "test",
  name: { "en-US": "Test", "ru-RU": "Тест" },
  requiredMods: [{ name: "Test", version: "" }],
  modifiedVanillaItems: ["Terraria/ModifiedSword"],
  wikiSource: { name: "Test Wiki", url: "https://example.invalid" },
  wikiStageMap: {
    guide: { stageId: "boss", target: { "en-US": "Boss", "ru-RU": "Босс" } },
    earlyGuide: { stageId: "start", target: { "en-US": "Boss", "ru-RU": "Босс" } }
  },
  classes: [
    { id: "melee", name: { "en-US": "Melee", "ru-RU": "Воин" }, damageClassNames: ["Melee"] },
    { id: "magic", name: { "en-US": "Magic", "ru-RU": "Маг" }, damageClassNames: ["Magic"] }
  ],
  events: [
    {
      id: "test-event",
      stageId: "boss",
      customEventName: "Test Event",
      eventIcon: "Test/Bestiary/EventIcon",
      enemies: ["Test/EventEnemy"]
    }
  ],
  stages: [
    { id: "start", name: { "en-US": "Start", "ru-RU": "Начало" } },
    {
      id: "boss",
      name: { "en-US": "Boss", "ru-RU": "Босс" },
      dropSources: ["Test/Boss"],
      shops: ["Test/Merchant"]
    },
    { id: "empty", name: { "en-US": "Empty", "ru-RU": "Пусто" } },
    {
      id: "late",
      name: { "en-US": "Late", "ru-RU": "Поздно" },
      dropSources: ["Test/LateBoss"]
    }
  ],
  conditionUnlocks: [
    {
      stageId: "start",
      sources: ["drop"],
      sourceIds: ["Test/BossBag"],
      conditionTypes: ["Test.Lambda"]
    },
    {
      stageId: "boss",
      sources: ["shop"],
      conditionDescriptions: ["After Boss"]
    }
  ]
};

const wikiProfile = {
  requiredMods: [{ name: "Test", version: "1.2" }],
  entries: [
    {
      classes: ["melee"],
      itemGroups: [[{
        mod: "Terraria",
        item: "OutdatedWikiSword",
        displayName: "Localized Wiki Sword"
      }]],
      evaluations: [{ stageId: "guide" }]
    },
    {
      classes: ["melee"],
      itemGroups: [[{ mod: "Test", item: "EarlyWikiSword" }]],
      evaluations: [{ stageId: "earlyGuide" }]
    },
    {
      classes: ["melee"],
      itemGroups: [[{ mod: "Test", item: "EarlyWikiSword" }]],
      evaluations: [{ stageId: "earlyGuide" }]
    },
    {
      category: "Weapon",
      classes: ["melee"],
      itemGroups: [[{ mod: "Test", item: "OldBlade", displayName: "Renamed Blade" }]],
      evaluations: [{ stageId: "earlyGuide" }]
    },
    {
      category: "Armor",
      classes: ["melee"],
      itemGroups: [[{ mod: "Test", item: "Examplearmor", displayName: "Example armor" }]],
      evaluations: [{ stageId: "earlyGuide" }]
    },
    {
      category: "Support",
      classes: ["melee"],
      itemGroups: [[{ mod: "Test", item: "SupportTool", displayName: "Support Tool" }]],
      evaluations: [{ stageId: "earlyGuide" }]
    },
    {
      category: "Buff",
      classes: ["melee"],
      itemGroups: [[{ mod: "Test", item: "BuffStation" }]],
      evaluations: [{ stageId: "earlyGuide" }]
    },
    {
      category: "Weapon",
      classes: ["magic"],
      itemGroups: [[{ mod: "Test", item: "Chime", displayName: "Chime" }]],
      evaluations: [{ stageId: "earlyGuide" }]
    },
    {
      category: "Armor",
      classes: ["melee"],
      itemGroups: [[{ mod: "Test", item: "Twinarmor", displayName: "Twin armor" }]],
      evaluations: [{ stageId: "earlyGuide" }]
    },
    {
      category: "Weapon",
      classes: ["melee"],
      itemGroups: [[{ mod: "Test", item: "LateDrop", displayName: "Late Drop" }]],
      evaluations: [{ stageId: "earlyGuide" }]
    },
    {
      category: "Weapon",
      classes: ["melee"],
      itemGroups: [[{ mod: "Test", item: "WikiOnlyBlade", displayName: "Wiki Only Blade" }]],
      evaluations: [{ stageId: "earlyGuide" }]
    }
  ]
};
const { profile, report, review } = generateProfile(snapshot, manifest, wikiProfile);
assert.equal(profile.version, 1);
assert(profile.entries.some(entry => entry.itemGroups[0][0].item === "Sword"));
assert(profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "ForgeSword"
  && entry.evaluations[0].stageId === "boss"));
assert(!profile.entries.some(entry => entry.itemGroups[0][0].item === "SeedSword"));
assert(!profile.entries.some(entry => entry.itemGroups[0][0].item === "VanityAccessory"));
assert(!profile.entries.some(entry => entry.itemGroups[0][0].item === "CycleA"));
assert(profile.combatBuffs.some(entry => entry.itemGroups[0][0].item === "Potion"));
assert(!profile.combatBuffs.some(entry =>
  entry.itemGroups[0][0].item === "BuffStation"));
assert(report.wikiMissingItems.some(entry =>
  entry.id === "Test/BuffStation"
  && entry.reason === "recommendation has no proven availability"));
assert(!profile.combatBuffs.some(entry => entry.itemGroups[0][0].item === "Forge"));
assert.equal(
  profile.entries.find(entry => entry.itemGroups[0][0].item === "EventGun")?.customEventName,
  "Test Event");
assert.equal(
  profile.entries.find(entry => entry.itemGroups[0][0].item === "EventGun")?.eventIcon,
  "Test/Bestiary/EventIcon");
assert(!profile.entries.some(entry => entry.itemGroups[0][0].item === "FilteredSword"));
assert(profile.entries.some(entry => entry.itemGroups[0][0].item === "ModifiedSword"));
assert(profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "WikiSword"
  && entry.wiki.length === 1));
assert.deepEqual(
  profile.entries.find(entry => entry.itemGroups[0][0].item === "EarlyWikiSword")?.evaluations,
  [{ stageId: "boss", tier: "FromGuide", scope: "StageOnly" }]);
assert.deepEqual(
  profile.entries.find(entry => entry.itemGroups[0][0].item === "EarlyWikiSword")?.wiki
    .map(value => value.stageId),
  ["start"]);
assert.equal(
  profile.entries.find(entry => entry.itemGroups[0][0].item === "EarlyWikiSword")?.wiki.length,
  1);
assert.deepEqual(
  profile.entries.find(entry => entry.itemGroups[0][0].item === "WikiOnlyBlade")?.evaluations,
  []);
assert.deepEqual(
  profile.entries.find(entry => entry.itemGroups[0][0].item === "WikiOnlyBlade")?.wiki
    .map(value => value.stageId),
  ["start"]);
assert(profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "RenamedBlade"
  && entry.wiki.length === 1));
for (const itemName of ["ExampleHelmet", "ExampleBreastplate", "ExampleGreaves"]) {
  assert(profile.entries.some(entry =>
    entry.itemGroups[0][0].item === itemName
    && entry.wiki.length === 1
    && entry.classes.length === 1
    && entry.classes[0] === "melee"));
}
assert(!profile.entries.some(entry => entry.itemGroups[0][0].item === "ExamplePartyHat"));
assert(profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "SupportTool"
  && entry.category === "Support"
  && entry.isSupportWeapon));
assert(profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "ShopBlade"
  && entry.evaluations[0].stageId === "boss"));
assert.equal(report.paths["Test/ShopBlade"].via, "shop:Test/Merchant");
assert(profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "ClassTool"
  && entry.category === "Support"));
assert(profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "BagBlade"
  && entry.evaluations[0].stageId === "boss"));
assert.equal(report.paths["Test/BagBlade"].via, "container:Test/BossBag");
assert(profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "ConditionalBagBlade"
  && entry.evaluations[0].stageId === "boss"));
assert(!profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "ZeroRateBlade"));
assert.deepEqual(
  profile.entries.find(entry => entry.itemGroups[0][0].item === "LateDrop")?.evaluations,
  [{ stageId: "late", tier: "FromGuide", scope: "StageOnly" }]);
assert.deepEqual(
  profile.entries.find(entry => entry.itemGroups[0][0].item === "LateDrop")?.wiki
    .map(value => value.stageId),
  ["start"]);
assert.deepEqual(
  profile.entries.find(entry => entry.itemGroups[0][0].item === "MeleeAccessory")?.classes,
  ["melee"]);
assert.deepEqual(
  profile.entries.find(entry => entry.itemGroups[0][0].item === "UniversalAccessory")?.classes,
  ["melee", "magic"]);
assert.deepEqual(
  profile.entries.find(entry => entry.itemGroups[0][0].item === "MixedAccessory")?.classes,
  ["melee", "magic"]);
assert.deepEqual(
  profile.entries.find(entry => entry.itemGroups[0][0].item === "GenericMeleeAccessory")?.classes,
  ["melee", "magic"]);
assert(report.wikiResolvedItems.some(entry =>
  entry.from === "Test/OldBlade"
  && entry.to.includes("Test/RenamedBlade")));
assert(report.wikiResolvedItems.some(entry =>
  entry.from === "Test/Examplearmor"
  && entry.method === "armor-set"));
assert(report.wikiResolvedItems.some(entry =>
  entry.from === "Test/Chime"
  && entry.method === "variant-family"
  && entry.to.length === 2));
assert(report.wikiResolvedItems.some(entry =>
  entry.from === "Test/Twinarmor"
  && entry.method === "parallel-armor-sets"
  && entry.to.length === 6));
assert(!profile.entries.some(entry => entry.itemGroups[0][0].mod === "Other"));
assert(!profile.entries.some(entry => entry.itemGroups[0][0].item === "ForeignRecipeSword"));
assert(report.excludedItems.some(entry =>
  entry.id === "Terraria/FilteredSword"
  && entry.reason === "unchanged vanilla item in mod profile"));
assert(report.emptyStages.includes("empty"));
assert(!profile.entries.some(entry => entry.itemGroups[0][0].item === "UnknownShopBlade"));
assert(review.issues.some(issue =>
  issue.kind === "unresolved-condition"
  && issue.affected.some(value => value.item === "Test/UnknownShopBlade")));
assert(review.issues.some(issue =>
  issue.kind === "unassigned-combat-item"
  && issue.item === "Test/UnknownShopBlade"));
assert(review.issues.some(issue =>
  issue.kind === "unassigned-combat-item"
  && issue.item === "Test/CycleA"));

const manualAssignments = {
  format: "ProgressionJournalManualAssignments",
  version: 1,
  profileId: "test",
  itemStages: {
    "Test/CycleA": "boss",
    "Test/FlooredBlade": "late"
  },
  sourceStages: {
    "Test/ManualLateBoss": "boss",
    "Test/ManualMerchant": "boss"
  },
  stationStages: {},
  conditionStages: [{
    stageId: "boss",
    sources: ["shop"],
    sourceIds: ["Test/Merchant"],
    conditionTypes: ["Test.UnknownCondition"],
    conditionDescriptions: []
  }],
  itemOverrides: {
    "Test/MixedAccessory": { classes: ["melee"] }
  },
  ignoredItems: [],
  ignoredIssues: []
};
const manualResult = generateProfile(snapshot, manifest, wikiProfile, manualAssignments);
assert(manualResult.profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "CycleA"
  && entry.evaluations[0].stageId === "boss"));
assert(manualResult.profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "FlooredBlade"
  && entry.evaluations[0].stageId === "late"));
assert(manualResult.profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "ManualLateDrop"
  && entry.evaluations[0].stageId === "boss"));
assert(manualResult.profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "ManualShopBlade"
  && entry.evaluations[0].stageId === "boss"));
assert(manualResult.profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "UnknownShopBlade"
  && entry.evaluations[0].stageId === "boss"));
assert.deepEqual(
  manualResult.profile.entries.find(entry =>
    entry.itemGroups[0][0].item === "MixedAccessory")?.classes,
  ["melee"]);
assert(!manualResult.review.issues.some(issue =>
  issue.kind === "unresolved-condition"
  && issue.affected.some(value => value.item === "Test/UnknownShopBlade")));
assert(!manualResult.review.issues.some(issue =>
  issue.kind === "unassigned-combat-item"
  && issue.item === "Test/CycleA"));

const ignoredIssueId = review.issues.find(issue =>
  issue.kind === "unresolved-condition"
  && issue.affected.some(value => value.item === "Test/UnknownShopBlade")).id;
const ignoredReviewResult = generateProfile(snapshot, manifest, wikiProfile, {
  format: "ProgressionJournalManualAssignments",
  version: 1,
  profileId: "test",
  itemStages: {},
  sourceStages: {},
  stationStages: {},
  conditionStages: [],
  itemOverrides: {},
  ignoredItems: [],
  ignoredIssues: [ignoredIssueId]
});
assert(!ignoredReviewResult.review.issues.some(issue => issue.id === ignoredIssueId));

assert.equal(report.wikiAvailabilityCorrections.length, 0);
console.log("Profile generator tests: OK");
