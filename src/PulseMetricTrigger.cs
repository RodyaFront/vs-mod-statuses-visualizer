using System;

namespace PlayerStatusStrip;

internal static class PulseMetricTrigger
{
    internal const float RelativeThreshold = 0.012f;

    internal const float AbsoluteFloor = 0.001f;

    internal static bool ShouldPulse(float prevM, float nowM)
    {
        float d = Math.Abs(nowM - prevM);
        if (d < 1e-8f)
        {
            return false;
        }

        float scale = Math.Max(Math.Max(Math.Abs(prevM), Math.Abs(nowM)), 1e-9f);
        float relative = d / scale;

        if (relative >= RelativeThreshold)
        {
            return true;
        }

        return d >= AbsoluteFloor;
    }
}
