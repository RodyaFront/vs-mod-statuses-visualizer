using Xunit;

namespace PlayerStatusStrip.Tests;

public sealed class MockScenarioRunnerTests
{
    [Theory]
    [InlineData("mock:meal-full", 'z', "textures/icons/mock_meal-full.png")]
    [InlineData("mock:recovery-weary", 'a', "textures/icons/mock_recovery-weary.png")]
    [InlineData("mock:buzz", 'a', "textures/icons/mock_buzz.png")]
    public void MockScenarioIconPath_MockPrefix_UsesFilePerStableTail(string stableId, char letter, string expected)
    {
        Assert.Equal(expected, MockScenarioRunner.MockScenarioIconPath(stableId, letter));
    }

    [Fact]
    public void MockScenarioIconPath_NonMock_FallsBackToLetter()
    {
        Assert.Equal("textures/icons/mock_c.png", MockScenarioRunner.MockScenarioIconPath("other:thing", 'C'));
    }
}
