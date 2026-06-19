import fs from "node:fs";
import path from "node:path";
import { pathToFileURL } from "node:url";
import {
  generateProfile,
  readJson,
  writeJson
} from "./ProfileGeneratorCore.mjs";
import { applyVanillaSourceCatalog } from "./VanillaSourceCatalog.mjs";

const root = path.resolve(import.meta.dirname, "..");
const modsRoot = path.join(root, "Profiles", "Mods");
if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  run(process.argv[2]);
}

function run(requested) {
  if (!requested || (requested.startsWith("--") && requested !== "--all")) {
    throw new Error("Usage: node Tools/BuildModProfiles.mjs <InternalModName|--all>");
  }
  const modNames = requested === "--all"
    ? fs.readdirSync(modsRoot, { withFileTypes: true })
      .filter(entry => entry.isDirectory())
      .map(entry => entry.name)
      .filter(name => fs.existsSync(path.join(modsRoot, name, "support.json")))
      .sort()
    : [requested];
  let failed = false;
  for (const modName of modNames) {
    try {
      buildModProfile(modName);
    } catch (error) {
      failed = true;
      console.error(`${modName}: ${error.message}`);
    }
  }
  if (failed) process.exitCode = 1;
}

export function buildModProfile(modName) {
  const directory = path.join(modsRoot, modName);
  const supportPath = path.join(directory, "support.json");
  if (!fs.existsSync(supportPath)) {
    throw new Error(`Missing Profiles/Mods/${modName}/support.json.`);
  }

  const support = readJson(supportPath);
  validateSupport(support, modName);
  const snapshot = readRequiredJson(directory, "snapshot.json");
  validateSnapshot(snapshot, support);
  const agentRules = readOptionalJson(
    directory,
    "agent-rules.json",
    emptyAgentRules(support.id));
  const recommendations = readOptionalJson(directory, "recommendations.json", null);
  const { assignments, availabilityChecks, evidence, problems } =
    normalizeAgentRules(agentRules, support);
  if (problems.length > 0) {
    throw new Error(`Invalid agent-rules.json:\n- ${problems.join("\n- ")}`);
  }
  const legacyAssignments = structuredClone(assignments);
  const availabilityMigration = applyConfirmedAvailabilityChecks(
    snapshot,
    support,
    assignments,
    availabilityChecks);

  const manifest = {
    ...support,
    wikiSource: support.recommendationSource ?? support.wikiSource,
    wikiStageMap: support.recommendationStageMap ?? support.wikiStageMap
  };
  const { profile, report: generationReport, review } = generateProfile(
    snapshot,
    manifest,
    recommendations,
    assignments);
  if (availabilityMigration.confirmed > 0) {
    const legacyProfile = generateProfile(
      snapshot,
      manifest,
      recommendations,
      legacyAssignments).profile;
    availabilityMigration.stageComparison =
      compareProfileStages(legacyProfile, profile);
  }
  const audit = auditProfile(
    profile,
    generationReport,
    support,
    snapshot,
    assignments);
  const report = {
    format: "ProgressionJournalModProfileReport",
    version: 1,
    targetMod: support.targetMod,
    profileId: support.id,
    generatedAtUtc: new Date().toISOString(),
    snapshot: {
      version: snapshot.version,
      generatedAtUtc: snapshot.generatedAtUtc,
      targetMod: snapshot.targetMod ?? support.targetMod,
      mods: snapshot.mods ?? []
    },
    agentRules: {
      rules: agentRules.rules?.length ?? 0,
      ignoredItems: agentRules.ignoredItems?.length ?? 0,
      ignoredIssues: agentRules.ignoredIssues?.length ?? 0,
      evidence,
      availabilityMigration
    },
    review: review.summary,
    audit,
    ready: audit.errors.length === 0 && review.summary.total === 0,
    generation: generationReport
  };

  writeJson(path.join(directory, "profile.json"), profile);
  writeJson(path.join(directory, "review.json"), review);
  writeJson(path.join(directory, "report.json"), report);

  const state = report.ready ? "READY" : "needs review";
  console.log(
    `${modName}: ${profile.entries.length} equipment, `
    + `${profile.combatBuffs.length} buffs, ${review.summary.total} review, ${state}`);
}

function validateSupport(support, directoryName) {
  assert(support.format === "ProgressionJournalModSupport", "Invalid support.json format.");
  assert(support.version === 1, `Unsupported support.json version '${support.version}'.`);
  assert(support.targetMod === directoryName,
    `support targetMod '${support.targetMod}' must match directory '${directoryName}'.`);
  assert(support.id, "support.json requires id.");
  assert(Array.isArray(support.classes) && support.classes.length > 0,
    "support.json requires classes.");
  assert(Array.isArray(support.stages) && support.stages.length > 0,
    "support.json requires an ordered stage list.");
  assert(!hasDuplicates(support.stages.map(stage => stage.id)),
    "support.json contains duplicate stage ids.");
}

