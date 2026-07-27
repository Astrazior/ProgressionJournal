#!/usr/bin/env node
import fs from "node:fs";
import path from "node:path";

const args = parseArgs(process.argv.slice(2));
if (!args.before || !args.after) {
  fail("Usage: node ExplainProfileChanges.mjs --before <dir|item-audit.json> --after <dir|item-audit.json> [--output changes.md]");
}

const before = loadSide(args.before);
const after = loadSide(args.after);
const changes = compareSides(before, after);
const outputPath = path.resolve(args.output ?? "profile-changes.md");
const jsonPath = outputPath.replace(/\.md$/iu, "") + ".json";

fs.mkdirSync(path.dirname(outputPath), { recursive: true });
fs.writeFileSync(outputPath, renderMarkdown(before, after, changes));
fs.writeFileSync(jsonPath, JSON.stringify({
  format: "ProgressionJournalProfileChanges",
  version: 1,
  generatedAtUtc: new Date().toISOString(),
  before: before.metadata,
  after: after.metadata,
  summary: summarize(changes),
  changes
}, null, 2) + "\n");

console.log(`Wrote ${outputPath}`);
console.log(`Wrote ${jsonPath}`);

function parseArgs(values) {
  const result = {};
  for (let index = 0; index < values.length; index++) {
    const value = values[index];
    if (!value.startsWith("--")) fail(`Unexpected argument '${value}'.`);
    const key = value.slice(2);
    const next = values[index + 1];
    if (!next || next.startsWith("--")) fail(`Missing value for --${key}.`);
    result[key] = next;
    index++;
  }
  return result;
}

function loadSide(inputPath) {
  const absolute = path.resolve(inputPath);
  const stat = fs.statSync(absolute);
  const directory = stat.isDirectory() ? absolute : path.dirname(absolute);
  const auditPath = stat.isDirectory() ? path.join(absolute, "item-audit.json") : absolute;
  const reportPath = path.join(directory, "report.json");
  const audit = readJson(auditPath);
  const report = fs.existsSync(reportPath) ? readJson(reportPath) : { paths: {} };
  const items = new Map((audit.items ?? []).map(item => {
    const pathInfo = report.paths?.[item.id] ?? null;
    return [item.id, {
      ...item,
      stage: item.stage ?? pathInfo?.stage ?? "",
      via: item.via ?? pathInfo?.via ?? "",
      evidence: item.evidence ?? pathInfo?.evidence ?? null
    }];
  }));
  return {
    directory,
    audit,
    report,
    items,
    metadata: {
      source: absolute,
      targetMod: audit.targetMod ?? "",
      profileId: audit.profileId ?? "",
      generatedAtUtc: audit.generatedAtUtc ?? "",
      snapshotGeneratedAtUtc: audit.snapshotGeneratedAtUtc ?? ""
    }
  };
}

function compareSides(before, after) {
  const ids = [...new Set([...before.items.keys(), ...after.items.keys()])].sort();
  const changes = [];
  for (const id of ids) {
    const previous = before.items.get(id) ?? missingItem(id);
    const current = after.items.get(id) ?? missingItem(id);
    const previousSignature = stateSignature(previous);
    const currentSignature = stateSignature(current);
    if (previousSignature === currentSignature) continue;
    changes.push({
      id,
      name: current.name || previous.name || id,
      mod: current.mod || previous.mod || id.split("/")[0] || "",
      kind: classifyChange(previous, current),
      before: normalizeState(previous),
      after: normalizeState(current),
      determination: describeDetermination(current)
    });
  }
  return changes.sort((left, right) =>
    changeOrder(left.kind) - changeOrder(right.kind) || left.id.localeCompare(right.id));
}

function missingItem(id) {
  return { id, name: "", mod: id.split("/")[0] ?? "", status: "missing", stage: "", via: "", evidence: null };
}

function normalizeState(item) {
  return {
    status: item.status ?? "unknown",
    stage: item.stage ?? "",
    via: item.via ?? "",
    reason: item.reason ?? "",
    evidence: item.evidence ?? null
  };
}

function stateSignature(item) {
  return JSON.stringify(normalizeState(item));
}

function isProfileStatus(status) {
  return status === "equipment" || status === "buff";
}

function classifyChange(previous, current) {
  const beforeProfile = isProfileStatus(previous.status);
  const afterProfile = isProfileStatus(current.status);
  if (!beforeProfile && afterProfile) return "restored";
  if (beforeProfile && !afterProfile) return "removed";
  if (beforeProfile && afterProfile && previous.stage !== current.stage) return "moved";
  if (previous.status !== current.status) return "status-changed";
  return "evidence-changed";
}

function changeOrder(kind) {
  return ["restored", "moved", "removed", "status-changed", "evidence-changed"].indexOf(kind);
}

