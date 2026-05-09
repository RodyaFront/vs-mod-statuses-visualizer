namespace PlayerStatusStrip;

public interface IStatusStripDiagnosticsApi
{
    bool DiagnosticsAvailable { get; }

    StatusStripDiagnosticsSnapshot GetDiagnosticsSnapshot();
}
