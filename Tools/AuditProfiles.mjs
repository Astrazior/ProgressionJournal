import path from "node:path";
import { loadManifest, readJson, writeJson } from "./ProfileGeneratorCore.mjs";

const root = path.resolve(import.meta.dirname, "..");
let failed = false;

for (const name of ["calamity", "thorium", "fargosouls"]) {
  const manifestPath = path.join(root, "Profiles", "Manifests", `${name}.json`);
  const manifest = loadManifest(manifestPath);
  const profile = readJson(path.join(root, "Profiles", "Builtin", `${name}.json`));
  const generationReport = readJson(
    path.join(root, "Profiles", "Reports", `${name}-report.json`));
  const stageIndexes = new Map(profile.stages.map((stage, index) => [stage.id, index]));
  const allowedMods = new Set([
    "Terraria",
    ...(manifest.contentMods
      ?? (manifest.requiredMods ?? []).map(requiredMod => requiredMod.name))
  ]);
  const errors = [];
  const warnings = [];
  const wikiCompatible =
    generationReport.officialSourceCompatibility?.recommendations?.compatible === true;

  for (const entry of profile.entries) {
    const itemId = `${entry.itemGroups[0][0].mod}/${entry.itemGroups[0][0].item}`;
    const earliestEvaluation = earliestStage(
      entry.evaluations.map(evaluation => evaluation.stageId),
      stageIndexes);
    for (const wiki of entry.wiki ?? []) {
      if (!earliestEvaluation
          || stageIndexes.get(earliestEvaluation) > stageIndexes.get(wiki.stageId)) {
        const finding = {
          kind: "later-than-official-wiki",
          item: itemId,
          generatedStage: earliestEvaluation,
          wikiStage: wiki.stageId,
          sourceUrl: wiki.sourceUrl
        };
        (wikiCompatible ? errors : warnings).push(finding);
      }
    }
  }

  for (const [item, acquisition] of Object.entries(generationReport.paths ?? {})) {
    const foreignReferences = [...(acquisition.via ?? "").matchAll(
      /([A-Za-z0-9_]+)\/[A-Za-z0-9_]+/g)]
      .map(match => match[1])
      .filter(mod => !allowedMods.has(mod));
    if (foreignReferences.length > 0) {
      errors.push({
        kind: "foreign-mod-acquisition",
        item,
        stage: acquisition.stage,
        via: acquisition.via,
        foreignMods: [...new Set(foreignReferences)]
      });
    }
  }

  for (const correction of generationReport.availabilityCorrections ?? []) {
    if (!stageIndexes.has(correction.toStage)) {
      errors.push({
        kind: "unknown-official-availability-stage",
        ...correction
      });
    }
  }

  if ((generationReport.wikiUnresolvedItems?.length ?? 0) > 0) {
    warnings.push({
      kind: "unresolved-official-wiki-items",
      count: generationReport.wikiUnresolvedItems.length
    });
  }

  const audit = {
    format: "ProgressionJournalProfileAudit",
    version: 1,
    profileId: profile.id,
    generatedAtUtc: new Date().toISOString(),
    officialSources: {
      recommendations: manifest.wikiInput ?? null,
      availability: manifest.availabilityInput ?? null,
      compatibility: generationReport.officialSourceCompatibility
    },
    summary: {
      errors: errors.length,
      warnings: warnings.length,
      wikiAvailabilityCorrections:
        generationReport.wikiAvailabilityCorrections?.length ?? 0,
      exactAvailabilityCorrections:
        generationReport.availabilityCorrections?.length ?? 0
    },
    errors,
    warnings
  };
  writeJson(path.join(root, "Profiles", "Reports", `${name}-audit.json`), audit);

  if (errors.length > 0) {
    failed = true;
    console.error(`${name}: ${errors.length} audit errors`);
  } else {
    console.log(
      `${name}: audit OK; `
      + `${audit.summary.wikiAvailabilityCorrections} wiki upper-bound corrections, `
      + `${audit.summary.exactAvailabilityCorrections} exact availability corrections`);
  }
}

if (failed) process.exitCode = 1;

function earliestStage(stageIds, stageIndexes) {
  return [...new Set(stageIds)]
    .filter(stageId => stageIndexes.has(stageId))
    .sort((left, right) => stageIndexes.get(left) - stageIndexes.get(right))[0]
    ?? null;
}