function validateSnapshot(snapshot, support) {
  assert(snapshot.format === "ProgressionJournalSnapshot", "Invalid snapshot.json format.");
  assert(snapshot.version === 4,
    `Unsupported snapshot.json version '${snapshot.version}'.`);
  const target = snapshot.targetMod ?? support.targetMod;
  assert(target === support.targetMod,
    `Snapshot target '${target}' does not match '${support.targetMod}'.`);
  assert(snapshot.profileId === support.id,
    `Snapshot profile '${snapshot.profileId ?? ""}' does not match '${support.id}'.`);
  const loaded = new Map((snapshot.mods ?? []).map(mod => [mod.name, mod.version ?? ""]));
  for (const required of support.requiredMods ?? []) {
    assert(loaded.has(required.name),
      `Snapshot is missing required mod '${required.name}'.`);
    if (required.version) {
      assert(versionMatches(loaded.get(required.name), required.version),
        `Snapshot has ${required.name} ${loaded.get(required.name)}, expected ${required.version}.`);
    }
  }
}

export function normalizeAgentRules(document, support) {
  const problems = [];
  const evidence = [];
  const availabilityChecks = [];
  assert(document.format === "ProgressionJournalAgentRules",
    "Invalid agent-rules.json format.");
  assert(document.version === 1,
    `Unsupported agent-rules.json version '${document.version}'.`);
  assert(!document.profileId || document.profileId === support.id,
    `Agent rules profile '${document.profileId}' does not match '${support.id}'.`);
  const stages = new Set(support.stages.map(stage => stage.id));
  const assignments = {
    format: "ProgressionJournalManualAssignments",
    version: 1,
    profileId: support.id,
    itemStages: {},
    sourceStages: {},
    stationStages: {},
    conditionStages: [],
    itemOverrides: {},
    fishingSources: {},
    ignoredItems: [],
    ignoredIssues: []
  };

  for (const [index, rule] of (document.rules ?? []).entries()) {
    const label = rule.id || `rules[${index}]`;
    validateEvidence(rule, label, problems);
    evidence.push(pickEvidence(rule, label));
    if (rule.stageId && !stages.has(rule.stageId)) {
      problems.push(`${label}: unknown stage '${rule.stageId}'`);
      continue;
    }
    switch (rule.kind) {
      case "item-stage":
        {
          const items = rule.items ?? (rule.item ? [rule.item] : []);
          if (items.length === 0) problems.push(`${label}: item or items is required`);
          for (const item of items) {
            requireText(item, `${label}: item`, problems);
            if (item && rule.stageId) assignments.itemStages[item] = rule.stageId;
          }
        }
        break;
      case "source-stage":
        {
          const sources = rule.sources ?? (rule.source ? [rule.source] : []);
          if (sources.length === 0) problems.push(`${label}: source or sources is required`);
          for (const source of sources) {
            requireText(source, `${label}: source`, problems);
            if (source && rule.stageId) {
              assignments.sourceStages[source] = rule.stageId;
              availabilityChecks.push({
                id: label,
                kind: "npc-source",
                target: source,
                expectedStageId: rule.stageId
              });
            }
          }
        }
        break;
      case "station-stage":
        requireText(rule.station, `${label}: station`, problems);
        if (rule.station && rule.stageId) assignments.stationStages[rule.station] = rule.stageId;
        break;
      case "condition-stage":
        assignments.conditionStages.push({
          stageId: rule.stageId,
          sources: rule.sources ?? ["drop", "shop", "recipe"],
          sourceIds: rule.sourceIds ?? [],
          conditionTypes: rule.conditionTypes ?? [],
          conditionDescriptions: rule.conditionDescriptions ?? []
        });
        break;
      case "item-override":
        {
          const items = rule.items ?? (rule.item ? [rule.item] : []);
          if (items.length === 0) problems.push(`${label}: item or items is required`);
          for (const item of items) {
            requireText(item, `${label}: item`, problems);
          }
          if (!rule.override || typeof rule.override !== "object") {
            problems.push(`${label}: override object is required`);
          } else {
            for (const item of items) {
              if (item) assignments.itemOverrides[item] = rule.override;
            }
          }
        }
        break;
      case "fishing-source":
        {
          const items = rule.items ?? (rule.item ? [rule.item] : []);
          if (items.length === 0) problems.push(`${label}: item or items is required`);
          if (!Array.isArray(rule.conditions) || rule.conditions.length === 0) {
            problems.push(`${label}: conditions are required`);
          }
          for (const item of items) {
            requireText(item, `${label}: item`, problems);
            if (item && Array.isArray(rule.conditions) && rule.conditions.length > 0) {
              assignments.fishingSources[item] = [
                ...(assignments.fishingSources[item] ?? []),
                { conditions: rule.conditions }
              ];
              availabilityChecks.push({
                id: label,
                kind: "fishing",
                target: item
              });
            }
          }
        }
        break;
      default:
        problems.push(`${label}: unsupported kind '${rule.kind}'`);
    }
  }

  for (const [index, ignored] of (document.ignoredItems ?? []).entries()) {
    const label = ignored.id || `ignoredItems[${index}]`;
    validateEvidence(ignored, label, problems);
    const items = ignored.items ?? (ignored.item ? [ignored.item] : []);
    if (items.length === 0) problems.push(`${label}: item or items is required`);
    for (const item of items) {
      requireText(item, `${label}: item`, problems);
      if (item) assignments.ignoredItems.push(item);
    }
    evidence.push(pickEvidence(ignored, label));
  }
  for (const [index, ignored] of (document.ignoredIssues ?? []).entries()) {
    const label = ignored.id || `ignoredIssues[${index}]`;
    validateEvidence(ignored, label, problems);
    requireText(ignored.issueId, `${label}: issueId`, problems);
    if (ignored.issueId) assignments.ignoredIssues.push(ignored.issueId);
    evidence.push(pickEvidence(ignored, label));
  }

  return { assignments, availabilityChecks, evidence, problems };
}

