using System.Collections.Generic;

namespace PlayerStatusStrip;

internal static class MockScenarioCatalog
{
    internal static IReadOnlyDictionary<string, MockScenarioDefinition> All { get; } = Build();

    private static Dictionary<string, MockScenarioDefinition> Build()
    {
        Dictionary<string, MockScenarioDefinition> d = new();

        d["meal"] = new MockScenarioDefinition(
            "meal",
            "playerstatusstrip:mock-scenario-meal-title",
            new MockScenarioSegment[]
            {
                new(2.2, new MockSlotFrame("mock:meal-full", 'a', 5, "playerstatusstrip:mock-scenario-meal-satiety", MockPulseType.None, StatusAffectKind.Positive)),
                new(4.0, new MockSlotFrame("mock:meal-full", 'a', 5, "playerstatusstrip:mock-scenario-meal-satiety", MockPulseType.None, StatusAffectKind.Positive),
                    new MockSlotFrame("mock:meal-regen", 'b', 15, "playerstatusstrip:mock-scenario-meal-regen", MockPulseType.Sine01, StatusAffectKind.Positive)),
                new(3.5, new MockSlotFrame("mock:meal-regen", 'b', 15, "playerstatusstrip:mock-scenario-meal-regen", MockPulseType.Sine01, StatusAffectKind.Positive)),
                new(2.0, new MockSlotFrame[0]),
            });

        d["mining"] = new MockScenarioDefinition(
            "mining",
            "playerstatusstrip:mock-scenario-mining-title",
            new MockScenarioSegment[]
            {
                new(5.0, new MockSlotFrame("mock:mine-haste", 'c', 8, "playerstatusstrip:mock-scenario-mining-haste", MockPulseType.None, StatusAffectKind.Positive)),
                new(5.0, new MockSlotFrame("mock:mine-haste", 'c', 8, "playerstatusstrip:mock-scenario-mining-haste", MockPulseType.None, StatusAffectKind.Positive),
                    new MockSlotFrame("mock:mine-tired", 'd', 22, "playerstatusstrip:mock-scenario-mining-tired", MockPulseType.None, StatusAffectKind.Negative)),
                new(4.0, new MockSlotFrame("mock:mine-tired", 'd', 22, "playerstatusstrip:mock-scenario-mining-tired", MockPulseType.Sine01, StatusAffectKind.Negative)),
                new(1.5, new MockSlotFrame[0]),
            });

        d["combat"] = new MockScenarioDefinition(
            "combat",
            "playerstatusstrip:mock-scenario-combat-title",
            new MockScenarioSegment[]
            {
                new(2.5, new MockSlotFrame("mock:combat-str", 'a', 6, "playerstatusstrip:mock-scenario-combat-str", MockPulseType.None, StatusAffectKind.Positive)),
                new(4.0, new MockSlotFrame("mock:combat-str", 'a', 6, "playerstatusstrip:mock-scenario-combat-str", MockPulseType.None, StatusAffectKind.Positive),
                    new MockSlotFrame("mock:combat-bleed", 'd', 20, "playerstatusstrip:mock-scenario-combat-bleed", MockPulseType.FastSine, StatusAffectKind.Negative)),
                new(5.0, new MockSlotFrame("mock:combat-bleed", 'd', 20, "playerstatusstrip:mock-scenario-combat-bleed", MockPulseType.FastSine, StatusAffectKind.Negative),
                    new MockSlotFrame("mock:combat-regen", 'b', 14, "playerstatusstrip:mock-scenario-combat-regen", MockPulseType.Sine01, StatusAffectKind.Positive)),
                new(3.0, new MockSlotFrame("mock:combat-regen", 'b', 14, "playerstatusstrip:mock-scenario-combat-regen", MockPulseType.RampDown, StatusAffectKind.Positive)),
                new(1.5, new MockSlotFrame[0]),
            });

        d["weather"] = new MockScenarioDefinition(
            "weather",
            "playerstatusstrip:mock-scenario-weather-title",
            new MockScenarioSegment[]
            {
                new(3.0, new MockSlotFrame("mock:weather-wet", 'c', 10, "playerstatusstrip:mock-scenario-weather-wet", MockPulseType.None, StatusAffectKind.Negative)),
                new(4.5, new MockSlotFrame("mock:weather-wet", 'c', 10, "playerstatusstrip:mock-scenario-weather-wet", MockPulseType.Sine01, StatusAffectKind.Negative),
                    new MockSlotFrame("mock:weather-cold", 'd', 25, "playerstatusstrip:mock-scenario-weather-cold", MockPulseType.None, StatusAffectKind.Negative)),
                new(4.0, new MockSlotFrame("mock:weather-cold", 'd', 25, "playerstatusstrip:mock-scenario-weather-cold", MockPulseType.Sine01, StatusAffectKind.Negative)),
                new(2.0, new MockSlotFrame[0]),
            });

        d["recovery"] = new MockScenarioDefinition(
            "recovery",
            "playerstatusstrip:mock-scenario-recovery-title",
            new MockScenarioSegment[]
            {
                new(1.2, new MockSlotFrame[0]),
                new(1.5, new MockSlotFrame("mock:recovery-weary", 'a', 8, "playerstatusstrip:mock-scenario-recovery-weary", MockPulseType.None, StatusAffectKind.Neutral)),
                new(2.0, new MockSlotFrame("mock:recovery-weary", 'a', 8, "playerstatusstrip:mock-scenario-recovery-weary", MockPulseType.None, StatusAffectKind.Neutral),
                    new MockSlotFrame("mock:recovery-chill", 'b', 12, "playerstatusstrip:mock-scenario-recovery-chill", MockPulseType.None, StatusAffectKind.Negative)),
                new(2.5, new MockSlotFrame("mock:recovery-weary", 'a', 8, "playerstatusstrip:mock-scenario-recovery-weary", MockPulseType.None, StatusAffectKind.Neutral),
                    new MockSlotFrame("mock:recovery-chill", 'b', 12, "playerstatusstrip:mock-scenario-recovery-chill", MockPulseType.None, StatusAffectKind.Negative),
                    new MockSlotFrame("mock:recovery-relief", 'c', 16, "playerstatusstrip:mock-scenario-recovery-relief", MockPulseType.Sine01, StatusAffectKind.Positive)),
                new(2.0, new MockSlotFrame("mock:recovery-comfort", 'd', 5, "playerstatusstrip:mock-scenario-recovery-comfort", MockPulseType.RampUp, StatusAffectKind.Positive)),
                new(2.0, new MockSlotFrame[0]),
            });

        d["buzz"] = new MockScenarioDefinition(
            "buzz",
            "playerstatusstrip:mock-scenario-buzz-title",
            new MockScenarioSegment[]
            {
                new(3.0, new MockSlotFrame("mock:buzz", 'a', 7, "playerstatusstrip:mock-scenario-buzz-rise", MockPulseType.RampUp, StatusAffectKind.Positive)),
                new(5.0, new MockSlotFrame("mock:buzz", 'a', 7, "playerstatusstrip:mock-scenario-buzz-peak", MockPulseType.Sine01, StatusAffectKind.Positive)),
                new(6.0, new MockSlotFrame("mock:buzz", 'a', 7, "playerstatusstrip:mock-scenario-buzz-fade", MockPulseType.RampDown, StatusAffectKind.Neutral)),
                new(1.5, new MockSlotFrame[0]),
            });

        return d;
    }
}

