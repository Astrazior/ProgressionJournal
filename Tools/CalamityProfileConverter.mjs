import fs from "node:fs";
import path from "node:path";

const WIKI_API = "https://calamitymod.wiki.gg/api.php";
const WIKI_PAGES = [
  "Guide:Class_setups/Pre-Hardmode/data",
  "Guide:Class_setups/Hardmode/data",
  "Guide:Class_setups/Post-Moon_Lord/data",
];

const args = parseArgs(process.argv.slice(2));
const sourceRoot = path.resolve(args["calamity-source"] ?? "");
const outputPath = path.resolve(args.output ?? "Profiles/Wiki/calamity-recommendations.json");
const reportPath = path.resolve(args.report ?? "Profiles/Reports/calamity-wiki-report.json");

if (!sourceRoot || !fs.existsSync(sourceRoot)) {
  throw new Error("Pass --calamity-source <CalamityModPublic checkout>.");
}

const itemIndex = buildItemIndex(sourceRoot);
const armorSetIndex = buildArmorSetIndex(sourceRoot, itemIndex.byInternalName);
const pages = [];

for (const pageTitle of WIKI_PAGES) {
  pages.push(await fetchWikiPage(pageTitle));
}

const parsedRows = pages.flatMap(page => parseClassSetupRows(page.wikitext));
const stageIds = [...new Set(parsedRows.map(row => row.stageId))];
const report = {
  generatedAtUtc: new Date().toISOString(),
  sourcePages: pages.map(page => ({
    title: page.title,
    revisionId: page.revisionId,
  })),
  assumedVanillaItems: [],
  ambiguousArmorHelmets: [],
  skippedRows: [],
};

const entries = [];
const entryKeys = new Set();
const entrySignatures = new Set();

for (const row of parsedRows) {
  const category = mapCategory(row.itemType);
  if (category === null) {
    report.skippedRows.push({ ...row, reason: "Unsupported category" });
    continue;
  }

  const classes = mapClassIds(row.classId);
  const itemGroups = [];
  for (const wikiName of row.itemNames) {
    const resolved = resolveWikiItem(wikiName, itemIndex, armorSetIndex);
    if (resolved.kind === "armor-set") {
      const slotGroups = new Map();
      const selectedReferences = selectArmorReferences(
        resolved.references,
        classes,
        row,
        wikiName,
        report,
      );
      for (const reference of selectedReferences) {
        const slot = reference.slot;
        if (!slotGroups.has(slot)) slotGroups.set(slot, []);
        const { slot: _, classIds: __, ...storedReference } = reference;
        slotGroups.get(slot).push(storedReference);
      }
      for (const references of [...slotGroups.entries()]
        .sort(([left], [right]) => left - right)
        .map(([, references]) => references)) {
        itemGroups.push(references);
      }
      continue;
    }

    itemGroups.push(resolved.references);
    if (resolved.kind === "assumed-vanilla") {
      report.assumedVanillaItems.push(wikiName);
    }
  }

  if (itemGroups.length === 0) {
    report.skippedRows.push({ ...row, reason: "No item references" });
    continue;
  }

  const baseKey = normalizeId(
    `${row.classId}-${row.stageId}-${category}-${itemGroups
      .flat()
      .map(reference => `${reference.mod}-${reference.item}`)
      .join("-")}`,
  );
  const signature = JSON.stringify({
    category,
    classes: [...classes].sort(),
    stageId: row.stageId,
    itemGroups: itemGroups.map(group => group
      .map(reference => `${reference.mod}/${reference.item}`.toLowerCase())
      .sort()),
  });
  if (entrySignatures.has(signature)) {
    continue;
  }
  entrySignatures.add(signature);

  const key = uniqueKey(baseKey, entryKeys);
  entries.push({
    key,
    category,
    classes,
    itemGroups,
    evaluations: [{ stageId: row.stageId, tier: "FromGuide" }],
    isSupportWeapon: /support|whip|sentry/i.test(row.itemType),
  });
}

