const LEGACY_VANILLA_STAGE_ID_ALIASES = new Map([
  ["PreBoss", "start"],
  ["PostKingSlime", "king-slime"],
  ["PostEyeOfCthulhu", "eye-of-cthulhu"],
  ["PostWorldEvil", "world-evil"],
  ["PostQueenBee", "queen-bee"],
  ["PostSkeletron", "skeletron"],
  ["PostDeerclops", "deerclops"],
  ["HardmodeEntry", "wall-of-flesh"],
  ["PostQueenSlime", "queen-slime"],
  ["PostOneMechBoss", "destroyer"],
  ["PostThreeMechBosses", "skeletron-prime"],
  ["PostPlantera", "plantera"],
  ["PostDukeFishron", "duke-fishron"],
  ["PostEmpressOfLight", "empress-of-light"],
  ["PostGolem", "golem"],
  ["PostCelestialPillars", "lunatic-cultist"],
  ["PostMoonLord", "moon-lord"]
]);

export function resolveSnapshotStageIndex(record, stages, label = "snapshot record") {
  const originalIndex = record.earliestStageIndex ?? -1;
  if (originalIndex < 0) return originalIndex;

  if (record.earliestStageId) {
    let stageIdIndex = stages.findIndex(stage => stage.id === record.earliestStageId);
    if (stageIdIndex < 0) {
      const alias = LEGACY_VANILLA_STAGE_ID_ALIASES.get(record.earliestStageId);
      stageIdIndex = alias
        ? stages.findIndex(stage => stage.id === alias)
        : -1;
    }
    if (stageIdIndex < 0) {
      throw new Error(
        `${label} references missing stage id '${record.earliestStageId}'. Re-export the snapshot.`);
    }
    return stageIdIndex;
  }

  const stageName = normalizeStageName(record.earliestStageName);
  if (!stageName) {
    if (originalIndex < stages.length) return originalIndex;
    throw new Error(`${label} has no stable stage id or name. Re-export the snapshot.`);
  }

  const matchingIndexes = stages
    .map((stage, index) => ({ stage, index }))
    .filter(({ stage }) => getStageNames(stage).some(name => normalizeStageName(name) === stageName))
    .map(({ index }) => index);
  if (matchingIndexes.length === 0
      && originalIndex < stages.length
      && stages.every(stage => getStageNames(stage).length === 0)) {
    return originalIndex;
  }
  if (matchingIndexes.length !== 1) {
    throw new Error(
      `${label} stage '${record.earliestStageName}' matched ${matchingIndexes.length} profile stages. `
      + "Re-export the snapshot.");
  }

  return matchingIndexes[0];
}

function getStageNames(stage) {
  if (typeof stage.name === "string") return [stage.name];
  return Object.values(stage.name ?? {}).filter(value => typeof value === "string");
}

function normalizeStageName(value) {
  return typeof value === "string" ? value.trim().toLocaleLowerCase() : "";
}
