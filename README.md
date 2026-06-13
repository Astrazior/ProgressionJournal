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

Each supported mod has one self-contained directory under
`Profiles/Mods/<InternalModName>`. `profile.json` is the only file packaged into the mod;
the snapshot, support data, agent rules, recommendations, review, and report remain
development files.

1. Start tModLoader with the supported mods enabled.
2. Run `/pjexport <InternalModName>` once for each supported mod.
3. Fill unresolved decisions in that mod's `agent-rules.json`.
4. Build one profile or all profiles:

```powershell
node Tools\BuildModProfiles.mjs CalamityMod
node Tools\BuildModProfiles.mjs --all
node Tools\TestProfileGenerator.mjs
node Tools\TestModProfilePipeline.mjs
```

Anything the generator cannot place without guessing is excluded from automatic availability
and written to that mod's `review.json`. Every rule or ignored issue in `agent-rules.json`
must contain an official source URL, source version or revision, check date, and explanation.
The single build command applies those rules, writes the review and report, audits cross-mod
paths, validates the result, and regenerates `profile.json`.

`recommendations.json` is display-only data. It can annotate entries but never creates or
moves availability evaluations. Boss order and other structural facts live in `support.json`;
verified exceptions and facts that tModLoader cannot expose live in `agent-rules.json`.

## License

MIT. See `LICENSE`.
