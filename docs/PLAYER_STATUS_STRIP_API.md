# Player Status Strip — API for modders

**Mod id:** `playerstatusstrip`  
**Current `IStatusStripHudApi.ApiVersion`:** `1` (bump when breaking the contract).

**Architecture baseline:** see `docs/ARCHITECTURE.md` and `docs/DECISIONS/ADR-0001-architecture-baseline.md`.

## What the base mod does

- Renders a horizontal strip of status icons with pop-in, pulse, pop-out, and optional wave offset.
- Merges entries from all registered **providers** each frame, sorts by `SortOrder` then `StableId`, and resolves duplicate `StableId` by **last registered provider wins** (order in the internal list is registration order).
- Tooltips use **VTML** from each slot’s `StatusDescriptor`.

## Registering a provider (client)

1. In your mod’s **client** `ModSystem`, get the API from the base mod’s system (same app domain, your project references `PlayerStatusStrip.dll` and `VintagestoryAPI`):

   ```csharp
   var baseSys = api.ModLoader.GetModSystem<PlayerStatusStrip.PlayerStatusStripModSystem>();
   IStatusStripHudApi? api = baseSys?.StatusApi;
   if (api == null) return;
   api.RegisterProvider(myProvider);
   ```

2. Implement `IStatusStripProvider`:

   - `void Collect(ICoreClientAPI capi, float deltaTime, List<StatusDescriptor> dest)`  
     **Append** your slots to `dest` (the base mod clears its scratch list before calling you). Use `deltaTime` for time-based mock or smoothing if needed.

3. `StatusDescriptor` fields:

   | Field | Role |
   |--------|------|
   | `StableId` | Stable between frames (drives animation state). Use a unique, namespaced id (e.g. `mymod:bleed`). |
   | `Icon` | `AssetLocation` for a texture in **your** mod or in `playerstatusstrip` (for shared icons). |
   | `SortOrder` | Lower = further **left** in the strip. |
   | `TooltipVtml` | Tooltip body as VTML. |
   | `PulseMetric` | Optional; when the value changes enough frame-to-frame, the strip triggers a pulse animation (see `PulseMetricTrigger` thresholds in source). |
   | `AffectKind` | Optional semantic kind (`Neutral` default, `Positive`, `Negative`) used by UX animation profiles. |

4. On unload or when your logic stops, call `UnregisterProvider(myProvider)` to avoid leaks.

## Provider quickstart (copy-paste)

Use the full cookbook for production-ready examples: `docs/INTEGRATION_COOKBOOK.md`.

Minimal shape:

```csharp
var stripSystem = api.ModLoader.GetModSystem<PlayerStatusStrip.PlayerStatusStripModSystem>();
IStatusStripHudApi? stripApi = stripSystem?.StatusApi;
if (stripApi == null) return;

var provider = new MyStatusProvider();
stripApi.RegisterProvider(provider);
// later on shutdown:
stripApi.UnregisterProvider(provider);
```

Provider output checklist:

- Keep `StableId` stable across frames.
- Prefix IDs with your own mod id.
- Keep `SortOrder` deterministic.
- Use `PulseMetric` only when value-driven pulse is desired.
- Use `AffectKind` to communicate semantic sign (`Positive` / `Negative` / `Neutral`).

## Common mistakes

| Symptom | Likely cause | Fix |
|---|---|---|
| Status flickers or reappears each frame | Unstable `StableId` | Use durable ids (`modid:domain-status-name`) |
| Status disappears with another integration loaded | ID collision | Namespace ids with your mod prefix |
| Status remains after unload | Provider still registered | Always call `UnregisterProvider` |
| Excessive pulse animation | Noisy `PulseMetric` | Debounce or smooth the metric |

## ID naming convention

Recommended format:

- `modid:domain-status-name`

Examples:

- Good: `slowtoxvisualized:tox-poison`
- Good: `mymod:food-well-fed`
- Bad: `WellFed`
- Bad: `status1`
- Reserved prefix warning: avoid using `playerstatusstrip:*` unless this mod owns the status.

## Config files (player data)

