using ScadaNet.Protocols;

namespace ScadaNet.Runtime;

public sealed class DeviceConnectionFactory : IDeviceConnectionFactory
{
    private readonly IDeviceRegistry _registry;
    private readonly IReadOnlyDictionary<string, IDeviceDriver> _driversByName;

    public DeviceConnectionFactory(
        IDeviceRegistry registry,
        IEnumerable<IDeviceDriver> drivers)
    {
        _registry = registry;
        _driversByName = drivers.ToDictionary(
            driver => NormalizeDriverName(driver.DriverName),
            StringComparer.OrdinalIgnoreCase);
    }

    public ValueTask<IDeviceConnection> ConnectAsync(
        string deviceName,
        CancellationToken cancellationToken = default)
    {
        var device = _registry.GetRequired(deviceName);
        var driverKey = NormalizeDriverName(device.Driver);

        if (!_driversByName.TryGetValue(driverKey, out var driver))
        {
            throw new InvalidOperationException(
                $"Driver '{device.Driver}' is not registered for device '{device.Name}'.");
        }

        return driver.ConnectAsync(
            device.ToConnectionOptions(),
            cancellationToken);
    }

    private static string NormalizeDriverName(string value)
    {
        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
    }
}