report.assumedVanillaItems = [...new Set(report.assumedVanillaItems)]
  .sort((left, right) => left.localeCompare(right));

const profile = {
  format: "ProgressionJournalProfile",
  version: 1,
  id: "source.calamity-wiki",
  name: "Calamity Wiki",
  author: "Calamity Wiki contributors",
  profileVersion: getCalamityVersion(sourceRoot),
  readOnly: true,
  sourceUrl: "https://calamitymod.wiki.gg/wiki/Guide:Class_setups",
  sourceRevision: pages.map(page => page.revisionId).join(","),
  generatedAtUtc: report.generatedAtUtc,
  requiredMods: [
    {
      name: "CalamityMod",
      version: getCalamityVersion(sourceRoot),
    },
  ],
  classes: buildClasses(parsedRows),
  stages: stageIds.map(createStage),
  entries,
};

validateProfile(profile);
fs.mkdirSync(path.dirname(outputPath), { recursive: true });
fs.mkdirSync(path.dirname(reportPath), { recursive: true });
fs.writeFileSync(outputPath, `${JSON.stringify(profile, null, 2)}\n`, "utf8");
fs.writeFileSync(reportPath, `${JSON.stringify(report, null, 2)}\n`, "utf8");

console.log(`Wrote ${entries.length} entries to ${outputPath}`);
console.log(`Assumed vanilla item names: ${report.assumedVanillaItems.length}`);
console.log(`Ambiguous armor helmet selections: ${report.ambiguousArmorHelmets.length}`);
console.log(`Skipped rows: ${report.skippedRows.length}`);

function validateProfile(profile) {
  const classIds = new Set(profile.classes.map(value => value.id));
  const stageIds = new Set(profile.stages.map(value => value.id));
  const entryKeys = new Set();

  for (const entry of profile.entries) {
    if (!entry.key || entryKeys.has(entry.key)) {
      throw new Error(`Duplicate or empty entry key '${entry.key}'.`);
    }
    entryKeys.add(entry.key);

    if (entry.classes.length === 0 || entry.classes.some(value => !classIds.has(value))) {
      throw new Error(`Entry '${entry.key}' references an unknown class.`);
    }
    if (entry.evaluations.length === 0
      || entry.evaluations.some(value => !stageIds.has(value.stageId))) {
      throw new Error(`Entry '${entry.key}' references an unknown stage.`);
    }
    if (entry.itemGroups.length === 0
      || entry.itemGroups.some(group => group.length === 0)) {
      throw new Error(`Entry '${entry.key}' has no item references.`);
    }
  }
}

function parseArgs(values) {
  const result = {};
  for (let index = 0; index < values.length; index += 2) {
    const key = values[index]?.replace(/^--/, "");
    if (!key || values[index + 1] === undefined) {
      throw new Error(`Invalid argument near '${values[index] ?? ""}'.`);
    }
    result[key] = values[index + 1];
  }
  return result;
}

async function fetchWikiPage(title) {
  const url = new URL(WIKI_API);
  url.searchParams.set("action", "parse");
  url.searchParams.set("page", title);
  url.searchParams.set("prop", "wikitext|revid");
  url.searchParams.set("format", "json");
  url.searchParams.set("formatversion", "2");

  const response = await fetch(url, {
    headers: {
      "user-agent": "ProgressionJournal/3.0 profile converter",
    },
  });
  if (!response.ok) {
    throw new Error(`Wiki request failed for '${title}': HTTP ${response.status}`);
  }

  const payload = await response.json();
  if (!payload.parse?.wikitext) {
    throw new Error(`Wiki response for '${title}' did not contain wikitext.`);
  }

  return {
    title,
    revisionId: String(payload.parse.revid ?? ""),
    wikitext: payload.parse.wikitext,
  };
}

