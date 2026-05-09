using System;

namespace PlayerStatusStrip;

internal sealed class StripLayoutWizardPreviewSession : IDisposable
{
    private readonly IStatusStripHudApi _hudApi;
    private readonly IStatusStripProvider _previewProvider;
    private bool _disposed;

    internal StripLayoutWizardPreviewSession(IStatusStripHudApi hudApi, IStatusStripProvider previewProvider)
    {
        _hudApi = hudApi;
        _previewProvider = previewProvider;
        _hudApi.RegisterProvider(_previewProvider);
        _hudApi.SetPreviewExclusiveProvider(_previewProvider);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _hudApi.SetPreviewExclusiveProvider(null);
        _hudApi.UnregisterProvider(_previewProvider);
        _disposed = true;
    }
}
