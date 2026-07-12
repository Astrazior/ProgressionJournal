import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import {
  applyConfirmedAvailabilityChecks,
  auditRuntimeSourceCoverage,
  normalizeAgentRules
} from "./BuildModProfiles.mjs";
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
const fishingResolverSource = fs.readFileSync(
  path.join(root, "Data", "Resolvers", "JournalFishingSourceResolver.cs"),
  "utf8");
const fishingAttemptMethod = fishingResolverSource.slice(
  fishingResolverSource.indexOf("private static void ProbeContextDrops"),
  fishingResolverSource.indexOf("private static FishingAttempt CreateAttempt"));
const fishingHookOrder = [
  "PlayerLoader.ModifyFishingAttempt",
  "InvokeFishingAttempt(pipeline.RollEnemySpawns",
  "InvokeFishingAttempt(pipeline.RollItemDrop",
  "PlayerLoader.CatchFish",
  "AddCaughtItem"
].map(token => fishingAttemptMethod.indexOf(token));
assert(fishingHookOrder.every(index => index >= 0)
  && fishingHookOrder.every((index, position) =>
    position === 0 || index > fishingHookOrder[position - 1]),
"Fishing hooks are not executed in tModLoader order");
assert(fishingResolverSource.includes("PlayerLoader.ModifyCaughtFish"),
  "The final caught item is not passed through ModifyCaughtFish");
assert(fishingResolverSource.includes("attempt.legendary = rarity == 4")
  && fishingResolverSource.includes("attempt.crate = rarity == 5"),
  "Fishing pipeline must deterministically probe every catch rarity");
assert(fishingResolverSource.includes("progressionIndexes.Count == 0"),
  "Fishing conditions must tolerate observations with no trustworthy progression evidence");
assert(fishingResolverSource.includes("hasSyntheticModEnvironment")
  && fishingResolverSource.includes("? -1"),
  "Synthetic ModBiome fishing observations must not invent progression evidence");
assert(fishingResolverSource.includes("ModContent.GetContent<ModBiome>()"),
  "ModBiome scenarios are missing from the fishing pipeline");
assert(fishingResolverSource.includes("ModContent.GetContent<ModWaterStyle>()"),
  "ModWaterStyle scenarios are missing from the fishing pipeline");
assert(!fishingResolverSource.includes("FishingPoleCondition")
  && !fishingResolverSource.includes("FishingBaitCondition"),
"Observed random catches must not be presented as mandatory pole or bait requirements");
const townNpcResolverSource = fs.readFileSync(
  path.join(root, "Data", "Resolvers", "JournalTownNpcAvailabilityResolver.cs"),
  "utf8");
assert(townNpcResolverSource.includes("\"UpdateTime_SpawnTownNPCs\""),
  "Town NPC availability does not invoke Terraria's runtime calculation");
assert(townNpcResolverSource.includes("Main.townNPCCanSpawn"),
  "Town NPC availability does not read Terraria's calculated result");
assert(townNpcResolverSource.includes("WorldGen.prioritizedTownNPCType"),
  "Town NPC priority state is not isolated and restored");
assert(townNpcResolverSource.includes("NPCShopDatabase.AllShops"),
  "Town NPC availability is not linked to NPCShopDatabase");
assert(townNpcResolverSource.includes("shop.FillShop(items, npc)"),
  "Shop item conditions are not executed through FillShop");
assert(townNpcResolverSource.includes("CreateCompletedBestiaryTracker"),
  "Bestiary-gated town NPC scenarios are missing");
assert(townNpcResolverSource.includes("BirthdayParty.GenuineParty"),
  "Event-gated town NPC scenarios are missing");
assert(townNpcResolverSource.includes("ModContent.GetContent<ModSystem>()")
  && townNpcResolverSource.includes("system.PreUpdateWorld()")
  && townNpcResolverSource.includes("CaptureStaticFieldState"),
"Custom travelling town NPC systems are not runtime-probed or isolated");
const snapshotExporterClassificationSource = fs.readFileSync(
  path.join(root, "Commands", "ExportProgressionSnapshotCommand.cs"),
  "utf8");
const vanillaClassificationMethod = snapshotExporterClassificationSource.slice(
  snapshotExporterClassificationSource.indexOf("CreateVanillaItemClassifications"),
  snapshotExporterClassificationSource.indexOf("private static bool StatModifierEquals"));
assert(!vanillaClassificationMethod.includes("entry.Evaluations"),
  "Vanilla classifications must not depend on the previously generated profile");
const itemSourceResolverSource = fs.readFileSync(
  path.join(root, "Data", "Resolvers", "JournalItemSourceResolver.cs"),
  "utf8");
const containerLootCatalogSource = fs.readFileSync(
  path.join(root, "Data", "Resolvers", "JournalContainerLootCatalog.cs"),
  "utf8");
assert(containerLootCatalogSource.includes("JournalContainerLootCatalog")
  && containerLootCatalogSource.includes("AddVanilla")
  && containerLootCatalogSource.includes("AddCalamity")
  && containerLootCatalogSource.includes("AddThorium")
  && containerLootCatalogSource.includes("AddFargo")
  && containerLootCatalogSource.includes("Provenance")
  && containerLootCatalogSource.includes("\"Terraria/ShadowChest\"")
  && containerLootCatalogSource.includes("\"Terraria/DarkLance\"")
  && containerLootCatalogSource.includes("\"CalamityMod/AbyssTreasureChest\"")
  && containerLootCatalogSource.includes("\"CalamityMod/RustyChest\"")
  && containerLootCatalogSource.includes("\"CalamityMod/EffigyOfDecay\"")
  && containerLootCatalogSource.includes("\"CalamityMod/RustyBeaconPrototype\""),
  "Container loot must come from a strict multi-mod catalog instead of a generated-world sample");
assert(!itemSourceResolverSource.includes("JournalGeneratedContainerSourceSystem")
  && !itemSourceResolverSource.includes("Main.chest"),
  "Journal UI item sources must not fall back to scanning the current world's chests");
