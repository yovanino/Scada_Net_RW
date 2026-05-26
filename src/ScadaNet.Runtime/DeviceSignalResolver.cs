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
}
