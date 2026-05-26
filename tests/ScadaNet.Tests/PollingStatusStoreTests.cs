using ScadaNet.Runtime;

namespace ScadaNet.Tests;

public class PollingStatusStoreTests
{
    [Fact]
    public void MarkSuccess_records_healthy_status()
    {
        var store = new PollingStatusStore();
        var group = new SignalPollingGroupDefinition
        {
            Name = "line1-fast",
            DeviceName = "line1-plc"
        };

        store.MarkSuccess(group, TimeSpan.FromMilliseconds(12), signalCount: 2);

        Assert.True(store.TryGet("line1-fast", out var status));
        Assert.True(status.Healthy);
        Assert.Equal("line1-plc", status.DeviceName);
        Assert.Equal(2, status.SignalCount);
        Assert.Null(status.Error);
    }

    [Fact]
    public void MarkFailure_records_degraded_status()
    {
        var store = new PollingStatusStore();
        var group = new SignalPollingGroupDefinition
        {
            Name = "line1-fast",
            DeviceName = "line1-plc"
        };

        store.MarkFailure(group, TimeSpan.FromMilliseconds(12), new TimeoutException("timeout"));

        Assert.True(store.TryGet("line1-fast", out var status));
        Assert.False(status.Healthy);
        Assert.Equal("timeout", status.Error);
    }
}
