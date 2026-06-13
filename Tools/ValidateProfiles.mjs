import fs from "node:fs";
import path from "node:path";

const root = path.resolve(import.meta.dirname, "..");
const directory = path.join(root, "Profiles", "Builtin");
let failed = false;

for (const name of fs.readdirSync(directory).filter(name => name.endsWith(".json") && !name.endsWith("-report.json"))) {
  const profile = JSON.parse(fs.readFileSync(path.join(directory, name), "utf8"));
  const errors = [];
  if (profile.version !== 1) errors.push("version must be 1");
  if (!profile.name?.["en-US"] || !profile.name?.["ru-RU"]) errors.push("profile name requires en-US and ru-RU");
  for (const cls of profile.classes ?? []) {
    if (!cls.name?.["en-US"] || !cls.name?.["ru-RU"]) errors.push(`class '${cls.id}' requires en-US and ru-RU`);
  }
  if (!profile.requiredMods?.every(mod => mod.name)) errors.push("unknown requiredMod");
  if (hasDuplicates(profile.stages?.map(stage => stage.id) ?? [])) errors.push("duplicate stage id");
  if (hasDuplicates(profile.entries?.map(entry => entry.key) ?? [])) errors.push("duplicate entry id");
  for (const stage of profile.stages ?? []) {
    if (!stage.name?.["en-US"] || !stage.name?.["ru-RU"]) errors.push(`stage '${stage.id}' requires en-US and ru-RU`);
    if (/(\bpre\b|\bpost\b|до |после )/i.test(`${stage.name?.["en-US"]} ${stage.name?.["ru-RU"]}`)) {
      errors.push(`stage '${stage.id}' contains before/after wording`);
    }
  }
  for (const wiki of (profile.entries ?? []).flatMap(entry => entry.wiki ?? [])) {
    if (!wiki.target?.["en-US"] || !wiki.target?.["ru-RU"]) errors.push("Wiki target requires en-US and ru-RU");
  }
  if (errors.length) {
    failed = true;
    console.error(`${name}: ${errors.join("; ")}`);
  } else {
    console.log(`${name}: OK`);
  }
}

if (failed) process.exitCode = 1;

function hasDuplicates(values) {
  return new Set(values.map(value => value.toLowerCase())).size !== values.length;
}
