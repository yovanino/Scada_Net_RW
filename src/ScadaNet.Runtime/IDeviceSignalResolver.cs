namespace ScadaNet.Runtime;

public interface IDeviceSignalResolver
{
    bool TryResolve(
        string deviceName,
        string signalName,
        out DeviceSignalResolution resolution);
}
