using System.Collections.Generic;
using Vintagestory.API.Client;

namespace PlayerStatusStrip;

public interface IStatusStripProvider
{
    void Collect(ICoreClientAPI capi, float deltaTime, List<StatusDescriptor> dest);
}