Paths are under the Vintage Story **data** folder, file names:

- **Layout:** `ModConfig/playerstatusstrip-hudlayout.json` — **where the invisible HUD anchor sits** on screen (`DialogArea` — any name of `EnumDialogArea` from the game API, e.g. `RightTop`, `RightBottom`; unknown strings fall back to `RightBottom`, `DialogOffsetX` / `DialogOffsetY`, `DialogWidth` / `DialogHeight`) and **how the status strip attaches** to that block (`StatusStripOffsetX` / `Y`, `StatusStripSide`, `StatusStripVerticalAlign`, `AnchorWidthPx`, `StatusIconSize`, `StatusIconGapPx`, wave and tooltip fields). **`StatusStripLockRowBaseline`**: omit or **`true`** to keep every icon on the same vertical baseline (ignores update-pulse vertical slide from `SlideDownPx` and ignores vertical wave offsets). Set **`false`** to allow that motion again. `StatusStripAnchorMode`: **`Reference`** (fixed width = `AnchorWidthPx`), **`Dialog`** (full composer width), **`Max`** (max of reference and composer). Legacy value **`Mug`** is accepted and behaves like **`Reference`** (no mug/jug is drawn in this mod). For **`RightTop` / `RightBottom` / `RightMiddle` / `RightFixed`** with **`StatusStripSide` `Left`**, the default geometry places the icon row flush to the **trailing** edge of the anchor (no extra empty band the width of `DialogWidth` to the right of the icons). Set **`StatusStripUseLegacyLeadingEdgeRow`: true** to restore the old “mug slot” gap. UX animation profiles live in `NeutralAnim`, `PositiveAnim`, `NegativeAnim` (`Enabled`, `EnterDurationSec`, `UpdateDurationSec`, `ExitDurationSec`, `ScaleAmplitude`, `ShakePx`, `SlideDownPx`). `Enabled` works with fallback: if requested profile is disabled, renderer falls back to `NeutralAnim`; if `NeutralAnim.Enabled` is also false, renderer uses baseline non-profile modifiers (no shake/slide profile offsets). Extra: `StatusStripAnchorHeightPx`, `StatusStripAnchorOuterWidthPx`, `StatusStripIconNudgeX` / `Y`, `HudDrawOrder`. Reload: **F8** (summary line in chat).
- **Dev:** `ModConfig/playerstatusstrip-dev.json` — `DevMode` enables **client** mock tooling (`.stripmock` / `/stripmock`). **`UseMockStatuses`** defaults to **false**: no HUD icons from the base mod until you **`run <id>`** a scenario. Set **`UseMockStatuses`: true** only if you want the four placeholder icons while **no** scenario is running. With `DevMode` true, use **`list`**, **`run <id>`**, **`stop`**. The **integrated server** registers commands and relays a small packet to your client. A client-only chat intercept still handles a **plain chat line** that starts with `.stripmock` / `/stripmock` if it is not parsed as a command. **Dedicated server** must also have this mod installed; only players with `DevMode` in their client config get the client-side provider, but the server still needs the mod for the command to exist.

## Versioning

- Prefer depending on **`game`** only; depend on `playerstatusstrip` if you need a known minimum feature set.
- Check `api.ApiVersion` before relying on new behavior.
- Internal source layout refactors do not require an API version bump when contract semantics are unchanged.

### Optional diagnostics surface

Diagnostics are exposed via an optional cast to avoid breaking existing consumers:

```csharp
if (stripApi is IStatusStripDiagnosticsApi diagnosticsApi && diagnosticsApi.DiagnosticsAvailable)
{
    StatusStripDiagnosticsSnapshot snapshot = diagnosticsApi.GetDiagnosticsSnapshot();
}
```

Runtime diagnostics are intended for dev-mode workflows and can include duplicate-id overwrites, unstable id churn signals, and pulse churn counters.

## Build reference

To compile against the base mod, add a **project or assembly reference** to `PlayerStatusStrip.dll` from `Mods/playerstatusstrip/` (or your build output). Do not reference server-only assemblies unless your mod needs them.