for (const heavyResolver of [
  "JournalFishingSourceResolver.",
  "JournalTownNpcAvailabilityResolver.",
  "JournalNpcSpawnAvailabilityResolver."
]) {
  assert(!itemSourceResolverSource.includes(heavyResolver),
    `Journal UI must not lazily execute ${heavyResolver}`);
}
assert(fishingResolverSource.includes("JournalRuntimeProgressionScenarios")
  && townNpcResolverSource.includes("JournalRuntimeProgressionScenarios"),
"Fishing and town NPC probes must share the progression scenario model");
const npcSpawnResolverSource = fs.readFileSync(
  path.join(root, "Data", "Resolvers", "JournalNpcSpawnAvailabilityResolver.cs"),
  "utf8");
assert(npcSpawnResolverSource.includes("modNpc.SpawnChance(spawnInfo)"),
  "Enemy availability does not execute ModNPC.SpawnChance");
assert(npcSpawnResolverSource.includes("ModContent.GetContent<ModNPC>()"),
  "Enemy availability must enumerate ModNPC through the public content API");
assert(npcSpawnResolverSource.includes("globalNpc.EditSpawnPool(pool, spawnInfo)"),
  "Enemy availability does not execute GlobalNPC.EditSpawnPool");
assert(npcSpawnResolverSource.includes("NPCLoader.EditSpawnRate"),
  "Enemy availability does not execute GlobalNPC.EditSpawnRate");
assert(npcSpawnResolverSource.includes("NPCLoader.ChooseSpawn(spawnInfo)"),
  "Enemy availability does not execute the final modded spawn selector");
assert(npcSpawnResolverSource.includes("NPC.SpawnNPC()"),
  "Enemy availability does not observe the full vanilla spawn pipeline");
assert(npcSpawnResolverSource.includes("DefaultSpawnRateField?.SetValue(null, 1)")
  && npcSpawnResolverSource.includes("FocusedFullSpawnSeedCount"),
"Vanilla spawn probing must bypass the spawn-timer roll and sample the real selector");
assert(npcSpawnResolverSource.includes("class SpawnArena")
  && npcSpawnResolverSource.includes("spawnArena.Restore()"),
"Vanilla spawn probing must provide and restore valid temporary tile geometry");
assert(npcSpawnResolverSource.includes("GetEnvironmentDepths")
  && npcSpawnResolverSource.includes("VanillaProgressionStages"),
"Vanilla spawn probing must combine biome depths and progression changes");
assert(!npcSpawnResolverSource.includes("NpcSpawnAvailabilityUnknown"),
  "Technical unknown diagnostics must not be shown in the player UI");
assert(npcSpawnResolverSource.includes("ModContent.GetContent<ModBiome>()"),
  "ModBiome scenarios are missing from enemy availability");
assert(npcSpawnResolverSource.includes("DD2Event.Ongoing")
  && npcSpawnResolverSource.includes("Main.invasionType")
  && npcSpawnResolverSource.includes("Main.pumpkinMoon"),
"Event scenarios are incomplete in enemy availability");
assert(npcSpawnResolverSource.includes("CreateCustomEventFlags")
  && npcSpawnResolverSource.includes("field.Name.Contains(\"Ongoing\""),
"Mod event flags are not included in enemy availability scenarios");
assert(npcSpawnResolverSource.includes("static () => false"),
  "Unstructured mod event flags must remain unknown instead of inventing an early stage");
assert(npcSpawnResolverSource.includes("JournalRuntimeProgressionScenarios"),
  "Enemy availability does not use the shared progression scenarios");
assert(npcSpawnResolverSource.includes(
  "catalog.Environments[context.EnvironmentIndex].ModBiome is null"),
"Synthetic ModBiome flags must not be treated as proof of progression stage");
assert(npcSpawnResolverSource.includes("RecordFailure(catalog")
  && npcSpawnResolverSource.includes("JournalNpcSpawnProbeDiagnostics"),
"NPC probe failures must remain visible in snapshot diagnostics");
assert(npcSpawnResolverSource.includes("PositiveSpawnChanceTypes")
  && npcSpawnResolverSource.includes("ChosenSpawnTypes")
  && npcSpawnResolverSource.includes("FullSpawnTypes")
  && npcSpawnResolverSource.includes("SpawnRateBlockedContextCount"),
"NPC probe stages must expose measurable runtime diagnostics");
assert(npcSpawnResolverSource.indexOf("ObserveExactSpawnPool(catalog, spawnInfo, context);")
  < npcSpawnResolverSource.indexOf("NPCLoader.EditSpawnRate(player, ref spawnRate, ref maxSpawns);"),
"Per-NPC spawn APIs must run before the global spawn-rate gate");
const progressionScenarioSource = fs.readFileSync(
  path.join(root, "Data", "Resolvers", "JournalRuntimeProgressionScenarios.cs"),
  "utf8");
assert(progressionScenarioSource.includes("\"vanilla:downedMechBossAny\"")
  && progressionScenarioSource.includes("ApplyDerivedVanillaFlags"),
"Derived Terraria progression flags are not reproduced by runtime scenarios");
assert(progressionScenarioSource.includes("AddIsolationAccessors")
  && progressionScenarioSource.includes("IsProgressionFlagName"),
"Runtime scenarios do not isolate progression flags from the current world");
assert(progressionScenarioSource.includes("BuildNpcKillCountAccessors")
  && progressionScenarioSource.includes("SetKillCountDirectly")
  && progressionScenarioSource.includes('case "npc"'),
"Runtime scenarios do not reproduce NPC-backed progression conditions");
const snapshotExporterSource = fs.readFileSync(
  path.join(root, "Commands", "ExportProgressionSnapshotCommand.cs"),
  "utf8");
assert(snapshotExporterSource.includes("Fishing = CreateFishing(itemIds, npcIds)"),
  "Runtime fishing observations are not exported to snapshot.json");
assert(snapshotExporterSource.includes("var npcAvailability = CreateNpcAvailability(npcIds)")
  && snapshotExporterSource.includes("NpcAvailability = npcAvailability")
  && snapshotExporterSource.includes("NpcSpawnProbe = new SnapshotNpcSpawnProbe"),
  "Runtime NPC availability is not exported to snapshot.json");
