import fs from "node:fs";
import path from "node:path";

const ROOT = path.resolve(import.meta.dirname, "..");
const PROFILES_ROOT = path.join(ROOT, "Profiles", "Mods");
const STATE_DIR = path.join(ROOT, ".profile-check");
const BASELINE_FILE = path.join(STATE_DIR, "baseline.json");
const DELTA_FILE = path.join(STATE_DIR, "last-delta.json");
const MODS = [
  "AAModClassic",
  "CalamityMod",
  "FargowiltasSouls",
  "ThoriumMod"
];
const WATCH_ITEMS = [
  "CalamityMod/BloodRune",
  "CalamityMod/IceBarrage",
  "Terraria/Cascade"
];

function fail(message) {
  console.error(`ERROR: ${message}`);
  process.exitCode = 1;
}

function readJson(file) {
  try {
    return JSON.parse(fs.readFileSync(file, "utf8"));
  } catch (error) {
    throw new Error(`Cannot read ${path.relative(ROOT, file)}: ${error.message}`);
  }
}

function writeJson(file, value) {
  fs.mkdirSync(path.dirname(file), { recursive: true });
  fs.writeFileSync(file, `${JSON.stringify(value, null, 2)}\n`, "utf8");
}

function localizedName(value) {
  if (typeof value === "string") return value;
  return value?.["en-US"] ?? value?.["ru-RU"] ?? "";
}

function itemId(reference) {
  const mod = reference?.mod ?? "";
  const item = reference?.item ?? "";
  return mod && item ? `${mod}/${item}` : "";
}

function stageData(profile) {
  const order = new Map();
  const labels = new Map();
  for (const [index, stage] of (profile.stages ?? []).entries()) {
    order.set(stage.id, index);
    labels.set(stage.id, localizedName(stage.name) || stage.id);
  }
  return { order, labels };
}

function sortedUnique(values, order) {
  return [...new Set(values.filter(Boolean))].sort((left, right) => {
    const leftIndex = order.get(left) ?? Number.MAX_SAFE_INTEGER;
    const rightIndex = order.get(right) ?? Number.MAX_SAFE_INTEGER;
    return leftIndex - rightIndex || left.localeCompare(right);
  });
}

function collectProfileItems(profile) {
  const { order, labels } = stageData(profile);
  const items = new Map();

  const add = (reference, stageIds, metadata) => {
    const id = itemId(reference);
    if (!id) return;
    const existing = items.get(id) ?? {
      id,
      displayName: reference.displayName ?? id,
      stageIds: [],
      stageLabels: [],
      categories: [],
      classes: [],
      kinds: [],
      entryKeys: []
    };
    existing.stageIds = sortedUnique([...existing.stageIds, ...stageIds], order);
    existing.stageLabels = existing.stageIds.map(stageId => labels.get(stageId) ?? stageId);
    existing.categories = [...new Set([...existing.categories, metadata.category].filter(Boolean))].sort();
    existing.classes = [...new Set([...existing.classes, ...(metadata.classes ?? [])])].sort();
    existing.kinds = [...new Set([...existing.kinds, metadata.kind].filter(Boolean))].sort();
    existing.entryKeys = [...new Set([...existing.entryKeys, metadata.entryKey].filter(Boolean))].sort();
    items.set(id, existing);
  };

  for (const entry of profile.entries ?? []) {
    const stageIds = sortedUnique(
      (entry.evaluations ?? []).map(evaluation => evaluation.stageId),
      order);
    for (const group of entry.itemGroups ?? []) {
      for (const reference of group ?? []) {
        add(reference, stageIds, {
          category: entry.category,
          classes: entry.classes,
          kind: "entry",
          entryKey: entry.key
        });
      }
    }
  }

  for (const buff of profile.combatBuffs ?? []) {
    const stageIds = sortedUnique([buff.stageId], order);
    for (const group of buff.itemGroups ?? []) {
      for (const reference of group ?? []) {
        add(reference, stageIds, {
          category: buff.category,
          classes: buff.classes,
          kind: "combatBuff",
          entryKey: buff.key
        });
      }
    }
  }

  return Object.fromEntries([...items.entries()].sort(([left], [right]) => left.localeCompare(right)));
}

