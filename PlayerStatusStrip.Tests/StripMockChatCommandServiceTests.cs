using System.Collections.Generic;
using Xunit;

namespace PlayerStatusStrip.Tests;

public sealed class StripMockChatCommandServiceTests
{
    [Fact]
    public void TryHandle_ReturnsFalse_WhenProviderIsMissing()
    {
        var notes = new List<string>();
        var service = new StripMockChatCommandService(
            () => null,
            notes.Add);

        string message = ".stripmock list";
        global::Vintagestory.API.Common.EnumHandling handling = global::Vintagestory.API.Common.EnumHandling.PassThrough;

        bool handledByService = service.TryHandle(ref message, ref handling);

        Assert.False(handledByService);
        Assert.Equal(global::Vintagestory.API.Common.EnumHandling.PassThrough, handling);
        Assert.Empty(notes);
    }

    [Fact]
    public void TryHandle_NonMatchingPrefix_ReturnsFalse()
    {
        var notes = new List<string>();
        var provider = new MockDevProvider(useStaticMocks: false);
        var service = new StripMockChatCommandService(
            () => provider,
            notes.Add);

        string message = ".striplayout list";
        global::Vintagestory.API.Common.EnumHandling handling = global::Vintagestory.API.Common.EnumHandling.PassThrough;

        bool handledByService = service.TryHandle(ref message, ref handling);

        Assert.False(handledByService);
        Assert.Equal(global::Vintagestory.API.Common.EnumHandling.PassThrough, handling);
        Assert.Empty(notes);
    }

    [Theory]
    [InlineData(".stripmock list", true, "list")]
    [InlineData("/stripmock   run demo", true, "run demo")]
    [InlineData(".striplayout list", false, "")]
    [InlineData("", false, "")]
    public void TryExtractTail_ParsesExpectedPrefix(string input, bool expectedOk, string expectedTail)
    {
        bool ok = StripMockChatCommandService.TryExtractTail(input, out string tail);

        Assert.Equal(expectedOk, ok);
        Assert.Equal(expectedTail, tail);
    }
}
