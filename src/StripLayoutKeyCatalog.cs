using System.Collections.Generic;

namespace PlayerStatusStrip;

internal static class StripLayoutKeyCatalog
{
    internal sealed record Entry(string Key, string Description);

    /// <summary>Keys supported by chat <c>get</c>/<c>set</c> (scalar JSON fields). Anim blocks are file-only.</summary>
    internal static readonly IReadOnlyList<Entry> ChatEditableKeys = new[]
    {
        new Entry("DialogArea", "HUD anchor corner name (e.g. RightBottom, RightTop, CenterBottom). See EnumDialogArea."),
        new Entry("DialogOffsetX", "HUD block offset X from anchor (px, GUI space)."),
        new Entry("DialogOffsetY", "HUD block offset Y from anchor (px)."),
        new Entry("DialogWidth", "HUD block width (px)."),
        new Entry("DialogHeight", "HUD block height (px)."),
        new Entry("AnchorWidthPx", "Logical width of anchor used for strip placement when anchor mode uses a fixed width."),
        new Entry("StatusStripAnchorHeightPx", "If positive, vertical align uses this height instead of AnchorWidthPx."),
        new Entry("StatusStripAnchorOuterWidthPx", "If positive, outer width for Max/Dialog anchor modes instead of composer bounds."),
        new Entry("StatusStripIconNudgeX", "Extra X offset for every icon (draw + hit test)."),
        new Entry("StatusStripIconNudgeY", "Extra Y offset for every icon."),
        new Entry("HudDrawOrder", "Draw order for this HUD layer (lower = earlier)."),
        new Entry("ZStatusIcons", "Z depth for status icon sprites."),
        new Entry("StatusTooltipMaxWidth", "Max width (px) for status tooltip richtext."),
        new Entry("StatusTooltipZ", "Z depth for tooltip panel."),
        new Entry("StatusStripOffsetX", "Strip offset from HUD anchor along layout X."),
        new Entry("StatusStripOffsetY", "Strip offset from HUD anchor along layout Y."),
        new Entry("StatusIconGapPx", "Gap between icons (px)."),
        new Entry("StatusIconSize", "Icon size (px); 0 = auto from layout."),
        new Entry("StatusStripAnchorMode", "How strip attaches: e.g. Max, Dialog (string)."),
        new Entry("StatusStripVerticalAlign", "Vertical align of strip: Top, Center, Bottom."),
        new Entry("StatusStripSide", "Which side of anchor icons grow: Left or Right."),
        new Entry("StatusStripUseLegacyLeadingEdgeRow", "true = old row geometry (mug slot band); false = flush trailing edge on right HUD."),
        new Entry("StatusIconAnimEnabled", "Enable enter/update/exit animations for icons."),
        new Entry("StatusIconsWaveEnabled", "Per-slot vertical wave when row baseline is unlocked."),
        new Entry("StatusStripLockRowBaseline", "null/true = lock row; false = allow slide + wave Y."),
        new Entry("StatusIconsWaveAmplitudePx", "Wave amplitude (px)."),
        new Entry("StatusIconsWavePeriodSec", "Wave period (seconds)."),
        new Entry("StatusIconsWaveStaggerSec", "Phase stagger between slots (seconds)."),
    };

    internal const string AnimBlocksNote =
        "Nested objects NeutralAnim, PositiveAnim, NegativeAnim (Enabled, durations, ShakePx, SlideDownPx, тАж) are only in JSON; use ModConfig file or editor.";
}
