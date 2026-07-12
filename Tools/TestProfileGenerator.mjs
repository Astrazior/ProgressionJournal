import assert from "node:assert/strict";
import { generateProfile } from "./ProfileGeneratorCore.mjs";

const item = (id, values = {}) => ({
  id, name: id, englishName: "", damageClass: "", damage: 0, defense: 0,
  headSlot: -1, bodySlot: -1, legSlot: -1, accessory: false, vanity: false,
  ammo: 0, useAmmo: 0, buffType: 0, buffTime: 0, consumable: false,
  potion: false, healLife: 0, healMana: 0, food: false, flask: false, maxStack: 1,
  createTile: -1, placedTile: "", createWall: -1, pick: 0, axe: 0,
  hammer: 0, mountType: -1, shoot: 0, sentry: false, sourceNamespace: "",
  classEffects: [], ...values
});
const snapshot = {
  format: "ProgressionJournalSnapshot",
  version: 4,
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
    item("Test/GlobalBlade", { damageClass: "Melee", damage: 37 }),
    item("Test/ManualLateDrop", { damageClass: "Melee", damage: 39 }),
    item("Test/CollisionSource"),
    item("Test/CollisionBlade", { damageClass: "Melee", damage: 38 }),
    item("Test/WikiOnlyBlade", { damageClass: "Melee", damage: 41 }),
    item("Terraria/FilteredSword", { damageClass: "Melee", damage: 25 }),
    item("Terraria/ModifiedSword", { damageClass: "Melee", damage: 26 }),
    item("Terraria/UnknownVanillaSword", { damageClass: "Melee", damage: 24 }),
    item("Terraria/UnknownSourceSword", { damageClass: "Melee", damage: 23 }),
    item("Terraria/WikiSword", {
      englishName: "Localized Wiki Sword",
      damageClass: "Melee",
      damage: 27
    }),
    item("Terraria/Present"),
    item("Terraria/CandyCaneSword", { damageClass: "Melee", damage: 20 }),
    item("Terraria/VanillaSummonerAccessory", { accessory: true }),
    item("Terraria/VanillaUtilityAccessory", { accessory: true }),
    item("Terraria/VanillaMagicHelmet", { defense: 5, headSlot: 12 }),
    item("Terraria/VanillaMeleeHelmet", { defense: 8, headSlot: 13 }),
    item("Test/EarlyWikiSword", { damageClass: "Melee", damage: 28 }),
    item("Test/RenamedBlade", { name: "Renamed Blade", damageClass: "Melee", damage: 29 }),
    item("Test/ExampleHelmet", {
      name: "Локализованный шлем",
      englishName: "Example Helmet",
      defense: 3,
      headSlot: 1
    }),
    item("Test/ExampleBreastplate", {
      name: "Локализованный нагрудник",
      englishName: "Example Breastplate",
      defense: 4,
      bodySlot: 1
    }),
    item("Test/ExampleGreaves", {
      name: "Локализованные поножи",
      englishName: "Example Greaves",
      defense: 2,
      legSlot: 1
    }),
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
      damage: 10,
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
    item("Test/UtilityAccessory", {
      name: "Utility Accessory",
      accessory: true
    }),
    item("Test/SupportTool", { damageClass: "Melee" }),
    item("Other/Material"),
    item("Other/Sword", { damageClass: "Melee", damage: 100 }),
    item("Test/ForeignRecipeSword", { damageClass: "Melee", damage: 90 }),
    item("Test/CycleA", { damageClass: "Melee", damage: 1 }),
    item("Test/CycleB"),
    item("Test/Forge", { createTile: 1, placedTile: "Test/ForgeTile" }),
    item("Test/ForgeSword", { damageClass: "Melee", damage: 35 }),
    item("Test/SeedSword", { damageClass: "Melee", damage: 99 }),
    item("Test/VanityAccessory", { accessory: true, vanity: true }),
    item("Test/NamespaceVanity", {
      accessory: true,
      sourceNamespace: "Test.Items.Accessories.Vanity"
    }),
    item("Terraria/WhiteString", { accessory: true }),
    item("Terraria/BrownString", { accessory: true }),
    item("Terraria/BlackString", { accessory: true })
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
    },
    { result: "Terraria/WhiteString", ingredients: [], stations: [], conditions: [] },
    {
      result: "Terraria/BrownString",
      ingredients: [{ item: "Terraria/WhiteString", stack: 1 }],
      stations: [],
      conditions: []
    },
    {
      result: "Terraria/BlackString",
      ingredients: [
        { item: "Terraria/WhiteString", stack: 1 },
        { item: "Test/LateDrop", stack: 1 }
      ],
      stations: [],
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
    {
      source: "Test/Boss",
      sourceType: "npc",
      item: "Terraria/UnknownVanillaSword",
      conditions: [{ type: "Test.UnknownVanillaCondition", description: "Unknown vanilla gate" }]
    },
    {
      source: "Terraria/UnknownEnemy",
      sourceType: "npc",
      item: "Terraria/UnknownSourceSword",
      conditions: []
    },
    { source: "Test/Boss", sourceType: "npc", item: "Terraria/WikiSword", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Test/EarlyWikiSword", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Test/RenamedBlade", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Test/ExampleHelmet", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Test/ExampleBreastplate", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Test/ExampleGreaves", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Test/SupportTool", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Test/ClassTool", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Test/BossBag", conditions: [] },
    { source: "Test/BossBag", sourceType: "container", item: "Test/BagBlade", conditions: [] },
    {
      source: "Terraria/Present",
      sourceType: "container",
      item: "Terraria/CandyCaneSword",
      conditions: [{
        type: "Terraria.GameContent.ItemDropRules.Conditions+IsHardmode",
        description: ""
      }]
    },
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
    { source: "Test/CollisionSource", sourceType: "npc", item: "Test/CollisionBlade", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Test/MeleeAccessory", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Test/UniversalAccessory", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Test/MixedAccessory", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Test/GenericMeleeAccessory", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Test/UtilityAccessory", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Terraria/VanillaSummonerAccessory", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Terraria/VanillaUtilityAccessory", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Terraria/VanillaMagicHelmet", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Terraria/VanillaMeleeHelmet", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Other/Material", conditions: [] },
    { source: "Test/Boss", sourceType: "npc", item: "Other/Sword", conditions: [] },
    {
      source: "Test/Boss",
      sourceType: "npc",
      item: "Test/SeedSword",
      conditions: [{ type: "Test.SpecialSeed", description: "Special seed only" }]
    },
    {
      source: "Terraria/GlobalNPCDrops",
      sourceType: "global",
      item: "Test/GlobalBlade",
      conditions: [{ type: "Test.Condition", description: "After Boss" }]
    },
    { source: "Test/EventEnemy", sourceType: "npc", item: "Test/EventMaterial", conditions: [] }
  ],
  shops: [
    {
      npc: "Test/Merchant",
      item: "Test/ShopBlade",
      observed: true,
      earliestStageIndex: 1,
      earliestStageName: "Boss",
      conditions: [{ type: "Test.Condition", description: "After Boss" }]
    },
    {
      npc: "Test/Merchant",
      item: "Test/UnknownShopBlade",
      observed: true,
      earliestStageIndex: 1,
      earliestStageName: "Boss",
      conditions: [{ type: "Test.UnknownCondition", description: "After unknown event" }]
    },
    {
      npc: "Test/ManualMerchant",
      item: "Test/ManualShopBlade",
      observed: true,
      earliestStageIndex: 1,
      earliestStageName: "Boss",
      conditions: []
    }
  ],
  vanillaItemClassifications: [
    {
      item: "Terraria/VanillaSummonerAccessory",
      category: "Accessory",
      classes: ["magic"]
    },
    {
      item: "Terraria/VanillaMagicHelmet",
      category: "Armor",
      classes: ["magic"]
    },
    {
      item: "Terraria/VanillaMeleeHelmet",
      category: "Armor",
      classes: ["melee"]
    },
    {
      item: "Terraria/WhiteString",
      category: "Accessory",
      classes: ["melee"]
    },
    {
      item: "Terraria/BrownString",
      category: "Accessory",
      classes: ["melee"]
    },
    {
      item: "Terraria/BlackString",
      category: "Accessory",
      classes: ["melee"]
    }
  ]
};
const manifest = {
  id: "test",
  name: { "en-US": "Test", "ru-RU": "Тест" },
  requiredMods: [{ name: "Test", version: "" }],
  modifiedVanillaItems: ["Terraria/ModifiedSword"],
  initialItems: ["Test/CollisionSource"],
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
      dropSources: ["Test/Boss", "Test/CollisionSource"],
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
      sources: ["shop", "drop"],
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
      evaluations: [{ stageId: "guide" }]
    },
    {
      category: "Armor",
      classes: ["melee"],
      itemGroups: [[{ mod: "Test", item: "Examplearmor", displayName: "Example armor" }]],
      evaluations: [{ stageId: "guide" }]
    },
    {
      category: "Support",
      classes: ["melee"],
      itemGroups: [[{ mod: "Test", item: "SupportTool", displayName: "Support Tool" }]],
      evaluations: [{ stageId: "guide" }]
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
assert(!profile.entries.some(entry => entry.itemGroups[0][0].item === "NamespaceVanity"));
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
assert(profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "FilteredSword"
  && entry.evaluations[0].stageId === "boss"));
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
  []);
