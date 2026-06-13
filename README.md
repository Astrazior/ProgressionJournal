# Progression Journal

Client-side progression helper for Terraria/tModLoader.

Steam Workshop:
https://steamcommunity.com/sharedfiles/filedetails?id=3710545729

## What it does

- Shows equipment first unlocked by each boss or combat event
- Shows combat buffs and utility information
- Shows item acquisition sources: drops, shops, recipes, stations, conditions
- Supports selectable vanilla, bundled, add-on, and user-created progression profiles
- Includes bundled Calamity, Thorium, and Fargo's Souls profiles
- Supports custom classes, stages, unlock conditions, and recommendation tiers

## Project notes

- tModLoader version: `1.4.4`
- Language: C#
- Main curated data is currently stored in `Data/Repositories/`
- Stage logic is in `Data/Catalogs/ProgressionStageCatalog.cs`
- Item source resolution is in `Data/Resolvers/JournalItemSourceResolver.cs`

## Build

Open tModLoader and use `Workshop -> Develop Mods -> Build + Reload`.

## API

External mods can register additional journal entries through `Mod.Call`.

- `GetApiVersion`
- `RegisterEntry`
- `RegisterProfileJson`
- `RegisterUnlockCondition`

Register external entries in `PostSetupContent`.

`RegisterEntry` arguments:

1. `key`
2. `category`
3. `classes`
4. `itemGroups`
5. `evaluations`
6. optional `eventCategory`
7. optional `isSupportWeapon`

Enums can be passed as enum values, names, or integers.

Example:

```csharp
public override void PostSetupContent()
{
    if (!ModLoader.TryGetMod("ProgressionJournal", out Mod journal))
    {
        return;
    }

    journal.Call(
        "RegisterEntry",
        "ExampleMod.ExampleBlade",
        "Weapon",
        "Melee",
        new[] { ItemType<Items.Weapons.ExampleBlade>() },
        new object[] { new object[] { "PostEyeOfCthulhu", "Recommended" } });
}
```

API version 2 can register a complete profile:

```csharp
journal.Call("RegisterProfileJson", profileJson, "ExampleMod");
journal.Call("RegisterUnlockCondition", "ExampleMod/DownedBoss", (Func<bool>)(() => DownedBoss));
```

Profile JSON uses stable string IDs for profiles, classes, stages, and item references.
Old `RegisterEntry` calls remain supported and add entries to `builtin.vanilla`.

## Profile files

User profiles are stored under:

`Documents/My Games/Terraria/tModLoader/Mods/ProgressionJournal/Profiles`

Built-in profiles are read-only. Use `Copy / Edit` in the profile manager before changing one.
Profile builds store `profileId`, `classId`, and `stageId`; old build JSON is migrated from
the former `combatClass` field when it is imported or saved again.

## Built-in profile generation

All profile documents use `version: 1`. Stage manifests live in `Profiles/Manifests`.
They describe progression facts that tModLoader cannot infer: boss order, persistent flags,
new enemies and materials, special stations, and manual include/exclude rules.

1. Start tModLoader with the supported mods enabled.
2. Run `/pjexport CalamityMod ThoriumMod FargowiltasSouls`.
3. Use the exported file from `Mods/ProgressionJournal/Exports`.
4. Regenerate and validate all bundled profiles:

```powershell
node Tools\GenerateProfiles.mjs --snapshot C:\path\to\content-snapshot.json --all
node Tools\TestProfileGenerator.mjs
node Tools\AuditProfiles.mjs
node Tools\ValidateProfiles.mjs
```

Pass `--previous C:\path\to\older-snapshot.json` to include added, removed, renamed-candidate,
and changed content in each report. Reports are written to `Profiles/Reports`.

Anything the generator cannot place without guessing is excluded from automatic availability
and written to `Profiles/Review/<profile>-review.json`. Review issue IDs are stable across
regeneration while their evidence remains unchanged.

Resolve an issue in `Profiles/Manual/<profile>.json`, then regenerate:

- `itemStages`: place one item at a stage; recipes reachable from it are expanded automatically.
- `sourceStages`: place an NPC or container source at a stage and include its drops.
- `stationStages`: mark a crafting station as available at a stage.
- `conditionStages`: map a captured drop, shop, or recipe condition to a stage.
- `itemOverrides`: correct category or class detection.
- `ignoredItems`: mark a non-combat or intentionally unsupported item as reviewed.
- `ignoredIssues`: hide a specific review issue by its stable ID when no assignment is needed.

The generated review file includes evidence and a resolution example for every issue. Manual
files are never overwritten by the generator.

`AuditProfiles.mjs` checks that generated availability is not later than a compatible
official Wiki snapshot and that acquisition paths do not cross into unrelated mods.
Wiki data can move an item earlier only when its mod version has the same major/minor
version family as the exported snapshot. Older Wiki snapshots remain visible as audit
warnings and never change generated availability automatically.

Official Wiki parsers are input adapters only. Their recommendations appear in a separate
book-marked block and are not used to define the boss scale. Fargo's official enchantment
availability data is retained as neutral availability data, not marked as a recommendation.

## License

MIT. See `LICENSE`.
