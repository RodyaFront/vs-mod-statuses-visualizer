using Xunit;

namespace PlayerStatusStrip.Tests;

public sealed class PulseMetricTriggerTests
{
    [Fact]
    public void ShouldPulse_NoChange_ReturnsFalse()
    {
        Assert.False(PulseMetricTrigger.ShouldPulse(0.5f, 0.5f));
    }

    [Fact]
    public void ShouldPulse_LargeRelativeJump_ReturnsTrue()
    {
        Assert.True(PulseMetricTrigger.ShouldPulse(0.5f, 0.52f));
    }

    [Fact]
    public void ShouldPulse_TinyChangeBelowFloor_ReturnsFalse()
    {
        Assert.False(PulseMetricTrigger.ShouldPulse(0.5f, 0.5003f));
    }

    [Fact]
    public void ShouldPulse_AbsoluteFloorCross_ReturnsTrue()
    {
        Assert.True(PulseMetricTrigger.ShouldPulse(0f, PulseMetricTrigger.AbsoluteFloor));
    }
}
