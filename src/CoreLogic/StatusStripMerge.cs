using System.Collections.Generic;
using Vintagestory.API.Client;

namespace PlayerStatusStrip;

internal sealed class StatusStripMergeReport
{
    internal readonly struct DuplicateOverwrite
    {
        internal DuplicateOverwrite(string stableId, string replacedProvider, string winnerProvider)
        {
            StableId = stableId;
            ReplacedProvider = replacedProvider;
            WinnerProvider = winnerProvider;
        }

        internal string StableId { get; }
        internal string ReplacedProvider { get; }
        internal string WinnerProvider { get; }
    }

    internal readonly struct ProviderFrameSnapshot
    {
        internal ProviderFrameSnapshot(string providerName, int emittedCount, HashSet<string> stableIds)
        {
            ProviderName = providerName;
            EmittedCount = emittedCount;
            StableIds = stableIds;
        }

        internal string ProviderName { get; }
        internal int EmittedCount { get; }
        internal HashSet<string> StableIds { get; }
    }

    internal int InputStatusCount { get; private set; }
    internal int OutputStatusCount { get; set; }
    internal List<DuplicateOverwrite> DuplicateOverwrites { get; } = new();
    internal List<ProviderFrameSnapshot> Providers { get; } = new();

    internal void BeginFrame()
    {
        InputStatusCount = 0;
        OutputStatusCount = 0;
        DuplicateOverwrites.Clear();
        Providers.Clear();
    }

    internal void AddInputStatusCount(int count)
    {
        InputStatusCount += count;
    }
}

internal static class StatusStripMerge
{
    private static readonly List<StatusDescriptor> Scratch = new();

    internal static void MergeInto(
        IReadOnlyList<IStatusStripProvider> providers,
        ICoreClientAPI capi,
        float deltaTime,
        List<StatusDescriptor> dest)
    {
        MergeInto(providers, capi, deltaTime, dest, report: null);
    }

    internal static void MergeInto(
        IReadOnlyList<IStatusStripProvider> providers,
        ICoreClientAPI capi,
        float deltaTime,
        List<StatusDescriptor> dest,
        StatusStripMergeReport? report)
    {
        report?.BeginFrame();
        dest.Clear();

        Dictionary<string, StatusDescriptor> byId = new();
        Dictionary<string, string> ownerById = new();

        foreach (IStatusStripProvider provider in providers)
        {
            Scratch.Clear();
            provider.Collect(capi, deltaTime, Scratch);
            report?.AddInputStatusCount(Scratch.Count);
            string providerName = provider.GetType().Name;
            HashSet<string>? providerStableIds = report != null ? new HashSet<string>() : null;

            foreach (StatusDescriptor s in Scratch)
            {
                providerStableIds?.Add(s.StableId);
                if (report != null
                    && ownerById.TryGetValue(s.StableId, out string? replacedProvider))
                {
                    report.DuplicateOverwrites.Add(
                        new StatusStripMergeReport.DuplicateOverwrite(
                            s.StableId,
                            replacedProvider,
                            providerName));
                }

                byId[s.StableId] = s;
                ownerById[s.StableId] = providerName;
            }

            if (report != null)
            {
                report.Providers.Add(
                    new StatusStripMergeReport.ProviderFrameSnapshot(
                        providerName,
                        Scratch.Count,
                        providerStableIds ?? new HashSet<string>()));
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

        if (report != null)
        {
            report.OutputStatusCount = dest.Count;
        }
    }
}
