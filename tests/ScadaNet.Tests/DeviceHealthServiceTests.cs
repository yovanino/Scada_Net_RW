using ScadaNet.Model;
using ScadaNet.Runtime;

namespace ScadaNet.Tests;

public class DeviceHealthServiceTests
{
    [Fact]
    public void TryGet_returns_unknown_without_polling_status()
    {
        var service = CreateService(out _, out _);

        var found = service.TryGet("line1-plc", out var health);

        Assert.True(found);
        Assert.Equal(DeviceHealthState.Unknown, health.State);
        Assert.Contains("No polling status recorded.", health.Messages);
    }

    [Fact]
    public void TryGet_returns_healthy_when_polling_status_is_healthy()
    {
        var service = CreateService(out var snapshots, out var statuses);
        var signal = new SignalRef("line1-plc", "ProductionCounter");
        snapshots.Update(new SignalValue(signal, 123, SignalQuality.Good, DateTimeOffset.UtcNow));
        statuses.MarkSuccess(new SignalPollingGroupDefinition
        {
            Name = "line1-fast",
            DeviceName = "line1-plc"
        }, TimeSpan.FromMilliseconds(10), signalCount: 1);

        var found = service.TryGet("line1-plc", out var health);

        Assert.True(found);
        Assert.Equal(DeviceHealthState.Healthy, health.State);
        Assert.Equal(1, health.SnapshotCount);
        Assert.Equal(1, health.PollingGroupCount);
        Assert.Empty(health.Messages);
    }

    [Fact]
    public void TryGet_returns_degraded_when_any_polling_group_failed()
    {
        var service = CreateService(out _, out var statuses);
        statuses.MarkFailure(new SignalPollingGroupDefinition
        {
            Name = "line1-fast",
            DeviceName = "line1-plc"
        }, TimeSpan.FromMilliseconds(10), new TimeoutException("timeout"));

        var found = service.TryGet("line1-plc", out var health);

        Assert.True(found);
        Assert.Equal(DeviceHealthState.Degraded, health.State);
        Assert.Contains("line1-fast: timeout", health.Messages);
    }

    private static DeviceHealthService CreateService(
        out SignalSnapshotStore snapshots,
        out PollingStatusStore statuses)
    {
        var registry = new DeviceRegistry([
            new DeviceDefinition("line1-plc", "logix", "192.168.0.10")
        ]);
        snapshots = new SignalSnapshotStore();
        statuses = new PollingStatusStore();

        return new DeviceHealthService(registry, snapshots, statuses);
    }
}