function summarize(changes) {
  const result = { total: changes.length };
  for (const change of changes) result[change.kind] = (result[change.kind] ?? 0) + 1;
  return result;
}

function renderMarkdown(before, after, changes) {
  const summary = summarize(changes);
  const lines = [
    "# Изменения сгенерированного профиля",
    "",
    `- Профиль: \`${escapeInline(after.metadata.profileId || before.metadata.profileId)}\``,
    `- До: \`${escapeInline(before.metadata.generatedAtUtc || before.metadata.source)}\``,
    `- После: \`${escapeInline(after.metadata.generatedAtUtc || after.metadata.source)}\``,
    `- Всего изменений: **${summary.total}**; возвращено: **${summary.restored ?? 0}**; перемещено: **${summary.moved ?? 0}**; убрано: **${summary.removed ?? 0}**; изменено доказательство/статус: **${(summary["status-changed"] ?? 0) + (summary["evidence-changed"] ?? 0)}**.`,
    "",
    "Текст описания условия не используется как доказательство. В колонке «Как определено» перечислены машинный ключ/факт, наблюдённый этап или доказанная рецептурная цепочка.",
    ""
  ];

  for (const kind of ["restored", "moved", "removed", "status-changed", "evidence-changed"]) {
    const group = changes.filter(change => change.kind === kind);
    if (group.length === 0) continue;
    lines.push(`## ${changeHeading(kind)} (${group.length})`, "");
    lines.push("| Предмет | Было | Стало | Как определено |", "|---|---|---|---|");
    for (const change of group) {
      lines.push(`| ${cell(`${change.name} (${change.id})`)} | ${cell(renderState(change.before))} | ${cell(renderState(change.after))} | ${cell(change.determination)} |`);
    }
    lines.push("");
  }

  if (changes.length === 0) lines.push("Изменений нет.", "");
  return lines.join("\n") + "\n";
}

function changeHeading(kind) {
  return ({
    restored: "Возвращено в профиль",
    moved: "Перемещено между этапами",
    removed: "Убрано из профиля",
    "status-changed": "Изменён статус",
    "evidence-changed": "Изменён путь или доказательство"
  })[kind] ?? kind;
}

function renderState(state) {
  const parts = [state.status || "unknown"];
  if (state.stage) parts.push(`этап ${state.stage}`);
  if (state.via) parts.push(`через ${state.via}`);
  if (!state.stage && !state.via && state.reason) parts.push(state.reason);
  return parts.join("; ");
}

function describeDetermination(item) {
  const evidence = item.evidence;
  if (evidence?.method === "observed-shop") {
    const details = [`наблюдённый магазин: ${evidence.observedStageId || item.stage || "этап из snapshot"}`];
    if ((evidence.conditionKeys ?? []).length > 0) details.push(`ключи: ${(evidence.conditionKeys ?? []).join(", ")}`);
    if ((evidence.prerequisites ?? []).length > 0) {
      details.push(`предпосылки: ${evidence.prerequisites.map(value => `${value.item}${value.stage ? `@${value.stage}` : ""}`).join(", ")}`);
    }
    return details.join("; ");
  }
  if (evidence?.method === "shop") {
    const details = ["магазин с машинно разрешёнными условиями"];
    if ((evidence.conditionKeys ?? []).length > 0) details.push(`ключи: ${evidence.conditionKeys.join(", ")}`);
    if ((evidence.prerequisites ?? []).length > 0) details.push(`предпосылки: ${evidence.prerequisites.map(value => value.item).join(", ")}`);
    return details.join("; ");
  }
  if (evidence?.method === "recipe") {
    const ingredients = (evidence.ingredients ?? []).map(value => `${value.item}${value.stage ? `@${value.stage}` : ""}`);
    const stations = evidence.stations ?? [];
    return `рецептурное замыкание${ingredients.length ? `; ингредиенты: ${ingredients.join(", ")}` : ""}${stations.length ? `; станции: ${stations.join(", ")}` : ""}`;
  }
  if (evidence?.method) return evidence.method;
  if (item.via?.startsWith("shop:")) return "магазин; подробное доказательство отсутствует в старом item-audit/report";
  if (item.via?.startsWith("recipe:")) return "рецептурная цепочка; подробное доказательство отсутствует в старом item-audit/report";
  if (item.via) return item.via;
  return item.reason || "доказательство отсутствует";
}

function cell(value) {
  return String(value ?? "").replace(/\|/gu, "\\|").replace(/\r?\n/gu, " ");
}

function escapeInline(value) {
  return String(value ?? "").replace(/`/gu, "\\`");
}

function readJson(filePath) {
  try {
    return JSON.parse(fs.readFileSync(filePath, "utf8"));
  } catch (error) {
    fail(`Cannot read '${filePath}': ${error.message}`);
  }
}

function fail(message) {
  console.error(message);
  process.exit(1);
}