function canonicalCondition(condition) {
  return {
    type: condition?.type ?? "",
    key: condition?.key ?? "",
    description: condition?.description ?? ""
  };
}

function collectReview(review, targetMod) {
  const issues = {};
  const unassignedModItems = [];
  for (const issue of review.issues ?? []) {
    const normalized = {
      id: issue.id ?? "",
      kind: issue.kind ?? "",
      item: issue.item ?? "",
      displayName: issue.displayName ?? "",
      sourceKind: issue.sourceKind ?? "",
      conditions: (issue.conditions ?? []).map(canonicalCondition),
      affected: (issue.affected ?? []).map(value => ({
        item: value.item ?? "",
        source: value.source ?? ""
      })).sort((left, right) =>
        left.item.localeCompare(right.item) || left.source.localeCompare(right.source))
    };
    const key = normalized.id || JSON.stringify(normalized);
    issues[key] = normalized;
    if (normalized.kind === "unassigned-combat-item"
        && normalized.item.startsWith(`${targetMod}/`)) {
      unassignedModItems.push(normalized.item);
    }
  }
  return {
    total: review.summary?.total ?? Object.keys(issues).length,
    byKind: review.summary?.byKind ?? {},
    issues,
    unassignedModItems: [...new Set(unassignedModItems)].sort()
  };
}

function collectReport(report) {
  const generation = report.generation ?? {};
  return {
    ready: report.ready ?? null,
    auditErrors: Array.isArray(report.audit?.errors)
      ? report.audit.errors.length
      : (report.audit?.errors ?? null),
    auditWarnings: Array.isArray(report.audit?.warnings)
      ? report.audit.warnings.length
      : (report.audit?.warnings ?? null),
    unknownReferences: generation.unknownReferences ?? null,
    ambiguousClasses: generation.ambiguousClasses ?? null,
    emptyStages: generation.emptyStages ?? null,
    staleRules: generation.staleRules ?? null,
    manualAssignmentProblems: generation.manualAssignmentProblems ?? null,
    unavailableCombatItems: generation.unavailableCombatItems ?? null,
    unresolvedAvailabilityItems: generation.unresolvedAvailabilityItems ?? null
  };
}

function captureCurrent() {
  const result = {
    format: "ProgressionJournalProfileCheck",
    version: 1,
    capturedAtUtc: new Date().toISOString(),
    mods: {}
  };

  for (const mod of MODS) {
    const directory = path.join(PROFILES_ROOT, mod);
    const profileFile = path.join(directory, "profile.json");
    const reviewFile = path.join(directory, "review.json");
    const reportFile = path.join(directory, "report.json");
    for (const file of [profileFile, reviewFile, reportFile]) {
      if (!fs.existsSync(file)) {
        throw new Error(`Missing ${path.relative(ROOT, file)}`);
      }
    }
    const profile = readJson(profileFile);
    const review = readJson(reviewFile);
    const report = readJson(reportFile);
    result.mods[mod] = {
      profileId: profile.id ?? "",
      items: collectProfileItems(profile),
      review: collectReview(review, mod),
      report: collectReport(report)
    };
  }

  return result;
}

function stageText(item) {
  const ids = item?.stageIds ?? [];
  const labels = item?.stageLabels ?? [];
  if (ids.length === 0) return "<no stage>";
  return ids.map((id, index) => {
    const label = labels[index] ?? id;
    return label && label !== id ? `${id} (${label})` : id;
  }).join(", ");
}

function itemMetadataChanged(before, after) {
  for (const key of ["categories", "classes", "kinds", "entryKeys"]) {
    if (JSON.stringify(before[key] ?? []) !== JSON.stringify(after[key] ?? [])) return true;
  }
  return false;
}

function issueText(issue) {
  if (issue.kind === "unassigned-combat-item") {
    return `${issue.item}${issue.displayName ? ` (${issue.displayName})` : ""}`;
  }
  const conditions = (issue.conditions ?? [])
    .map(condition => condition.type || condition.key || condition.description)
    .filter(Boolean)
    .join(" | ");
  const affected = (issue.affected ?? [])
    .map(value => value.item)
    .filter(Boolean);
  const shortAffected = affected.length <= 4
    ? affected.join(", ")
    : `${affected.slice(0, 4).join(", ")} ... +${affected.length - 4}`;
  return `${issue.sourceKind || issue.kind}: ${conditions || "<unknown condition>"}`
    + `${shortAffected ? ` -> ${shortAffected}` : ""}`;
}

