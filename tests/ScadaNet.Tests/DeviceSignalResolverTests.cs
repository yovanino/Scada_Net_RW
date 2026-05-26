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

    [Fact]
    public void TryResolveMany_maps_all_logical_signal_names_to_plc_addresses()
    {
        var device = new DeviceDefinition("line1-plc", "fake", "127.0.0.1");
        device.Signals.Add(new DeviceSignalDefinition
        {
            Name = "counter",
            Address = "ProductionCounter"
        });
        device.Signals.Add(new DeviceSignalDefinition
        {
            Name = "speed",
            Address = "Motor.Speed"
        });
        var resolver = new DeviceSignalResolver(new DeviceRegistry([device]));

        var found = resolver.TryResolveMany(
            "line1-plc",
            ["COUNTER", "speed"],
            out var resolutions,
            out var missingSignalName);

        Assert.True(found);
        Assert.Null(missingSignalName);
        Assert.Equal(["ProductionCounter", "Motor.Speed"], resolutions.Select(item => item.Signal.Address));
    }

    [Fact]
    public void TryResolveMany_returns_missing_signal_name()
    {
        var device = new DeviceDefinition("line1-plc", "fake", "127.0.0.1");
        device.Signals.Add(new DeviceSignalDefinition
        {
            Name = "counter",
            Address = "ProductionCounter"
        });
        var resolver = new DeviceSignalResolver(new DeviceRegistry([device]));

        var found = resolver.TryResolveMany(
            "line1-plc",
            ["counter", "speed"],
            out var resolutions,
            out var missingSignalName);

        Assert.False(found);
        Assert.Empty(resolutions);
        Assert.Equal("speed", missingSignalName);
    }
}
