using ScadaNet.Protocols;

namespace ScadaNet.Runtime;

public sealed record DeviceDefinition
{
    public DeviceDefinition()
    {
    }

    public DeviceDefinition(
        string name,
        string driver,
        string address)
    {
        Name = name;
        Driver = driver;
        Address = address;
    }

    public string Name { get; init; } = string.Empty;
    public string Driver { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public int? Port { get; init; }
    public string? Path { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(3);

    public DeviceConnectionOptions ToConnectionOptions()
    {
        return new DeviceConnectionOptions
        {
            DeviceName = Name,
            Address = Address,
            Port = Port,
            Path = Path,
            Timeout = Timeout
        };
    }

    public ProbeRequest ToProbeRequest()
    {
        var ports = Port.HasValue
            ? new[] { Port.Value }
            : Array.Empty<int>();

        return new ProbeRequest(Address, ports, Timeout);
    }
}
