using System;

namespace PlayerStatusStrip;

internal static class StatusAnimMath
{
    internal static StatusAffectKind ResolveEffectiveKind(
        StatusAffectKind requestedKind,
        bool requestedEnabled,
        bool neutralEnabled)
    {
        if (requestedEnabled || requestedKind == StatusAffectKind.Neutral)
        {
            return requestedKind;
        }

        return neutralEnabled ? StatusAffectKind.Neutral : requestedKind;
    }

    internal static bool ResolveEffectiveEnabled(
        StatusAffectKind requestedKind,
        bool requestedEnabled,
        bool neutralEnabled)
    {
        if (requestedEnabled)
        {
            return true;
        }

        if (requestedKind == StatusAffectKind.Neutral)
        {
            return false;
        }

        return neutralEnabled;
    }

    internal static float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * (float)Math.Pow(t - 1f, 3f) + c1 * (float)Math.Pow(t - 1f, 2f);
    }

    internal static float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    internal static float ComputePopupScale(float popupElapsed)
    {
        float t = Math.Clamp(popupElapsed, 0f, 1f);
        return EaseOutBack(t);
    }

    internal static float ComputePulseScale(float progress01, float amplitude)
    {
        float t = Math.Clamp(progress01, 0f, 1f);
        return 1f + amplitude * (float)Math.Sin(Math.PI * t);
    }

    internal static float EnterScale(StatusAffectKind kind, float progress01)
    {
        float t = Math.Clamp(progress01, 0f, 1f);
        return kind switch
        {
            StatusAffectKind.Positive => 1f + 0.08f * (float)Math.Sin(Math.PI * t),
            StatusAffectKind.Negative => 0.92f + 0.08f * t,
            _ => ComputePopupScale(t)
        };
    }

    internal static float ExitScale(StatusAffectKind kind, float progress01)
    {
        float t = SmoothStep(Math.Clamp(progress01, 0f, 1f));
        float baseScale = 1f - t;
        return kind == StatusAffectKind.Negative ? baseScale * 0.95f : baseScale;
    }

    internal static float ExitAlpha(float progress01)
    {
        float t = SmoothStep(Math.Clamp(progress01, 0f, 1f));
        return 1f - t;
    }

    internal static float HorizontalShakeOffset(StatusAffectKind kind, float progress01, float shakePx)
    {
        if (kind != StatusAffectKind.Negative || shakePx <= 1e-4f)
        {
            return 0f;
        }

        float t = Math.Clamp(progress01, 0f, 1f);
        float envelope = 1f - t;
        return shakePx * envelope * (float)Math.Sin(t * Math.PI * 8f);
    }

    internal static float UpdateVerticalOffset(StatusAffectKind kind, float progress01, float slideDownPx)
    {
        if (slideDownPx <= 1e-4f)
        {
            return 0f;
        }

        float t = SmoothStep(Math.Clamp(progress01, 0f, 1f));
        return kind switch
        {
            StatusAffectKind.Negative => slideDownPx * (1f - t),
            StatusAffectKind.Positive => -0.5f * slideDownPx * (1f - t),
            _ => 0f
        };
    }
}
