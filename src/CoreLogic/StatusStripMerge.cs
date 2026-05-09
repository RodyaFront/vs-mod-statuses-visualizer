using System.Collections.Generic;
using Vintagestory.API.Client;

namespace PlayerStatusStrip;

internal static class StatusStripMerge
{
    private static readonly List<StatusDescriptor> Scratch = new();

    internal static void MergeInto(
        IReadOnlyList<IStatusStripProvider> providers,
        ICoreClientAPI capi,
        float deltaTime,
        List<StatusDescriptor> dest)
    {
        dest.Clear();

        Dictionary<string, StatusDescriptor> byId = new();

        foreach (IStatusStripProvider provider in providers)
        {
            Scratch.Clear();
            provider.Collect(capi, deltaTime, Scratch);

            foreach (StatusDescriptor s in Scratch)
            {
                byId[s.StableId] = s;
            }
        }

        List<StatusDescriptor> sorted = new(byId.Count);
        foreach (KeyValuePair<string, StatusDescriptor> kv in byId)
        {
            sorted.Add(kv.Value);
        }

        sorted.Sort(static (a, b) =>
        {
            int c = a.SortOrder.CompareTo(b.SortOrder);
            return c != 0 ? c : string.CompareOrdinal(a.StableId, b.StableId);
        });

        foreach (StatusDescriptor s in sorted)
        {
            dest.Add(s);
        }
    }
}
