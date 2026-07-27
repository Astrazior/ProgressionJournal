import fs from "node:fs";
import path from "node:path";

const root = process.argv[2]
  ? path.resolve(process.argv[2])
  : path.resolve(import.meta.dirname, "..");
const modsRoot = path.join(root, "Profiles", "Mods");
const outputDirectory = path.join(root, ".profile-check");
const outputPath = path.join(outputDirectory, "recovery-plan.json");

if (!fs.existsSync(modsRoot)) {
  throw new Error(`Missing profile directory: ${modsRoot}`);
}

const readJson = file => JSON.parse(fs.readFileSync(file, "utf8"));
const exists = file => fs.existsSync(file);
const required = ["support.json", "knowledge.json", "review.json", "item-audit.json"];
const directories = fs.readdirSync(modsRoot, { withFileTypes: true })
  .filter(entry => entry.isDirectory())
  .map(entry => path.join(modsRoot, entry.name))
  .filter(directory => required.every(name => exists(path.join(directory, name))))
  .sort();

const plans = directories.map(buildPlan);
fs.mkdirSync(outputDirectory, { recursive: true });
fs.writeFileSync(outputPath, `${JSON.stringify({
  format: "ProgressionJournalRecoveryPlan",
  version: 1,
  generatedAtUtc: new Date().toISOString(),
  profiles: plans
}, null, 2)}\n`, "utf8");

console.log("=== RECOVERY ROOTS ===");
for (const plan of plans) {
  console.log(`\n${plan.modName}: ${plan.unassignedItems} unassigned item(s), ${plan.rootGroups.length} root group(s)`);
  const targetGroups = plan.rootGroups.filter(group => group.targetItems.length > 0);
  if (targetGroups.length === 0) {
    console.log("  Target-mod items: none");
  } else {
    console.log(`  Target-mod roots: ${targetGroups.length}`);
    for (const group of targetGroups) {
      console.log(`  [${group.kind}] ${group.label}`);
      console.log(`    affects ${group.targetItems.length}: ${group.targetItems.join(", ")}`);
      if (group.machineTypes.length > 0) {
        console.log(`    machine types: ${group.machineTypes.join(", ")}`);
      }
      if (group.textOnlyConditions.length > 0) {
        console.log(`    text-only conditions: ${group.textOnlyConditions.join(" | ")}`);
      }
    }
  }
}
console.log(`\nSaved: ${path.relative(root, outputPath)}`);

