import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { normalizeAgentRules } from "./BuildModProfiles.mjs";
import { generateProfile, readJson } from "./ProfileGeneratorCore.mjs";
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
  const supportWithVanillaSources = applyVanillaSourceCatalog(support);
  assert.equal(support.targetMod, modName);
  assert.equal(snapshot.targetMod, modName);
  assert.equal(profile.format, "ProgressionJournalProfile");
  const stageForFlag = key => supportWithVanillaSources.stages.find(stage =>
    stage.unlock?.type === "vanilla-flag" && stage.unlock.key === key);
  assert(stageForFlag("downedBoss2")?.include?.includes("Terraria/MeteoriteBar"),
    `${modName}: shared vanilla catalog did not add Meteorite Bar`);
  assert(stageForFlag("hardMode")?.enemies?.includes("Terraria/VampireBat"),
    `${modName}: shared vanilla catalog did not add Vampire Bat`);
  assert(stageForFlag("downedPlantBoss")?.shops?.includes("Terraria/Cyborg"),
    `${modName}: shared vanilla catalog did not add the Cyborg shop`);
  assert(supportWithVanillaSources.stages[0]?.shops?.includes("Terraria/ArmsDealer"),
    `${modName}: shared vanilla catalog did not add the Arms Dealer shop`);
  for (const event of support.events ?? []) {
    if (event.customEventName) {
      assert(event.eventIcon,
        `${modName}: custom event '${event.customEventName}' has no event icon`);
    }
  }
  for (const entry of profile.entries) {
    if (entry.customEventName) {
      assert(entry.eventIcon,
        `${modName}: generated custom event '${entry.customEventName}' has no event icon`);
    }
  }
  if (modName === "CalamityMod") {
    const profileItems = new Set(profile.entries.flatMap(entry =>
      (entry.itemGroups ?? []).flat().map(item => `${item.mod}/${item.item}`)));
    for (const item of [
      "CalamityMod/AcrobaticBobber",
      "CalamityMod/AlluringBait",
      "CalamityMod/EnchantedPearl",
      "CalamityMod/FeralBobber",
      "CalamityMod/SunkenSinker",
      "CalamityMod/SupremeBaitTackleBoxFishingStation",
      "CalamityMod/VolcanicSinker",
      "CalamityMod/WulfrumBobber"
    ]) {
      assert(!profileItems.has(item), `${item} is fishing utility and must be excluded`);
    }
    assert(profileItems.has("CalamityMod/FishStocks"),
      "Fish Stocks provides combat stats and must remain in the combat profile");
    const stageOf = itemId => {
      const [mod, item] = itemId.split("/");
      return profile.entries.find(entry =>
        (entry.itemGroups ?? []).flat().some(reference =>
          reference.mod === mod && reference.item === item))
        ?.evaluations?.[0]?.stageId;
    };
    for (const [item, stageId] of Object.entries({
      "CalamityMod/OpalStriker": "world-evil",
      "CalamityMod/Prismalline": "wall-of-flesh",
      "CalamityMod/PlagueReaperMask": "golem",
      "CalamityMod/OntologicalDespoiler": "ceaseless-void",
      "CalamityMod/Supernova": "exo-mechs"
    })) {
      assert.equal(stageOf(item), stageId, `${item} must follow its latest recipe dependency`);
    }
  }
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
    },
    {
      id: "verified-overrides",
      kind: "item-override",
      items: ["Test/HelmetA", "Test/HelmetB"],
      override: { classes: ["melee"] },
      sourceUrl: "https://example.invalid/source/Helmets",
      sourceVersion: "revision-1",
      checkedAt: "2026-06-15",
      reason: "Both helmets have the same class-specific equip effect."
    },
    {
      id: "verified-fishing",
      kind: "fishing-source",
      item: "Test/Amulet",
      conditions: [{ "en-US": "In test water", "ru-RU": "В тестовой воде" }],
      sourceUrl: "https://example.invalid/source/Fishing",
      sourceVersion: "revision-1",
      checkedAt: "2026-06-15",
      reason: "The item is returned by the custom fishing hook in this biome."
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
assert.deepEqual(normalized.assignments.itemOverrides["Test/HelmetA"], { classes: ["melee"] });
assert.deepEqual(normalized.assignments.itemOverrides["Test/HelmetB"], { classes: ["melee"] });
assert.deepEqual(normalized.assignments.fishingSources["Test/Amulet"], [{
  conditions: [{ "en-US": "In test water", "ru-RU": "В тестовой воде" }]
}]);
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
    { id: "world-evil", unlock: { type: "vanilla-flag", key: "downedBoss2" } },
    { id: "dungeon", unlock: { type: "vanilla-flag", key: "downedBoss3" } },
    { id: "hardmode", unlock: { type: "vanilla-flag", key: "hardMode" } },
    { id: "plantera", unlock: { type: "vanilla-flag", key: "downedPlantBoss" } }
  ]
});
assert(vanillaSources.initialStations.includes("Terraria/DemonAltar"));
assert(vanillaSources.initialStations.includes("Terraria/Hellforge"));
assert(vanillaSources.initialItems.includes("Terraria/PinkGel"));
assert(vanillaSources.initialItems.includes("Terraria/JungleRose"));
assert(vanillaSources.stages.find(stage => stage.id === "start")
  .shops.includes("Terraria/ArmsDealer"));
