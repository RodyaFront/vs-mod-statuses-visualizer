using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace PlayerStatusStrip;

internal sealed class MockScenarioRunner
{
    private readonly MockScenarioDefinition _def;
    private int _segmentIndex;
    private double _timeInSegment;
    private double _scenarioElapsed;

    internal MockScenarioRunner(MockScenarioDefinition def)
    {
        _def = def;
    }

    internal string ScenarioId => _def.Id;

    internal bool IsFinished => _segmentIndex >= _def.Segments.Length;

    internal void Stop()
    {
        _segmentIndex = _def.Segments.Length;
    }

    internal void Advance(float deltaTime)
    {
        if (IsFinished)
        {
            return;
        }

        _timeInSegment += deltaTime;
        _scenarioElapsed += deltaTime;
        while (!IsFinished && _timeInSegment >= _def.Segments[_segmentIndex].DurationSec - 1e-6)
        {
            _timeInSegment -= _def.Segments[_segmentIndex].DurationSec;
            _segmentIndex++;
        }
    }

    internal void Collect(ICoreClientAPI capi, List<StatusDescriptor> dest)
    {
        if (IsFinished)
        {
            return;
        }

        MockScenarioSegment seg = _def.Segments[_segmentIndex];
        double segT = _timeInSegment;
        double segDur = seg.DurationSec;
        float phase01 = segDur > 1e-6 ? (float)Math.Clamp(segT / segDur, 0, 1) : 0f;

        foreach (MockSlotFrame slot in seg.Slots)
        {
            float? pulse = PulseValue(slot.PulseType, _scenarioElapsed, phase01);
            string mod = "playerstatusstrip";
            string path = MockScenarioIconPath(slot.StableId, slot.IconLetter);
            dest.Add(new StatusDescriptor(
                slot.StableId,
                new AssetLocation(mod, path),
                slot.SortOrder,
                Lang.Get(slot.TooltipLangKey),
                pulse,
                slot.AffectKind));
        }
    }

    internal static string MockScenarioIconPath(string stableId, char iconLetter)
    {
        if (stableId.StartsWith("mock:", StringComparison.Ordinal))
        {
            return $"textures/icons/mock_{stableId.AsSpan(5)}.png";
        }

        return $"textures/icons/mock_{char.ToLowerInvariant(iconLetter)}.png";
    }

    private static float? PulseValue(MockPulseType type, double scenarioElapsed, float segmentPhase01)
    {
        return type switch
        {
            MockPulseType.None => null,
            MockPulseType.Sine01 => (float)(0.5 + 0.5 * Math.Sin(scenarioElapsed * 2.2)),
            MockPulseType.FastSine => (float)(0.5 + 0.5 * Math.Sin(scenarioElapsed * 5.5)),
            MockPulseType.RampUp => segmentPhase01,
            MockPulseType.RampDown => 1f - segmentPhase01,
            _ => null,
        };
    }
}
