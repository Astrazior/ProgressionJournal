# UI Visuals

This folder contains visual-only UI code. Screen behavior, input handling, data
selection, and layout composition remain under `UI/Controls`, `UI/Composition`,
and `UI/States`.

Namespaces mirror the folder structure:

- `ProgressionJournal.UI.Visuals.Styles`
- `ProgressionJournal.UI.Visuals.Renderers`
- `ProgressionJournal.UI.Visuals.Elements`

## Styles

- `Styles/JournalUiTheme.cs`: colors and reusable visual palettes.
- `Styles/JournalUiMetrics.cs`: shared UI dimensions and spacing.

## Renderers

- `Renderers/JournalItemSlotRenderer.cs`: volumetric item slots and markers.
- `Renderers/JournalVolumetricPanelRenderer.cs`: shared panel frame, bevel, and shadow.
- `Renderers/JournalSourceCardRenderer.cs`: Bestiary-style source card background.

## Elements

- `Elements/JournalVolumetricPanel.cs`: panel wrapper using the shared renderer.
- `Elements/JournalCategoryHeader.cs`: item-category heading visuals.
- `Elements/JournalConditionAlertIcon.cs`: condition warning icon.
- `Elements/JournalDimOverlay.cs`: modal dimming layer.
- `Elements/JournalRecommendationHeader.cs`: recommendation block title plaque.
- `Elements/JournalSourceCard.cs`: source card visual element.
- `Elements/JournalSourceSectionHeader.cs`: source-section heading visuals.