assert(vanillaSources.stages.find(stage => stage.id === "start")
  .shops.includes("Terraria/SkeletonMerchant"));
assert(vanillaSources.stages.find(stage => stage.id === "start")
  .enemies.includes("Terraria/Harpy"));
assert(vanillaSources.stages.find(stage => stage.id === "goblin-stage")
  .include.includes("Terraria/TinkerersWorkshop"));
assert(vanillaSources.stages.find(stage => stage.id === "dungeon")
  .stations.includes("Terraria/AlchemyTable"));
assert(vanillaSources.stages.find(stage => stage.id === "hardmode")
  .include.includes("Terraria/AdamantiteForge"));
assert(vanillaSources.stages.find(stage => stage.id === "hardmode")
  .enemies.includes("Terraria/VampireBat"));
assert(vanillaSources.stages.find(stage => stage.id === "plantera")
  .enemies.includes("Terraria/Reaper"));
assert(vanillaSources.stages.find(stage => stage.id === "plantera")
  .shops.includes("Terraria/Cyborg"));
assert(vanillaSources.stages.find(stage => stage.id === "world-evil")
  .include.includes("Terraria/MeteoriteBar"));
assert.equal(vanillaSources.itemStageFloors["Terraria/MeteoriteBar"], "world-evil");
assert.equal(vanillaSources.stationStageFloors["Terraria/AlchemyTable"], "dungeon");

const prefixedConditionSnapshot = {
  format: "ProgressionJournalSnapshot",
  version: 2,
  items: [{
    id: "Test/ConditionWeapon",
    name: "Condition Weapon",
    damageClass: "Magic",
    damage: 10,
    defense: 0,
    headSlot: -1,
    bodySlot: -1,
    legSlot: -1,
    accessory: false,
    vanity: false,
    ammo: 0,
    useAmmo: 0,
    buffType: 0,
    buffTime: 0,
    consumable: false,
    potion: false,
    healLife: 0,
    healMana: 0,
    food: false,
    flask: false,
    maxStack: 1,
    createTile: -1,
    placedTile: "",
    createWall: -1,
    pick: 0,
    axe: 0,
    hammer: 0,
    mountType: -1,
    shoot: 1,
    sentry: false,
    sourceNamespace: "",
    classEffects: []
  }],
  npcs: [],
  recipes: [],
  shops: [],
  drops: [{
    sourceType: "npc",
    source: "Terraria/Harpy",
    item: "Test/ConditionWeapon",
    rate: 1,
    stackMin: 1,
    stackMax: 1,
    conditions: [{
      type: "Test.Condition",
      description: "Предметы: After defeating the Eye of Cthulhu"
    }]
  }]
};
const prefixedConditionSupport = {
  id: "test.conditions",
  contentMods: ["Test"],
  classes: [{ id: "magic", damageClassNames: ["Magic"] }],
  conditionUnlocks: [{
    stageId: "eye",
    sources: ["drop"],
    conditionDescriptions: ["After defeating the Eye of Cthulhu"]
  }],
  stages: [
    { id: "start", unlock: { type: "always" } },
    { id: "eye", unlock: { type: "vanilla-flag", key: "downedBoss1" } }
  ]
};
const prefixedConditionResult = generateProfile(
  prefixedConditionSnapshot,
  prefixedConditionSupport);
assert.equal(
  prefixedConditionResult.report.paths["Test/ConditionWeapon"]?.stage,
  "eye",
  "localized item-condition prefixes must not break stage matching");

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
assert(exporter.includes('!mod.Name.Equals("ModLoader"'));
assert(builder.includes("rule.sources ?? (rule.source ? [rule.source] : [])"));
assert(builder.includes("rule.items ?? (rule.item ? [rule.item] : [])"));
assert(exporter.includes("File.Move(temporaryPath, path, overwrite: true)"));
assert(exporter.includes("EnvironmentMods = ModLoader.Mods"));
assert(
  exporter.includes("result[property.Name] = property.Value.GetString() ?? string.Empty"),
  "English vanilla item names must tolerate duplicate localization keys",
);

console.log("Mod profile pipeline tests: OK");

function modOf(reference) {
  return reference.split("/", 1)[0];
}
