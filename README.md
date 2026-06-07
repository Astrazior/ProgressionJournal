# Progression Journal

Client-side progression helper for Terraria/tModLoader.

Steam Workshop:
https://steamcommunity.com/sharedfiles/filedetails?id=3710545729

## What it does

- Shows equipment recommendations by progression stage and class
- Shows combat buffs and utility information
- Shows item acquisition sources: drops, shops, recipes, stations, conditions
- Supports selectable vanilla, bundled, add-on, and user-created progression profiles
- Includes a read-only Calamity Wiki snapshot when Calamity is installed
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

## Calamity snapshot

The bundled profile is generated from the structured Calamity Wiki class setup data pages.
Wiki entries use the neutral `FromGuide` tier because the source does not rank items within
each section.

Regenerate it from an official Calamity source checkout:

```powershell
node Tools\CalamityProfileConverter.mjs `
  --calamity-source C:\path\to\CalamityModPublic `
  --output Profiles\Builtin\calamity-wiki.json `
  --report Profiles\Builtin\calamity-wiki-report.json
```

The report records assumed vanilla names and skipped rows instead of silently discarding data.

## License

MIT. See `LICENSE`.
