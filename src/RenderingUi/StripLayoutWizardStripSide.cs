using System;

namespace PlayerStatusStrip;

/// <summary>
/// Picks <see cref="StatusStripLayoutConfig.StatusStripSide"/> so the icon row stays on-screen for the chosen HUD anchor.
/// With <c>Left</c>, the row grows to the left of the anchor; with <c>Right</c>, to the right — see <see cref="StatusStripLayoutMath"/>.
/// </summary>
internal static class StripLayoutWizardStripSide
{
    internal static string ForDialogArea(string? dialogArea)
    {
        if (string.IsNullOrWhiteSpace(dialogArea))
        {
            return "Left";
        }

        string a = dialogArea.Trim();
        if (a.StartsWith("Left", StringComparison.OrdinalIgnoreCase))
        {
            return "Right";
        }

        if (a.StartsWith("Center", StringComparison.OrdinalIgnoreCase))
        {
            return "Center";
        }

        return "Left";
    }
}
