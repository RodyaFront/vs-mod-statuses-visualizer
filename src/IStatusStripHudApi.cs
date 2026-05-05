namespace PlayerStatusStrip;

public interface IStatusStripHudApi
{
    int ApiVersion { get; }

    void RegisterProvider(IStatusStripProvider provider);

    void UnregisterProvider(IStatusStripProvider provider);
}
