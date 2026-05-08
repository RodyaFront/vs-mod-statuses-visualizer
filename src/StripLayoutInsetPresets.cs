using System;

namespace PlayerStatusStrip;

/// <summary>Maps wizard inset steps to <see cref="StatusStripLayoutConfig.DialogOffsetX"/> / Y for each <see cref="EnumDialogArea"/> code.</summary>
internal static class StripLayoutInsetPresets
{
    internal static readonly int[] StepPx = { 4, 8, 16, 32 };

    /// <summary>
    /// Vintage Story HUD top anchors often look farther from the physical top than the horizontal inset,
    /// partly due to layer/chrome above the playable HUD. Pulls Y inward (smaller positive offset) for top corners.
    /// </summary>
    internal const double TopAnchorYTrimPx = 10;

    internal static (double X, double Y) OffsetsForArea(string? dialogArea, int stepIndex)
    {
        int k = StepPx[Math.Clamp(stepIndex, 0, StepPx.Length - 1)];
        if (string.IsNullOrWhiteSpace(dialogArea))
        {
            return (-k, k);
        }

        string a = dialogArea.Trim();
        bool top = a.EndsWith("Top", StringComparison.OrdinalIgnoreCase);
        bool bottom = a.EndsWith("Bottom", StringComparison.OrdinalIgnoreCase);
        bool middle = a.EndsWith("Middle", StringComparison.OrdinalIgnoreCase);
        bool right = a.StartsWith("Right", StringComparison.OrdinalIgnoreCase);
        bool left = a.StartsWith("Left", StringComparison.OrdinalIgnoreCase);
        bool center = a.StartsWith("Center", StringComparison.OrdinalIgnoreCase);

        double yTop = top ? CompensateTopY(k) : k;

        if (right && top)
        {
            return (-k, yTop);
        }

        if (left && top)
        {
            return (k, yTop);
        }

        if (center && top)
        {
            return (0, yTop);
        }

        if (right && bottom)
        {
            return (-k, -k);
        }

        if (left && bottom)
        {
            return (k, -k);
        }

        if (center && bottom)
        {
            return (0, -k);
        }

        if (right && middle)
        {
            return (-k, 0);
        }

        if (left && middle)
        {
            return (k, 0);
        }

        return (-k, k);
    }

    private static double CompensateTopY(int k)
    {
        return Math.Max(2, k - TopAnchorYTrimPx);
    }

    internal static int NearestStepIndex(string? dialogArea, double offsetX, double offsetY)
    {
        int best = 0;
        double bestD = double.MaxValue;
        for (int i = 0; i < StepPx.Length; i++)
        {
            (double px, double py) = OffsetsForArea(dialogArea, i);
            double d = (offsetX - px) * (offsetX - px) + (offsetY - py) * (offsetY - py);
            if (d < bestD)
            {
                bestD = d;
                best = i;
            }
        }

        return best;
    }
}
