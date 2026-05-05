namespace PlayerStatusStrip;

internal readonly struct StripLayoutNumbers
{
    internal StripLayoutNumbers(int anchorWidthPx, int sz, int gap, double stripLeft, double yBase)
    {
        AnchorWidthPx = anchorWidthPx;
        Sz = sz;
        Gap = gap;
        StripLeft = stripLeft;
        YBase = yBase;
    }

    internal int AnchorWidthPx { get; }

    internal int Sz { get; }

    internal int Gap { get; }

    internal double StripLeft { get; }

    internal double YBase { get; }
}

internal static class StatusStripLayoutMath
{
    internal static StripLayoutNumbers Compute(
        double rootRenderX,
        double rootRenderY,
        double rootOuterWidth,
        int anchorWidthPx,
        int statusIconSizeOrZero,
        int gapPx,
        string stripSide,
        double stripOffsetX,
        double stripOffsetY,
        string anchorMode,
        string verticalAlign,
        int activeCount,
        int anchorHeightPxOrZero = 0,
        int anchorOuterWidthPxOrZero = 0,
        bool alignStatusRowToTrailingEdge = false)
    {
        int anchorH = anchorHeightPxOrZero > 0 ? anchorHeightPxOrZero : anchorWidthPx;
        double outer = anchorOuterWidthPxOrZero > 0 ? anchorOuterWidthPxOrZero : rootOuterWidth;
        int sz = statusIconSizeOrZero > 0 ? statusIconSizeOrZero : anchorWidthPx;
        int gap = gapPx;

        double stripLeft;
        if (IsStripLeft(stripSide) && activeCount > 0)
        {
            double span = activeCount * sz + System.Math.Max(0, activeCount - 1) * gap;
            if (alignStatusRowToTrailingEdge)
            {
                stripLeft = rootRenderX + outer - stripOffsetX - span;
            }
            else
            {
                stripLeft = rootRenderX - stripOffsetX - span;
            }
        }
        else
        {
            double anchorW = AnchorWidth(outer, anchorWidthPx, anchorMode);
            stripLeft = rootRenderX + anchorW + stripOffsetX;
        }

        double yBase = StripOriginY(rootRenderY, anchorH, sz, stripOffsetY, verticalAlign);
        return new StripLayoutNumbers(anchorWidthPx, sz, gap, stripLeft, yBase);
    }

    internal static bool IsStripLeft(string? side)
    {
        return string.Equals(side?.Trim(), "Left", System.StringComparison.OrdinalIgnoreCase);
    }

    internal static double AnchorWidth(double outer, int anchorPx, string anchorMode)
    {
        return anchorMode.Trim() switch
        {
            "Reference" => anchorPx,
            "Mug" => anchorPx,
            "Dialog" => outer,
            _ => System.Math.Max(anchorPx, outer)
        };
    }

    internal static double StripOriginY(double rootY, int anchorHeightPx, int iconHeight, double stripOffsetY, string verticalAlign)
    {
        double y = rootY;
        switch (verticalAlign.Trim().ToLowerInvariant())
        {
            case "center":
                y += (anchorHeightPx - iconHeight) / 2.0;
                break;
            case "bottom":
                y += anchorHeightPx - iconHeight;
                break;
        }

        return y + stripOffsetY;
    }

    internal static void IconRect(
        ref StripLayoutNumbers strip,
        int visibleIndex,
        double waveYOffset,
        out double left,
        out double top,
        out int sz)
    {
        sz = strip.Sz;
        left = strip.StripLeft + visibleIndex * (sz + strip.Gap);
        top = strip.YBase - waveYOffset;
    }
}
