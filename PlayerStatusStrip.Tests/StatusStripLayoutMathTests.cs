using Xunit;

namespace PlayerStatusStrip.Tests;

public sealed class StatusStripLayoutMathTests
{
    [Fact]
    public void Compute_LeftSide_SubtractsSpanAndOffset()
    {
        StripLayoutNumbers n = StatusStripLayoutMath.Compute(
            rootRenderX: 100,
            rootRenderY: 20,
            rootOuterWidth: 200,
            rootOuterHeight: 0,
            anchorWidthPx: 32,
            statusIconSizeOrZero: 0,
            gapPx: 4,
            stripSide: "Left",
            stripOffsetX: 4,
            stripOffsetY: 2,
            anchorMode: "Reference",
            verticalAlign: "top",
            activeCount: 3);

        double span = 3 * 32 + 2 * 4;
        Assert.Equal(100 + 200 - 4 - span, n.StripLeft, 5);
        Assert.Equal(32, n.Sz);
        Assert.Equal(4, n.Gap);
        Assert.Equal(22, n.YBase, 5);
    }

    [Fact]
    public void Compute_RightSide_UsesReferenceAnchorWidth()
    {
        StripLayoutNumbers n = StatusStripLayoutMath.Compute(
            rootRenderX: 100,
            rootRenderY: 0,
            rootOuterWidth: 200,
            rootOuterHeight: 0,
            anchorWidthPx: 32,
            statusIconSizeOrZero: 0,
            gapPx: 4,
            stripSide: "Right",
            stripOffsetX: 6,
            stripOffsetY: 0,
            anchorMode: "Reference",
            verticalAlign: "top",
            activeCount: 2);

        Assert.Equal(100 + 6, n.StripLeft, 5);
    }

    [Fact]
    public void AnchorMode_LegacyMug_MatchesReference()
    {
        double outer = 200;
        Assert.Equal(
            StatusStripLayoutMath.AnchorWidth(outer, 32, "Reference"),
            StatusStripLayoutMath.AnchorWidth(outer, 32, "Mug"));
    }

    [Fact]
    public void IconRect_IndexAdvancesBySizePlusGap()
    {
        var strip = new StripLayoutNumbers(32, 16, 5, 10, 20);
        StatusStripLayoutMath.IconRect(ref strip, 2, 3, out double left, out double top, out int sz);
        Assert.Equal(16, sz);
        Assert.Equal(10 + 2 * (16 + 5), left, 5);
        Assert.Equal(20 - 3, top, 5);
    }

    [Fact]
    public void AnchorWidth_Dialog_UsesOuter()
    {
        Assert.Equal(200, StatusStripLayoutMath.AnchorWidth(200, 32, "Dialog"));
    }

    [Fact]
    public void StripOriginY_Center_OffsetsHalfDifference()
    {
        double y = StatusStripLayoutMath.StripOriginY(10, 40, 16, 0, "center");
        Assert.Equal(10 + (40 - 16) / 2.0, y, 5);
    }

    [Fact]
    public void Compute_AnchorOuterWidthOverride_AffectsMaxMode()
    {
        StripLayoutNumbers n = StatusStripLayoutMath.Compute(
            rootRenderX: 100,
            rootRenderY: 0,
            rootOuterWidth: 20,
            rootOuterHeight: 0,
            anchorWidthPx: 32,
            statusIconSizeOrZero: 0,
            gapPx: 4,
            stripSide: "Right",
            stripOffsetX: 0,
            stripOffsetY: 0,
            anchorMode: "Max",
            verticalAlign: "top",
            activeCount: 1,
            anchorHeightPxOrZero: 0,
            anchorOuterWidthPxOrZero: 80);

        Assert.Equal(100, n.StripLeft, 5);
    }

    [Fact]
    public void Compute_AnchorHeight_OverridesVerticalReference()
    {
        StripLayoutNumbers n = StatusStripLayoutMath.Compute(
            rootRenderX: 0,
            rootRenderY: 10,
            rootOuterWidth: 200,
            rootOuterHeight: 0,
            anchorWidthPx: 32,
            statusIconSizeOrZero: 16,
            gapPx: 4,
            stripSide: "Right",
            stripOffsetX: 0,
            stripOffsetY: 0,
            anchorMode: "Reference",
            verticalAlign: "center",
            activeCount: 1,
            anchorHeightPxOrZero: 48,
            anchorOuterWidthPxOrZero: 0);

        Assert.Equal(10 + (48 - 16) / 2.0, n.YBase, 5);
    }

    [Fact]
    public void Compute_LeftSide_TrailingAlign_PlacesRowFlushToOuterRight()
    {
        double rootX = 1000;
        double outerW = 32;
        int sz = 32;
        int gap = 4;
        int count = 3;
        double span = count * sz + (count - 1) * gap;

        StripLayoutNumbers n = StatusStripLayoutMath.Compute(
            rootRenderX: rootX,
            rootRenderY: 20,
            rootOuterWidth: outerW,
            rootOuterHeight: 0,
            anchorWidthPx: 32,
            statusIconSizeOrZero: 0,
            gapPx: gap,
            stripSide: "Left",
            stripOffsetX: 0,
            stripOffsetY: 2,
            anchorMode: "Reference",
            verticalAlign: "top",
            activeCount: count,
            anchorHeightPxOrZero: 0,
            anchorOuterWidthPxOrZero: 0,
            alignStatusRowToTrailingEdge: true);

        Assert.Equal(rootX + outerW - span, n.StripLeft, 5);
    }
}
