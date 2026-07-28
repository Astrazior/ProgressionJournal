import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { runSafeBuild } from "./BuildModProfilesSafely.mjs";
import {
  captureProfileState,
  compareProfileStates,
  createRegressionReport,
  renderRegressionMarkdown
} from "./ProfileRegressionGuard.mjs";

const stages = [
  { id: "start", name: { "en-US": "Start" } },
  { id: "eye-of-cthulhu", name: { "en-US": "Eye of Cthulhu" } },
  { id: "wall-of-flesh", name: { "en-US": "Wall of Flesh" } }
];

function reference(id) {
  const separator = id.indexOf("/");
  return {
    mod: id.slice(0, separator),
    item: id.slice(separator + 1),
    displayName: id.slice(separator + 1)
  };
}

function defaultSourceCoverage() {
  return {
    observed: [{
      source: "TestMod/Merchant",
      kinds: ["shop"],
      availabilityKind: "town",
      earliestStageIndex: 0
    }],
    declared: [],
    uncovered: [],
    observedSpawnCount: 0,
    observedTownCount: 1
  };
}

function defaultAcquisitions() {
  return {
    recipes: [],
    drops: [],
    shops: [{
      npc: "TestMod/Merchant",
      shop: "Shop",
      item: "TestMod/GuardedItem",
      conditions: [],
      observed: true,
      earliestStageIndex: 0,
      earliestStageId: "start"
    }],
    fishing: [],
    shimmerTransforms: []
  };
}

function writeState(directory, options = {}) {
  const profileItems = options.profileItems ?? [{
    id: "TestMod/GuardedItem",
    stage: "start",
    via: "shop:TestMod/Merchant"
  }];
  const auditItems = options.auditItems ?? profileItems.map(item => ({
    id: item.id,
    name: reference(item.id).displayName,
    mod: reference(item.id).mod,
    contentItem: true,
    status: item.status ?? "equipment",
    stage: item.stage,
    via: item.via,
    reason: "Included in the generated profile."
  }));
  const sourceCoverage = options.sourceCoverage ?? defaultSourceCoverage();
  const acquisitions = options.acquisitions ?? defaultAcquisitions();
  const reviewIssues = options.reviewIssues ?? [];
  const unresolved = auditItems.filter(item => item.status === "unresolved-availability").length;
  const unavailable = auditItems.filter(item => item.status === "unavailable-combat").length;
  const noPath = auditItems.filter(item => item.status === "no-acquisition-path").length;
  const paths = Object.fromEntries(profileItems.map(item => [item.id, {
    stage: item.stage,
    via: item.via
  }]));

  fs.mkdirSync(directory, { recursive: true });
  fs.writeFileSync(path.join(directory, "profile.json"), `${JSON.stringify({
    format: "ProgressionJournalProfile",
    version: 1,
    id: "test-profile",
    stages,
    entries: profileItems.map(item => ({
      key: item.id,
      category: "Weapon",
      classes: ["melee"],
      itemGroups: [[reference(item.id)]],
      evaluations: [{ stageId: item.stage }]
    })),
    combatBuffs: []
  }, null, 2)}\n`);
  fs.writeFileSync(path.join(directory, "item-audit.json"), `${JSON.stringify({
    format: "ProgressionJournalItemAudit",
    version: 1,
    targetMod: "TestMod",
    profileId: "test-profile",
    generatedAtUtc: "2026-07-28T00:00:00.000Z",
    snapshotGeneratedAtUtc: "2026-07-28T00:00:00.000Z",
    summary: {
      snapshotItems: auditItems.length,
      contentItems: auditItems.length,
      profileItemReferences: profileItems.length,
      unresolvedAvailability: unresolved,
      unavailableCombat: unavailable,
      noAcquisitionPath: noPath
    },
    items: auditItems
  }, null, 2)}\n`);
  fs.writeFileSync(path.join(directory, "review.json"), `${JSON.stringify({
    format: "ProgressionJournalReview",
    version: 1,
    summary: { total: reviewIssues.length },
    issues: reviewIssues
  }, null, 2)}\n`);
  fs.writeFileSync(path.join(directory, "report.json"), `${JSON.stringify({
    format: "ProgressionJournalModProfileReport",
    version: 1,
    targetMod: "TestMod",
    profileId: "test-profile",
    generatedAtUtc: "2026-07-28T00:00:00.000Z",
    snapshot: { generatedAtUtc: "2026-07-28T00:00:00.000Z" },
    audit: {
      errors: options.auditErrors ?? [],
      warnings: options.auditWarnings ?? [],
      sourceCoverage
    },
    generation: {
      paths,
      unresolvedAvailabilityItems: options.unresolvedAvailabilityItems ?? [],
      unavailableCombatItems: [],
      excludedItems: []
    }
  }, null, 2)}\n`);
  fs.writeFileSync(path.join(directory, "knowledge.json"), `${JSON.stringify({
    format: "ProgressionJournalKnowledge",
    version: 1,
    acquisitions,
    summary: {
      items: auditItems.length,
      recipes: acquisitions.recipes.length,
      drops: acquisitions.drops.length,
      shops: acquisitions.shops.length,
      fishing: acquisitions.fishing.length,
      npcAvailability: [
        ...(sourceCoverage.observed ?? []),
        ...(sourceCoverage.declared ?? []),
        ...(sourceCoverage.uncovered ?? [])
      ].length
    }
  }, null, 2)}\n`);
}

