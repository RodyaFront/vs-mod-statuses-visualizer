using System.Collections.Generic;

namespace PlayerStatusStrip;

public readonly struct StatusStripProviderDiagnostics
{
    public StatusStripProviderDiagnostics(string providerName, int emittedCount, int uniqueStableIdCount)
    {
        ProviderName = providerName;
        EmittedCount = emittedCount;
        UniqueStableIdCount = uniqueStableIdCount;
    }

    public string ProviderName { get; }
    public int EmittedCount { get; }
    public int UniqueStableIdCount { get; }
}

public sealed class StatusStripDiagnosticsSnapshot
{
    public StatusStripDiagnosticsSnapshot(
        long frameCount,
        long totalDuplicateOverwrites,
        long unstableProviderWarnings,
        long pulseChurnWarnings,
        int lastFrameInputStatusCount,
        int lastFrameOutputStatusCount,
        int lastFrameDuplicateCount,
        IReadOnlyList<StatusStripProviderDiagnostics> providers)
    {
        FrameCount = frameCount;
        TotalDuplicateOverwrites = totalDuplicateOverwrites;
        UnstableProviderWarnings = unstableProviderWarnings;
        PulseChurnWarnings = pulseChurnWarnings;
        LastFrameInputStatusCount = lastFrameInputStatusCount;
        LastFrameOutputStatusCount = lastFrameOutputStatusCount;
        LastFrameDuplicateCount = lastFrameDuplicateCount;
        Providers = providers;
    }

    public long FrameCount { get; }
    public long TotalDuplicateOverwrites { get; }
    public long UnstableProviderWarnings { get; }
    public long PulseChurnWarnings { get; }
    public int LastFrameInputStatusCount { get; }
    public int LastFrameOutputStatusCount { get; }
    public int LastFrameDuplicateCount { get; }
    public IReadOnlyList<StatusStripProviderDiagnostics> Providers { get; }
}
