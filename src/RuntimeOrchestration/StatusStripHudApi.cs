using System.Collections.Generic;
using Vintagestory.API.Client;

namespace PlayerStatusStrip;

internal sealed class StatusStripHudApi : IStatusStripHudApi, IStatusStripDiagnosticsApi
{
    private readonly List<IStatusStripProvider> _providers = new();
    private IStatusStripProvider? _previewExclusiveProvider;
    private readonly List<IStatusStripProvider> _singleProvider = new(1);
    private readonly StatusStripMergeReport _mergeReport = new();
    private readonly StatusStripDiagnosticsTracker _diagnostics = new();
    private bool _devDiagnosticsWarningsEnabled;

    public int ApiVersion => 1;
    public bool DiagnosticsAvailable => true;

    public void RegisterProvider(IStatusStripProvider provider)
    {
        if (!_providers.Contains(provider))
        {
            _providers.Add(provider);
        }
    }

    public void UnregisterProvider(IStatusStripProvider provider)
    {
        _providers.Remove(provider);
    }

    public void SetPreviewExclusiveProvider(IStatusStripProvider? provider)
    {
        _previewExclusiveProvider = provider;
    }

    public StatusStripDiagnosticsSnapshot GetDiagnosticsSnapshot()
    {
        return _diagnostics.Snapshot();
    }

    internal void SetDiagnosticsWarningsEnabled(bool enabled)
    {
        _devDiagnosticsWarningsEnabled = enabled;
    }

    internal void CollectMerged(ICoreClientAPI capi, float deltaTime, List<StatusDescriptor> dest)
    {
        if (_previewExclusiveProvider != null)
        {
            _singleProvider.Clear();
            _singleProvider.Add(_previewExclusiveProvider);
            StatusStripMerge.MergeInto(_singleProvider, capi, deltaTime, dest, _mergeReport);
            _diagnostics.OnFrame(capi, _mergeReport, dest, _devDiagnosticsWarningsEnabled);
            return;
        }

        StatusStripMerge.MergeInto(_providers, capi, deltaTime, dest, _mergeReport);
        _diagnostics.OnFrame(capi, _mergeReport, dest, _devDiagnosticsWarningsEnabled);
    }
}