assert.equal(
  profile.entries.find(entry => entry.itemGroups[0][0].item === "EarlyWikiSword")?.wiki.length,
  0);
assert(!profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "WikiOnlyBlade"));
assert(report.wikiMissingItems.some(entry =>
  entry.id === "Test/WikiOnlyBlade"
  && entry.reason === "recommendation has no proven availability"));
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
assert(profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "GlobalBlade"
  && entry.evaluations[0].stageId === "boss"));
assert.equal(report.paths["Test/GlobalBlade"].via, "global:Terraria/GlobalNPCDrops");
assert.equal(report.paths["Test/ShopBlade"].via, "shop:Test/Merchant");
assert(profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "ClassTool"
  && entry.category === "Support"));
assert(profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "BagBlade"
  && entry.evaluations[0].stageId === "boss"));
assert.equal(report.paths["Test/BagBlade"].via, "container:Test/BossBag");
assert(profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "CandyCaneSword"
  && entry.evaluations[0].stageId === "start"));
assert.equal(report.paths["Terraria/CandyCaneSword"].via, "container:Terraria/Present");
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
  []);
assert.deepEqual(
  profile.entries.find(entry => entry.itemGroups[0][0].item === "MeleeAccessory")?.classes,
  ["melee"]);
assert.equal(
  profile.entries.find(entry => entry.itemGroups[0][0].item === "MeleeAccessory")?.category,
  "Accessory");