function parseClassSetupRows(wikitext) {
  const rows = [];
  for (const template of findBalancedTemplates(wikitext, "{{ClassSetupsItem")) {
    const fields = splitTopLevel(template.slice(2, -2), "|");
    if (fields.length < 5) {
      continue;
    }

    const itemNames = [...fields[4].matchAll(/\{\{\s*item\s*\|\s*([^|}]+?)(?:\||\}\})/gi)]
      .map(match => cleanWikiName(match[1]))
      .filter(Boolean);
    if (itemNames.length === 0) {
      continue;
    }

    rows.push({
      classId: fields[1].trim().toLowerCase(),
      stageId: fields[2].trim().toLowerCase(),
      itemType: fields[3].trim(),
      itemNames,
    });
  }
  return rows;
}

function findBalancedTemplates(text, prefix) {
  const result = [];
  let cursor = 0;

  while (cursor < text.length) {
    const start = text.indexOf(prefix, cursor);
    if (start < 0) {
      break;
    }

    let depth = 0;
    let end = start;
    for (; end < text.length - 1; end += 1) {
      const pair = text.slice(end, end + 2);
      if (pair === "{{") {
        depth += 1;
        end += 1;
        continue;
      }
      if (pair === "}}") {
        depth -= 1;
        end += 1;
        if (depth === 0) {
          result.push(text.slice(start, end + 1));
          break;
        }
      }
    }

    cursor = Math.max(start + prefix.length, end + 1);
  }

  return result;
}

function splitTopLevel(text, separator) {
  const parts = [];
  let depth = 0;
  let start = 0;

  for (let index = 0; index < text.length; index += 1) {
    const pair = text.slice(index, index + 2);
    if (pair === "{{" || pair === "[[") {
      depth += 1;
      index += 1;
      continue;
    }
    if (pair === "}}" || pair === "]]") {
      depth = Math.max(0, depth - 1);
      index += 1;
      continue;
    }
    if (text[index] === separator && depth === 0) {
      parts.push(text.slice(start, index));
      start = index + 1;
    }
  }

  parts.push(text.slice(start));
  return parts;
}

function buildItemIndex(root) {
  const localizationRoot = path.join(root, "Localization", "en-US");
  const byDisplayName = new Map();
  const byInternalName = new Map();

  for (const filePath of walkFiles(localizationRoot, file => file.endsWith(".hjson"))) {
    let currentKey = null;
    for (const line of fs.readFileSync(filePath, "utf8").split(/\r?\n/)) {
      const keyMatch = line.match(/^([A-Za-z0-9_]+):\s*\{/);
      if (keyMatch) {
        currentKey = keyMatch[1];
        byInternalName.set(normalizeName(currentKey), currentKey);
        continue;
      }

      const displayMatch = line.match(/^\s+DisplayName:\s*(.+?)\s*$/);
      if (currentKey && displayMatch) {
        const displayName = stripQuotes(displayMatch[1]);
        const normalized = normalizeName(displayName);
        if (!byDisplayName.has(normalized)) {
          byDisplayName.set(normalized, []);
        }
        byDisplayName.get(normalized).push(currentKey);
      }
    }
  }

  return { byDisplayName, byInternalName };
}

function buildArmorSetIndex(root, internalNames) {
  const armorRoot = path.join(root, "Items", "Armor");
  const result = new Map();

  for (const directory of walkDirectories(armorRoot)) {
    const directoryName = path.basename(directory);
    const setKey = normalizeName(`${directoryName} armor`);
    const items = fs.readdirSync(directory, { withFileTypes: true })
      .filter(entry => entry.isFile() && entry.name.endsWith(".cs"))
      .flatMap(entry => {
        const source = fs.readFileSync(path.join(directory, entry.name), "utf8");
        if (!source.includes("AutoloadEquip")) {
          return [];
        }
        return [...source.matchAll(
          /AutoloadEquip\s*\(\s*EquipType\.(Head|Body|Legs)[^)]*\)\s*\][\s\S]{0,500}?\bpublic\s+(?:sealed\s+)?class\s+([A-Za-z0-9_]+)/g,
        )].map(match => ({
          slot: { Head: 0, Body: 1, Legs: 2 }[match[1]],
          item: match[2],
          classIds: match[1] === "Head"
            ? detectArmorClassIds(match[2], extractClassBody(source, match[2]))
            : [],
        }));
      })
      .filter(piece => internalNames.has(normalizeName(piece.item)))
      .filter((piece, index, values) => values.findIndex(value => value.item === piece.item) === index)
      .sort((left, right) => left.slot - right.slot);
    if (items.length >= 2) {
      result.set(setKey, items);
    }
  }

  return result;
}

