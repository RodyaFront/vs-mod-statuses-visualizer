using System;

namespace PlayerStatusStrip;

internal static class StatusStripScreenPlacement
{
    internal static bool TryResolveBounds(
        string? dialogArea,
        double offsetX,
        double offsetY,
        double width,
        double height,
        int frameWidthPx,
        int frameHeightPx,
        double guiScale,
        out (double X, double Y, double Width, double Height) bounds)
    {
        bounds = default;
        if (string.IsNullOrWhiteSpace(dialogArea))
        {
            return false;
        }

        string area = dialogArea.Trim();
        if (!TryResolveAxes(area, out HorizontalAxis hx, out VerticalAxis vy))
        {
            return false;
        }

        double scale = Math.Max(1e-6, guiScale);
        double frameW = frameWidthPx / scale;
        double frameH = frameHeightPx / scale;

        double x = hx switch
        {
            HorizontalAxis.Left => offsetX,
            HorizontalAxis.Center => (frameW - width) * 0.5 + offsetX,
            HorizontalAxis.Right => frameW - width + offsetX,
            _ => offsetX
        };

        double y = vy switch
        {
            VerticalAxis.Top => offsetY,
            VerticalAxis.Middle => (frameH - height) * 0.5 + offsetY,
            VerticalAxis.Bottom => frameH - height + offsetY,
            _ => offsetY
        };

        bounds = (x, y, width, height);
        return true;
    }

    private static bool TryResolveAxes(string area, out HorizontalAxis hx, out VerticalAxis vy)
    {
        hx = default;
        vy = default;

        if (area.StartsWith("Left", StringComparison.OrdinalIgnoreCase))
        {
            hx = HorizontalAxis.Left;
        }
        else if (area.StartsWith("Center", StringComparison.OrdinalIgnoreCase))
        {
            hx = HorizontalAxis.Center;
        }
        else if (area.StartsWith("Right", StringComparison.OrdinalIgnoreCase))
        {
            hx = HorizontalAxis.Right;
        }
        else
        {
            return false;
        }

        if (area.EndsWith("Top", StringComparison.OrdinalIgnoreCase))
        {
            vy = VerticalAxis.Top;
            return true;
        }

        if (area.EndsWith("Middle", StringComparison.OrdinalIgnoreCase))
        {
            vy = VerticalAxis.Middle;
            return true;
        }

        if (area.EndsWith("Bottom", StringComparison.OrdinalIgnoreCase))
        {
            vy = VerticalAxis.Bottom;
            return true;
        }

        return false;
    }

    private enum HorizontalAxis
    {
        Left,
        Center,
        Right
    }

    private enum VerticalAxis
    {
        Top,
        Middle,
        Bottom
    }
}