assert.deepEqual(
  profile.entries.find(entry => entry.itemGroups[0][0].item === "UniversalAccessory")?.classes,
  ["melee", "magic"]);
assert.deepEqual(
  profile.entries.find(entry => entry.itemGroups[0][0].item === "MixedAccessory")?.classes,
  ["melee", "magic"]);
assert.deepEqual(
  profile.entries.find(entry => entry.itemGroups[0][0].item === "GenericMeleeAccessory")?.classes,
  ["melee", "magic"]);
assert(!profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "UtilityAccessory"));
assert.deepEqual(
  profile.entries.find(entry =>
    entry.itemGroups[0][0].item === "VanillaSummonerAccessory")?.classes,
  ["magic"]);
assert.deepEqual(
  profile.entries.find(entry =>
    entry.itemGroups[0][0].item === "VanillaMagicHelmet")?.classes,
  ["magic"]);
assert.deepEqual(
  profile.entries.find(entry =>
    entry.itemGroups[0][0].item === "VanillaMeleeHelmet")?.classes,
  ["melee"]);
assert(!profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "VanillaUtilityAccessory"));
const stringEntry = profile.entries.find(entry =>
  entry.itemGroups[0].some(reference => reference.item === "WhiteString"));
assert.deepEqual(stringEntry?.evaluations, [{ stageId: "start", tier: "FromGuide", scope: "StageOnly" }]);
assert.deepEqual(
  stringEntry?.itemGroups[0].map(reference => reference.item),
  ["WhiteString", "BrownString", "BlackString"]);