function resolveWikiItem(wikiName, itemIndex, armorSetIndex) {
  const normalized = normalizeName(wikiName);
  const armorPieces = armorSetIndex.get(normalized);
  if (armorPieces) {
    return {
      kind: "armor-set",
      references: armorPieces.map(piece => ({
        ...createReference("CalamityMod", piece.item, wikiName),
        slot: piece.slot,
        classIds: piece.classIds,
      })),
    };
  }

  const localizedMatches = itemIndex.byDisplayName.get(normalized);
  if (localizedMatches?.length) {
    return {
      kind: "calamity",
      references: [createReference("CalamityMod", localizedMatches[0], wikiName)],
    };
  }

  const internalMatch = itemIndex.byInternalName.get(normalized);
  if (internalMatch) {
    return {
      kind: "calamity",
      references: [createReference("CalamityMod", internalMatch, wikiName)],
    };
  }

  if (/armor|armour|set/i.test(wikiName)) {
    const inferredArmorPieces = [...armorSetIndex.entries()]
      .find(([key]) => {
        const setName = key.replace(/armor$/, "");
        return setName.length >= 4 && normalized.includes(setName);
      })?.[1];
    if (inferredArmorPieces) {
      return {
        kind: "armor-set",
        references: inferredArmorPieces.map(piece => ({
          ...createReference("CalamityMod", piece.item, wikiName),
          slot: piece.slot,
          classIds: piece.classIds,
        })),
      };
    }
  }

  return {
    kind: "assumed-vanilla",
    references: [createReference("Terraria", wikiName, wikiName)],
  };
}

function createReference(mod, item, displayName) {
  return { mod, item, displayName };
}

function selectArmorReferences(references, classes, row, wikiName, report) {
  const headReferences = references.filter(reference => reference.slot === 0);
  if (headReferences.length <= 1 || classes.length !== 1) {
    return references;
  }

  const classId = classes[0];
  const matchingHeads = headReferences
    .filter(reference => reference.classIds.includes(classId));
  if (matchingHeads.length > 0) {
    return references.filter(reference => reference.slot !== 0
      || matchingHeads.includes(reference));
  }

  report.ambiguousArmorHelmets.push({
    classId,
    stageId: row.stageId,
    wikiName,
    candidates: headReferences.map(reference => ({
      item: reference.item,
      detectedClasses: reference.classIds,
    })),
  });
  return references;
}

function detectArmorClassIds(itemName, source) {
  const nameSignals = {
    melee: /melee|warrior/i,
    ranged: /ranged|ranger/i,
    magic: /magic|mage/i,
    summoner: /summon/i,
    rogue: /rogue|stealth/i,
  };
  const nameMatches = Object.entries(nameSignals)
    .filter(([, pattern]) => pattern.test(itemName))
    .map(([classId]) => classId);
  if (nameMatches.length > 0) {
    return nameMatches;
  }

  const sourceSignals = {
    melee: /MeleeDamageClass|DamageClass\.Melee|\bmelee(?:Damage|Crit|Speed)\b/i,
    ranged: /RangedDamageClass|DamageClass\.Ranged|\branged(?:Damage|Crit)\b/i,
    magic: /MagicDamageClass|DamageClass\.Magic|\bmagic(?:Damage|Crit)\b/i,
    summoner: /SummonDamageClass|DamageClass\.Summon|\bsummon(?:Damage|Crit)\b|maxMinions/i,
    rogue: /RogueDamageClass|\brogue(?:Damage|Crit|Velocity)\b|stealth/i,
  };
  return Object.entries(sourceSignals)
    .filter(([, pattern]) => pattern.test(source))
    .map(([classId]) => classId);
}

