using System.Collections.Generic;
using Vintagestory.API.Client;

namespace PlayerStatusStrip;

internal sealed class StripLayoutWizardPreviewProvider : IStatusStripProvider
{
    private float _accumSec;

    public void Collect(ICoreClientAPI capi, float deltaTime, List<StatusDescriptor> dest)
    {
        MockStatusSampleIcons.Append(deltaTime, ref _accumSec, dest, includePulseMetrics: false);
    }
}
