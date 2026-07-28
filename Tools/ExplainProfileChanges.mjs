#!/usr/bin/env node
import fs from "node:fs";
import path from "node:path";
import {
  captureProfileState,
  compareProfileStates,
  createRegressionReport,
  formatConsoleSummary,
  renderRegressionMarkdown
} from "./ProfileRegressionGuard.mjs";

function parseArgs(values) {
  const result = {
    before: "",
    after: "",
    output: "profile-changes.md"
  };
  for (let index = 0; index < values.length; index++) {
    const value = values[index];
    if (!["--before", "--after", "--output"].includes(value)) {
      throw new Error(`Unknown option '${value}'.`);
    }
    const next = values[index + 1];
    if (!next || next.startsWith("--")) {
      throw new Error(`Missing value for ${value}.`);
    }
    result[value.slice(2)] = next;
    index++;
  }
  if (!result.before || !result.after) {
    throw new Error(
      "Usage: node Tools/ExplainProfileChanges.mjs "
      + "--before <profile-directory> --after <profile-directory> [--output changes.md]");
  }
  return result;
}

function resolveDirectory(input) {
  const absolute = path.resolve(input);
  const stat = fs.statSync(absolute);
  return stat.isDirectory() ? absolute : path.dirname(absolute);
}

try {
  const args = parseArgs(process.argv.slice(2));
  const beforeDirectory = resolveDirectory(args.before);
  const afterDirectory = resolveDirectory(args.after);
  const before = captureProfileState(beforeDirectory);
  const after = captureProfileState(afterDirectory);
  const modName = after.metadata.targetMod
    || before.metadata.targetMod
    || path.basename(afterDirectory);
  const comparison = compareProfileStates(modName, before, after);
  const report = createRegressionReport([comparison], {
    command: ["ExplainProfileChanges.mjs", ...process.argv.slice(2)],
    candidateRoot: afterDirectory
  });
  const markdownFile = path.resolve(args.output);
  const jsonFile = markdownFile.replace(/\.md$/iu, "") + ".json";
  fs.mkdirSync(path.dirname(markdownFile), { recursive: true });
  fs.writeFileSync(markdownFile, renderRegressionMarkdown(report), "utf8");
  fs.writeFileSync(jsonFile, `${JSON.stringify(report, null, 2)}\n`, "utf8");
  console.log(formatConsoleSummary(report));
  console.log(`\nWrote ${markdownFile}`);
  console.log(`Wrote ${jsonFile}`);
} catch (error) {
  console.error(error.stack ?? error.message);
  process.exitCode = 1;
}
