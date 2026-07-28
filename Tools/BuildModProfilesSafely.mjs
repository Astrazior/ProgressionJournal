#!/usr/bin/env node
import fs from "node:fs";
import path from "node:path";
import { pathToFileURL } from "node:url";
import { buildModProfile } from "./BuildModProfiles.mjs";
import {
  GENERATED_PROFILE_FILES,
  captureProfileState,
  compareProfileStates,
  createRegressionReport,
  formatConsoleSummary,
  renderRegressionMarkdown
} from "./ProfileRegressionGuard.mjs";

const DEFAULT_ROOT = path.resolve(import.meta.dirname, "..");

function parseArgs(values) {
  const result = {
    target: "",
    acceptChanges: false,
    dryRun: false
  };
  for (const value of values) {
    if (value === "--accept-changes") {
      result.acceptChanges = true;
      continue;
    }
    if (value === "--dry-run") {
      result.dryRun = true;
      continue;
    }
    if (value.startsWith("--") && value !== "--all") {
      throw new Error(`Unknown option '${value}'.`);
    }
    if (result.target) {
      throw new Error(`Unexpected argument '${value}'.`);
    }
    result.target = value;
  }
  if (!result.target) {
    throw new Error(
      "Usage: node Tools/BuildModProfiles.mjs <InternalModName|--all> "
      + "[--dry-run] [--accept-changes]");
  }
  if (result.dryRun && result.acceptChanges) {
    throw new Error("--dry-run and --accept-changes cannot be used together.");
  }
  return result;
}

function availableMods(profilesRoot) {
  return fs.readdirSync(profilesRoot, { withFileTypes: true })
    .filter(entry => entry.isDirectory())
    .map(entry => entry.name)
    .filter(name =>
      fs.existsSync(path.join(profilesRoot, name, "support.json"))
      && fs.existsSync(path.join(profilesRoot, name, "snapshot.json")))
    .sort((left, right) => left.localeCompare(right));
}

function selectedMods(profilesRoot, target) {
  const mods = target === "--all" ? availableMods(profilesRoot) : [target];
  for (const modName of mods) {
    const directory = path.join(profilesRoot, modName);
    if (!fs.existsSync(path.join(directory, "support.json"))) {
      throw new Error(`Unknown profile '${modName}': support.json is missing.`);
    }
    if (!fs.existsSync(path.join(directory, "snapshot.json"))) {
      throw new Error(`Cannot build '${modName}': snapshot.json is missing.`);
    }
  }
  return mods;
}

function resetDirectory(directory) {
  if (fs.existsSync(directory)) fs.rmSync(directory, { recursive: true, force: true });
  fs.mkdirSync(directory, { recursive: true });
}

function copyGeneratedFiles(sourceDirectory, targetDirectory) {
  fs.mkdirSync(targetDirectory, { recursive: true });
  for (const name of GENERATED_PROFILE_FILES) {
    const source = path.join(sourceDirectory, name);
    if (!fs.existsSync(source)) {
      throw new Error(`Candidate is incomplete: ${source} is missing.`);
    }
    fs.copyFileSync(source, path.join(targetDirectory, name));
  }
}

function restoreInterruptedCommit(profilesRoot, stateRoot) {
  const transactionFile = path.join(stateRoot, "transaction.json");
  if (!fs.existsSync(transactionFile)) return false;
  const transaction = JSON.parse(fs.readFileSync(transactionFile, "utf8"));
  const backupRoot = path.join(stateRoot, "backup");
  for (const [modName, files] of Object.entries(transaction.files ?? {})) {
    const profileDirectory = path.join(profilesRoot, modName);
    for (const [name, existed] of Object.entries(files)) {
      const target = path.join(profileDirectory, name);
      const backup = path.join(backupRoot, modName, name);
      if (existed) {
        if (!fs.existsSync(backup)) {
          throw new Error(`Cannot recover interrupted safe build: ${backup} is missing.`);
        }
        fs.copyFileSync(backup, target);
      } else if (fs.existsSync(target)) {
        fs.rmSync(target, { force: true });
      }
    }
  }
  fs.rmSync(transactionFile, { force: true });
  fs.rmSync(backupRoot, { recursive: true, force: true });
  return true;
}

