using ScadaNet.Model;
using ScadaNet.Runtime;

namespace ScadaNet.Tests;

public class DeviceDashboardServiceTests
{
    [Fact]
    public async Task TryGet_combines_device_health_connection_polling_and_signals()
    {
        var device = new DeviceDefinition("line1-plc", "fake", "127.0.0.1");
        device.Signals.Add(new DeviceSignalDefinition
        {
            Name = "counter",
            Address = "Counter"
        });
        var registry = new DeviceRegistry([device]);
        var snapshots = new SignalSnapshotStore();
        snapshots.Update(new SignalValue(
            new SignalRef("line1-plc", "Counter"),
            123,
            SignalQuality.Good,
            DateTimeOffset.UtcNow));
        var statuses = new PollingStatusStore();
        var group = new SignalPollingGroupDefinition
        {
            Name = "line1-fast",
            DeviceName = "line1-plc"
        };
        group.Addresses.Add("Counter");
        statuses.MarkSuccess(group, TimeSpan.FromMilliseconds(10), signalCount: 1);
        await using var connections = new DeviceConnectionPool(new FakeConnectionFactory());
        await using (await connections.RentAsync("line1-plc"))
        {
        }
        var service = new DeviceDashboardService(
            registry,
            new DeviceHealthService(registry, snapshots, statuses),
            connections,
            new PollingGroupMonitor(new PollingGroupRegistry([group]), statuses),
            new DeviceSignalSnapshotReader(registry, snapshots));

        var found = service.TryGet("LINE1-PLC", out var dashboard);

        Assert.True(found);
        Assert.Equal("line1-plc", dashboard.Device.Name);
        Assert.Equal(DeviceHealthState.Healthy, dashboard.Health.State);
        Assert.NotNull(dashboard.Connection);
        Assert.Single(dashboard.PollingGroups);
        Assert.Single(dashboard.Signals);
    }

    [Fact]
    public void GetAll_returns_dashboards_ordered_by_device_name()
    {
        var line2 = new DeviceDefinition("line2-plc", "fake", "127.0.0.2");
        var line1 = new DeviceDefinition("line1-plc", "fake", "127.0.0.1");
        var registry = new DeviceRegistry([line2, line1]);
        var snapshots = new SignalSnapshotStore();
        var statuses = new PollingStatusStore();
        using var connections = new DeviceConnectionPool(new FakeConnectionFactory());
        var service = new DeviceDashboardService(
            registry,
            new DeviceHealthService(registry, snapshots, statuses),
            connections,
            new PollingGroupMonitor(new PollingGroupRegistry([]), statuses),
            new DeviceSignalSnapshotReader(registry, snapshots));

        var dashboards = service.GetAll();

        Assert.Equal(["line1-plc", "line2-plc"], dashboards.Select(dashboard => dashboard.Device.Name));
    }

    [Fact]
    public void TryGet_returns_false_for_unknown_device()
    {
        var registry = new DeviceRegistry([]);
        var snapshots = new SignalSnapshotStore();
        var statuses = new PollingStatusStore();
        using var connections = new DeviceConnectionPool(new FakeConnectionFactory());
        var service = new DeviceDashboardService(
            registry,
            new DeviceHealthService(registry, snapshots, statuses),
            connections,
            new PollingGroupMonitor(new PollingGroupRegistry([]), statuses),
            new DeviceSignalSnapshotReader(registry, snapshots));

        var found = service.TryGet("missing", out _);

        Assert.False(found);
    }

    private sealed class FakeConnectionFactory : IDeviceConnectionFactory
    {
        public ValueTask<Protocols.IDeviceConnection> ConnectAsync(
            string deviceName,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<Protocols.IDeviceConnection>(new FakeConnection());
        }
    }

    private sealed class FakeConnection : Protocols.IDeviceConnection
    {
        public DeviceIdentity Identity { get; } = new("Test", "Fake", null, null, null);
        public DeviceCapabilities Capabilities => DeviceCapabilities.Read;

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask<SignalValue> ReadAsync(
            SignalRef signal,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask<IReadOnlyList<SignalValue>> ReadManyAsync(
            IReadOnlyList<SignalRef> signals,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask WriteAsync(
            SignalRef signal,
            object? value,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