assert.equal(
  profile.entries.filter(entry =>
    entry.itemGroups[0].some(reference =>
      ["WhiteString", "BrownString", "BlackString"].includes(reference.item))).length,
  1);
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
assert(!report.excludedItems.some(entry =>
  entry.id === "Terraria/FilteredSword"));
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
assert(review.issues.some(issue =>
  issue.kind === "unresolved-condition"
  && issue.affected.some(value => value.item === "Terraria/UnknownVanillaSword")));
assert(review.issues.some(issue =>
  issue.kind === "unassigned-combat-item"
  && issue.item === "Terraria/UnknownVanillaSword"));
assert(report.unassignedVanillaNpcSources.some(record =>
  record.source === "Terraria/UnknownEnemy"
  && record.items.includes("Terraria/UnknownSourceSword")));
assert(profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "CollisionBlade"
  && entry.evaluations[0].stageId === "boss"));

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
  fishingSources: {
    "Test/MixedAccessory": [{
      conditions: [{ "en-US": "In test water", "ru-RU": "В тестовой воде" }]
    }]
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
assert.deepEqual(
  manualResult.profile.entries.find(entry =>
    entry.itemGroups[0][0].item === "MixedAccessory")?.fishingSources,
  [{ conditions: [{ "en-US": "In test water", "ru-RU": "В тестовой воде" }] }]);
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
  fishingSources: {},
  ignoredItems: [],
  ignoredIssues: [ignoredIssueId]
});
assert(!ignoredReviewResult.review.issues.some(issue => issue.id === ignoredIssueId));

const runtimeSnapshot = structuredClone(snapshot);
runtimeSnapshot.version = 4;
runtimeSnapshot.items.push(
  item("Test/RuntimeFishBlade", { damageClass: "Melee", damage: 31 }),
  item("Test/RuntimeSpawnBlade", { damageClass: "Melee", damage: 32 }),
  item("Test/RuntimeShopBlade", { damageClass: "Melee", damage: 33 }),
  item("Test/EventFloorBlade", { damageClass: "Melee", damage: 34 }),
  item("Test/ManualFloorBlade", { damageClass: "Melee", damage: 35 }));
runtimeSnapshot.drops.push({
  source: "Test/RuntimeEnemy",
  sourceType: "npc",
  item: "Test/RuntimeSpawnBlade",
  conditions: []
}, {
  source: "Test/EventEnemy",
  sourceType: "npc",
  item: "Test/EventFloorBlade",
  conditions: []
}, {
  source: "Test/ManualLateBoss",
  sourceType: "npc",
  item: "Test/ManualFloorBlade",
  conditions: []
});
runtimeSnapshot.shops.push({
  npc: "Test/RuntimeMerchant",
  shop: "Shop",
  item: "Test/RuntimeShopBlade",
  conditions: [],
  observed: true,
  earliestStageIndex: 3,
  earliestStageName: "Late"
});
runtimeSnapshot.fishing = [{
  targetType: "item",
  target: "Test/RuntimeFishBlade",
  earliestStageIndex: 1,
  earliestStageName: "Boss",
  conditions: ["In runtime fishing scenario"]
}];
runtimeSnapshot.npcAvailability = [
  {
    npc: "Test/RuntimeEnemy",
    kind: "spawn",
    observed: true,
    earliestStageIndex: 1,
    earliestStageName: "Boss",
    conditions: []
  },
  {
    npc: "Test/RuntimeMerchant",
    kind: "town",
    observed: true,
    earliestStageIndex: 3,
    earliestStageName: "Late",
    conditions: []
  },
  {
    npc: "Test/EventEnemy",
    kind: "spawn",
    observed: true,
    earliestStageIndex: 0,
    earliestStageName: "Start",
    conditions: []
  },
  {
    npc: "Test/ManualLateBoss",
    kind: "spawn",
    observed: true,
    earliestStageIndex: 0,
    earliestStageName: "Start",
    conditions: []
  }
];
const runtimeResult = generateProfile(
  runtimeSnapshot,
  manifest,
  wikiProfile,
  manualAssignments);