function prepareCommitBackup(mods, profilesRoot, stateRoot) {
  const backupRoot = path.join(stateRoot, "backup");
  const transactionFile = path.join(stateRoot, "transaction.json");
  resetDirectory(backupRoot);
  const transaction = {
    format: "ProgressionJournalSafeBuildTransaction",
    version: 1,
    preparedAtUtc: new Date().toISOString(),
    files: {}
  };
  for (const modName of mods) {
    const sourceDirectory = path.join(profilesRoot, modName);
    const backupDirectory = path.join(backupRoot, modName);
    fs.mkdirSync(backupDirectory, { recursive: true });
    transaction.files[modName] = {};
    for (const name of GENERATED_PROFILE_FILES) {
      const source = path.join(sourceDirectory, name);
      const existed = fs.existsSync(source);
      transaction.files[modName][name] = existed;
      if (existed) fs.copyFileSync(source, path.join(backupDirectory, name));
    }
  }
  fs.writeFileSync(transactionFile, `${JSON.stringify(transaction, null, 2)}\n`, "utf8");
}

function commitCandidates(mods, profilesRoot, candidateRoot, stateRoot) {
  const transactionFile = path.join(stateRoot, "transaction.json");
  const backupRoot = path.join(stateRoot, "backup");
  prepareCommitBackup(mods, profilesRoot, stateRoot);
  try {
    for (const modName of mods) {
      copyGeneratedFiles(
        path.join(candidateRoot, modName),
        path.join(profilesRoot, modName));
    }
    fs.rmSync(transactionFile, { force: true });
    fs.rmSync(backupRoot, { recursive: true, force: true });
  } catch (error) {
    restoreInterruptedCommit(profilesRoot, stateRoot);
    throw error;
  }
}

function writeReports(stateRoot, report) {
  const jsonFile = path.join(stateRoot, "last-report.json");
  const markdownFile = path.join(stateRoot, "last-report.md");
  fs.mkdirSync(stateRoot, { recursive: true });
  fs.writeFileSync(jsonFile, `${JSON.stringify(report, null, 2)}\n`, "utf8");
  fs.writeFileSync(markdownFile, renderRegressionMarkdown(report), "utf8");
  return { jsonFile, markdownFile };
}

export function runSafeBuild(values, options = {}) {
  const root = options.root ?? DEFAULT_ROOT;
  const profilesRoot = options.profilesRoot ?? path.join(root, "Profiles", "Mods");
  const stateRoot = options.stateRoot ?? path.join(root, ".profile-check", "safe-build");
  const candidateRoot = path.join(stateRoot, "candidate");
  const build = options.buildModProfile ?? buildModProfile;
  const args = parseArgs(values);
  const mods = selectedMods(profilesRoot, args.target);

  fs.mkdirSync(stateRoot, { recursive: true });
  if (restoreInterruptedCommit(profilesRoot, stateRoot)) {
    console.warn("Recovered profiles from an interrupted previous safe-build transaction.");
  }
  resetDirectory(candidateRoot);

  const comparisons = [];
  for (const modName of mods) {
    const profileDirectory = path.join(profilesRoot, modName);
    const candidateDirectory = path.join(candidateRoot, modName);
    const before = captureProfileState(profileDirectory);
    build(modName, { outputDirectory: candidateDirectory });
    const after = captureProfileState(candidateDirectory);
    comparisons.push(compareProfileStates(modName, before, after));
  }

  const report = createRegressionReport(comparisons, {
    command: ["BuildModProfiles.mjs", ...values],
    candidateRoot,
    acceptedWithOverride: args.acceptChanges
  });
  let reportFiles = writeReports(stateRoot, report);
  console.log(formatConsoleSummary(report));
  console.log(`\nПодробный отчёт: ${reportFiles.markdownFile}`);
  console.log(`JSON-отчёт: ${reportFiles.jsonFile}`);

  if (report.blocked && !args.acceptChanges) {
    console.error(
      "\nЗапись отменена. Кандидаты сохранены для проверки. "
      + "После проверки осознанные изменения можно принять повторным запуском "
      + "с флагом --accept-changes.");
    return {
      accepted: false,
      blocked: true,
      report,
      reportFiles,
      candidateRoot
    };
  }

  if (args.dryRun) {
    console.log("\nDry run: рабочие профили не изменены, кандидаты сохранены.");
    return {
      accepted: true,
      blocked: false,
      dryRun: true,
      report,
      reportFiles,
      candidateRoot
    };
  }

  commitCandidates(mods, profilesRoot, candidateRoot, stateRoot);
  report.committed = true;
  report.committedAtUtc = new Date().toISOString();
  reportFiles = writeReports(stateRoot, report);
  fs.rmSync(candidateRoot, { recursive: true, force: true });
  console.log(report.blocked
    ? "\nИзменения записаны с явным --accept-changes."
    : "\nПроверка пройдена, рабочие профили обновлены.");
  return {
    accepted: true,
    blocked: report.blocked,
    report,
    reportFiles,
    candidateRoot
  };
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    const result = runSafeBuild(process.argv.slice(2));
    if (!result.accepted) process.exitCode = 1;
  } catch (error) {
    console.error(error.stack ?? error.message);
    process.exitCode = 1;
  }
}
