# Progression Journal Architecture

This project now has a base structure intended for a data-heavy guide mod.

## Core idea

- `ProgressionStageId` defines the game stage the player is currently in.
- `CombatClass` defines the selected loadout lens: melee, ranged, magic, or summoner.
- `JournalEntry` describes one weapon, armor set, or accessory.
- `StageEvaluation` assigns a recommendation tier for a specific stage.
- `JournalDatabase` aggregates entries from one or more content sources.

## Why this structure scales

- Stage logic is isolated in `Common/Progression/ProgressionStageCatalog.cs`.
- Guide content is isolated in `Content/Sources/VanillaJournalContentSource.cs`.
- Inventory entry and journal window are coordinated in `Common/Systems/ProgressionJournalUISystem.cs`.
- UI only queries the database and does not know where data comes from.
- Additional providers can be added later without rewriting the UI.

## Recommended next steps

1. Split vanilla data into multiple source files by class and category once the list grows.
2. Add notes, tags, biome requirements, and acquisition hints to `StageEvaluation` or `JournalEntry`.
3. Add item icons, tooltips, and click-to-open recipe/wiki integration in the UI.
4. Add optional providers for other mods by implementing `IJournalContentSource`.
5. Persist player-side filters such as selected class, favorites, or hidden entries.

## External mod integration

tModLoader is enough for the whole base feature set:

- boss progression detection
- UI
- save data
- localization
- inventory hooks and keybinds

Use other mod APIs only for optional integrations, for example:

- Boss Checklist: sync or deep-link boss stages
- Recipe Browser: jump to crafting context
- content mods: extend the database when a mod is loaded

The clean extension point for that is a new `IJournalContentSource` implementation.
