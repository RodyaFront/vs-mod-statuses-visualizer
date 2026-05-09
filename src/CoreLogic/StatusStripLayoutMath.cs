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
        double rootOuterHeight,
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
        if (IsStripCenter(stripSide))
        {
            double span = activeCount * sz + System.Math.Max(0, activeCount - 1) * gap;
            // Center model: center the full row inside the container.
            stripLeft = rootRenderX + (outer - span) * 0.5 + stripOffsetX;
        }
        else if (IsStripLeft(stripSide))
        {
            double span = activeCount * sz + System.Math.Max(0, activeCount - 1) * gap;
            // Edge model: for right-edge anchors the row grows inward to the left.
            stripLeft = rootRenderX + outer - stripOffsetX - span;
        }
        else
        {
            // Edge model: for left-edge anchors the row grows inward to the right.
            stripLeft = rootRenderX + stripOffsetX;
        }

        double effectiveH = rootOuterHeight > 0 ? rootOuterHeight : anchorH;
        double yBase = StripOriginY(rootRenderY, effectiveH, sz, stripOffsetY, verticalAlign);
        return new StripLayoutNumbers(anchorWidthPx, sz, gap, stripLeft, yBase);
    }

    internal static bool IsStripLeft(string? side)
    {
        return string.Equals(side?.Trim(), "Left", System.StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsStripCenter(string? side)
    {
        return string.Equals(side?.Trim(), "Center", System.StringComparison.OrdinalIgnoreCase);
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

    internal static double StripOriginY(double rootY, double containerHeight, int iconHeight, double stripOffsetY, string verticalAlign)
    {
        string va = verticalAlign?.Trim().ToLowerInvariant() ?? "top";
        return va switch
        {
            "bottom" => rootY + System.Math.Max(0, containerHeight - iconHeight) + stripOffsetY,
            "center" => rootY + System.Math.Max(0, (containerHeight - iconHeight) * 0.5) + stripOffsetY,
            _ => rootY + stripOffsetY
        };
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
