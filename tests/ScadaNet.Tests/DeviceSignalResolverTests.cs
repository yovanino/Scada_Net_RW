using ScadaNet.Runtime;

namespace ScadaNet.Tests;

public class DeviceSignalResolverTests
{
    [Fact]
    public void TryResolve_maps_logical_signal_name_to_plc_address()
    {
        var device = new DeviceDefinition("line1-plc", "fake", "127.0.0.1");
        device.Signals.Add(new DeviceSignalDefinition
        {
            Name = "production-counter",
            Address = "ProductionCounter",
            DataType = "DINT"
        });
        var resolver = new DeviceSignalResolver(new DeviceRegistry([device]));

        var found = resolver.TryResolve(
            "LINE1-PLC",
            "PRODUCTION-COUNTER",
            out var resolution);

        Assert.True(found);
        Assert.Equal(device, resolution.Device);
        Assert.Equal("production-counter", resolution.Definition.Name);
        Assert.Equal("line1-plc", resolution.Signal.DeviceName);
        Assert.Equal("ProductionCounter", resolution.Signal.Address);
    }

    [Fact]
    public void TryResolve_returns_false_for_unknown_signal()
    {
        var resolver = new DeviceSignalResolver(new DeviceRegistry([
            new DeviceDefinition("line1-plc", "fake", "127.0.0.1")
        ]));

        var found = resolver.TryResolve("line1-plc", "missing", out _);

        Assert.False(found);
    }
}
