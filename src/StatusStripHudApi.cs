using System.Collections.Generic;
using Vintagestory.API.Client;

namespace PlayerStatusStrip;

internal sealed class StatusStripHudApi : IStatusStripHudApi
{
    private readonly List<IStatusStripProvider> _providers = new();

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

    internal void CollectMerged(ICoreClientAPI capi, float deltaTime, List<StatusDescriptor> dest)
    {
        StatusStripMerge.MergeInto(_providers, capi, deltaTime, dest);
    }
}
