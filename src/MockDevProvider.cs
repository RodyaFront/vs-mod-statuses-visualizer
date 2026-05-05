using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace PlayerStatusStrip;

internal sealed class MockDevProvider : IStatusStripProvider
{
    private readonly bool _useStaticMocks;
    private float _staticAccumSec;
    private MockScenarioRunner? _scenario;

    internal MockDevProvider(bool useStaticMocks)
    {
        _useStaticMocks = useStaticMocks;
    }

    internal bool TryStartScenario(string id, out string error)
    {
        error = "";
        if (!MockScenarioCatalog.All.TryGetValue(id.Trim().ToLowerInvariant(), out MockScenarioDefinition? def))
        {
            error = "unknown";
            return false;
        }

        _scenario = new MockScenarioRunner(def);
        return true;
    }

    internal void StopScenario()
    {
        _scenario = null;
    }

    internal bool HasActiveScenario => _scenario != null && !_scenario.IsFinished;

    public void Collect(ICoreClientAPI capi, float deltaTime, List<StatusDescriptor> dest)
    {
        if (_scenario != null)
        {
            _scenario.Advance(deltaTime);
            if (_scenario.IsFinished)
            {
                _scenario = null;
                return;
            }

            _scenario.Collect(capi, dest);
            return;
        }

        if (!_useStaticMocks)
        {
            return;
        }

        _staticAccumSec += deltaTime;
        string mod = "playerstatusstrip";
        float pulse = (float)(System.Math.Sin(_staticAccumSec * 2.0) * 0.5 + 0.5);

        dest.Add(new StatusDescriptor(
            "mock:a",
            new AssetLocation(mod, "textures/icons/mock_a.png"),
            10,
            Lang.Get("playerstatusstrip:mock-tooltip-a"),
            pulse,
            StatusAffectKind.Neutral));

        dest.Add(new StatusDescriptor(
            "mock:b",
            new AssetLocation(mod, "textures/icons/mock_b.png"),
            20,
            Lang.Get("playerstatusstrip:mock-tooltip-b"),
            pulse * 0.9f,
            StatusAffectKind.Positive));

        dest.Add(new StatusDescriptor(
            "mock:c",
            new AssetLocation(mod, "textures/icons/mock_c.png"),
            30,
            Lang.Get("playerstatusstrip:mock-tooltip-c"),
            null,
            StatusAffectKind.Neutral));

        dest.Add(new StatusDescriptor(
            "mock:d",
            new AssetLocation(mod, "textures/icons/mock_d.png"),
            40,
            Lang.Get("playerstatusstrip:mock-tooltip-d"),
            0.01f,
            StatusAffectKind.Negative));
    }
}
