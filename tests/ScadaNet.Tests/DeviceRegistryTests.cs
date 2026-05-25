using ScadaNet.Runtime;

namespace ScadaNet.Tests;

public class DeviceRegistryTests
{
    [Fact]
    public void TryGet_finds_devices_by_name_case_insensitively()
    {
        var registry = new DeviceRegistry([
            new DeviceDefinition("Line1-Plc", "ethernetip", "192.168.0.10")
        ]);

        var found = registry.TryGet("line1-plc", out var device);

        Assert.True(found);
        Assert.Equal("192.168.0.10", device.Address);
    }

    [Fact]
    public void Constructor_rejects_duplicate_device_names()
    {
        var error = Assert.Throws<ArgumentException>(() => new DeviceRegistry([
            new DeviceDefinition("line1-plc", "ethernetip", "192.168.0.10"),
            new DeviceDefinition("LINE1-PLC", "ethernetip", "192.168.0.11")
        ]));

        Assert.Contains("already registered", error.Message);
    }

    [Fact]
    public void DeviceDefinition_creates_connection_options_and_probe_request()
    {
        var device = new DeviceDefinition("line1-plc", "ethernetip", "192.168.0.10")
        {
            Port = 44818,
            Path = "1,0",
            Timeout = TimeSpan.FromSeconds(2)
        };

        var connectionOptions = device.ToConnectionOptions();
        var probe = device.ToProbeRequest();

        Assert.Equal("line1-plc", connectionOptions.DeviceName);
        Assert.Equal("192.168.0.10", connectionOptions.Address);
        Assert.Equal(44818, connectionOptions.Port);
        Assert.Equal("1,0", connectionOptions.Path);
        Assert.Equal(TimeSpan.FromSeconds(2), connectionOptions.Timeout);

        Assert.Equal("192.168.0.10", probe.Address);
        Assert.Equal([44818], probe.Ports);
        Assert.Equal(TimeSpan.FromSeconds(2), probe.Timeout);
    }
}
