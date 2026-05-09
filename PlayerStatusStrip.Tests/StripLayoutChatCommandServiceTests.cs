using Xunit;

namespace PlayerStatusStrip.Tests;

public sealed class StripLayoutChatCommandServiceTests
{
    [Theory]
    [InlineData(".striplayout show", true, "show")]
    [InlineData("/striplayout   set StatusIconSize 64", true, "set StatusIconSize 64")]
    [InlineData(".stripmock list", false, "")]
    [InlineData("", false, "")]
    public void TryExtractTail_ParsesExpectedPrefix(string input, bool expectedOk, string expectedTail)
    {
        bool ok = StripLayoutChatCommandService.TryExtractTail(input, out string tail);

        Assert.Equal(expectedOk, ok);
        Assert.Equal(expectedTail, tail);
    }
}
