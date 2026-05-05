using System.IO;
using Vintagestory.API.Common;

namespace PlayerStatusStrip;

public class StatusStripDevConfig
{
    public const string DevConfigFileName = "playerstatusstrip-dev.json";

    public bool DevMode { get; set; }

    public bool UseMockStatuses { get; set; }

    public static StatusStripDevConfig LoadOrCreate(ICoreAPI api)
    {
        string dir = api.GetOrCreateDataPath("ModConfig");
        StatusStripDevConfig? loaded = api.LoadModConfig<StatusStripDevConfig>(DevConfigFileName);
        if (loaded != null)
        {
            return loaded;
        }

        StatusStripDevConfig d = new();
        api.StoreModConfig(d, DevConfigFileName);
        api.Logger.Notification(
            "[Player Status Strip] Created dev config: {0}",
            Path.Combine(dir, DevConfigFileName));
        return d;
    }
}