function compareDirectories(beforeDirectory, afterDirectory) {
  return compareProfileStates(
    "TestMod",
    captureProfileState(beforeDirectory),
    captureProfileState(afterDirectory));
}

const temporaryRoot = fs.mkdtempSync(path.join(os.tmpdir(), "pj-profile-guard-test-"));
try {
  const beforeDirectory = path.join(temporaryRoot, "before");
  const identicalDirectory = path.join(temporaryRoot, "identical");
  writeState(beforeDirectory);
  writeState(identicalDirectory);
  const identical = compareDirectories(beforeDirectory, identicalDirectory);
  assert.equal(identical.blocked, false);
  assert.equal(identical.itemChanges.length, 0);

  const legacyEvidenceDirectory = path.join(temporaryRoot, "legacy-evidence");
  const enrichedEvidenceDirectory = path.join(temporaryRoot, "enriched-evidence");
  const legacyEvidence = structuredClone(defaultAcquisitions());
  legacyEvidence.shops[0].conditions = [{
    type: "Terraria.Condition",
    description: "Available after a machine-readable condition."
  }];
  const enrichedEvidence = structuredClone(legacyEvidence);
  enrichedEvidence.shops[0].conditions[0] = {
    ...enrichedEvidence.shops[0].conditions[0],
    key: "Conditions.InHardmode",
    facts: [{ kind: "item-owned", item: "Terraria/GuideVoodooDoll" }]
  };
  writeState(legacyEvidenceDirectory, { acquisitions: legacyEvidence });
  writeState(enrichedEvidenceDirectory, { acquisitions: enrichedEvidence });
  const evidenceEnrichment = compareDirectories(
    legacyEvidenceDirectory,
    enrichedEvidenceDirectory);
  assert.equal(evidenceEnrichment.blocked, false);
  assert.equal(
    evidenceEnrichment.acquisitionChanges[0].kind,
    "acquisition-evidence-enriched");

  const evidenceRegression = compareDirectories(
    enrichedEvidenceDirectory,
    legacyEvidenceDirectory);
  assert.equal(evidenceRegression.blocked, true);
  assert.equal(
    evidenceRegression.acquisitionChanges[0].kind,
    "acquisition-evidence-regressed");

  const quantityBaselineDirectory = path.join(temporaryRoot, "quantity-baseline");
  const quantityChangedDirectory = path.join(temporaryRoot, "quantity-changed");
  const quantityBaselineAcquisitions = structuredClone(defaultAcquisitions());
  quantityBaselineAcquisitions.shops = [];
  quantityBaselineAcquisitions.drops = [{
    sourceType: "npc",
    source: "TestMod/Monster",
    item: "TestMod/GuardedItem",
    rate: 0.5,
    stackMin: 1,
    stackMax: 2,
    conditions: []
  }];
  const quantityChangedAcquisitions = structuredClone(quantityBaselineAcquisitions);
  quantityChangedAcquisitions.drops[0].rate = 0.25;
  quantityChangedAcquisitions.drops[0].stackMax = 1;
  writeState(quantityBaselineDirectory, { acquisitions: quantityBaselineAcquisitions });
  writeState(quantityChangedDirectory, { acquisitions: quantityChangedAcquisitions });
  const quantityChange = compareDirectories(
    quantityBaselineDirectory,
    quantityChangedDirectory);
  assert.equal(quantityChange.blocked, false);
  assert.equal(
    quantityChange.acquisitionChanges[0].kind,
    "acquisition-metadata-changed");

  const shopMetadataDirectory = path.join(temporaryRoot, "shop-metadata");
  const shopMetadataAcquisitions = structuredClone(defaultAcquisitions());
  shopMetadataAcquisitions.shops[0] = {
    ...shopMetadataAcquisitions.shops[0],
    observed: false,
    earliestStageIndex: -1,
    earliestStageId: "",
    earliestStageName: "Localized stage name"
  };
  writeState(shopMetadataDirectory, { acquisitions: shopMetadataAcquisitions });
  const shopMetadataChange = compareDirectories(beforeDirectory, shopMetadataDirectory);
  assert.equal(shopMetadataChange.blocked, false);
  assert.equal(
    shopMetadataChange.acquisitionChanges[0].kind,
    "acquisition-metadata-changed");

  const duplicateDropDirectory = path.join(temporaryRoot, "duplicate-drop");
  const singleDropDirectory = path.join(temporaryRoot, "single-drop");
  const duplicateDropAcquisitions = structuredClone(defaultAcquisitions());
  duplicateDropAcquisitions.shops = [];
  duplicateDropAcquisitions.drops = [
    quantityBaselineAcquisitions.drops[0],
    structuredClone(quantityBaselineAcquisitions.drops[0])
  ];
  const singleDropAcquisitions = structuredClone(duplicateDropAcquisitions);
  singleDropAcquisitions.drops.pop();
  writeState(duplicateDropDirectory, { acquisitions: duplicateDropAcquisitions });
  writeState(singleDropDirectory, { acquisitions: singleDropAcquisitions });
  const duplicateDropRemoval = compareDirectories(
    duplicateDropDirectory,
    singleDropDirectory);
  assert.equal(duplicateDropRemoval.blocked, false);
  assert.equal(
    duplicateDropRemoval.acquisitionChanges[0].kind,
    "acquisition-duplicates-removed");
  assert.equal(
    duplicateDropRemoval.metricChanges.find(change => change.metric === "drops").blocking,
    false);

  const legacyReviewDirectory = path.join(temporaryRoot, "legacy-review");
  const enrichedReviewDirectory = path.join(temporaryRoot, "enriched-review");
  const legacyReviewIssue = {
    id: "unresolved-condition.legacy",
    kind: "unresolved-condition",
    sourceKind: "shop",
    affected: [{ item: "TestMod/GuardedItem", source: "TestMod/Merchant" }],
    affectedCount: 1,
    conditions: [{
      type: "Terraria.Condition",
      description: "In hardmode"
    }],
    resolution: {
      conditionStages: [{
        conditionDescriptions: ["In hardmode"],
        conditionKeys: [],
        conditionTypes: [],
        sourceIds: ["TestMod/Merchant"],
        sources: ["shop"],
        stageId: "<stage-id>"
      }]
    }
  };
  const enrichedReviewIssue = structuredClone(legacyReviewIssue);
  enrichedReviewIssue.id = "unresolved-condition.enriched";
  enrichedReviewIssue.conditions[0].key = "Conditions.InHardmode";
  enrichedReviewIssue.conditions[0].facts = [];
  enrichedReviewIssue.resolution.conditionStages[0].conditionKeys = [
    "Conditions.InHardmode"
  ];
  writeState(legacyReviewDirectory, { reviewIssues: [legacyReviewIssue] });
  writeState(enrichedReviewDirectory, { reviewIssues: [enrichedReviewIssue] });
  const reviewEvidenceEnrichment = compareDirectories(
    legacyReviewDirectory,
    enrichedReviewDirectory);
  assert.equal(reviewEvidenceEnrichment.blocked, false);
  assert.equal(reviewEvidenceEnrichment.reviewChanges.added.length, 0);

  const removedDirectory = path.join(temporaryRoot, "removed");
  writeState(removedDirectory, {
    profileItems: [],
    auditItems: [{
      id: "TestMod/GuardedItem",
      name: "GuardedItem",
      mod: "TestMod",
      contentItem: true,
      status: "unresolved-availability",
      stage: "",
      via: "",
      reason: "Combat item has no proven acquisition source."
    }],
    acquisitions: {
      ...defaultAcquisitions(),
      shops: []
    },
    reviewIssues: [{
      id: "unassigned-combat-item.guarded",
      kind: "unassigned-combat-item",
      item: "TestMod/GuardedItem",
      evidence: {
        shops: [{
          source: "TestMod/Merchant",
          conditions: [{ type: "Test.Condition", description: "Observed condition disappeared" }]
        }]
      }
    }],
    unresolvedAvailabilityItems: [{
      item: "TestMod/GuardedItem",
      displayName: "GuardedItem"
    }]
  });
  const removed = compareDirectories(beforeDirectory, removedDirectory);
  assert.equal(removed.blocked, true);
  assert.equal(removed.summary.removedItems, 1);
  assert(removed.blockingReasons.some(reason => reason.kind === "removed-from-profile"));
  assert(removed.blockingReasons.some(reason => reason.kind === "acquisition-removed"));
  const removedMarkdown = renderRegressionMarkdown(createRegressionReport([removed]));
  assert.match(removedMarkdown, /Combat item has no proven acquisition source/u);
  assert.match(removedMarkdown, /Observed condition disappeared/u);
  assert.match(removedMarkdown, /shop TestMod\/Merchant\/Shop/u);

  const laterDirectory = path.join(temporaryRoot, "later");
  writeState(laterDirectory, {
    profileItems: [{
      id: "TestMod/GuardedItem",
      stage: "wall-of-flesh",
      via: "shop:TestMod/Merchant"
    }],
    auditItems: [{
      id: "TestMod/GuardedItem",
      name: "GuardedItem",
      mod: "TestMod",
      contentItem: true,
      status: "equipment",
      stage: "wall-of-flesh",
      via: "shop:TestMod/Merchant",
      reason: "Included in the generated profile."
    }]
  });
  const later = compareDirectories(beforeDirectory, laterDirectory);
  assert.equal(later.blocked, true);
  assert.equal(later.itemChanges[0].stageDirection, "later");

  const earlierDirectory = path.join(temporaryRoot, "earlier");
  writeState(earlierDirectory, {
    profileItems: [{
      id: "TestMod/GuardedItem",
      stage: "start",
      via: "shop:TestMod/Merchant"
    }]
  });
  const lateBaselineDirectory = path.join(temporaryRoot, "late-baseline");
  writeState(lateBaselineDirectory, {
    profileItems: [{
      id: "TestMod/GuardedItem",
      stage: "wall-of-flesh",
      via: "shop:TestMod/Merchant"
    }]
  });
  const earlier = compareDirectories(lateBaselineDirectory, earlierDirectory);
  assert.equal(earlier.blocked, true);
  assert.equal(earlier.itemChanges[0].stageDirection, "earlier");

  const downgradedSourceDirectory = path.join(temporaryRoot, "downgraded-source");
  writeState(downgradedSourceDirectory, {
    sourceCoverage: {
      observed: [],
      declared: [],
      uncovered: [{
        source: "TestMod/Merchant",
        kinds: ["shop"],
        availabilityKind: "town",
        earliestStageIndex: -1
      }],
      observedSpawnCount: 0,
      observedTownCount: 0
    }
  });
  const downgradedSource = compareDirectories(beforeDirectory, downgradedSourceDirectory);
  assert.equal(downgradedSource.blocked, true);
  assert(downgradedSource.blockingReasons.some(reason =>
    reason.kind === "source-changed" && reason.subject === "TestMod/Merchant"));

  const lostShopOnlyDirectory = path.join(temporaryRoot, "lost-shop-only");
  writeState(lostShopOnlyDirectory, {
    acquisitions: {
      ...defaultAcquisitions(),
      shops: []
    }
  });
  const lostShopOnly = compareDirectories(beforeDirectory, lostShopOnlyDirectory);
  assert.equal(lostShopOnly.blocked, true);
  assert(lostShopOnly.blockingReasons.some(reason =>
    reason.kind === "acquisition-removed"
    && reason.subject === "TestMod/GuardedItem"));
  assert.equal(lostShopOnly.summary.removedItems, 0);

  const addedDirectory = path.join(temporaryRoot, "added");
  writeState(addedDirectory, {
    profileItems: [
      {
        id: "TestMod/GuardedItem",
        stage: "start",
        via: "shop:TestMod/Merchant"
      },
      {
        id: "TestMod/NewItem",
        stage: "start",
        via: "initial"
      }
    ],
    auditItems: [
      {
        id: "TestMod/GuardedItem",
        name: "GuardedItem",
        mod: "TestMod",
        contentItem: true,
        status: "equipment",
        stage: "start",
        via: "shop:TestMod/Merchant",
        reason: "Included in the generated profile."
      },
      {
        id: "TestMod/NewItem",
        name: "NewItem",
        mod: "TestMod",
        contentItem: true,
        status: "equipment",
        stage: "start",
        via: "initial",
        reason: "Included in the generated profile."
      }
    ]
  });
  const added = compareDirectories(beforeDirectory, addedDirectory);
  assert.equal(added.blocked, false);
  assert.equal(added.summary.addedItems, 1);

  const safeRoot = path.join(temporaryRoot, "safe-root");
  const safeProfileDirectory = path.join(safeRoot, "Profiles", "Mods", "TestMod");
  fs.mkdirSync(safeProfileDirectory, { recursive: true });
  fs.writeFileSync(path.join(safeProfileDirectory, "support.json"), "{}\n");
  fs.writeFileSync(path.join(safeProfileDirectory, "snapshot.json"), "{}\n");
  writeState(safeProfileDirectory);
  const originalProfile = fs.readFileSync(path.join(safeProfileDirectory, "profile.json"));
  assert.throws(() => runSafeBuild(["TestMod"], {
    root: safeRoot,
    buildModProfile: () => {
      throw new Error("Synthetic candidate build failure");
    }
  }), /Synthetic candidate build failure/u);
  assert.deepEqual(
    fs.readFileSync(path.join(safeProfileDirectory, "profile.json")),
    originalProfile);

  let resetProbeEvidenceForwarded = false;
  const resetDryRun = runSafeBuild(
    ["TestMod", "--dry-run", "--reset-probe-evidence"],
    {
      root: safeRoot,
      buildModProfile: (_modName, options) => {
        resetProbeEvidenceForwarded = options.resetProbeEvidence === true;
        writeState(options.outputDirectory);
      }
    });
  assert.equal(resetDryRun.accepted, true);
  assert.equal(resetDryRun.dryRun, true);
  assert.equal(resetProbeEvidenceForwarded, true);
  assert.deepEqual(
    fs.readFileSync(path.join(safeProfileDirectory, "profile.json")),
    originalProfile);

  const fakeBuild = (_modName, options) => writeState(options.outputDirectory, {
    profileItems: [],
    auditItems: [{
      id: "TestMod/GuardedItem",
      name: "GuardedItem",
      mod: "TestMod",
      contentItem: true,
      status: "unresolved-availability",
      stage: "",
      via: "",
      reason: "Combat item has no proven acquisition source."
    }],
    acquisitions: {
      ...defaultAcquisitions(),
      shops: []
    },
    reviewIssues: [{
      id: "unassigned-combat-item.guarded",
      kind: "unassigned-combat-item",
      item: "TestMod/GuardedItem"
    }]
  });

  const blockedBuild = runSafeBuild(["TestMod"], {
    root: safeRoot,
    buildModProfile: fakeBuild
  });
  assert.equal(blockedBuild.accepted, false);
  assert.deepEqual(
    fs.readFileSync(path.join(safeProfileDirectory, "profile.json")),
    originalProfile);
  assert(fs.existsSync(blockedBuild.reportFiles.markdownFile));
  assert(fs.existsSync(path.join(blockedBuild.candidateRoot, "TestMod", "profile.json")));

  const acceptedBuild = runSafeBuild(["TestMod", "--accept-changes"], {
    root: safeRoot,
    buildModProfile: fakeBuild
  });
  assert.equal(acceptedBuild.accepted, true);
  const acceptedProfile = JSON.parse(
    fs.readFileSync(path.join(safeProfileDirectory, "profile.json"), "utf8"));
  assert.equal(acceptedProfile.entries.length, 0);

  console.log("Profile regression guard tests passed.");
} finally {
  fs.rmSync(temporaryRoot, { recursive: true, force: true });
}
