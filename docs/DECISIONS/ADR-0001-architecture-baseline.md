# ADR-0001: PlayerStatusStrip architecture baseline

## Status

Accepted

## Context

The project grew from a single HUD module into a reusable status-strip platform with an external provider API.
The codebase remained in a flat `src` layout, which made growth manageable early on but increasingly obscured boundaries between API, rendering, runtime orchestration, and dev/mock tooling.

## Decision

Adopt a lightweight zone-based architecture:

- `ApiContracts`
- `CoreLogic`
- `RenderingUi`
- `RuntimeOrchestration`
- `DevMock`
- `Config`

Use `RuntimeOrchestration` as the composition root.
Keep public API contract compatibility explicit and versioned.
Prefer pure/helper logic in `CoreLogic` and keep rendering/event wiring isolated.

## Consequences

Positive:

- Clear ownership of responsibilities by zone.
- Safer API evolution for external provider integrations.
- Lower regression risk by isolating deterministic logic and tests.

Trade-offs:

- Initial file/folder churn.
- More discipline required when adding new features.

## Follow-ups

1. Move source files to zone folders without behavior changes.
2. Extract command handling from `ModSystem` into runtime helpers.
3. Add tests for extracted command handlers.
4. Keep API docs/version checks aligned with refactors.
