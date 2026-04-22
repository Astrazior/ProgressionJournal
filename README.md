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

## License

MIT. See `LICENSE`.