export function applyConfirmedAvailabilityChecks(
  snapshot,
  support,
  assignments,
  checks) {
  const stageIds = support.stages.map(stage => stage.id);
  const npcAvailability = new Map(
    (snapshot.npcAvailability ?? []).map(record => [record.npc, record]));
  const shopsByNpc = new Map();
  for (const shop of snapshot.shops ?? []) {
    if (!shopsByNpc.has(shop.npc)) shopsByNpc.set(shop.npc, []);
    shopsByNpc.get(shop.npc).push(shop);
  }
  const fishing = new Set(
    (snapshot.fishing ?? [])
      .filter(record => record.targetType === "item" && record.earliestStageIndex >= 0)
      .map(record => record.target));
  const confirmed = [];
  const pending = [];
  const mismatched = [];

  for (const check of checks) {
    if (check.kind === "fishing") {
      if (fishing.has(check.target)) {
        delete assignments.fishingSources[check.target];
        confirmed.push(check);
      } else {
        pending.push(check);
      }
      continue;
    }

    const observation = npcAvailability.get(check.target);
    if (!observation?.observed || observation.earliestStageIndex < 0) {
      pending.push(check);
      continue;
    }
    if (observation.kind === "town"
        && (shopsByNpc.get(check.target) ?? []).some(shop => !shop.observed)) {
      pending.push(check);
      continue;
    }

    const actualStageId = stageIds[observation.earliestStageIndex] ?? "";
    if (actualStageId !== check.expectedStageId) {
      mismatched.push({ ...check, actualStageId });
      continue;
    }

    delete assignments.sourceStages[check.target];
    confirmed.push({ ...check, actualStageId });
  }

  return {
    confirmed: confirmed.length,
    pending: pending.length + mismatched.length,
    mismatched: mismatched.length,
    confirmedRules: confirmed,
    pendingRules: [...pending, ...mismatched]
  };
}

function compareProfileStages(legacyProfile, automaticProfile) {
  const collect = profile => new Map(
    [...(profile.entries ?? []), ...(profile.combatBuffs ?? [])]
      .flatMap(entry => (entry.itemGroups ?? []).flat().map(item => [
        `${item.mod}/${item.item}`,
        entry.evaluations?.[0]?.stageId ?? entry.stageId ?? ""
      ])));
  const legacyStages = collect(legacyProfile);
  const automaticStages = collect(automaticProfile);
  const moved = [];
  for (const [item, expectedStageId] of legacyStages) {
    const actualStageId = automaticStages.get(item);
    if (actualStageId !== expectedStageId) {
      moved.push({ item, expectedStageId, actualStageId: actualStageId ?? "" });
    }
  }
  if (moved.length > 0) {
    throw new Error(
      "Removing confirmed legacy availability moved generated items:\n- "
      + moved.map(value =>
        `${value.item}: '${value.expectedStageId}' -> '${value.actualStageId || "missing"}'`)
        .join("\n- "));
  }
  return { compared: legacyStages.size, moved: 0 };
}