function buildPlan(directory) {
  const support = readJson(path.join(directory, "support.json"));
  const knowledge = readJson(path.join(directory, "knowledge.json"));
  const review = readJson(path.join(directory, "review.json"));
  const audit = readJson(path.join(directory, "item-audit.json"));
  const modName = support.targetMod ?? path.basename(directory);
  const statusByItem = new Map((audit.items ?? []).map(item => [item.id, item.status]));
  const recipesByResult = groupBy(knowledge.acquisitions?.recipes ?? [], recipe => recipe.result);
  const shopsByItem = groupBy(knowledge.acquisitions?.shops ?? [], shop => shop.item);
  const dropsByItem = groupBy(knowledge.acquisitions?.drops ?? [], drop => drop.item);
  const fishingByItem = groupBy(
    (knowledge.acquisitions?.fishing ?? []).filter(record => record.targetType === "item"),
    record => record.target);
  const shimmerByOutput = groupBy(
    knowledge.acquisitions?.shimmerTransforms ?? [],
    record => record.output);
  const unassigned = (review.issues ?? [])
    .filter(issue => issue.kind === "unassigned-combat-item")
    .map(issue => issue.item);
  const memo = new Map();
  const groups = new Map();

  for (const item of unassigned) {
    for (const rootCause of resolveRoots(item, [])) {
      const signature = JSON.stringify(rootCause);
      const group = groups.get(signature) ?? {
        ...rootCause,
        items: new Set(),
        targetItems: new Set()
      };
      group.items.add(item);
      if (item.startsWith(`${modName}/`)) group.targetItems.add(item);
      groups.set(signature, group);
    }
  }

  return {
    modName,
    profileId: support.id,
    unassignedItems: unassigned.length,
    rootGroups: [...groups.values()]
      .map(group => ({
        kind: group.kind,
        label: group.label,
        source: group.source ?? "",
        machineTypes: group.machineTypes ?? [],
        textOnlyConditions: group.textOnlyConditions ?? [],
        items: [...group.items].sort(),
        targetItems: [...group.targetItems].sort()
      }))
      .sort((left, right) =>
        right.targetItems.length - left.targetItems.length
        || right.items.length - left.items.length
        || left.label.localeCompare(right.label))
  };

  function resolveRoots(item, stack) {
    if (memo.has(item)) return memo.get(item);
    if (stack.includes(item)) {
      return [{ kind: "cycle", label: `Recipe cycle at ${item}` }];
    }

    const recipes = recipesByResult.get(item) ?? [];
    if (recipes.length > 0) {
      const roots = [];
      for (const recipe of recipes) {
        const blockers = (recipe.ingredients ?? [])
          .map(ingredient => ingredient.item)
          .filter(ingredient => !isResolvedStatus(statusByItem.get(ingredient)));
        if (blockers.length === 0) {
          roots.push({
            kind: "recipe-or-station",
            label: `${item} has resolved ingredients; inspect crafting station or recipe condition`
          });
          continue;
        }
        for (const blocker of blockers) {
          roots.push(...resolveRoots(blocker, [...stack, item]));
        }
      }
      const unique = uniqueObjects(roots);
      memo.set(item, unique);
      return unique;
    }

    const acquisitions = [
      ...(shopsByItem.get(item) ?? []).map(value => ({ kind: "shop", value })),
      ...(dropsByItem.get(item) ?? []).map(value => ({ kind: "drop", value })),
      ...(fishingByItem.get(item) ?? []).map(value => ({ kind: "fishing", value })),
      ...(shimmerByOutput.get(item) ?? []).map(value => ({ kind: "shimmer", value }))
    ];
    if (acquisitions.length === 0) {
      const result = [{ kind: "no-source", label: `${item} has no recorded acquisition source` }];
      memo.set(item, result);
      return result;
    }

    const paths = acquisitions.map(({ kind, value }) => {
      const conditions = value.conditions ?? [];
      const machineTypes = [...new Set(conditions
        .map(condition => condition.type ?? "")
        .filter(type => type && type !== "Terraria.Condition"))].sort();
      const textOnlyConditions = [...new Set(conditions
        .filter(condition => !condition.type || condition.type === "Terraria.Condition")
        .map(condition => normalizeText(condition.description))
        .filter(Boolean))].sort();
      const source = kind === "shop"
        ? value.npc
        : kind === "drop"
          ? value.source
          : kind === "fishing"
            ? value.target
            : value.input;
      const descriptions = conditions
        .map(condition => normalizeText(condition.description) || condition.type || "")
        .filter(Boolean);
      return { kind, source, machineTypes, textOnlyConditions, descriptions };
    });
    const textOnlyConditions = [...new Set(paths.flatMap(value => value.textOnlyConditions))].sort();
    const machineTypes = [...new Set(paths.flatMap(value => value.machineTypes))].sort();
    const sourceExamples = paths.slice(0, 5).map(value =>
      `${value.kind}:${value.source}${value.descriptions.length > 0 ? ` — ${value.descriptions.join(" && ")}` : ""}`);
    const result = [{
      kind: textOnlyConditions.length > 0 ? "acquisition-text-only" : "acquisition-structured",
      label: `${item} has ${paths.length} recorded acquisition path(s): ${sourceExamples.join(" | ")}${paths.length > sourceExamples.length ? " | ..." : ""}`,
      source: item,
      machineTypes,
      textOnlyConditions
    }];
    memo.set(item, result);
    return result;
  }
}

function isResolvedStatus(status) {
  return new Set(["equipment", "buff", "excluded", "acquired-non-profile"]).has(status);
}

function groupBy(values, selector) {
  const result = new Map();
  for (const value of values) {
    const key = selector(value);
    const group = result.get(key) ?? [];
    group.push(value);
    result.set(key, group);
  }
  return result;
}

function uniqueObjects(values) {
  return [...new Map(values.map(value => [JSON.stringify(value), value])).values()];
}

function normalizeText(value) {
  return typeof value === "string" ? value.trim().replace(/\s+/gu, " ") : "";
}