internal sealed class MockScenarioDefinition
{
    internal MockScenarioDefinition(string id, string titleLangKey, MockScenarioSegment[] segments)
    {
        Id = id;
        TitleLangKey = titleLangKey;
        Segments = segments;
    }

    internal string Id { get; }

    internal string TitleLangKey { get; }

    internal MockScenarioSegment[] Segments { get; }
}

internal readonly struct MockScenarioSegment
{
    internal MockScenarioSegment(double durationSec, params MockSlotFrame[] slots)
    {
        DurationSec = durationSec;
        Slots = slots;
    }

    internal double DurationSec { get; }

    internal MockSlotFrame[] Slots { get; }
}

internal readonly struct MockSlotFrame
{
    internal MockSlotFrame(
        string stableId,
        char iconLetter,
        int sortOrder,
        string tooltipLangKey,
        MockPulseType pulseType,
        StatusAffectKind affectKind = StatusAffectKind.Neutral)
    {
        StableId = stableId;
        IconLetter = iconLetter;
        SortOrder = sortOrder;
        TooltipLangKey = tooltipLangKey;
        PulseType = pulseType;
        AffectKind = affectKind;
    }

    internal string StableId { get; }

    internal char IconLetter { get; }

    internal int SortOrder { get; }

    internal string TooltipLangKey { get; }

    internal MockPulseType PulseType { get; }

    internal StatusAffectKind AffectKind { get; }
}

internal enum MockPulseType
{
    None,
    Sine01,
    FastSine,
    RampUp,
    RampDown,
}
