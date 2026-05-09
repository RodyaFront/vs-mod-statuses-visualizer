# PlayerStatusStrip Architecture Baseline

## Purpose

`PlayerStatusStrip` is a HUD platform mod with a stable provider API for other mods.
The architecture favors a lightweight boundary model over heavyweight layered patterns.

## Zones

- `ApiContracts`: public/stable contracts used by integrators.
- `CoreLogic`: deterministic merge/layout/animation decisions.
- `RenderingUi`: HUD and dialog rendering.
- `RuntimeOrchestration`: lifecycle wiring, commands, event/network orchestration.
- `DevMock`: development scenarios and mock-only providers/packets.
- `Config`: runtime/player config models and load/store behavior.

## Dependency Direction

Allowed direction is intentionally simple:

- `ApiContracts` depends on nothing inside the mod.
- `CoreLogic` may depend on `ApiContracts`.
- `RenderingUi` may depend on `CoreLogic` and `ApiContracts`.
- `DevMock` may depend on `CoreLogic` and `ApiContracts`.
- `RuntimeOrchestration` is the composition root and may depend on all zones.
- `Config` can be consumed by runtime/render/dev paths.

Disallowed by policy:

- `ApiContracts` depending on runtime/render/dev implementation details.
- Core math/merge logic embedded in orchestration-only classes.
- Dev/mock-specific behavior coupled into production rendering flow.

## Public API Stability

Public contract surface:

- `IStatusStripHudApi`
- `IStatusStripProvider`
- `StatusDescriptor` and related semantic enums

Breaking changes include:

- Signature changes in public API interfaces.
- Behavior contract changes in merge ordering/dedup semantics.
- Removing or changing required semantics of descriptor fields.

For breaking changes:

1. Bump API version.
2. Document migration notes in API docs.
3. Record rationale in changelog.

## Testing Baseline

- Unit tests are required for `CoreLogic` behavior changes.
- Runtime command extraction must have focused parser/dispatch tests.
- UI changes require smoke validation (`HUD`, wizard, `.striplayout`, `.stripmock`).