assert(runtimeResult.profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "RuntimeFishBlade"
  && entry.evaluations[0].stageId === "boss"
  && entry.fishingSources[0]?.conditions[0] === "In runtime fishing scenario"));
assert(runtimeResult.profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "RuntimeSpawnBlade"
  && entry.evaluations[0].stageId === "boss"));
assert(runtimeResult.profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "RuntimeShopBlade"
  && entry.evaluations[0].stageId === "late"));
assert(runtimeResult.profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "EventFloorBlade"
  && entry.evaluations[0].stageId === "boss"));
assert(runtimeResult.profile.entries.some(entry =>
  entry.itemGroups[0][0].item === "ManualFloorBlade"
  && entry.evaluations[0].stageId === "boss"));
assert(runtimeResult.report.sourceAvailabilityCorrections.some(entry =>
  entry.source === "Test/EventEnemy"
  && entry.observedStageId === "start"
  && entry.effectiveStageId === "boss"));

assert(report.wikiAvailabilityCorrections.some(entry =>
  entry.id === "Test/EarlyWikiSword"
  && entry.factualStage === "boss"
  && entry.recommendedStage === "start"));
assert(report.wikiAvailabilityCorrections.some(entry =>
  entry.id === "Test/LateDrop"
  && entry.factualStage === "late"
  && entry.recommendedStage === "start"));

const compoundBossConditionSnapshot = {
  format: "ProgressionJournalSnapshot",
  version: 4,
  mods: [{ name: "Test", version: "1.2.3" }],
  items: [
    item("Test/DisjunctiveBlade", { damageClass: "Melee", damage: 20 }),
    item("Test/ConjunctiveBlade", { damageClass: "Melee", damage: 20 })
  ],
  npcs: [],
  recipes: [],
  drops: [
    {
      source: "Terraria/GlobalNPCDrops",
      sourceType: "global",
      item: "Test/DisjunctiveBlade",
      conditions: [{
        type: "Test.CompositeBossCondition",
        description: "After defeating the Eye of Cthulhu or Skeletron"
      }]
    },
    {
      source: "Terraria/GlobalNPCDrops",
      sourceType: "global",
      item: "Test/ConjunctiveBlade",
      conditions: [{
        type: "Test.CompositeBossCondition",
        description: "After defeating the Eye of Cthulhu and Skeletron"
      }]
    }
  ],
  shops: []
};
const compoundBossConditionManifest = {
  id: "test.compound-conditions",
  name: { "en-US": "Test", "ru-RU": "Тест" },
  requiredMods: [{ name: "Test", version: "" }],
  classes: [
    { id: "melee", name: { "en-US": "Melee", "ru-RU": "Воин" }, damageClassNames: ["Melee"] }
  ],
  stages: [
    { id: "start", name: { "en-US": "Start", "ru-RU": "Начало" } },
    { id: "eye-of-cthulhu", name: { "en-US": "Eye of Cthulhu", "ru-RU": "Глаз Ктулху" } },
    { id: "skeletron", name: { "en-US": "Skeletron", "ru-RU": "Скелетрон" } }
  ]
};
const compoundBossConditionResult = generateProfile(
  compoundBossConditionSnapshot,
  compoundBossConditionManifest);
assert.equal(
  compoundBossConditionResult.report.paths["Test/DisjunctiveBlade"]?.stage,
  "eye-of-cthulhu");
assert.equal(
  compoundBossConditionResult.report.paths["Test/ConjunctiveBlade"]?.stage,
  "skeletron");

console.log("Profile generator tests: OK");
