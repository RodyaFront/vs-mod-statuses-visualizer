using System.Collections.Generic;
using Vintagestory.API.Client;

namespace PlayerStatusStrip;

internal sealed class StatusStripHudApi : IStatusStripHudApi
{
    private readonly List<IStatusStripProvider> _providers = new();
    private IStatusStripProvider? _previewExclusiveProvider;
    private readonly List<IStatusStripProvider> _singleProvider = new(1);

    public int ApiVersion => 1;

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

    internal void CollectMerged(ICoreClientAPI capi, float deltaTime, List<StatusDescriptor> dest)
    {
        if (_previewExclusiveProvider != null)
        {
            _singleProvider.Clear();
            _singleProvider.Add(_previewExclusiveProvider);
            StatusStripMerge.MergeInto(_singleProvider, capi, deltaTime, dest);
            return;
        }

        StatusStripMerge.MergeInto(_providers, capi, deltaTime, dest);
    }
}
