using ScadaNet.Model;

namespace ScadaNet.Runtime;

public sealed class DeviceSignalResolver : IDeviceSignalResolver
{
    private readonly IDeviceRegistry _registry;

    public DeviceSignalResolver(IDeviceRegistry registry)
    {
        _registry = registry;
    }

    public bool TryResolve(
        string deviceName,
        string signalName,
        out DeviceSignalResolution resolution)
    {
        if (!_registry.TryGet(deviceName, out var device) ||
            !device.TryGetSignal(signalName, out var definition))
        {
            resolution = default!;
            return false;
        }

        resolution = new DeviceSignalResolution(
            device,
            definition,
            new SignalRef(device.Name, definition.Address));
        return true;
    }

    public bool TryResolveMany(
        string deviceName,
        IReadOnlyList<string> signalNames,
        out IReadOnlyList<DeviceSignalResolution> resolutions,
        out string? missingSignalName)
    {
        ArgumentNullException.ThrowIfNull(signalNames);

        var items = new DeviceSignalResolution[signalNames.Count];
        for (var index = 0; index < signalNames.Count; index++)
        {
            var signalName = signalNames[index];
            if (!TryResolve(deviceName, signalName, out var resolution))
            {
                resolutions = [];
                missingSignalName = signalName;
                return false;
            }

            items[index] = resolution;
        }

        resolutions = items;
        missingSignalName = null;
        return true;
    }
}
