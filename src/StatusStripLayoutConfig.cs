using System;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace PlayerStatusStrip;

public class StatusStripLayoutConfig
{
    public sealed class StatusAnimProfileConfig
    {
        public bool Enabled { get; set; } = true;

        public double EnterDurationSec { get; set; } = 0.28;

        public double UpdateDurationSec { get; set; } = 0.38;

        public double ExitDurationSec { get; set; } = 0.24;

        public double ScaleAmplitude { get; set; } = 0.11;

        public double ShakePx { get; set; } = 0;

        public double SlideDownPx { get; set; } = 0;
    }

    public const string LayoutConfigFileName = "playerstatusstrip-hudlayout.json";

    public string DialogArea { get; set; } = "RightBottom";

    public double DialogOffsetX { get; set; } = -8;

    public double DialogOffsetY { get; set; } = -2;

    public double DialogWidth { get; set; } = 32;

    public double DialogHeight { get; set; } = 54;

    /// <summary>Logical width (px) of the HUD anchor block for strip placement when anchor mode uses a fixed reference.</summary>
    public int AnchorWidthPx { get; set; } = 32;

    /// <summary>If &gt; 0, vertical alignment (Top/Center/Bottom) uses this block height instead of <see cref="AnchorWidthPx"/>.</summary>
    public int StatusStripAnchorHeightPx { get; set; } = 0;

    /// <summary>If &gt; 0, used as the HUD &quot;outer&quot; width for <c>Max</c>/<c>Dialog</c> anchor modes instead of the composer bounds.</summary>
    public int StatusStripAnchorOuterWidthPx { get; set; } = 0;

    /// <summary>Extra screen-space offset applied to every status icon (hit-test and draw).</summary>
    public double StatusStripIconNudgeX { get; set; } = 0;

    public double StatusStripIconNudgeY { get; set; } = 0;

    public double HudDrawOrder { get; set; } = 0.21;

    public float ZStatusIcons { get; set; } = 570f;

    public int StatusTooltipMaxWidth { get; set; } = 260;

    public float StatusTooltipZ { get; set; } = 600f;

    public double StatusStripOffsetX { get; set; } = 4;

    public double StatusStripOffsetY { get; set; } = 2;

    public int StatusIconGapPx { get; set; } = 4;

    public int StatusIconSize { get; set; } = 0;

    public string StatusStripAnchorMode { get; set; } = "Max";

    public string StatusStripVerticalAlign { get; set; } = "Top";

    public string StatusStripSide { get; set; } = "Left";

    /// <summary>
    /// When false (default) and <see cref="DialogArea"/> is a right-edge HUD anchor (RightTop, RightMiddle, RightBottom, RightFixed)
    /// with <see cref="StatusStripSide"/> Left, the icon row is flush to the anchor's trailing (screen-right) edge.
    /// When true, restores legacy placement: row ends at the anchor's leading edge, leaving an empty band ≈ anchor width (old &quot;mug slot&quot; geometry).
    /// </summary>
    public bool StatusStripUseLegacyLeadingEdgeRow { get; set; }

    public bool StatusIconAnimEnabled { get; set; } = true;

    public bool StatusIconsWaveEnabled { get; set; } = false;

    /// <summary>
    /// Omitted or null: lock the row baseline (default). Explicit <c>false</c>: allow update-pulse vertical slide
    /// (<see cref="StatusAnimProfileConfig.SlideDownPx"/>) and per-slot wave Y when <see cref="StatusIconsWaveEnabled"/> is on.
    /// </summary>
    public bool? StatusStripLockRowBaseline { get; set; }

    /// <summary>True unless <see cref="StatusStripLockRowBaseline"/> is explicitly false.</summary>
    public bool LockRowBaseline => StatusStripLockRowBaseline != false;

    public double StatusIconsWaveAmplitudePx { get; set; } = 1;

    public double StatusIconsWavePeriodSec { get; set; } = 2.5;

    public double StatusIconsWaveStaggerSec { get; set; } = 0.2;

    public StatusAnimProfileConfig NeutralAnim { get; set; } = new()
    {
        EnterDurationSec = 0.28,
        UpdateDurationSec = 0.38,
        ExitDurationSec = 0.24,
        ScaleAmplitude = 0.11,
        ShakePx = 0,
        SlideDownPx = 0
    };

    public StatusAnimProfileConfig PositiveAnim { get; set; } = new()
    {
        EnterDurationSec = 0.32,
        UpdateDurationSec = 0.35,
        ExitDurationSec = 0.24,
        ScaleAmplitude = 0.13,
        ShakePx = 0,
        SlideDownPx = 0
    };

    public StatusAnimProfileConfig NegativeAnim { get; set; } = new()
    {
        EnterDurationSec = 0.36,
        UpdateDurationSec = 0.30,
        ExitDurationSec = 0.20,
        ScaleAmplitude = 0.10,
        ShakePx = 6,
        SlideDownPx = 5
    };