function extractClassBody(source, className) {
  const classMatch = new RegExp(`\\bclass\\s+${className}\\b`).exec(source);
  if (!classMatch) return "";

  const openingBrace = source.indexOf("{", classMatch.index);
  if (openingBrace < 0) return "";

  let depth = 0;
  for (let index = openingBrace; index < source.length; index++) {
    if (source[index] === "{") depth++;
    if (source[index] === "}") {
      depth--;
      if (depth === 0) {
        return source.slice(openingBrace, index + 1);
      }
    }
  }
  return source.slice(openingBrace);
}

function buildClasses(rows) {
  const found = new Set(rows.flatMap(row => mapClassIds(row.classId)));
  const definitions = [
    ["melee", "Melee", "MeleeDamageClass"],
    ["ranged", "Ranged", "RangedDamageClass"],
    ["magic", "Magic", "MagicDamageClass"],
    ["summoner", "Summoner", "SummonDamageClass"],
    ["rogue", "Rogue", "RogueDamageClass"],
  ];

  return definitions
    .filter(([id]) => found.has(id))
    .map(([id, name, damageClassName]) => ({
      id,
      name,
      iconMod: "",
      iconItem: "",
      damageClassNames: [damageClassName],
    }));
}

function createStage(stageId) {
  const definition = {
    id: stageId,
    name: stageDisplayName(stageId),
    iconMod: "",
    iconNpc: "",
    accessorySlots: hardmodeStageIds().has(stageId) ? 6 : 5,
    unlock: { type: "always", key: "", mod: "", npc: "" },
  };

  const vanillaUnlocks = {
    "pre-evil1": "PostEyeOfCthulhu",
    "pre-evil2": "PostWorldEvil",
    "pre-skeletron": "PostWorldEvil",
    "pre-wof": "PostSkeletron",
    "pre-mech": "HardmodeEntry",
    "post-mech1": "PostOneMechBoss",
    "post-mech2": "PostOneMechBoss",
    "pre-plantera": "PostThreeMechBosses",
    "pre-golem": "PostPlantera",
    "post-golem": "PostGolem",
    "pre-lunar": "PostGolem",
    "pre-moonlord": "PostCelestialPillars",
    "pre-provi": "PostMoonLord",
  };
  const modFlags = {
    "pre-polter": "downedProvidence",
    "pre-dog": "downedPolterghast",
    "pre-yharon": "downedDoG",
    "pre-scal-exo": "downedYharon",
    "pre-scal": "downedExoMechs",
    "pre-exo": "downedCalamitas",
    endgame: "downedCalamitas,downedExoMechs",
  };

  if (vanillaUnlocks[stageId]) {
    definition.unlock = {
      type: "vanilla-stage",
      key: vanillaUnlocks[stageId],
      mod: "",
      npc: "",
    };
  } else if (modFlags[stageId]) {
    definition.unlock = {
      type: "mod-flag",
      key: modFlags[stageId],
      mod: "CalamityMod",
      npc: "",
    };
  }

  return definition;
}

