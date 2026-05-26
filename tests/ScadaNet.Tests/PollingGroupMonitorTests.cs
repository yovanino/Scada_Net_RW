using ScadaNet.Runtime;

namespace ScadaNet.Tests;

public class PollingGroupMonitorTests
{
    [Fact]
    public void GetAll_returns_configured_groups_without_status()
    {
        var monitor = CreateMonitor(out _, [
            new SignalPollingGroupDefinition
            {
                Name = "line1-fast",
                DeviceName = "line1-plc",
                Interval = TimeSpan.FromMilliseconds(500),
                Enabled = true
            }
        ]);

        var summary = Assert.Single(monitor.GetAll());

        Assert.Equal("line1-fast", summary.GroupName);
        Assert.Equal("line1-plc", summary.DeviceName);
        Assert.True(summary.Enabled);
        Assert.Equal(TimeSpan.FromMilliseconds(500), summary.Interval);
        Assert.False(summary.HasStatus);
        Assert.Null(summary.Healthy);
        Assert.Null(summary.LastRun);
    }

    [Fact]
    public void TryGet_merges_configuration_with_latest_status()
    {
        var group = new SignalPollingGroupDefinition
        {
            Name = "line1-fast",
            DeviceName = "line1-plc",
            Interval = TimeSpan.FromSeconds(1)
        };
        group.Addresses.Add("ProductionCounter");
        group.Addresses.Add("Motor.Speed");

        var monitor = CreateMonitor(out var statuses, [group]);
        statuses.MarkSuccess(group, TimeSpan.FromMilliseconds(12), signalCount: 2);

        var found = monitor.TryGet("LINE1-FAST", out var summary);

        Assert.True(found);
        Assert.True(summary.HasStatus);
        Assert.True(summary.Healthy);
        Assert.Equal(2, summary.ConfiguredSignalCount);
        Assert.Equal(["ProductionCounter", "Motor.Speed"], summary.Addresses);
        Assert.Equal(TimeSpan.FromMilliseconds(12), summary.Duration);
        Assert.Null(summary.Error);
    }

    [Fact]
    public void TryGet_returns_false_for_unknown_group()
    {
        var monitor = CreateMonitor(out _, []);

        var found = monitor.TryGet("missing", out _);

        Assert.False(found);
    }

    private static PollingGroupMonitor CreateMonitor(
        out PollingStatusStore statuses,
        IEnumerable<SignalPollingGroupDefinition> groups)
    {
        var registry = new PollingGroupRegistry(groups);
        statuses = new PollingStatusStore();
        return new PollingGroupMonitor(registry, statuses);
    }
}