assert(snapshotExporterSource.includes("includeGlobalDrops: false")
  && snapshotExporterSource.includes("\"Terraria/GlobalNPCDrops\""),
"Global NPC drops must be exported once instead of being duplicated for every NPC");
assert(snapshotExporterSource.includes("public int Version { get; set; } = 4"),
  "The snapshot schema version was not advanced for runtime availability");
assert(snapshotExporterSource.includes("TryGetShopStage"),
  "Observed shop stages are not exported to snapshot.json");
assert(snapshotExporterSource.includes("JournalFishingSourceResolver.GetItemAvailability")
  && snapshotExporterSource.includes("JournalTownNpcAvailabilityResolver.GetAvailability")
  && snapshotExporterSource.includes("JournalNpcSpawnAvailabilityResolver.GetAvailability"),
"Heavy availability probes must remain confined to snapshot export");
assert(snapshotExporterSource.includes("PlayerLoaderSetupPlayerMethod?.Invoke")
  && snapshotExporterSource.includes("PlayerLoader.PostUpdateMiscEffects(player)"),
"Item class-effect probing must initialize ModPlayers and execute delayed equipment effects");
assert(snapshotExporterSource.includes("DamageClassLoader.DamageClassCount"),
  "Modded damage classes are not fully enumerated for combat classification");
assert(snapshotExporterSource.includes("VanillaItemClassifications = CreateVanillaItemClassifications()"),
  "Vanilla combat item classifications are not exported for profile filtering");
assert(!vanillaClassificationMethod.includes("entry.Evaluations"),
  "Vanilla combat classification must remain independent from recommendation tiers");
const profileGeneratorSource = fs.readFileSync(
  path.join(root, "Tools", "ProfileGeneratorCore.mjs"),
  "utf8");
assert(profileGeneratorSource.includes("snapshot.npcAvailability")
  && profileGeneratorSource.includes("snapshot.fishing"),
"ProfileGeneratorCore does not consume runtime availability observations");
assert(profileGeneratorSource.includes("drop.sourceType === \"global\"")
  && profileGeneratorSource.includes("globalDrops"),
"ProfileGeneratorCore does not process global NPC drops independently");
assert(profileGeneratorSource.includes("snapshot.vanillaItemClassifications")
  && profileGeneratorSource.includes("context.vanillaItems?.get(item.id)"),
"ProfileGeneratorCore does not reuse the curated vanilla combat classification");
assert(profileGeneratorSource.includes("summonmeleespeeddamageclass"),
  "Whips must resolve exclusively to summoner instead of substring-matching melee");
assert(profileGeneratorSource.includes("createWikiClassificationMap"),
  "Available mod accessories need recommendation metadata as a classification fallback");
assert(profileGeneratorSource.includes("conditionDependencyIds")
  && profileGeneratorSource.includes("DownedAllMechBosses")
  && profileGeneratorSource.includes("isDefaultExcludedVariantCondition")
  && profileGeneratorSource.includes("isSafeOpaqueDropCondition")
  && profileGeneratorSource.includes("EmpressOfLightIsGenuinelyEnraged"),
"ProfileGeneratorCore must normalize progression, dependency, variant, challenge, and safe opaque drop conditions before review fallback");
const vanillaSourceCatalogSource = fs.readFileSync(
  path.join(root, "Tools", "VanillaSourceCatalog.mjs"),
  "utf8");
assert(!vanillaSourceCatalogSource.includes('"Terraria/Uzi",')
  && !vanillaSourceCatalogSource.includes('"Terraria/PulseBow",')
  && !vanillaSourceCatalogSource.includes('"Terraria/DeathSickle",'),
"Vanilla source catalog must not force audited weapons through stale item-stage floors");
assert(vanillaSourceCatalogSource.includes('"Terraria/AngryTrapper"')
  && vanillaSourceCatalogSource.includes('"Terraria/Mothron"')
  && vanillaSourceCatalogSource.includes('"Terraria/HallowBoss"'),
"Vanilla source catalog must expose source-level progression for audited vanilla drops");