function compareMaps(before, after) {
  const added = [];
  const removed = [];
  const shared = [];
  for (const key of Object.keys(after)) {
    if (!(key in before)) added.push(key);
    else shared.push(key);
  }
  for (const key of Object.keys(before)) {
    if (!(key in after)) removed.push(key);
  }
  return {
    added: added.sort(),
    removed: removed.sort(),
    shared: shared.sort()
  };
}

function buildDelta(baseline, current) {
  const delta = {
    format: "ProgressionJournalProfileDelta",
    version: 1,
    baselineCapturedAtUtc: baseline.capturedAtUtc,
    comparedAtUtc: new Date().toISOString(),
    mods: {}
  };

  for (const mod of MODS) {
    const before = baseline.mods[mod];
    const after = current.mods[mod];
    if (!before || !after) throw new Error(`Baseline/current data missing for ${mod}`);

    const itemKeys = compareMaps(before.items, after.items);
    const moved = [];
    const metadataChanged = [];
    for (const id of itemKeys.shared) {
      const beforeItem = before.items[id];
      const afterItem = after.items[id];
      if (JSON.stringify(beforeItem.stageIds) !== JSON.stringify(afterItem.stageIds)) {
        moved.push({ id, before: beforeItem, after: afterItem });
      }
      if (itemMetadataChanged(beforeItem, afterItem)) {
        metadataChanged.push({ id, before: beforeItem, after: afterItem });
      }
    }

    const issueKeys = compareMaps(before.review.issues, after.review.issues);
    delta.mods[mod] = {
      itemCountBefore: Object.keys(before.items).length,
      itemCountAfter: Object.keys(after.items).length,
      reviewCountBefore: before.review.total,
      reviewCountAfter: after.review.total,
      addedItems: itemKeys.added.map(id => after.items[id]),
      removedItems: itemKeys.removed.map(id => before.items[id]),
      movedItems: moved,
      metadataChangedItems: metadataChanged,
      addedReviewIssues: issueKeys.added.map(id => after.review.issues[id]),
      resolvedReviewIssues: issueKeys.removed.map(id => before.review.issues[id]),
      reportBefore: before.report,
      reportAfter: after.report,
      watchedItems: Object.fromEntries(WATCH_ITEMS.map(id => [id, {
        before: before.items[id] ?? null,
        after: after.items[id] ?? null
      }]))
    };
  }
  return delta;
}

function printSummary(snapshot, heading) {
  console.log(`\n=== ${heading} ===`);
  for (const mod of MODS) {
    const value = snapshot.mods[mod];
    const itemCount = Object.keys(value.items).length;
    const byKind = value.review.byKind ?? {};
    console.log(
      `${mod}: items=${itemCount}; review=${value.review.total}`
      + ` (unassigned=${byKind["unassigned-combat-item"] ?? 0},`
      + ` unresolved=${byKind["unresolved-condition"] ?? 0});`
      + ` audit=${value.report.auditErrors ?? "?"} errors,`
      + ` ${value.report.auditWarnings ?? "?"} warnings`);
  }

  console.log("\nWatched items:");
  for (const id of WATCH_ITEMS) {
    const matches = [];
    for (const mod of MODS) {
      const item = snapshot.mods[mod].items[id];
      if (item) matches.push(`${mod}: ${stageText(item)}`);
    }
    console.log(`  ${id}: ${matches.length ? matches.join(" | ") : "<missing in all profiles>"}`);
  }

  console.log("\nUnassigned items from each target mod:");
  for (const mod of MODS) {
    const items = snapshot.mods[mod].review.unassignedModItems;
    console.log(`  ${mod} (${items.length}): ${items.length ? items.join(", ") : "none"}`);
  }
}

function printList(title, values, formatter) {
  if (values.length === 0) return;
  console.log(`  ${title} (${values.length}):`);
  for (const value of values) console.log(`    ${formatter(value)}`);
}