    public void EnsureDefaults()
    {
        if (string.IsNullOrWhiteSpace(StatusStripSide))
        {
            StatusStripSide = "Left";
        }

        if (StatusTooltipMaxWidth <= 0)
        {
            StatusTooltipMaxWidth = 260;
        }

        if (AnchorWidthPx <= 0)
        {
            AnchorWidthPx = 32;
        }

        if (StatusStripAnchorHeightPx < 0)
        {
            StatusStripAnchorHeightPx = 0;
        }

        if (StatusStripAnchorOuterWidthPx < 0)
        {
            StatusStripAnchorOuterWidthPx = 0;
        }

        if (double.IsNaN(HudDrawOrder) || double.IsInfinity(HudDrawOrder))
        {
            HudDrawOrder = 0.21;
        }

        NeutralAnim ??= new StatusAnimProfileConfig();
        PositiveAnim ??= new StatusAnimProfileConfig();
        NegativeAnim ??= new StatusAnimProfileConfig();

        EnsureProfileDefaults(NeutralAnim, 0.28, 0.38, 0.24, 0.11);
        EnsureProfileDefaults(PositiveAnim, 0.32, 0.35, 0.24, 0.13);
        EnsureProfileDefaults(NegativeAnim, 0.36, 0.30, 0.20, 0.10);
    }

    private static void EnsureProfileDefaults(
        StatusAnimProfileConfig? profile,
        double enterDefault,
        double updateDefault,
        double exitDefault,
        double amplitudeDefault)
    {
        if (profile == null)
        {
            return;
        }

        if (profile.EnterDurationSec <= 0)
        {
            profile.EnterDurationSec = enterDefault;
        }

        if (profile.UpdateDurationSec <= 0)
        {
            profile.UpdateDurationSec = updateDefault;
        }

        if (profile.ExitDurationSec <= 0)
        {
            profile.ExitDurationSec = exitDefault;
        }

        if (double.IsNaN(profile.ScaleAmplitude) || double.IsInfinity(profile.ScaleAmplitude))
        {
            profile.ScaleAmplitude = amplitudeDefault;
        }

        if (double.IsNaN(profile.ShakePx) || double.IsInfinity(profile.ShakePx))
        {
            profile.ShakePx = 0;
        }

        if (double.IsNaN(profile.SlideDownPx) || double.IsInfinity(profile.SlideDownPx))
        {
            profile.SlideDownPx = 0;
        }
    }

    public static StatusStripLayoutConfig LoadOrCreate(ICoreClientAPI capi)
    {
        string modConfigDir = capi.GetOrCreateDataPath("ModConfig");
        StatusStripLayoutConfig? loaded = capi.LoadModConfig<StatusStripLayoutConfig>(LayoutConfigFileName);
        if (loaded != null)
        {
            loaded.EnsureDefaults();
            capi.Logger.Notification(
                "[Player Status Strip] HUD layout: {0}",
                Path.Combine(modConfigDir, LayoutConfigFileName));
            return loaded;
        }

        StatusStripLayoutConfig defaults = new();
        defaults.EnsureDefaults();
        capi.StoreModConfig(defaults, LayoutConfigFileName);
        capi.Logger.Notification(
            "[Player Status Strip] Created default HUD layout: {0} (reload hotkey after edits)",
            Path.Combine(modConfigDir, LayoutConfigFileName));
        return defaults;
    }

    public static StatusStripLayoutConfig Reload(ICoreClientAPI capi)
    {
        string dir = capi.GetOrCreateDataPath("ModConfig");
        string path = Path.Combine(dir, LayoutConfigFileName);
        if (File.Exists(path))
        {
            StatusStripLayoutConfig? cfg = capi.LoadModConfig<StatusStripLayoutConfig>(LayoutConfigFileName);
            cfg ??= new StatusStripLayoutConfig();
            cfg.EnsureDefaults();
            return cfg;
        }

        StatusStripLayoutConfig fallback = new();
        fallback.EnsureDefaults();
        return fallback;
    }

    public ElementBounds DialogBounds()
    {
        return ElementBounds.Fixed(ParseDialogArea(DialogArea), DialogOffsetX, DialogOffsetY, DialogWidth, DialogHeight);
    }

    internal bool UseTrailingEdgeStatusStripAlign()
    {
        if (StatusStripUseLegacyLeadingEdgeRow)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(DialogArea)
            || !Enum.TryParse(DialogArea.Trim(), ignoreCase: true, out EnumDialogArea area))
        {
            return false;
        }

        return area is EnumDialogArea.RightTop
            or EnumDialogArea.RightMiddle
            or EnumDialogArea.RightBottom
            or EnumDialogArea.RightFixed;
    }

    private static EnumDialogArea ParseDialogArea(string? code)
    {
        if (!string.IsNullOrWhiteSpace(code)
            && Enum.TryParse(code.Trim(), ignoreCase: true, out EnumDialogArea area))
        {
            return area;
        }

        return EnumDialogArea.RightBottom;
    }
}