function stageDisplayName(stageId) {
  const names = {
    "pre-boss": "Pre-Boss",
    "pre-evil1": "Pre-Evil Boss 1",
    "pre-evil2": "Pre-Evil Boss 2",
    "pre-skeletron": "Pre-Skeletron",
    "pre-wof": "Pre-Wall of Flesh",
    "pre-mech": "Pre-Mech Bosses",
    "post-mech1": "Post-Mech Boss 1",
    "post-mech2": "Post-Mech Boss 2",
    "pre-plantera": "Pre-Plantera",
    "pre-golem": "Pre-Golem",
    "post-golem": "Post-Golem",
    "pre-lunar": "Pre-Lunar Events",
    "pre-moonlord": "Pre-Moon Lord",
    "pre-provi": "Pre-Providence",
    "pre-polter": "Pre-Polterghast",
    "pre-dog": "Pre-Devourer of Gods",
    "pre-yharon": "Pre-Yharon",
    "pre-scal-exo": "Pre-Supreme Calamitas / Exo Mechs",
    "pre-scal": "Pre-Supreme Calamitas",
    "pre-exo": "Pre-Exo Mechs",
    endgame: "Endgame",
  };
  return names[stageId] ?? stageId;
}

function hardmodeStageIds() {
  return new Set([
    "pre-mech",
    "post-mech1",
    "post-mech2",
    "pre-plantera",
    "pre-golem",
    "post-golem",
    "pre-lunar",
    "pre-moonlord",
    "pre-provi",
    "pre-polter",
    "pre-dog",
    "pre-yharon",
    "pre-scal-exo",
    "pre-scal",
    "pre-exo",
    "endgame",
  ]);
}

function mapClassIds(value) {
  if (value === "all") {
    return ["melee", "ranged", "magic", "summoner", "rogue"];
  }
  if (value === "all-but-summoner") {
    return ["melee", "ranged", "magic", "rogue"];
  }
  if (value === "all-but-stealth") {
    return ["melee", "ranged", "magic", "summoner"];
  }
  if (value === "all-but-summoner-stealth") {
    return ["melee", "ranged", "magic"];
  }
  return [value === "summon" ? "summoner" : value];
}

function mapCategory(value) {
  if (/^armor$/i.test(value)) return "Armor";
  if (/^accessor/i.test(value)) return "Accessory";
  if (/^buff/i.test(value)) return "Buff";
  if (/^ammo$/i.test(value)) return "Ammunition";
  if (/^support$/i.test(value)) return "Support";
  if (/mount|hook|tool/i.test(value)) return "ClassSpecific";
  if (/weapon|minion|sentr/i.test(value)) return "Weapon";
  return null;
}

function cleanWikiName(value) {
  return value
    .replace(/<!--.*?-->/gs, "")
    .replace(/''+/g, "")
    .trim();
}

function normalizeName(value) {
  return value.toLowerCase().replace(/[^a-z0-9]/g, "");
}

function normalizeId(value) {
  return value.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "");
}

function uniqueKey(baseKey, existingKeys) {
  const safeBase = baseKey || "entry";
  let candidate = safeBase;
  let suffix = 2;
  while (existingKeys.has(candidate)) {
    candidate = `${safeBase}-${suffix++}`;
  }
  existingKeys.add(candidate);
  return candidate;
}

function stripQuotes(value) {
  return value.replace(/^['"]|['"]$/g, "").trim();
}

function armorPieceOrder(value) {
  if (/head|helm|hat|mask|hood|helmet/i.test(value)) return 0;
  if (/body|breast|chest|shirt|plate/i.test(value)) return 1;
  if (/leg|greave|boot|pant/i.test(value)) return 2;
  return 3;
}

function getCalamityVersion(root) {
  const buildText = fs.readFileSync(path.join(root, "build.txt"), "utf8");
  return buildText.match(/^version\s*=\s*(.+)$/m)?.[1]?.trim() ?? "";
}

function* walkFiles(root, predicate) {
  for (const entry of fs.readdirSync(root, { withFileTypes: true })) {
    const fullPath = path.join(root, entry.name);
    if (entry.isDirectory()) {
      yield* walkFiles(fullPath, predicate);
    } else if (predicate(fullPath)) {
      yield fullPath;
    }
  }
}

function* walkDirectories(root) {
  for (const entry of fs.readdirSync(root, { withFileTypes: true })) {
    if (!entry.isDirectory()) continue;
    const fullPath = path.join(root, entry.name);
    yield fullPath;
    yield* walkDirectories(fullPath);
  }
}
