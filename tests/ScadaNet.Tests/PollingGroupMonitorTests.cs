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
        Assert.Null(summary.LastRunAge);
        Assert.Equal(TimeSpan.FromMilliseconds(1500), summary.StaleAfter);
        Assert.False(summary.IsStale);
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
        Assert.False(summary.IsStale);
        Assert.Null(summary.Error);
    }

    [Fact]
    public void TryGet_marks_enabled_group_as_stale_when_last_run_exceeds_stale_window()
    {
        var now = new DateTimeOffset(2026, 5, 26, 10, 0, 0, TimeSpan.Zero);
        var group = new SignalPollingGroupDefinition
        {
            Name = "line1-fast",
            DeviceName = "line1-plc",
            Interval = TimeSpan.FromSeconds(1)
        };
        var statuses = new FakePollingStatusStore(new PollingGroupStatus(
            "line1-fast",
            "line1-plc",
            Healthy: true,
            now.AddSeconds(-4),
            Duration: TimeSpan.FromMilliseconds(10),
            SignalCount: 1,
            Error: null));
        var monitor = new PollingGroupMonitor(
            new PollingGroupRegistry([group]),
            statuses,
            new FixedTimeProvider(now));

        var found = monitor.TryGet("line1-fast", out var summary);

        Assert.True(found);
        Assert.True(summary.IsStale);
        Assert.Equal(TimeSpan.FromSeconds(3), summary.StaleAfter);
        Assert.Equal(TimeSpan.FromSeconds(4), summary.LastRunAge);
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

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }

    private sealed class FakePollingStatusStore : IPollingStatusStore
    {
        private readonly PollingGroupStatus _status;

        public FakePollingStatusStore(PollingGroupStatus status)
        {
            _status = status;
        }

        public void MarkSuccess(
            SignalPollingGroupDefinition group,
            TimeSpan duration,
            int signalCount)
        {
            throw new NotSupportedException();
        }

        public void MarkSkipped(
            SignalPollingGroupDefinition group,
            string reason)
        {
            throw new NotSupportedException();
        }

        public void MarkFailure(
            SignalPollingGroupDefinition group,
            TimeSpan duration,
            Exception exception)
        {
            throw new NotSupportedException();
        }

        public IReadOnlyList<PollingGroupStatus> GetAll()
        {
            return [_status];
        }

        public bool TryGet(string groupName, out PollingGroupStatus status)
        {
            if (string.Equals(groupName, _status.GroupName, StringComparison.OrdinalIgnoreCase))
            {
                status = _status;
                return true;
            }

            status = default!;
            return false;
        }
    }
}
