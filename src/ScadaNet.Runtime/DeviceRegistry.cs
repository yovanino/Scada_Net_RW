namespace ScadaNet.Runtime;

public sealed class DeviceRegistry : IDeviceRegistry
{
    private readonly IReadOnlyDictionary<string, DeviceDefinition> _devicesByName;

    public DeviceRegistry(IEnumerable<DeviceDefinition> devices)
    {
        ArgumentNullException.ThrowIfNull(devices);

        var devicesByName = new Dictionary<string, DeviceDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var device in devices)
        {
            if (string.IsNullOrWhiteSpace(device.Name))
            {
                throw new ArgumentException("Device name cannot be empty.", nameof(devices));
            }

            if (!devicesByName.TryAdd(device.Name, device))
            {
                throw new ArgumentException(
                    $"Device '{device.Name}' is already registered.",
                    nameof(devices));
            }
        }

        _devicesByName = devicesByName;
        Devices = devicesByName.Values.ToArray();
    }

    public IReadOnlyCollection<DeviceDefinition> Devices { get; }

    public DeviceDefinition GetRequired(string name)
    {
        if (TryGet(name, out var device))
        {
            return device;
        }

        throw new KeyNotFoundException($"Device '{name}' is not registered.");
    }

    public bool TryGet(string name, out DeviceDefinition device)
    {
        return _devicesByName.TryGetValue(name, out device!);
    }
}
