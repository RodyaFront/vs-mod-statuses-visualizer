using System;
using Vintagestory.API.Client;

namespace PlayerStatusStrip;

public sealed class StatusStripOnboardingConfig
{
    public const string FileName = "playerstatusstrip-onboarding.json";

    /// <summary>When true, the layout wizard is not shown automatically on first world session.</summary>
    public bool SuppressAutoLayoutWizard { get; set; }

    /// <summary>Installed mod version string when the player last closed the wizard (Skip or Apply). If it no longer matches the running mod version, auto-show is enabled again.</summary>
    public string? WizardDismissedForModVersion { get; set; }

    public static StatusStripOnboardingConfig LoadOrCreate(ICoreClientAPI capi)
    {
        string currentVer = PlayerStatusStripModVersion.Current(capi);
        StatusStripOnboardingConfig? loaded = capi.LoadModConfig<StatusStripOnboardingConfig>(FileName);
        if (loaded != null)
        {
            if (loaded.SuppressAutoLayoutWizard
                && !string.Equals(loaded.WizardDismissedForModVersion ?? "", currentVer, StringComparison.Ordinal))
            {
                loaded.SuppressAutoLayoutWizard = false;
                Save(capi, loaded);
            }

            return loaded;
        }

        StatusStripOnboardingConfig created = new();
        capi.StoreModConfig(created, FileName);
        return created;
    }

    public static void Save(ICoreClientAPI capi, StatusStripOnboardingConfig cfg)
    {
        capi.StoreModConfig(cfg, FileName);
    }
}