function validateEvidence(value, label, problems) {
  requireText(value.sourceUrl, `${label}: sourceUrl`, problems);
  if (value.sourceUrl) {
    try {
      const url = new URL(value.sourceUrl);
      if (url.protocol !== "https:" && url.protocol !== "http:") {
        problems.push(`${label}: sourceUrl must use http or https`);
      }
    } catch {
      problems.push(`${label}: sourceUrl is invalid`);
    }
  }
  requireText(value.sourceVersion, `${label}: sourceVersion`, problems);
  requireText(value.reason, `${label}: reason`, problems);
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value.checkedAt ?? "")) {
    problems.push(`${label}: checkedAt must be YYYY-MM-DD`);
  }
}

function pickEvidence(value, id) {
  return {
    id,
    sourceUrl: value.sourceUrl,
    sourceVersion: value.sourceVersion,
    checkedAt: value.checkedAt,
    reason: value.reason
  };
}

function auditProfile(
  profile,
  generationReport,
  support,
  snapshot,
  manualAssignments = null) {
  const errors = [];
  const warnings = [];
  const stageIds = new Set(profile.stages.map(stage => stage.id));
  const allowedMods = new Set(["Terraria", ...(support.contentMods ?? [support.targetMod])]);
  validateLocalized(profile.name, "profile name", errors);
  if (hasDuplicates(profile.stages.map(stage => stage.id))) errors.push("duplicate stage id");
  if (hasDuplicates(profile.entries.map(entry => entry.key))) errors.push("duplicate entry key");
  for (const stage of profile.stages) {
    validateLocalized(stage.name, `stage '${stage.id}' name`, errors);
  }
  for (const entry of profile.entries) {
    for (const evaluation of entry.evaluations ?? []) {
      if (!stageIds.has(evaluation.stageId)) {
        errors.push(`entry '${entry.key}' uses unknown stage '${evaluation.stageId}'`);
      }
    }
  }
  for (const [item, acquisition] of Object.entries(generationReport.paths ?? {})) {
    const foreign = [...(acquisition.via ?? "").matchAll(
      /([A-Za-z0-9_]+)\/[A-Za-z0-9_]+/g)]
      .map(match => match[1])
      .filter(mod => !allowedMods.has(mod));
    if (foreign.length > 0) {
      errors.push(`${item} has foreign acquisition path through ${[...new Set(foreign)].join(", ")}`);
    }
  }
  if ((generationReport.staleRules ?? []).length > 0) {
    warnings.push(`${generationReport.staleRules.length} support or agent references are stale`);
  }
  if ((generationReport.wikiAvailabilityCorrections ?? []).length > 0) {
    warnings.push(
      `${generationReport.wikiAvailabilityCorrections.length} recommendations precede proven availability and were suppressed`);
  }
  const contentMods = new Set(snapshot.contentMods ?? support.contentMods ?? [support.targetMod]);
  for (const item of snapshot.items ?? []) {
    const mod = item.id.split("/", 1)[0];
    if (mod !== "Terraria" && !contentMods.has(mod)) {
      errors.push(`snapshot contains foreign item '${item.id}'`);
      break;
    }
  }
  const sourceCoverage = auditRuntimeSourceCoverage(
    snapshot,
    applyVanillaSourceCatalog(support, snapshot),
    manualAssignments);
  errors.push(...sourceCoverage.errors);
  if (sourceCoverage.uncovered.length > 0) {
    warnings.push(
      `${sourceCoverage.uncovered.length} NPC drop or shop sources have neither a runtime observation nor an assigned stage`);
  }
  return { errors, warnings, sourceCoverage };
}

