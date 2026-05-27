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
            snapshots,
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
            snapshots,
            new DeviceSignalSnapshotReader(registry, snapshots));

        var dashboards = service.GetAll();

        Assert.Equal(["line1-plc", "line2-plc"], dashboards.Select(dashboard => dashboard.Device.Name));
    }

    [Fact]
    public async Task GetSummaries_returns_lightweight_device_status()
    {
        var line1 = new DeviceDefinition("line1-plc", "fake", "127.0.0.1");
        line1.Signals.Add(new DeviceSignalDefinition
        {
            Name = "counter",
            Address = "Counter"
        });
        var line2 = new DeviceDefinition("line2-plc", "fake", "127.0.0.2");
        line2.Signals.Add(new DeviceSignalDefinition
        {
            Name = "speed",
            Address = "Speed"
        });
        var registry = new DeviceRegistry([line2, line1]);
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
        group.SignalNames.Add("counter");
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
            snapshots,
            new DeviceSignalSnapshotReader(registry, snapshots));

        var summaries = service.GetSummaries();

        Assert.Equal(["line1-plc", "line2-plc"], summaries.Select(summary => summary.DeviceName));
        var line1Summary = summaries[0];
        Assert.Equal(DeviceHealthState.Healthy, line1Summary.HealthState);
        Assert.True(line1Summary.HasConnection);
        Assert.Equal(1, line1Summary.PollingGroupCount);
        Assert.Equal(1, line1Summary.SignalCount);
        Assert.Equal(1, line1Summary.SignalWithValueCount);
        Assert.Equal(0, line1Summary.IssueCount);

        var line2Summary = summaries[1];
        Assert.Equal(DeviceHealthState.Unknown, line2Summary.HealthState);
        Assert.False(line2Summary.HasConnection);
        Assert.Equal(1, line2Summary.SignalCount);
        Assert.Equal(0, line2Summary.SignalWithValueCount);
        Assert.Equal(1, line2Summary.IssueCount);
        Assert.Equal(1, line2Summary.WarningIssueCount);
    }

    [Fact]
    public void GetSummaries_and_overview_do_not_build_detailed_signal_snapshots()
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
        using var connections = new DeviceConnectionPool(new FakeConnectionFactory());
        var service = new DeviceDashboardService(
            registry,
            new DeviceHealthService(registry, snapshots, statuses),
            connections,
            new PollingGroupMonitor(new PollingGroupRegistry([]), statuses),
            snapshots,
            new ThrowingSignalSnapshotReader());

        var summaries = service.GetSummaries();
        var overview = service.GetOverview();

        Assert.Single(summaries);
        Assert.Equal(1, overview.SignalWithValueCount);
    }

    [Fact]
    public async Task GetOverview_counts_devices_connections_polling_and_signals()
    {
        var line1 = new DeviceDefinition("line1-plc", "fake", "127.0.0.1");
        line1.Signals.Add(new DeviceSignalDefinition
        {
            Name = "counter",
            Address = "Counter",
            Writable = true
        });
        var line2 = new DeviceDefinition("line2-plc", "fake", "127.0.0.2");
        line2.Signals.Add(new DeviceSignalDefinition
        {
            Name = "speeds",
            Address = "Speeds",
            IsArray = true,
            ElementCount = 4
        });
        var registry = new DeviceRegistry([line1, line2]);
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
        group.SignalNames.Add("counter");
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
            snapshots,
            new DeviceSignalSnapshotReader(registry, snapshots));

        var overview = service.GetOverview();

        Assert.Equal(2, overview.DeviceCount);
        Assert.Equal(1, overview.HealthyDeviceCount);
        Assert.Equal(0, overview.DegradedDeviceCount);
        Assert.Equal(1, overview.UnknownDeviceCount);
        Assert.Equal(1, overview.ActiveConnectionCount);
        Assert.Equal(0, overview.FailedConnectionCount);
        Assert.Equal(1, overview.PollingGroupCount);
        Assert.Equal(0, overview.StalePollingGroupCount);
        Assert.Equal(2, overview.SignalCount);
        Assert.Equal(1, overview.SignalWithValueCount);
        Assert.Equal(1, overview.WritableSignalCount);
        Assert.Equal(1, overview.ArraySignalCount);
        Assert.Equal(1, overview.IssueCount);
        Assert.Equal(1, overview.WarningIssueCount);
        Assert.Equal(0, overview.CriticalIssueCount);
    }

    [Fact]
    public async Task GetIssues_returns_actionable_dashboard_issues()
    {
        var device = new DeviceDefinition("line1-plc", "fake", "127.0.0.1");
        device.Signals.Add(new DeviceSignalDefinition
        {
            Name = "counter",
            Address = "Counter"
        });
        var registry = new DeviceRegistry([device]);
        var snapshots = new SignalSnapshotStore();
        var statuses = new PollingStatusStore();
        var group = new SignalPollingGroupDefinition
        {
            Name = "line1-fast",
            DeviceName = "line1-plc"
        };
        statuses.MarkFailure(
            group,
            TimeSpan.FromMilliseconds(25),
            new InvalidOperationException("PLC read timeout."));
        await using var connections = new DeviceConnectionPool(new FailingConnectionFactory());
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await connections.RentAsync("line1-plc"));
        var service = new DeviceDashboardService(
            registry,
            new DeviceHealthService(registry, snapshots, statuses),
            connections,
            new PollingGroupMonitor(new PollingGroupRegistry([group]), statuses),
            snapshots,
            new DeviceSignalSnapshotReader(registry, snapshots));

        var issues = service.GetIssues();

        Assert.Contains(issues, issue =>
            issue.Code == "device-health-degraded" &&
            issue.Severity == DeviceDashboardIssueSeverity.Critical);
        Assert.Contains(issues, issue =>
            issue.Code == "connection-rent-failed" &&
            issue.Source == "connection" &&
            issue.Severity == DeviceDashboardIssueSeverity.Critical);
        Assert.Contains(issues, issue =>
            issue.Code == "polling-group-failed" &&
            issue.Source == "polling" &&
            issue.Message == "PLC read timeout.");
    }

    [Fact]
    public async Task TryGetIssues_returns_issues_for_one_device()
    {
        var line1 = new DeviceDefinition("line1-plc", "fake", "127.0.0.1");
        var line2 = new DeviceDefinition("line2-plc", "fake", "127.0.0.2");
        var registry = new DeviceRegistry([line1, line2]);
        var snapshots = new SignalSnapshotStore();
        var statuses = new PollingStatusStore();
        var group1 = new SignalPollingGroupDefinition
        {
            Name = "line1-fast",
            DeviceName = "line1-plc"
        };
        var group2 = new SignalPollingGroupDefinition
        {
            Name = "line2-fast",
            DeviceName = "line2-plc"
        };
        statuses.MarkFailure(
            group1,
            TimeSpan.FromMilliseconds(25),
            new InvalidOperationException("Line 1 timeout."));
        statuses.MarkFailure(
            group2,
            TimeSpan.FromMilliseconds(25),
            new InvalidOperationException("Line 2 timeout."));
        await using var connections = new DeviceConnectionPool(new FakeConnectionFactory());
        var service = new DeviceDashboardService(
            registry,
            new DeviceHealthService(registry, snapshots, statuses),
            connections,
            new PollingGroupMonitor(new PollingGroupRegistry([group1, group2]), statuses),
            snapshots,
            new DeviceSignalSnapshotReader(registry, snapshots));

        var found = service.TryGetIssues("LINE1-PLC", out var issues);

        Assert.True(found);
        Assert.All(issues, issue => Assert.Equal("line1-plc", issue.DeviceName));
        Assert.Contains(issues, issue => issue.Message.Contains("Line 1 timeout."));
        Assert.DoesNotContain(issues, issue => issue.Message.Contains("Line 2 timeout."));
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
            snapshots,
            new DeviceSignalSnapshotReader(registry, snapshots));

        var found = service.TryGet("missing", out _);

        Assert.False(found);
    }

    [Fact]
    public void TryGetIssues_returns_false_for_unknown_device()
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
            snapshots,
            new DeviceSignalSnapshotReader(registry, snapshots));

        var found = service.TryGetIssues("missing", out var issues);

        Assert.False(found);
        Assert.Empty(issues);
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

    private sealed class FailingConnectionFactory : IDeviceConnectionFactory
    {
        public ValueTask<Protocols.IDeviceConnection> ConnectAsync(
            string deviceName,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Connection refused.");
        }
    }

    private sealed class ThrowingSignalSnapshotReader : IDeviceSignalSnapshotReader
    {
        public bool TryGetDeviceSnapshots(
            string deviceName,
            out IReadOnlyList<DeviceSignalSnapshot> snapshots)
        {
            throw new InvalidOperationException("Detailed snapshots should not be built.");
        }

        public bool TryGet(
            string deviceName,
            string signalName,
            out DeviceSignalSnapshot snapshot)
        {
            throw new InvalidOperationException("Detailed snapshots should not be built.");
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