function printDelta(delta) {
  console.log("\n=== CHANGES FROM BASELINE ===");
  let totalChanges = 0;

  for (const mod of MODS) {
    const value = delta.mods[mod];
    const reportChanged = JSON.stringify(value.reportBefore) !== JSON.stringify(value.reportAfter);
    const changeCount = value.addedItems.length
      + value.removedItems.length
      + value.movedItems.length
      + value.metadataChangedItems.length
      + value.addedReviewIssues.length
      + value.resolvedReviewIssues.length
      + (reportChanged ? 1 : 0);
    totalChanges += changeCount;

    console.log(`\n${mod}: items ${value.itemCountBefore} -> ${value.itemCountAfter}`
      + ` (${value.itemCountAfter - value.itemCountBefore >= 0 ? "+" : ""}`
      + `${value.itemCountAfter - value.itemCountBefore}); review ${value.reviewCountBefore}`
      + ` -> ${value.reviewCountAfter}`
      + ` (${value.reviewCountAfter - value.reviewCountBefore >= 0 ? "+" : ""}`
      + `${value.reviewCountAfter - value.reviewCountBefore})`);

    printList("ADDED", value.addedItems,
      item => `+ ${item.id} @ ${stageText(item)}`);
    printList("REMOVED", value.removedItems,
      item => `- ${item.id} @ ${stageText(item)}`);
    printList("MOVED", value.movedItems,
      move => `~ ${move.id}: ${stageText(move.before)} -> ${stageText(move.after)}`);
    printList("METADATA CHANGED", value.metadataChangedItems,
      change => `* ${change.id}: category/classes/entry changed`);
    printList("REVIEW RESOLVED", value.resolvedReviewIssues,
      issue => `- ${issue.kind}: ${issueText(issue)}`);
    printList("REVIEW ADDED", value.addedReviewIssues,
      issue => `+ ${issue.kind}: ${issueText(issue)}`);

    if (reportChanged) {
      console.log("  REPORT DIAGNOSTICS:");
      const keys = Object.keys(value.reportBefore);
      for (const key of keys) {
        const before = value.reportBefore[key];
        const after = value.reportAfter[key];
        if (JSON.stringify(before) !== JSON.stringify(after)) {
          console.log(`    ${key}: ${JSON.stringify(before)} -> ${JSON.stringify(after)}`);
        }
      }
    }

    const watchedChanges = [];
    for (const [id, watched] of Object.entries(value.watchedItems)) {
      const before = watched.before;
      const after = watched.after;
      const beforeText = before ? stageText(before) : "<missing>";
      const afterText = after ? stageText(after) : "<missing>";
      if (beforeText !== afterText) watchedChanges.push(`${id}: ${beforeText} -> ${afterText}`);
    }
    printList("WATCHED CHANGES", watchedChanges, value => `! ${value}`);

    if (changeCount === 0) console.log("  no semantic changes");
  }

  console.log(totalChanges === 0
    ? "\nRESULT: no semantic changes from baseline."
    : `\nRESULT: detected ${totalChanges} change group(s). Review every line above.`);
}

function resetBaseline() {
  fs.rmSync(STATE_DIR, { recursive: true, force: true });
  console.log(`Removed ${path.relative(ROOT, STATE_DIR)}.`);
}

try {
  if (process.argv.includes("--reset")) resetBaseline();
  const current = captureCurrent();

  if (!fs.existsSync(BASELINE_FILE)) {
    writeJson(BASELINE_FILE, current);
    printSummary(current, "BASELINE CAPTURED");
    console.log(`\nSaved: ${path.relative(ROOT, BASELINE_FILE)}`);
    console.log("Run this same command after regenerating profiles to see added, removed and moved items.");
  } else {
    const baseline = readJson(BASELINE_FILE);
    const delta = buildDelta(baseline, current);
    writeJson(DELTA_FILE, delta);
    printSummary(current, "CURRENT STATE");
    printDelta(delta);
    console.log(`\nMachine-readable delta: ${path.relative(ROOT, DELTA_FILE)}`);
  }
} catch (error) {
  fail(error.stack ?? error.message);
}
