using System;
using System.Collections.Generic;
using Vintagestory.API.Client;

namespace PlayerStatusStrip;

internal sealed class StatusStripDiagnosticsTracker
{
    private const float PulseDeltaThreshold = 0.05f;
    private const int PulseWarningThreshold = 8;

    private readonly Dictionary<string, HashSet<string>> _providerLastStableIds = new();
    private readonly Dictionary<string, PulseState> _pulseByStableId = new();
    private readonly HashSet<string> _unstableProviderWarned = new();

    private long _frameCount;
    private long _totalDuplicateOverwrites;
    private long _unstableProviderWarnings;
    private long _pulseChurnWarnings;
    private int _lastFrameInputStatusCount;
    private int _lastFrameOutputStatusCount;
    private int _lastFrameDuplicateCount;
    private List<StatusStripProviderDiagnostics> _lastProviders = new();

    private struct PulseState
    {
        internal bool HasMetric;
        internal float LastMetric;
        internal int ConsecutiveLargeDeltas;
        internal bool Warned;
    }

    internal void OnFrame(
        ICoreClientAPI? capi,
        StatusStripMergeReport report,
        IReadOnlyList<StatusDescriptor> mergedStatuses,
        bool devWarningsEnabled)
    {
        _frameCount++;
        _lastFrameInputStatusCount = report.InputStatusCount;
        _lastFrameOutputStatusCount = report.OutputStatusCount;
        _lastFrameDuplicateCount = report.DuplicateOverwrites.Count;
        _totalDuplicateOverwrites += report.DuplicateOverwrites.Count;

        _lastProviders = new List<StatusStripProviderDiagnostics>(report.Providers.Count);
        foreach (StatusStripMergeReport.ProviderFrameSnapshot p in report.Providers)
        {
            _lastProviders.Add(new StatusStripProviderDiagnostics(
                p.ProviderName,
                p.EmittedCount,
                p.StableIds.Count));
        }

        DetectUnstableProviders(capi, report, devWarningsEnabled);
        DetectPulseChurn(capi, mergedStatuses, devWarningsEnabled);

        if (devWarningsEnabled)
        {
            foreach (StatusStripMergeReport.DuplicateOverwrite overwrite in report.DuplicateOverwrites)
            {
                capi?.Logger?.Warning(
                    "[Player Status HUD][diag] duplicate StableId '{0}': provider '{1}' was replaced by '{2}'.",
                    overwrite.StableId,
                    overwrite.ReplacedProvider,
                    overwrite.WinnerProvider);
            }
        }
    }

    internal StatusStripDiagnosticsSnapshot Snapshot()
    {
        return new StatusStripDiagnosticsSnapshot(
            _frameCount,
            _totalDuplicateOverwrites,
            _unstableProviderWarnings,
            _pulseChurnWarnings,
            _lastFrameInputStatusCount,
            _lastFrameOutputStatusCount,
            _lastFrameDuplicateCount,
            _lastProviders);
    }

    private void DetectUnstableProviders(
        ICoreClientAPI? capi,
        StatusStripMergeReport report,
        bool devWarningsEnabled)
    {
        foreach (StatusStripMergeReport.ProviderFrameSnapshot p in report.Providers)
        {
            string key = p.ProviderName;
            if (_providerLastStableIds.TryGetValue(key, out HashSet<string>? prev) && prev.Count > 0)
            {
                int overlap = 0;
                foreach (string id in p.StableIds)
                {
                    if (prev.Contains(id))
                    {
                        overlap++;
                    }
                }

                float overlapRatio = overlap / (float)Math.Max(1, prev.Count);
                bool unstable = overlapRatio < 0.5f && p.StableIds.Count > 0;
                if (unstable && _unstableProviderWarned.Add(key))
                {
                    _unstableProviderWarnings++;
                    if (devWarningsEnabled)
                    {
                        capi?.Logger?.Warning(
                            "[Player Status HUD][diag] provider '{0}' appears unstable (stable-id overlap {1:P0}). Check StableId continuity.",
                            p.ProviderName,
                            overlapRatio);
                    }
                }
            }

            _providerLastStableIds[key] = new HashSet<string>(p.StableIds);
        }
    }

    private void DetectPulseChurn(
        ICoreClientAPI? capi,
        IReadOnlyList<StatusDescriptor> mergedStatuses,
        bool devWarningsEnabled)
    {
        HashSet<string> currentIds = new();
        foreach (StatusDescriptor s in mergedStatuses)
        {
            currentIds.Add(s.StableId);
            if (!s.PulseMetric.HasValue)
            {
                continue;
            }

            PulseState state = _pulseByStableId.TryGetValue(s.StableId, out PulseState prior) ? prior : default;
            float metric = s.PulseMetric.Value;
            if (state.HasMetric)
            {
                float delta = Math.Abs(metric - state.LastMetric);
                state.ConsecutiveLargeDeltas = delta >= PulseDeltaThreshold
                    ? state.ConsecutiveLargeDeltas + 1
                    : 0;
                if (!state.Warned && state.ConsecutiveLargeDeltas >= PulseWarningThreshold)
                {
                    state.Warned = true;
                    _pulseChurnWarnings++;
                    if (devWarningsEnabled)
                    {
                        capi?.Logger?.Warning(
                            "[Player Status HUD][diag] status '{0}' has noisy PulseMetric (>= {1} large deltas). Consider smoothing/debouncing.",
                            s.StableId,
                            PulseWarningThreshold);
                    }
                }
            }

            state.HasMetric = true;
            state.LastMetric = metric;
            _pulseByStableId[s.StableId] = state;
        }

        List<string> toRemove = new();
        foreach (string key in _pulseByStableId.Keys)
        {
            if (!currentIds.Contains(key))
            {
                toRemove.Add(key);
            }
        }

        foreach (string key in toRemove)
        {
            _pulseByStableId.Remove(key);
        }
    }
}
