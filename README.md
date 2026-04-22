# Progression Journal

Client-side progression helper for Terraria/tModLoader.

Steam Workshop:
https://steamcommunity.com/sharedfiles/filedetails?id=3710545729

## What it does

- Shows equipment recommendations by progression stage and class
- Shows combat buffs and utility information
- Shows item acquisition sources: drops, shops, recipes, stations, conditions

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
        new object[] { new object[] { "EyeOfCthulhu", "Core" } });
}
```

## License

MIT. See `LICENSE`.
