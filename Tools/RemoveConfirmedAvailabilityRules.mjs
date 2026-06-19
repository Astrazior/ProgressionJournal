import fs from "node:fs";
import path from "node:path";

const root = path.resolve(import.meta.dirname, "..");
const modsRoot = path.join(root, "Profiles", "Mods");

for (const directory of fs.readdirSync(modsRoot, { withFileTypes: true })
  .filter(entry => entry.isDirectory())) {
  const modDirectory = path.join(modsRoot, directory.name);
  const reportPath = path.join(modDirectory, "report.json");
  const rulesPath = path.join(modDirectory, "agent-rules.json");
  if (!fs.existsSync(reportPath) || !fs.existsSync(rulesPath)) continue;

  const report = JSON.parse(fs.readFileSync(reportPath, "utf8"));
  const confirmed = report.agentRules?.availabilityMigration?.confirmedRules ?? [];
  if (confirmed.length === 0) continue;

  const confirmedByRule = new Map();
  for (const check of confirmed) {
    if (!confirmedByRule.has(check.id)) confirmedByRule.set(check.id, new Set());
    confirmedByRule.get(check.id).add(check.target);
  }

  const document = JSON.parse(fs.readFileSync(rulesPath, "utf8"));
  const updatedRules = [];
  let removedTargets = 0;
  for (const rule of document.rules ?? []) {
    const targets = confirmedByRule.get(rule.id);
    if (!targets || !["source-stage", "fishing-source"].includes(rule.kind)) {
      updatedRules.push(rule);
      continue;
    }

    const property = rule.kind === "source-stage" ? "source" : "item";
    const pluralProperty = rule.kind === "source-stage" ? "sources" : "items";
    const values = rule[pluralProperty] ?? (rule[property] ? [rule[property]] : []);
    const remaining = values.filter(value => !targets.has(value));
    removedTargets += values.length - remaining.length;
    if (remaining.length === 0) continue;

    const updated = { ...rule };
    delete updated[property];
    delete updated[pluralProperty];
    if (remaining.length === 1) updated[property] = remaining[0];
    else updated[pluralProperty] = remaining;
    updatedRules.push(updated);
  }

  document.rules = updatedRules;
  fs.writeFileSync(rulesPath, `${JSON.stringify(document, null, 2)}\n`, "utf8");
  console.log(`${directory.name}: removed ${removedTargets} confirmed legacy targets`);
}
