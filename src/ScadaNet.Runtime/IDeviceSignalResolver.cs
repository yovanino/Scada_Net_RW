namespace ScadaNet.Runtime;

public interface IDeviceSignalResolver
{
    bool TryResolve(
        string deviceName,
        string signalName,
        out DeviceSignalResolution resolution);

    bool TryResolveMany(
        string deviceName,
        IReadOnlyList<string> signalNames,
        out IReadOnlyList<DeviceSignalResolution> resolutions,
        out string? missingSignalName);
}