for (const modName of expected) {
  const directory = path.join(modsRoot, modName);
  for (const name of requiredFiles) {
    assert(fs.existsSync(path.join(directory, name)), `${modName}/${name} is missing`);
  }
  const support = readJson(path.join(directory, "support.json"));
  const snapshot = readJson(path.join(directory, "snapshot.json"));
  const profile = readJson(path.join(directory, "profile.json"));
  const review = readJson(path.join(directory, "review.json"));
  const report = readJson(path.join(directory, "report.json"));
  const supportWithVanillaSources = applyVanillaSourceCatalog(support, snapshot);
  assert.equal(support.targetMod, modName);
  assert.equal(snapshot.targetMod, modName);
  assert.equal(profile.format, "ProgressionJournalProfile");
  const stageForFlag = key => supportWithVanillaSources.stages.find(stage =>
    stage.unlock?.type === "vanilla-flag" && stage.unlock.key === key);
  assert(
    stageForFlag("downedBoss2")?.include?.includes("Terraria/MeteoriteBar"),
    `${modName}: natural Meteorite Bar availability must not disappear when a mod adds a recipe`);
  const expectsLegacySource = (source, expectedStageIndex, kind) => {
    const observed = snapshot.npcAvailability?.find(record => record.npc === source);
    if (!observed?.observed
        || observed.kind !== kind
        || observed.earliestStageIndex !== expectedStageIndex) {
      return true;
    }
    return kind === "town"
      && snapshot.shops?.some(shop => shop.npc === source && !shop.observed);
  };
  assert.equal(
    stageForFlag("hardMode")?.enemies?.includes("Terraria/VampireBat") ?? false,
    expectsLegacySource(
      "Terraria/VampireBat",
      supportWithVanillaSources.stages.indexOf(stageForFlag("hardMode")),
      "spawn"),
    `${modName}: Vampire Bat fallback does not match runtime coverage`);
  assert.equal(
    stageForFlag("downedPlantBoss")?.shops?.includes("Terraria/Cyborg") ?? false,
    expectsLegacySource(
      "Terraria/Cyborg",
      supportWithVanillaSources.stages.indexOf(stageForFlag("downedPlantBoss")),
      "town"),
    `${modName}: Cyborg fallback does not match runtime coverage`);
  assert.equal(
    supportWithVanillaSources.stages[0]?.shops?.includes("Terraria/ArmsDealer") ?? false,
    expectsLegacySource("Terraria/ArmsDealer", 0, "town"),
    `${modName}: Arms Dealer fallback does not match runtime coverage`);
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
  const generatedStageOf = itemId => {
    const [mod, item] = itemId.split("/");
    return [...profile.entries, ...(profile.combatBuffs ?? [])].find(entry =>
      (entry.itemGroups ?? []).flat().some(reference =>
        reference.mod === mod && reference.item === item))
      ?.evaluations?.[0]?.stageId
      ?? [...profile.entries, ...(profile.combatBuffs ?? [])].find(entry =>
        (entry.itemGroups ?? []).flat().some(reference =>
          reference.mod === mod && reference.item === item))
        ?.stageId;
  };
  for (const [itemId, stageId] of Object.entries({
    "Terraria/DD2BallistraTowerT1Popper": "world-evil",
    "Terraria/DD2BallistraTowerT2Popper": "destroyer",
    "Terraria/DD2BallistraTowerT3Popper": "golem",
    "Terraria/HallowedMask": "destroyer",
    "Terraria/LuckPotion": "start",
    "Terraria/Megaphone": "wall-of-flesh"
  })) {
    assert.equal(generatedStageOf(itemId), stageId,
      `${modName}: ${itemId} must follow the correct Old One's Army tier`);
  }
  assert.equal(
    generatedStageOf("Terraria/FireGauntlet"),
    modName === "CalamityMod" ? "golem" : "skeletron-prime",
    `${modName}: Fire Gauntlet must follow its actual recipe dependencies`);
  if (modName === "CalamityMod") {
    for (const [itemId, stageId] of Object.entries({
      "Terraria/Uzi": "wall-of-flesh",
      "Terraria/DeathSickle": "skeletron-prime",
      "Terraria/PulseBow": "skeletron-prime"
    })) {
      assert.equal(generatedStageOf(itemId), stageId,
        `${modName}: ${itemId} must follow the earliest proven availability path`);
    }
    const unresolvedSignatures = (report.generation?.unresolvedConditions ?? [])
      .map(record => `${record.condition?.type ?? ""} ${record.condition?.description ?? ""}`);
    for (const token of [
      "IsHardmode",
      "DownedPlantera",
      "DownedAllMechBosses",
      "MechdusaKill"
    ]) {
      assert(!unresolvedSignatures.some(signature => signature.includes(token)),
        `${modName}: ${token} must be normalized automatically`);
    }
  }
  assert(!review.issues.some(issue =>
    issue.kind === "ambiguous-classes"
    && ["Terraria/BlandWhip", "Terraria/MaceWhip", "Terraria/ScytheWhip"]
      .includes(issue.item)),
  `${modName}: whips must not be classified as melee/summoner hybrids`);
  assert(!(report.wikiMissingItems ?? []).some(item =>
    item.reason === "recommendation has no proven availability"
    && report.paths?.[item.id]),
  `${modName}: an acquired recommendation was lost from the generated profile`);
  for (const [itemId, expectedClasses] of Object.entries({
    "Terraria/TitaniumHelmet": ["ranged"],
    "Terraria/TitaniumMask": ["melee"],
    "Terraria/TitaniumHeadgear": ["magic"]
  })) {
    const [mod, item] = itemId.split("/");
    const entry = profile.entries.find(value =>
      (value.itemGroups ?? []).flat().some(reference =>
        reference.mod === mod && reference.item === item));
    assert.deepEqual(entry?.classes, expectedClasses,
      `${modName}: ${itemId} has an incorrect class assignment`);
  }
  if (modName === "CalamityMod") {
    const agentRules = readJson(path.join(directory, "agent-rules.json"));
    const report = readJson(path.join(directory, "report.json"));
    const migration = report.agentRules?.availabilityMigration;
    const seaSpiritObserved = snapshot.fishing?.some(record =>
      record.targetType === "item"
      && record.target === "CalamityMod/SeaSpiritAmulet");
    if (seaSpiritObserved) {
      assert(!agentRules.rules.some(rule =>
        rule.kind === "fishing-source"
        && (rule.item === "CalamityMod/SeaSpiritAmulet"
            || rule.items?.includes("CalamityMod/SeaSpiritAmulet"))),
      "Observed Sea Spirit Amulet fishing must not retain a manual fishing source");
    }
    const sparklingEmpressObserved = snapshot.fishing?.some(record =>
      record.targetType === "item"
      && record.target === "CalamityMod/SparklingEmpress");
    if (sparklingEmpressObserved) {
      const [mod, item] = "CalamityMod/SparklingEmpress".split("/");
      const entry = profile.entries.find(value =>
        (value.itemGroups ?? []).flat().some(reference =>
          reference.mod === mod && reference.item === item));
      assert((entry?.fishingSources ?? []).length > 0,
        "Sparkling Empress must retain its runtime fishing source when it is present in the snapshot");
    }
    assert(migration?.pendingRules.some(rule =>
      rule.id === "shady-salesman-source"),
    "Unobserved Shady Salesman availability must retain its manual fallback");
    const profileItems = new Set(profile.entries.flatMap(entry =>
      (entry.itemGroups ?? []).flat().map(item => `${item.mod}/${item.item}`)));
    const allProfileItems = new Set(
      [...profile.entries, ...(profile.combatBuffs ?? [])].flatMap(entry =>
        (entry.itemGroups ?? []).flat().map(item => `${item.mod}/${item.item}`)));
    const explicitlyExcluded = new Set(
      agentRules.rules
        .filter(rule => rule.kind === "item-override" && rule.override?.exclude)
        .flatMap(rule => rule.items ?? (rule.item ? [rule.item] : [])));
    for (const item of snapshot.items.filter(item =>
      item.id.startsWith("CalamityMod/")
      && report.generation?.paths?.[item.id]
      && !explicitlyExcluded.has(item.id)
      && ((item.accessory
              && !item.vanity
              && item.createTile < 0
              && item.sourceNamespace?.split(".").includes("Accessories"))
          || (item.consumable
              && item.maxStack === 1
              && item.sourceNamespace?.split(".").includes("PermanentBoosters"))))) {
      assert(allProfileItems.has(item.id),
        `${item.id} is acquired combat equipment but was excluded from the profile`);
    }
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
    for (const itemId of ["CalamityMod/CoinofDeceit", "CalamityMod/ScuttlersJewel"]) {
      const [mod, item] = itemId.split("/");
      const entry = profile.entries.find(value =>
        (value.itemGroups ?? []).flat().some(reference =>
          reference.mod === mod && reference.item === item));
      assert(entry?.classes.includes("rogue"),
        `${itemId} must remain available to rogue recommendations`);
    }
    const inkBomb = profile.entries.find(value =>
      (value.itemGroups ?? []).flat().some(reference =>
        reference.mod === "CalamityMod" && reference.item === "InkBomb"));
    assert.equal(inkBomb?.evaluations?.[0]?.stageId, "skeletron",
      "Ink Bomb must remain visible at its proven post-Skeletron source stage");
    assert.deepEqual(inkBomb?.classes, ["rogue"],
      "Ink Bomb must remain a rogue accessory");
    const stageOf = itemId => {
      const [mod, item] = itemId.split("/");
      return profile.entries.find(entry =>
        (entry.itemGroups ?? []).flat().some(reference =>
          reference.mod === mod && reference.item === item))
        ?.evaluations?.[0]?.stageId;
    };
    for (const [itemId, stageId] of Object.entries({
      "CalamityMod/LuxorsGift": "start",
      "CalamityMod/ContaminatedBile": "start",
      "CalamityMod/GacruxianMollusk": "wall-of-flesh",
      "CalamityMod/SeaSpiritAmulet": "desert-scourge",
      "CalamityMod/DepthCharm": "skeletron",
      "CalamityMod/AnechoicPlating": "skeletron",
      "CalamityMod/IronBoots": "skeletron",
      "CalamityMod/BlackAnurian": "skeletron",
      "CalamityMod/AeroStone": "wall-of-flesh",
      "CalamityMod/ElectrolyteGelPack": "slime-god",
      "CalamityMod/StarlightFuelCell": "astrum-aureus",
      "CalamityMod/PhantomHeart": "polterghast"
    })) {
      const [mod, item] = itemId.split("/");
      const entry = [...profile.entries, ...(profile.combatBuffs ?? [])].find(value =>
        (value.itemGroups ?? []).flat().some(reference =>
          reference.mod === mod && reference.item === item));
      assert.equal(entry?.evaluations?.[0]?.stageId ?? entry?.stageId, stageId,
        `${itemId} has an incorrect availability stage`);
    }
    for (const [itemId, stageId] of Object.entries({
      "Terraria/Katana": "king-slime",
      "Terraria/Trimarang": "desert-scourge"
    })) {
      assert.equal(stageOf(itemId), stageId,
        `${itemId} must follow its earliest available source`);
    }
    assert.equal(report.generation?.paths?.["Terraria/Shroomerang"]?.stage, "start",
      "Shroomerang must be available from pre-Hardmode Mushroom Chests");
    const gacruxianMollusk = profile.entries.find(entry =>
      (entry.itemGroups ?? []).flat().some(reference =>
        reference.mod === "CalamityMod" && reference.item === "GacruxianMollusk"));
    assert(gacruxianMollusk?.fishingSources?.some(source =>
      source.conditions?.some(condition =>
        condition["en-US"] === "Legendary catch in the Astral Infection")),
    "Gacruxian Mollusk must display its Astral legendary fishing source");
    for (const [itemId, stageId] of Object.entries({
      "CalamityMod/Swordsplosion": "moon-lord",
      "CalamityMod/BlunderBooster": "moon-lord",
      "CalamityMod/MadAlchemistsCocktailGlove": "moon-lord",
      "CalamityMod/OrnateShield": "destroyer",
      "CalamityMod/DaedalusHeadMelee": "destroyer"
    })) {
      assert.equal(stageOf(itemId), stageId,
        `${itemId} must include alternate sources and required crafting stations`);
    }
    for (const itemId of ["Terraria/WormScarf", "Terraria/BrainOfConfusion"]) {
      const entry = profile.entries.find(value =>
        (value.itemGroups ?? []).flat().some(reference =>
          reference.mod === "Terraria" && reference.item === itemId.slice(9)));
      assert(entry?.classes.includes("rogue"),
        `${itemId} is a generic accessory and must remain available to rogue`);
    }
    for (const [item, stageId] of Object.entries({
      "Terraria/Blindfold": "start",
      "CalamityMod/OpalStriker": "world-evil",
      "CalamityMod/Prismalline": "wall-of-flesh",
      "CalamityMod/PlagueReaperMask": "golem",
      "CalamityMod/OntologicalDespoiler": "ceaseless-void",
      "CalamityMod/Supernova": "exo-mechs"
    })) {
      assert.equal(stageOf(item), stageId, `${item} must follow its latest recipe dependency`);
    }
    for (const utilityItem of [
      "Terraria/LuckyCoin",
      "Terraria/DiscountCard",
      "Terraria/MechanicalLens"
    ]) {
      assert.equal(stageOf(utilityItem), undefined,
        `${utilityItem} has no current combat effect and must remain excluded`);
    }
    const heartOfDarkness = profile.entries.find(entry =>
      (entry.itemGroups ?? []).flat().some(reference =>
        reference.mod === "CalamityMod" && reference.item === "HeartofDarkness"));
    assert.equal(heartOfDarkness?.evaluations?.[0]?.stageId, "king-slime",
      "Heart of Darkness must remain available from the earliest Revengeance boss bag");
    assert.deepEqual(heartOfDarkness?.classes,
      ["melee", "ranged", "magic", "summoner", "rogue"],
      "Heart of Darkness rage generation benefits every combat class");
    assert.equal(
      profile.combatBuffs.find(entry =>
        (entry.itemGroups ?? []).flat().some(reference =>
          reference.mod === "Terraria" && reference.item === "RestorationPotion"))
        ?.stageId,
      "start",
      "Restoration Potion must be available from naturally growing Glowing Mushrooms");
    assert(snapshot.fishing?.some(record =>
      record.targetType === "item"
      && record.target === "CalamityMod/SeaSpiritAmulet"),
    "Sea Spirit Amulet fishing must remain present in the runtime snapshot");
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

const aaDirectory = path.join(modsRoot, "AAModClassic");
for (const name of requiredFiles) {
  assert(fs.existsSync(path.join(aaDirectory, name)), `AAModClassic/${name} is missing`);
}
const aaSupport = readJson(path.join(aaDirectory, "support.json"));
const aaSnapshot = readJson(path.join(aaDirectory, "snapshot.json"));
const aaRecommendations = readJson(path.join(aaDirectory, "recommendations.json"));
const aaProfile = readJson(path.join(aaDirectory, "profile.json"));
const aaReport = readJson(path.join(aaDirectory, "report.json"));
assert.equal(aaSupport.targetMod, "AAModClassic");
assert.equal(aaSnapshot.targetMod, "AAModClassic");
assert.equal(aaSnapshot.profileId, aaSupport.id);
assert.equal(aaProfile.id, aaSupport.id);
assert(aaSupport.stages.length >= 40, "AAModClassic progression stages are incomplete");
assert(aaRecommendations.entries.length >= 1000,
  "AAModClassic class-setup recommendations are incomplete");
assert.equal(aaReport.audit.errors.length, 0);
assert.equal(aaReport.review.total, 0, "AAModClassic manual review is not resolved");
assert.equal(aaReport.ready, true, "AAModClassic profile is not ready");
assert.equal(aaReport.audit.sourceCoverage.uncovered
  .filter(entry => entry.source.startsWith("AAModClassic/"))
  .length, 0, "AAModClassic has uncovered mod NPC or shop sources");
const aaStage = id => aaSupport.stages.find(stage => stage.id === id);
assert.equal(aaStage("grips-of-chaos")?.unlock?.key, "downedGrips");
assert.equal(aaStage("equinox-worms")?.unlock?.key, "downedEquinox");
assert.equal(aaStage("sisters-of-discord")?.unlock?.key, "downedSisters");
assert(aaStage("akuma")?.dropSources?.includes("AAModClassic/AkumaAHead"));
assert(aaStage("yamata")?.dropSources?.includes("AAModClassic/YamataABody"));
assert(aaStage("zero")?.dropSources?.includes("AAModClassic/ZeroA"));
assert(aaStage("shen-doragon")?.dropSources?.includes("AAModClassic/ShenDoragonA"));

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
const migration = applyConfirmedAvailabilityChecks(
  {
    fishing: [{
      targetType: "item",
      target: "Test/Amulet",
      earliestStageIndex: 1
    }],
    npcAvailability: [
      { npc: "Test/EnemyA", observed: true, earliestStageIndex: 1 },
      { npc: "Test/EnemyB", observed: true, earliestStageIndex: 1 }
    ]
  },
  testSupport,
  normalized.assignments,
  normalized.availabilityChecks);
assert.equal(migration.confirmed, 3);
assert.equal(migration.pending, 0);
assert.equal(normalized.assignments.sourceStages["Test/EnemyA"], undefined);
assert.equal(normalized.assignments.sourceStages["Test/EnemyB"], undefined);
assert.equal(normalized.assignments.fishingSources["Test/Amulet"], undefined);
const mismatchedMigration = applyConfirmedAvailabilityChecks(
  {
    fishing: [],
    npcAvailability: [
      { npc: "Test/EnemyA", observed: true, earliestStageIndex: 0 }
    ]
  },
  testSupport,
  { sourceStages: { "Test/EnemyA": "boss" }, fishingSources: {} },
  [{
    id: "wrong-stage",
    kind: "npc-source",
    target: "Test/EnemyA",
    expectedStageId: "boss"
  }]);
assert.equal(mismatchedMigration.confirmed, 0);
assert.equal(mismatchedMigration.mismatched, 1);
const runtimeCoverage = auditRuntimeSourceCoverage(
  {
    items: [{ id: "Test/Blade" }],
    npcs: [{ id: "Test/Enemy" }, { id: "Test/Merchant" }],
    drops: [{
      sourceType: "npc",
      source: "Test/Enemy",
      item: "Test/Blade"
    }],
    shops: [{
      npc: "Test/Merchant",
      item: "Test/Blade"
    }],
    fishing: [],
    npcAvailability: [
      {
        npc: "Test/Enemy",
        kind: "spawn",
        observed: true,
        earliestStageIndex: 0
      },
      {
        npc: "Test/Merchant",
        kind: "town",
        observed: false,
        earliestStageIndex: -1
      }
    ]
  },
  {
    stages: [{
      id: "start",
      shops: ["Test/Merchant"]
    }]
  });
assert.deepEqual(runtimeCoverage.errors, []);
assert.equal(runtimeCoverage.observedSpawnCount, 1);
assert.equal(runtimeCoverage.spawnCount, 1);
assert.equal(runtimeCoverage.observed[0]?.source, "Test/Enemy");
assert.equal(runtimeCoverage.declared[0]?.source, "Test/Merchant");
assert.deepEqual(runtimeCoverage.uncovered, []);
const emptyRuntimeCoverage = auditRuntimeSourceCoverage(
  {
    items: [],
    npcs: [{ id: "Test/Enemy" }],
    drops: [],
    shops: [],
    fishing: [],
    npcAvailability: [{
      npc: "Test/Enemy",
      kind: "spawn",
      observed: false,
      earliestStageIndex: -1
    }]
  },
  { stages: [{ id: "start" }] });
assert(emptyRuntimeCoverage.errors.some(error =>
  error.includes("ordinary NPC runtime probe produced 0 observations")));
const invalid = normalizeAgentRules({
  ...validRule,
  rules: [{ kind: "item-stage", item: "Test/Sword", stageId: "boss" }]
}, testSupport);
assert(invalid.problems.some(problem => problem.includes("sourceUrl")));
assert(invalid.problems.some(problem => problem.includes("checkedAt")));

const vanillaSourceSupport = {
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
};
const vanillaSources = applyVanillaSourceCatalog(vanillaSourceSupport, {
  recipes: [{
    result: "Terraria/FireGauntlet",
    ingredients: [{ item: "Test/LateMaterial", stack: 1 }],
    stations: [],
    conditions: []
  }],
  npcAvailability: [
    ["Terraria/Skeleton", "spawn", 0],
    ["Terraria/Harpy", "spawn", 0],
    ["Terraria/VampireBat", "spawn", 4],
    ["Terraria/Reaper", "spawn", 5],
    ["Terraria/DD2Bartender", "town", 2],
    ["Terraria/ArmsDealer", "town", 0],
    ["Terraria/SkeletonMerchant", "town", 0],
    ["Terraria/Cyborg", "town", 5]
  ].map(([npc, kind, earliestStageIndex]) => ({
    npc,
    kind,
    observed: true,
    earliestStageIndex
  })),
  shops: []
});
assert(vanillaSources.initialStations.includes("Terraria/DemonAltar"));
assert(vanillaSources.initialStations.includes("Terraria/Hellforge"));
assert(vanillaSources.initialItems.includes("Terraria/PinkGel"));
assert(vanillaSources.initialItems.includes("Terraria/JungleRose"));
assert(vanillaSources.initialItems.includes("Terraria/GlowingMushroom"));
assert(vanillaSources.initialItems.includes("Terraria/Shroomerang"));
assert(vanillaSources.initialVisibleItems.includes("Terraria/Shroomerang"));
assert(vanillaSources.stages[0].enemies?.includes("Terraria/GiantWormHead"));
assert(vanillaSources.stages[0].shops?.includes("Terraria/TravellingMerchant"));
assert(!vanillaSources.stages[0].enemies?.includes("Terraria/Skeleton"));
assert(!vanillaSources.stages.find(stage => stage.id === "world-evil")
  .shops?.includes("Terraria/DD2Bartender"));
assert(!vanillaSources.conditionUnlocks?.some(rule =>
  rule.sourceIds?.includes("Terraria/DD2Bartender")));
assert(!vanillaSources.stages.find(stage => stage.id === "start")
  .shops?.includes("Terraria/ArmsDealer"));
assert(!vanillaSources.stages.find(stage => stage.id === "start")
  .shops?.includes("Terraria/SkeletonMerchant"));
assert(!vanillaSources.stages.find(stage => stage.id === "start")
  .enemies?.includes("Terraria/Harpy"));
assert(vanillaSources.stages.find(stage => stage.id === "goblin-stage")
  .include.includes("Terraria/TinkerersWorkshop"));
assert(vanillaSources.stages.find(stage => stage.id === "dungeon")
  .stations.includes("Terraria/AlchemyTable"));
assert(vanillaSources.stages.find(stage => stage.id === "hardmode")
  .include.includes("Terraria/AdamantiteForge"));
assert(!vanillaSources.stages.find(stage => stage.id === "hardmode")
  .enemies?.includes("Terraria/VampireBat"));
assert(!vanillaSources.stages.find(stage => stage.id === "plantera")
  .enemies?.includes("Terraria/Reaper"));
assert(!vanillaSources.stages.find(stage => stage.id === "plantera")
  .shops?.includes("Terraria/Cyborg"));
assert(!vanillaSources.stages.find(stage => stage.id === "destroyer")
  ?.include?.includes("Terraria/FireGauntlet"));
assert.equal(vanillaSources.itemStageFloors["Terraria/FireGauntlet"], undefined);
assert(vanillaSources.stages.find(stage => stage.id === "world-evil")
  .include.includes("Terraria/MeteoriteBar"));
assert.equal(vanillaSources.itemStageFloors["Terraria/MeteoriteBar"], "world-evil");
assert.equal(vanillaSources.stationStageFloors["Terraria/AlchemyTable"], "dungeon");

const prefixedConditionSnapshot = {
  format: "ProgressionJournalSnapshot",
  version: 4,
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
  }],
  fishing: [],
  npcAvailability: [{
    npc: "Terraria/Harpy",
    kind: "spawn",
    observed: true,
    earliestStageIndex: 1,
    earliestStageName: "Eye",
    conditions: []
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

const conditionAlgebraItem = (id, values = {}) => ({
  id,
  name: values.name ?? id.split("/").pop(),
  damageClass: values.damageClass ?? "Generic",
  damage: values.damage ?? 0,
  defense: 0,
  headSlot: -1,
  bodySlot: -1,
  legSlot: -1,
  accessory: false,
  vanity: false,
  ammo: values.ammo ?? 0,
  useAmmo: values.useAmmo ?? 0,
  buffType: values.buffType ?? 0,
  buffTime: 0,
  consumable: values.consumable ?? false,
  potion: false,
  healLife: 0,
  healMana: 0,
  food: values.food ?? false,
  flask: false,
  maxStack: values.maxStack ?? 1,
  createTile: -1,
  placedTile: "",
  createWall: -1,
  pick: 0,
  axe: 0,
  hammer: 0,
  mountType: -1,
  shoot: values.shoot ?? 0,
  sentry: false,
  sourceNamespace: values.sourceNamespace ?? "",
  classEffects: values.classEffects ?? []
});
const conditionAlgebraSnapshot = {
  format: "ProgressionJournalSnapshot",
  version: 4,
  items: [
    conditionAlgebraItem("Test/HardmodeWeapon", { damageClass: "Magic", damage: 20, shoot: 1 }),
    conditionAlgebraItem("Test/AllMechWeapon", { damageClass: "Melee", damage: 30, shoot: 1 }),
    conditionAlgebraItem("Test/NailGun", { name: "Гвоздемет", damageClass: "Ranged", damage: 40, shoot: 1 }),
    conditionAlgebraItem("Test/Nail", { name: "Гвоздь", damageClass: "Ranged", ammo: 1 }),
    conditionAlgebraItem("Test/HybridBlade", { damageClass: "MeleeRangedDamageClass", damage: 50, shoot: 1 }),
    conditionAlgebraItem("Test/ModeWeapon", { damageClass: "Magic", damage: 15, shoot: 1 }),
    conditionAlgebraItem("Test/NeverWeapon", { damageClass: "Ranged", damage: 15, shoot: 1 }),
    conditionAlgebraItem("Test/Food", { name: "Food", food: true, consumable: true, maxStack: 30 })
  ],
  npcs: [
    { id: "Test/Enemy" },
    { id: "Test/PlanteraEnemy" },
    { id: "Test/Merchant" }
  ],
  recipes: [],
  shops: [{
    npc: "Test/Merchant",
    shop: "TestShop",
    item: "Test/Nail",
    observed: true,
    earliestStageIndex: 0,
    conditions: [{ type: "Terraria.Condition", description: "Когда в инвентаре находится Гвоздемет" }]
  }],
  drops: [{
    sourceType: "npc",
    source: "Test/Enemy",
    item: "Test/HardmodeWeapon",
    rate: 1,
    conditions: [{ type: "Terraria.GameContent.ItemDropRules.Conditions+IsHardmode", description: "" }]
  }, {
    sourceType: "npc",
    source: "Test/Enemy",
    item: "Test/AllMechWeapon",
    rate: 1,
    conditions: [{ type: "Terraria.GameContent.ItemDropRules.Conditions+DownedAllMechBosses", description: "" }]
  }, {
    sourceType: "npc",
    source: "Test/Enemy",
    item: "Test/ModeWeapon",
    rate: 1,
    conditions: [{ type: "FargowiltasSouls.Core.ItemDropRules.Conditions.EModeDropCondition", description: "[i:FargowiltasSouls/Masochist] Шанс выпадения в режиме Вечность" }]
  }, {
    sourceType: "npc",
    source: "Test/Enemy",
    item: "Test/NeverWeapon",
    rate: 1,
    conditions: [{ type: "Terraria.GameContent.ItemDropRules.Conditions+NeverTrue", description: null }]
  }, {
    sourceType: "npc",
    source: "Test/PlanteraEnemy",
    item: "Test/NailGun",
    rate: 1,
    conditions: []
  }],
  fishing: [],
  npcAvailability: [{
    npc: "Test/Enemy",
    kind: "spawn",
    observed: true,
    earliestStageIndex: 0,
    earliestStageName: "Start",
    conditions: []
  }, {
    npc: "Test/PlanteraEnemy",
    kind: "spawn",
    observed: true,
    earliestStageIndex: 4,
    earliestStageName: "Plantera",
    conditions: []
  }, {
    npc: "Test/Merchant",
    kind: "town",
    observed: true,
    earliestStageIndex: 0,
    earliestStageName: "Start",
    conditions: []
  }]
};
const conditionAlgebraSupport = {
  id: "test.condition-algebra",
  contentMods: ["Test"],
  classes: [
    { id: "melee", damageClassNames: ["Melee"] },
    { id: "ranged", damageClassNames: ["Ranged"] },
    { id: "magic", damageClassNames: ["Magic"] }
  ],
  stages: [
    { id: "start", unlock: { type: "always" }, enemies: ["Test/Enemy"], shops: ["Test/Merchant"], include: ["Test/HybridBlade"] },
    { id: "wall-of-flesh", unlock: { type: "vanilla-flag", key: "hardMode" } },
    { id: "destroyer", unlock: { type: "vanilla-flag", key: "downedMechBoss1" } },
    { id: "skeletron-prime", unlock: { type: "vanilla-flag", key: "downedMechBoss3" } },
    { id: "plantera", unlock: { type: "vanilla-flag", key: "downedPlantBoss" }, enemies: ["Test/PlanteraEnemy"] }
  ]
};
const conditionAlgebraResult = generateProfile(
  conditionAlgebraSnapshot,
  conditionAlgebraSupport);
assert.equal(
  generatedStageOfProfile(conditionAlgebraResult.profile, "Test/HardmodeWeapon"),
  "wall-of-flesh",
  "IsHardmode conditions must become a progression gate");
assert.equal(
  generatedStageOfProfile(conditionAlgebraResult.profile, "Test/AllMechWeapon"),
  "skeletron-prime",
  "DownedAllMechBosses conditions must become an all-mechs gate");
assert.equal(
  generatedStageOfProfile(conditionAlgebraResult.profile, "Test/Nail"),
  "plantera",
  "Inventory-gated shop items must inherit the dependency item stage");
assert.equal(
  generatedStageOfProfile(conditionAlgebraResult.profile, "Test/ModeWeapon"),
  "start",
  "Difficulty mode drop conditions must not block default availability");
assert.deepEqual(
  conditionAlgebraResult.profile.entries.find(entry =>
    (entry.itemGroups ?? []).flat().some(item => `${item.mod}/${item.item}` === "Test/HybridBlade"))?.classes,
  ["melee", "ranged"],
  "Hybrid damage classes must keep all confirmed classes");
assert(!conditionAlgebraResult.review.issues.some(issue =>
  issue.kind === "unassigned-combat-item" && issue.item === "Test/Food"),
"Food and basic buff items must not be reviewed as missing combat equipment");
assert(!conditionAlgebraResult.review.issues.some(issue =>
  issue.kind === "unassigned-combat-item" && issue.item === "Test/NeverWeapon"),
"NeverTrue-only items must not be reviewed as assignable combat equipment");

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

function generatedStageOfProfile(profile, itemId) {
  const [mod, item] = itemId.split("/");
  const entry = [...profile.entries, ...(profile.combatBuffs ?? [])].find(value =>
    (value.itemGroups ?? []).flat().some(reference =>
      reference.mod === mod && reference.item === item));
  return entry?.evaluations?.[0]?.stageId ?? entry?.stageId;
}

function modOf(reference) {
  return reference.split("/", 1)[0];
}
