# Player Status Strip Integration Cookbook

This page is a copy-paste oriented guide for modders integrating with `playerstatusstrip`.

## Provider quickstart

### 1) Wire API in your client ModSystem

```csharp
using PlayerStatusStrip;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

public sealed class MyModSystem : ModSystem
{
    private IStatusStripHudApi? _stripApi;
    private IStatusStripProvider? _provider;

    public override void StartClientSide(ICoreClientAPI api)
    {
        PlayerStatusStripModSystem? stripSystem = api.ModLoader.GetModSystem<PlayerStatusStripModSystem>();
        _stripApi = stripSystem?.StatusApi;
        if (_stripApi == null)
        {
            api.Logger.Warning("[MyMod] Player Status Strip API unavailable.");
            return;
        }

        _provider = new MyStatusProvider();
        _stripApi.RegisterProvider(_provider);
    }

    public override void Dispose()
    {
        if (_stripApi != null && _provider != null)
        {
            _stripApi.UnregisterProvider(_provider);
        }

        _provider = null;
        _stripApi = null;
        base.Dispose();
    }
}
```

### 2) Implement a minimal provider

```csharp
using System.Collections.Generic;
using PlayerStatusStrip;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

internal sealed class MyStatusProvider : IStatusStripProvider
{
    public void Collect(ICoreClientAPI capi, float deltaTime, List<StatusDescriptor> dest)
    {
        IClientPlayer? player = capi.World.Player;
        if (player?.Entity == null)
        {
            return;
        }

        float satiety = player.Entity.WatchedAttributes.GetFloat("satiety", 0f);
        if (satiety < 0.8f)
        {
            return;
        }

        dest.Add(new StatusDescriptor(
            "mymod:food-well-fed",
            new AssetLocation("mymod", "textures/icons/wellfed.png"),
            25,
            "<font color=\"#8fdc7a\"><b>Well fed</b></font>\nSatiety is high.",
            satiety,
            StatusAffectKind.Positive));
    }
}
```

## Common mistakes checklist

- [ ] Keep `StableId` stable between frames. Do not include timers/random values.
- [ ] Always `UnregisterProvider` on unload/disable paths.
- [ ] Namespace IDs with your mod id to avoid collisions.
- [ ] Keep `SortOrder` explicit and deterministic.
- [ ] Use `PulseMetric` for meaningful value changes, not noisy per-frame oscillation.

### Symptom -> likely cause -> fix

| Symptom | Likely cause | Fix |
|---|---|---|
| Icons constantly pop in/out | `StableId` changes every frame | Use one deterministic ID per logical status |
| Your status disappears when another mod is loaded | Duplicate `StableId` conflict | Prefix IDs with your mod id |
| Status remains after mod reload/world leave | Provider was not unregistered | Call `UnregisterProvider` in `Dispose`/shutdown paths |
| Icon order jumps unexpectedly | `SortOrder` overlaps with unstable IDs | Keep IDs stable and assign clear sort bands |
| Constant pulse spam | `PulseMetric` changes too often | Debounce/smooth metric and emit only meaningful deltas |

## Status ID naming convention

Normative format:

- `modid:domain-status-name`

Examples:

- Good: `slowtoxvisualized:tox-poison`
- Good: `mymod:food-well-fed`
- Bad: `WellFed`
- Bad: `status1`
- Bad: `playerstatusstrip:my-status` (reserved mod prefix)

Policy:

- Treat your IDs as durable contracts for animation continuity.
- Never reuse one ID for multiple unrelated meanings.
- If meaning changes, migrate to a new ID and keep old behavior window short.

## Optional diagnostics API

Consumers can probe diagnostics optionally (non-breaking cast):

```csharp
if (stripApi is IStatusStripDiagnosticsApi diagnosticsApi && diagnosticsApi.DiagnosticsAvailable)
{
    StatusStripDiagnosticsSnapshot snapshot = diagnosticsApi.GetDiagnosticsSnapshot();
    // use counters in dev tooling/logs
}
```
