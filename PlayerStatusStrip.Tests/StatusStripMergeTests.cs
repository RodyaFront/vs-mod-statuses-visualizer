using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Xunit;

namespace PlayerStatusStrip.Tests;

public sealed class StatusStripMergeTests
{
    private static AssetLocation Icon(string path) => new("playerstatusstrip", path);

    private sealed class Provider(params StatusDescriptor[] items) : IStatusStripProvider
    {
        public void Collect(ICoreClientAPI capi, float deltaTime, List<StatusDescriptor> dest)
        {
            foreach (StatusDescriptor s in items)
            {
                dest.Add(s);
            }
        }
    }

    [Fact]
    public void Merge_LaterProviderWinsOnDuplicateStableId()
    {
        var a1 = new StatusDescriptor("x", Icon("first.png"), 0, "a");
        var a2 = new StatusDescriptor("x", Icon("second.png"), 0, "b");
        IStatusStripProvider[] list = { new Provider(a1), new Provider(a2) };
        var dest = new List<StatusDescriptor>();
        StatusStripMerge.MergeInto(list, null!, 0f, dest);
        Assert.Single(dest);
        Assert.Equal("second.png", dest[0].Icon.Path);
    }

    [Fact]
    public void Merge_SortsBySortOrderThenStableId()
    {
        var s1 = new StatusDescriptor("b", Icon("b.png"), 2, "");
        var s2 = new StatusDescriptor("a", Icon("a.png"), 1, "");
        var s3 = new StatusDescriptor("c", Icon("c.png"), 1, "");
        IStatusStripProvider[] list = { new Provider(s1, s2, s3) };
        var dest = new List<StatusDescriptor>();
        StatusStripMerge.MergeInto(list, null!, 0f, dest);
        Assert.Equal(3, dest.Count);
        Assert.Equal("a", dest[0].StableId);
        Assert.Equal("c", dest[1].StableId);
        Assert.Equal("b", dest[2].StableId);
    }
}
