using ScadaNet.Model;
using ScadaNet.Runtime;

namespace ScadaNet.Tests;

public class DeviceSignalSnapshotReaderTests
{
    [Fact]
    public void TryGet_returns_named_snapshot_with_latest_value()
    {
        var device = new DeviceDefinition("line1-plc", "fake", "127.0.0.1");
        device.Signals.Add(new DeviceSignalDefinition
        {
            Name = "production-counter",
            Address = "ProductionCounter",
            DataType = "DINT",
            Unit = "parts",
            Description = "Good parts counter",
            Category = "OEE",
            DisplayOrder = 10,
            MinValue = 0,
            MaxValue = 999999,
            IsArray = true,
            ElementCount = 10
        });
        var store = new SignalSnapshotStore();
        store.Update(new SignalValue(
            new SignalRef("line1-plc", "ProductionCounter"),
            123,
            SignalQuality.Good,
            DateTimeOffset.UtcNow));
        var reader = new DeviceSignalSnapshotReader(new DeviceRegistry([device]), store);

        var found = reader.TryGet("LINE1-PLC", "PRODUCTION-COUNTER", out var snapshot);

        Assert.True(found);
        Assert.Equal("production-counter", snapshot.Name);
        Assert.Equal("ProductionCounter", snapshot.Address);
        Assert.Equal("DINT", snapshot.DataType);
        Assert.Equal("parts", snapshot.Unit);
        Assert.Equal("Good parts counter", snapshot.Description);
        Assert.Equal("OEE", snapshot.Category);
        Assert.Equal(10, snapshot.DisplayOrder);
        Assert.Equal(0, snapshot.MinValue);
        Assert.Equal(999999, snapshot.MaxValue);
        Assert.True(snapshot.IsArray);
        Assert.Equal((ushort)10, snapshot.ElementCount);
        Assert.True(snapshot.HasValue);
        Assert.Equal(123, snapshot.Value?.Value);
    }

    [Fact]
    public void TryGetDeviceSnapshots_returns_catalog_items_without_values()
    {
        var device = new DeviceDefinition("line1-plc", "fake", "127.0.0.1");
        device.Signals.Add(new DeviceSignalDefinition
        {
            Name = "speed",
            Address = "Motor.Speed"
        });
        var reader = new DeviceSignalSnapshotReader(
            new DeviceRegistry([device]),
            new SignalSnapshotStore());

        var found = reader.TryGetDeviceSnapshots("line1-plc", out var snapshots);

        Assert.True(found);
        var snapshot = Assert.Single(snapshots);
        Assert.Equal("speed", snapshot.Name);
        Assert.False(snapshot.HasValue);
        Assert.Null(snapshot.Value);
    }

    [Fact]
    public void TryGetDeviceSnapshots_orders_by_display_order_category_and_name()
    {
        var device = new DeviceDefinition("line1-plc", "fake", "127.0.0.1");
        device.Signals.Add(new DeviceSignalDefinition
        {
            Name = "z-signal",
            Address = "Z",
            Category = "Motor",
            DisplayOrder = 20
        });
        device.Signals.Add(new DeviceSignalDefinition
        {
            Name = "a-signal",
            Address = "A",
            Category = "OEE",
            DisplayOrder = 10
        });
        device.Signals.Add(new DeviceSignalDefinition
        {
            Name = "b-signal",
            Address = "B",
            Category = "Motor",
            DisplayOrder = 10
        });
        var reader = new DeviceSignalSnapshotReader(
            new DeviceRegistry([device]),
            new SignalSnapshotStore());

        var found = reader.TryGetDeviceSnapshots("line1-plc", out var snapshots);

        Assert.True(found);
        Assert.Equal(["b-signal", "a-signal", "z-signal"], snapshots.Select(snapshot => snapshot.Name));
    }

    [Fact]
    public void TryGet_returns_false_for_unknown_signal()
    {
        var reader = new DeviceSignalSnapshotReader(
            new DeviceRegistry([new DeviceDefinition("line1-plc", "fake", "127.0.0.1")]),
            new SignalSnapshotStore());

        var found = reader.TryGet("line1-plc", "missing", out _);

        Assert.False(found);
    }
}
