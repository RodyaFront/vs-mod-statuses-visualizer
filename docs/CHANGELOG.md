# Player Status HUD changelog

Source of truth version: [`modinfo.json`](../modinfo.json).

Public baseline on ModDB before this release: `1.0.1`.

## Unreleased

Release baseline verified on ModDB: `https://mods.vintagestory.at/playerstatusstrip` (`1.0.1`, released "18 hours ago" at verification time).

### Changed

- Refactor: reorganized `src` into architecture zones (`ApiContracts`, `CoreLogic`, `RenderingUi`, `RuntimeOrchestration`, `DevMock`, `Config`) without changing provider API semantics.
- Runtime: extracted `.stripmock` and `.striplayout` chat command handling from `PlayerStatusStripModSystem` into dedicated runtime services.
- Rendering/runtime boundary: isolated wizard preview provider lifecycle into a dedicated preview session helper.
- Docs: added architecture baseline (`docs/ARCHITECTURE.md`), ADR (`docs/DECISIONS/ADR-0001-architecture-baseline.md`), and local environment setup guide (`docs/DEV_ENV.md`).
- Build/dev ergonomics: added `Directory.Build.props` fallback for `VINTAGE_STORY` path when the environment variable is missing.
- API DX: documented provider quickstart, common integration mistakes, and status-id naming conventions in `docs/PLAYER_STATUS_STRIP_API.md` and `docs/INTEGRATION_COOKBOOK.md`.
- API/runtime diagnostics: added optional diagnostics API (`IStatusStripDiagnosticsApi`) with per-provider merge/duplicate stats and runtime signals for unstable ids/pulse churn in dev mode.
- Config: added `EnableDiagnosticsWarnings` to `playerstatusstrip-dev.json` so warning noise can be tuned without disabling diagnostics snapshots.

### Verification

- IDE diagnostics are clean for touched files.
- `dotnet test -c Release -p:UseSharedCompilation=false PlayerStatusStrip.Tests/PlayerStatusStrip.Tests.csproj` passes (`41/41`).
- `dotnet build -c Release PlayerStatusStrip.csproj` succeeds (`0` errors, `0` warnings).

## 1.0.1

Release date: 2026-05-08.

Public patch release after ModDB baseline `1.0.0`.

### Highlights

- Fix: stabilized edge-based HUD positioning across anchors.
- Fix: corrected center-anchor behavior for scaled status icons.
- i18n: added `ru` and `uk` translations for the layout wizard.
- UX: negative status update pulse now uses horizontal shake animation.
- UX: wizard preview now renders mock-only icons.
- Provider API and status strip integration model remain stable.

### Install / Update Notes

- Replace contents of `Mods/playerstatusstrip/` with this package (flat copy).
- Confirm version `1.0.1` in the in-game mod list.
- After `lang/assets` updates, perform a full client restart.

## 1.0.0

### Highlights (0.1.34 - 1.0.0)

- Edge-based HUD positioning: strip placement moved away from implicit dialog margins; inset behavior now matches screen edges across anchors.
- Bottom-anchor stability: removed size-dependent hidden base; row baseline logic now handles top/center/bottom explicitly.
- Wizard upgrades: presets for corner/inset/icon size/gap, better center behavior, and `CenterBottom` removed from active choices (with fallback for old configs).
- Tooltip placement: bottom-anchored rows prefer rendering tooltip above icons with automatic fallback when needed.
- Hotkeys finalized: `F8` reloads layout, `Ctrl+F8` opens/closes wizard; modifier registration fixed.
- UX polish: wizard label shortened from `Inset from corner` to `Inset`.

## 0.1.33

### Changes since 0.1.29 (0.1.30 - 0.1.33)

- `0.1.30`: `ShowChatNotification` strings for `.striplayout`, `.stripmock`, and key list output avoid raw `<` and `>` so Vintage Story VTML no longer breaks chat UI.
- `0.1.31`: default `StatusIconSize` is `46` px for new `playerstatusstrip-hudlayout.json` (`0` still means auto if set explicitly).
- `0.1.32`: default `DialogArea` is `RightTop` for new configs.
- `0.1.33`: default `DialogOffsetY` is `8` (matches curated top-right layout with SlowTox Visualized `1.1.7` defaults).

### Layout chat reminder

- Client chat supports `.striplayout` and `/striplayout`: `help`, `list`, `show`, `get [key]`, `set [key] [value]`, `reload`.
- Command updates persist `playerstatusstrip-hudlayout.json`.

## 0.1.29

### Highlights

- Hotkey `F8` (from `0.1.28`): `RegisterHotKeyFirst` runs only if `playerstatusstrip_reloadlayout` is not already in `Input.HotKeys`, preventing `StartClientSide` crashes on second world session.
- HUD lifecycle (from `0.1.28`): strip element is disposed on `LeftWorld` and recreated/reopened on `LevelFinalize` so overlay matches a fresh game session.
- Layout chat (`0.1.25+`): dedicated server boot creates `playerstatusstrip-dev.json`; client `.striplayout` and `/striplayout` can `get/set/reload` and persist `playerstatusstrip-hudlayout.json`.
- `.striplayout list` (`0.1.29`): prints every scalar key supported by `get/set` with short explanation; nested `NeutralAnim` / `PositiveAnim` / `NegativeAnim` remain JSON-only and are explained in output.
- Help text and startup log mention `list`.

### Fixes (0.1.28 baseline)

- Fixed duplicate hotkey exception after return-to-menu and re-enter world.
- Fixed empty status strip until full client restart when HUD state was stale across sessions.

## 0.1.25

### Highlights

- Dedicated server now creates `playerstatusstrip-dev.json` during startup.
- Added chat command group `.striplayout` and `/striplayout` for live layout edits.
- HUD layout updates persist to `playerstatusstrip-hudlayout.json` and reload immediately.

### Added / Improved

- New layout chat subcommands: `help`, `show`, `get <key>`, `set <key> <value>`, `reload`.
- Runtime parser supports common config field types: `string`, `bool`, `int`, `float`, and `double`.
- `README` updated with operator-facing layout command examples.

### Fixes

- Resolved missing config bootstrap on dedicated server installs.
- Reduced config edit friction by removing mandatory out-of-game file edits for common layout tuning.
- Improved feedback loop: chat command updates now trigger immediate HUD layout reload.

## 0.1.24

### Highlights

- Mock placeholders are disabled by default for production safety.
- API and developer guides were expanded for third-party integration.
- ModDB release materials were added and normalized to template-based docs.

### Added / Improved

- Added clear dev-config guidance in code and runtime logs for mock behavior.
- Expanded `PLAYER_STATUS_STRIP_API.md` with integration patterns and scenario overview.
- Added `PLAYER_STATUS_STRIP_DEV_GUIDE.md` with test/release checklist.

### Fixes

- Removed hardcoded version/package strings from landing and `README` docs.
- Switched ModDB notes to reusable template format to avoid per-release doc drift.
- Aligned release documentation with mod metadata as single source of truth.
