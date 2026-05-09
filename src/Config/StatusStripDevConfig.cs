using System.IO;
using Vintagestory.API.Common;

namespace PlayerStatusStrip;

public class StatusStripDevConfig
{
    public const string DevConfigFileName = "playerstatusstrip-dev.json";

    /// <summary>
    /// Enables dev-only mock command tooling (.stripmock / /stripmock).
    /// Keep false in production player config.
    /// </summary>
    public bool DevMode { get; set; }

    /// <summary>
    /// Keeps four static mock icons visible when no scenario is running.
    /// Default is false to avoid placeholder statuses in production.
    /// </summary>
    public bool UseMockStatuses { get; set; }

    /// <summary>
    /// When <see cref="DevMode"/> is true: open the layout wizard automatically on every world load, ignoring <c>playerstatusstrip-onboarding.json</c> suppress. For local iteration without bumping mod version.
    /// </summary>
    public bool AlwaysAutoLayoutWizard { get; set; }

    /// <summary>
    /// Enables diagnostics warnings for duplicate IDs, unstable stable-id continuity, and noisy pulse metrics.
    /// Applies only when <see cref="DevMode"/> is true.
    /// </summary>
    public bool EnableDiagnosticsWarnings { get; set; } = true;

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
            "[Player Status HUD] Created dev config: {0}",
            Path.Combine(dir, DevConfigFileName));
        return d;
    }
}
