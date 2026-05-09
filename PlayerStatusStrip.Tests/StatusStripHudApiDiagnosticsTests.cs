using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Xunit;

namespace PlayerStatusStrip.Tests;

public sealed class StatusStripHudApiDiagnosticsTests
{
    private static AssetLocation Icon(string path) => new("playerstatusstrip", path);

    private sealed class DuplicateProvider(string id, string iconPath) : IStatusStripProvider
    {
        public void Collect(ICoreClientAPI capi, float deltaTime, List<StatusDescriptor> dest)
        {
            dest.Add(new StatusDescriptor(id, Icon(iconPath), 1, "dup"));
        }
    }

    private sealed class FlappingStableIdProvider : IStatusStripProvider
    {
        private int _tick;

        public void Collect(ICoreClientAPI capi, float deltaTime, List<StatusDescriptor> dest)
        {
            _tick++;
            string id = _tick % 2 == 0 ? "mod:even" : "mod:odd";
            dest.Add(new StatusDescriptor(id, Icon("flap.png"), 5, "flap", pulseMetric: null));
        }
    }

    private sealed class PulseChurnProvider : IStatusStripProvider
    {
        private float _metric = 1f;

        public void Collect(ICoreClientAPI capi, float deltaTime, List<StatusDescriptor> dest)
        {
            _metric += 0.2f;
            dest.Add(new StatusDescriptor(
                "mod:pulse",
                Icon("pulse.png"),
                10,
                "pulse",
                pulseMetric: _metric,
                StatusAffectKind.Positive));
        }
    }

    [Fact]
    public void DiagnosticsSnapshot_TracksDuplicatesUnstableAndPulseChurn()
    {
        var api = new StatusStripHudApi();
        api.SetDiagnosticsWarningsEnabled(true);
        api.RegisterProvider(new DuplicateProvider("mod:dup", "first.png"));
        api.RegisterProvider(new DuplicateProvider("mod:dup", "second.png"));
        api.RegisterProvider(new FlappingStableIdProvider());
        api.RegisterProvider(new PulseChurnProvider());

        var dest = new List<StatusDescriptor>();
        for (int i = 0; i < 14; i++)
        {
            api.CollectMerged(null!, 0f, dest);
        }

        var diagnosticsApi = Assert.IsAssignableFrom<IStatusStripDiagnosticsApi>(api);
        StatusStripDiagnosticsSnapshot snapshot = diagnosticsApi.GetDiagnosticsSnapshot();

        Assert.True(diagnosticsApi.DiagnosticsAvailable);
        Assert.True(snapshot.FrameCount >= 14);
        Assert.True(snapshot.TotalDuplicateOverwrites > 0);
        Assert.True(snapshot.UnstableProviderWarnings > 0);
        Assert.True(snapshot.PulseChurnWarnings > 0);
        Assert.True(snapshot.LastFrameDuplicateCount > 0);
        Assert.NotEmpty(snapshot.Providers);
    }
}