export function auditRuntimeSourceCoverage(
  snapshot,
  manifest,
  manualAssignments = null) {
  const errors = [];
  const npcIds = new Set((snapshot.npcs ?? []).map(npc => npc.id));
  const itemIds = new Set((snapshot.items ?? []).map(item => item.id));
  const availability = new Map();
  for (const record of snapshot.npcAvailability ?? []) {
    if (!npcIds.has(record.npc)) {
      errors.push(`NPC availability references unknown NPC '${record.npc}'`);
    }
    if (availability.has(record.npc)) {
      errors.push(`duplicate NPC availability for '${record.npc}'`);
    }
    availability.set(record.npc, record);
  }
  for (const npc of npcIds) {
    if (!availability.has(npc)) {
      errors.push(`missing NPC availability for '${npc}'`);
    }
  }

  const declaredDrops = new Set(
    (manifest.stages ?? []).flatMap(stage => [
      ...(stage.dropSources ?? []),
      ...(stage.enemies ?? [])
    ]));
  const declaredShops = new Set(
    (manifest.stages ?? []).flatMap(stage => stage.shops ?? []));
  for (const event of manifest.events ?? []) {
    for (const source of [
      ...(event.dropSources ?? []),
      ...(event.enemies ?? [])
    ]) {
      declaredDrops.add(source);
    }
  }
  for (const source of Object.keys(manualAssignments?.sourceStages ?? {})) {
    declaredDrops.add(source);
    declaredShops.add(source);
  }

  const sourceKinds = new Map();
  const addSource = (source, kind) => {
    const kinds = sourceKinds.get(source) ?? new Set();
    kinds.add(kind);
    sourceKinds.set(source, kinds);
  };
  for (const drop of snapshot.drops ?? []) {
    if (!itemIds.has(drop.item)) {
      errors.push(`drop references unknown item '${drop.item}'`);
    }
    if (drop.sourceType === "npc") {
      if (!npcIds.has(drop.source)) {
        errors.push(`drop references unknown NPC '${drop.source}'`);
      }
      addSource(drop.source, "drop");
    } else if (drop.sourceType === "container" && !itemIds.has(drop.source)) {
      errors.push(`container drop references unknown item '${drop.source}'`);
    }
  }
  for (const shop of snapshot.shops ?? []) {
    if (!npcIds.has(shop.npc)) {
      errors.push(`shop references unknown NPC '${shop.npc}'`);
    }
    if (!itemIds.has(shop.item)) {
      errors.push(`shop references unknown item '${shop.item}'`);
    }
    addSource(shop.npc, "shop");
  }
  for (const catchRecord of snapshot.fishing ?? []) {
    const known = catchRecord.targetType === "npc"
      ? npcIds.has(catchRecord.target)
      : catchRecord.targetType === "item" && itemIds.has(catchRecord.target);
    if (!known) {
      errors.push(
        `fishing references unknown ${catchRecord.targetType} '${catchRecord.target}'`);
    }
  }

  const observed = [];
  const declared = [];
  const uncovered = [];
  for (const [source, kinds] of sourceKinds) {
    const record = availability.get(source);
    const runtimeCovered = record?.observed === true;
    const assigned = [...kinds].every(kind =>
      kind === "shop" ? declaredShops.has(source) : declaredDrops.has(source));
    const result = {
      source,
      kinds: [...kinds].sort(),
      availabilityKind: record?.kind ?? "",
      earliestStageIndex: record?.earliestStageIndex ?? -1
    };
    if (runtimeCovered) observed.push(result);
    else if (assigned) declared.push(result);
    else uncovered.push(result);
  }
  const sortSources = values => values.sort((left, right) =>
    left.source.localeCompare(right.source));
  return {
    npcCount: npcIds.size,
    availabilityCount: availability.size,
    fishingCount: (snapshot.fishing ?? []).length,
    dropSourceCount: new Set(
      (snapshot.drops ?? [])
        .filter(drop => drop.sourceType === "npc")
        .map(drop => drop.source)).size,
    shopSourceCount: new Set(
      (snapshot.shops ?? []).map(shop => shop.npc)).size,
    globalDropCount: (snapshot.drops ?? [])
      .filter(drop => drop.sourceType === "global").length,
    observed: sortSources(observed),
    declared: sortSources(declared),
    uncovered: sortSources(uncovered),
    errors: [...new Set(errors)].sort()
  };
}

function validateLocalized(value, label, errors) {
  if (!value?.["en-US"] || !value?.["ru-RU"]) {
    errors.push(`${label} requires en-US and ru-RU`);
  }
}

function readRequiredJson(directory, name) {
  const file = path.join(directory, name);
  if (!fs.existsSync(file)) throw new Error(`Missing ${name}.`);
  return readJson(file);
}

function readOptionalJson(directory, name, fallback) {
  const file = path.join(directory, name);
  return fs.existsSync(file) ? readJson(file) : fallback;
}

function emptyAgentRules(profileId) {
  return {
    format: "ProgressionJournalAgentRules",
    version: 1,
    profileId,
    rules: [],
    ignoredItems: [],
    ignoredIssues: []
  };
}

function versionMatches(actual, expected) {
  return actual === expected || actual.startsWith(`${expected}.`) || expected.startsWith(`${actual}.`);
}

function hasDuplicates(values) {
  return new Set(values.map(value => value.toLowerCase())).size !== values.length;
}

function requireText(value, label, problems) {
  if (typeof value !== "string" || value.trim().length === 0) {
    problems.push(`${label} is required`);
  }
}

function assert(condition, message) {
  if (!condition) throw new Error(message);
}
