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
        Assert.Equal(0, line1Summary.HealthIssueCount);
        Assert.Equal(0, line1Summary.ConnectionIssueCount);
        Assert.Equal(0, line1Summary.PollingIssueCount);
        Assert.Equal(0, line1Summary.WriteAuditIssueCount);

        var line2Summary = summaries[1];
        Assert.Equal(DeviceHealthState.Unknown, line2Summary.HealthState);
        Assert.False(line2Summary.HasConnection);
        Assert.Equal(1, line2Summary.SignalCount);
        Assert.Equal(0, line2Summary.SignalWithValueCount);
        Assert.Equal(1, line2Summary.IssueCount);
        Assert.Equal(1, line2Summary.WarningIssueCount);
        Assert.Equal(1, line2Summary.HealthIssueCount);
        Assert.Equal(0, line2Summary.ConnectionIssueCount);
        Assert.Equal(0, line2Summary.PollingIssueCount);
        Assert.Equal(0, line2Summary.WriteAuditIssueCount);
    }

    [Fact]
    public async Task TryGetSummary_returns_lightweight_status_for_one_device()
    {
        var line1 = new DeviceDefinition("line1-plc", "fake", "127.0.0.1");
        line1.Signals.Add(new DeviceSignalDefinition
        {
            Name = "counter",
            Address = "Counter"
        });
        var line2 = new DeviceDefinition("line2-plc", "fake", "127.0.0.2");
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
            new ThrowingSignalSnapshotReader());

        var found = service.TryGetSummary("LINE1-PLC", out var summary);

        Assert.True(found);
        Assert.Equal("line1-plc", summary.DeviceName);
        Assert.Equal(DeviceHealthState.Healthy, summary.HealthState);
        Assert.True(summary.HasConnection);
        Assert.Equal(1, summary.PollingGroupCount);
        Assert.Equal(1, summary.SignalCount);
        Assert.Equal(1, summary.SignalWithValueCount);
        Assert.Equal(0, summary.IssueCount);
    }

    [Fact]
    public void TryGetSummary_uses_device_scoped_connection_and_polling_queries()
    {
        var device = new DeviceDefinition("line1-plc", "fake", "127.0.0.1");
        var registry = new DeviceRegistry([device]);
        var snapshots = new SignalSnapshotStore();
        var statuses = new PollingStatusStore();
        var service = new DeviceDashboardService(
            registry,
            new DeviceHealthService(registry, snapshots, statuses),
            new ScopedConnectionPool(
                new DeviceConnectionPoolStatus(
                    "line1-plc",
                    HasConnection: true,
                    IsInUse: false,
                    RentCount: 1,
                    FailedRentCount: 0,
                    ConnectedAt: DateTimeOffset.UtcNow,
                    LastRentedAt: DateTimeOffset.UtcNow,
                    LastFailureAt: null,
                    LastError: null)),
            new ScopedPollingGroupMonitor([
                new PollingGroupSummary(
                    "line1-fast",
                    "line1-plc",
                    Enabled: true,
                    TimeSpan.FromSeconds(1),
                    ConfiguredSignalCount: 0,
                    Addresses: [],
                    SignalNames: [],
                    HasStatus: false,
                    Healthy: null,
                    LastRun: null,
                    LastRunAge: null,
                    Duration: null,
                    StaleAfter: TimeSpan.FromSeconds(3),
                    IsStale: false,
                    Error: null)
            ]),
            snapshots,
            new ThrowingSignalSnapshotReader());

        var found = service.TryGetSummary("LINE1-PLC", out var summary);

        Assert.True(found);
        Assert.True(summary.HasConnection);
        Assert.Equal(1, summary.PollingGroupCount);
    }

    [Fact]
    public async Task TryGetRuntimeStatus_returns_lightweight_device_runtime_state()
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
        await using var connections = new DeviceConnectionPool(new FakeConnectionFactory());
        await using (await connections.RentAsync("line1-plc"))
        {
        }
        var writeAudit = new WriteAuditStore();
        writeAudit.Add(new WriteAuditRecord(
            0,
            DateTimeOffset.UtcNow,
            new SignalRef("line1-plc", "ResetCommand"),
            true,
            Succeeded: false,
            Error: "PLC rejected write."));
        var service = new DeviceDashboardService(
            registry,
            new DeviceHealthService(registry, snapshots, statuses),
            connections,
            new PollingGroupMonitor(new PollingGroupRegistry([group]), statuses),
            snapshots,
            new ThrowingSignalSnapshotReader(),
            writeAudit);

        var before = DateTimeOffset.UtcNow;
        var found = service.TryGetRuntimeStatus("LINE1-PLC", out var status);
        var after = DateTimeOffset.UtcNow;

        Assert.True(found);
        Assert.Equal("line1-plc", status.Summary.DeviceName);
        Assert.True(status.Summary.HasConnection);
        Assert.NotNull(status.Connection);
        Assert.True(status.Connection.HasConnection);
        Assert.Single(status.PollingGroups);
        Assert.Contains(status.IssueSummaries, summary =>
            summary.Source == DeviceDashboardIssueSources.Health &&
            summary.CriticalIssueCount == 1);
        Assert.Contains(status.IssueSummaries, summary =>
            summary.Source == DeviceDashboardIssueSources.Polling &&
            summary.CriticalIssueCount == 1);
        Assert.Equal("line1-plc", status.WriteAudit.DeviceName);
        Assert.Equal(1, status.WriteAudit.WriteCount);
        Assert.Equal(1, status.WriteAudit.FailedWriteCount);
        Assert.Equal("PLC rejected write.", status.WriteAudit.LastError);
        Assert.Equal(1, status.Summary.WriteAuditIssueCount);
        Assert.Contains(status.IssueSummaries, summary =>
            summary.Source == DeviceDashboardIssueSources.WriteAudit &&
            summary.WarningIssueCount == 1);
        Assert.InRange(status.GeneratedAt, before, after);
    }

    [Fact]
    public void TryGetRuntimeStatus_reuses_device_write_audit_summary()
    {
        var device = new DeviceDefinition("line1-plc", "fake", "127.0.0.1");
        var registry = new DeviceRegistry([device]);
        var snapshots = new SignalSnapshotStore();
        var statuses = new PollingStatusStore();
        var writeAudit = new CountingWriteAuditStore();
        var service = new DeviceDashboardService(
            registry,
            new DeviceHealthService(registry, snapshots, statuses),
            new ScopedConnectionPool(
                new DeviceConnectionPoolStatus(
                    "line1-plc",
                    HasConnection: true,
                    IsInUse: false,
                    RentCount: 1,
                    FailedRentCount: 0,
                    ConnectedAt: DateTimeOffset.UtcNow,
                    LastRentedAt: DateTimeOffset.UtcNow,
                    LastFailureAt: null,
                    LastError: null)),
            new ScopedPollingGroupMonitor([]),
            snapshots,
            new ThrowingSignalSnapshotReader(),
            writeAudit);

        var found = service.TryGetRuntimeStatus("LINE1-PLC", out var status);

        Assert.True(found);
        Assert.Equal("line1-plc", status.WriteAudit.DeviceName);
        Assert.Equal(1, writeAudit.GetDeviceSummaryCount);
    }

    [Fact]
    public void GetAttentionSummaries_returns_only_devices_with_issues_first()
    {
        var line1 = new DeviceDefinition("line1-plc", "fake", "127.0.0.1");
        var line2 = new DeviceDefinition("line2-plc", "fake", "127.0.0.2");
        var line3 = new DeviceDefinition("line3-plc", "fake", "127.0.0.3");
        var registry = new DeviceRegistry([line2, line3, line1]);
        var snapshots = new SignalSnapshotStore();
        var statuses = new PollingStatusStore();
        var failedGroup = new SignalPollingGroupDefinition
        {
            Name = "line1-fast",
            DeviceName = "line1-plc"
        };
        var healthyGroup = new SignalPollingGroupDefinition
        {
            Name = "line3-fast",
            DeviceName = "line3-plc"
        };
        statuses.MarkFailure(
            failedGroup,
            TimeSpan.FromMilliseconds(25),
            new InvalidOperationException("PLC read timeout."));
        statuses.MarkSuccess(
            healthyGroup,
            TimeSpan.FromMilliseconds(10),
            signalCount: 0);
        using var connections = new DeviceConnectionPool(new FakeConnectionFactory());
        var service = new DeviceDashboardService(
            registry,
            new DeviceHealthService(registry, snapshots, statuses),
            connections,
            new PollingGroupMonitor(new PollingGroupRegistry([failedGroup, healthyGroup]), statuses),
            snapshots,
            new DeviceSignalSnapshotReader(registry, snapshots));

        var attention = service.GetAttentionSummaries();

        Assert.Equal(["line1-plc", "line2-plc"], attention.Select(summary => summary.DeviceName));
        Assert.True(attention[0].CriticalIssueCount > 0);
        Assert.Equal(0, attention[1].CriticalIssueCount);
        Assert.True(attention[1].WarningIssueCount > 0);
    }

    [Fact]
    public void GetAttentionSummaries_can_limit_result_count()
    {
        var line1 = new DeviceDefinition("line1-plc", "fake", "127.0.0.1");
        var line2 = new DeviceDefinition("line2-plc", "fake", "127.0.0.2");
        var registry = new DeviceRegistry([line2, line1]);
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
        using var connections = new DeviceConnectionPool(new FakeConnectionFactory());
        var service = new DeviceDashboardService(
            registry,
            new DeviceHealthService(registry, snapshots, statuses),
            connections,
            new PollingGroupMonitor(new PollingGroupRegistry([group1, group2]), statuses),
            snapshots,
            new DeviceSignalSnapshotReader(registry, snapshots));

        var attention = service.GetAttentionSummaries(count: 1);

        var summary = Assert.Single(attention);
        Assert.Equal("line1-plc", summary.DeviceName);
    }

    [Fact]
    public void GetAttentionSummaries_can_filter_by_minimum_severity()
    {
        var line1 = new DeviceDefinition("line1-plc", "fake", "127.0.0.1");
        var line2 = new DeviceDefinition("line2-plc", "fake", "127.0.0.2");
        var registry = new DeviceRegistry([line2, line1]);
        var snapshots = new SignalSnapshotStore();
        var statuses = new PollingStatusStore();
        var failedGroup = new SignalPollingGroupDefinition
        {
            Name = "line1-fast",
            DeviceName = "line1-plc"
        };
        statuses.MarkFailure(
            failedGroup,
            TimeSpan.FromMilliseconds(25),
            new InvalidOperationException("Line 1 timeout."));
        using var connections = new DeviceConnectionPool(new FakeConnectionFactory());
        var service = new DeviceDashboardService(
            registry,
            new DeviceHealthService(registry, snapshots, statuses),
            connections,
            new PollingGroupMonitor(new PollingGroupRegistry([failedGroup]), statuses),
            snapshots,
            new DeviceSignalSnapshotReader(registry, snapshots));

        var attention = service.GetAttentionSummaries(
            minimumSeverity: DeviceDashboardIssueSeverity.Critical);

        var summary = Assert.Single(attention);
        Assert.Equal("line1-plc", summary.DeviceName);
        Assert.True(summary.CriticalIssueCount > 0);
    }

    [Fact]
    public void GetRuntimeStatus_returns_overview_attention_issues_and_write_audit()
    {
        var line1 = new DeviceDefinition("line1-plc", "fake", "127.0.0.1");
        var line2 = new DeviceDefinition("line2-plc", "fake", "127.0.0.2");
        var registry = new DeviceRegistry([line2, line1]);
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
            new InvalidOperationException("Line 1 timeout."));
        var writeAudit = new WriteAuditStore();
        writeAudit.Add(new WriteAuditRecord(
            0,
            DateTimeOffset.UtcNow,
            new SignalRef("line1-plc", "ResetCommand"),
            true,
            Succeeded: false,
            Error: "PLC rejected write."));
        using var connections = new DeviceConnectionPool(new FakeConnectionFactory());
        var service = new DeviceDashboardService(
            registry,
            new DeviceHealthService(registry, snapshots, statuses),
            connections,
            new PollingGroupMonitor(new PollingGroupRegistry([group]), statuses),
            snapshots,
            new DeviceSignalSnapshotReader(registry, snapshots),
            writeAudit);

        var before = DateTimeOffset.UtcNow;
        var status = service.GetRuntimeStatus(
            attentionCount: 1,
            minimumSeverity: DeviceDashboardIssueSeverity.Warning);
        var after = DateTimeOffset.UtcNow;

        Assert.Equal(2, status.Overview.DeviceCount);
        Assert.True(status.Overview.IssueCount > 0);
        Assert.Equal(1, status.Overview.WriteAuditIssueCount);
        var attention = Assert.Single(status.Attention);
        Assert.Equal("line1-plc", attention.DeviceName);
        Assert.Contains(status.IssueSummaries, summary =>
            summary.Source == DeviceDashboardIssueSources.WriteAudit &&
            summary.WarningIssueCount == 1);
        Assert.Equal(1, status.WriteAudit.FailedWriteCount);
        Assert.Equal("PLC rejected write.", status.WriteAudit.LastError);
        Assert.InRange(status.GeneratedAt, before, after);
    }

    [Fact]
    public void GetRuntimeStatus_reuses_summary_status_reads_for_overview_and_attention()
    {
        var device = new DeviceDefinition("line1-plc", "fake", "127.0.0.1");
        var registry = new DeviceRegistry([device]);
        var snapshots = new SignalSnapshotStore();
        var statuses = new PollingStatusStore();
        var connections = new CountingConnectionPool();
        var pollingGroups = new CountingPollingGroupMonitor();
        var service = new DeviceDashboardService(
            registry,
            new DeviceHealthService(registry, snapshots, statuses),
            connections,
            pollingGroups,
            snapshots,
            new DeviceSignalSnapshotReader(registry, snapshots));

        var status = service.GetRuntimeStatus();

        Assert.Equal(1, status.Overview.DeviceCount);
        Assert.Equal(1, connections.GetStatusCount);
        Assert.Equal(1, pollingGroups.GetAllCount);
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
        Assert.Equal(1, overview.HealthIssueCount);
        Assert.Equal(0, overview.ConnectionIssueCount);
        Assert.Equal(0, overview.PollingIssueCount);
        Assert.Equal(0, overview.WriteAuditIssueCount);
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
            issue.Source == DeviceDashboardIssueSources.Connection &&
            issue.Severity == DeviceDashboardIssueSeverity.Critical);
        Assert.Contains(issues, issue =>
            issue.Code == "polling-group-failed" &&
            issue.Source == DeviceDashboardIssueSources.Polling &&
            issue.Message == "PLC read timeout.");
    }

    [Fact]
    public void GetIssues_includes_failed_write_audit_issue()
    {
        var device = new DeviceDefinition("line1-plc", "fake", "127.0.0.1");
        var registry = new DeviceRegistry([device]);
        var snapshots = new SignalSnapshotStore();
        var statuses = new PollingStatusStore();
        var writeAudit = new WriteAuditStore();
        writeAudit.Add(new WriteAuditRecord(
            0,
            DateTimeOffset.UtcNow,
            new SignalRef("line1-plc", "ResetCommand"),
            true,
            Succeeded: false,
            Error: "PLC rejected write."));
        using var connections = new DeviceConnectionPool(new FakeConnectionFactory());
        var service = new DeviceDashboardService(
            registry,
            new DeviceHealthService(registry, snapshots, statuses),
            connections,
            new PollingGroupMonitor(new PollingGroupRegistry([]), statuses),
            snapshots,
            new DeviceSignalSnapshotReader(registry, snapshots),
            writeAudit);

        var issues = service.GetIssues(new DeviceDashboardIssueFilter(
            Source: DeviceDashboardIssueSources.WriteAudit));

        var issue = Assert.Single(issues);
        Assert.Equal("line1-plc", issue.DeviceName);
        Assert.Equal(DeviceDashboardIssueSeverity.Warning, issue.Severity);
        Assert.Equal("write-audit-failed", issue.Code);
        Assert.Equal("PLC rejected write.", issue.Message);
    }

    [Fact]
    public void GetIssues_can_filter_by_source_severity_and_count()
    {
        var line1 = new DeviceDefinition("line1-plc", "fake", "127.0.0.1");
        var line2 = new DeviceDefinition("line2-plc", "fake", "127.0.0.2");
        var registry = new DeviceRegistry([line2, line1]);
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
        using var connections = new DeviceConnectionPool(new FakeConnectionFactory());
        var service = new DeviceDashboardService(
            registry,
            new DeviceHealthService(registry, snapshots, statuses),
            connections,
            new PollingGroupMonitor(new PollingGroupRegistry([group1, group2]), statuses),
            snapshots,
            new DeviceSignalSnapshotReader(registry, snapshots));

        var issues = service.GetIssues(new DeviceDashboardIssueFilter(
            MinimumSeverity: DeviceDashboardIssueSeverity.Critical,
            Source: DeviceDashboardIssueSources.Polling,
            Count: 1));

        var issue = Assert.Single(issues);
        Assert.Equal("line1-plc", issue.DeviceName);
        Assert.Equal(DeviceDashboardIssueSources.Polling, issue.Source);
        Assert.Equal(DeviceDashboardIssueSeverity.Critical, issue.Severity);
    }

    [Fact]
    public async Task GetIssueSummaries_groups_issues_by_source()
    {
        var device = new DeviceDefinition("line1-plc", "fake", "127.0.0.1");
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

        var summaries = service.GetIssueSummaries();

        Assert.Contains(summaries, summary =>
            summary.Source == DeviceDashboardIssueSources.Health &&
            summary.IssueCount == 1 &&
            summary.CriticalIssueCount == 1);
        Assert.Contains(summaries, summary =>
            summary.Source == DeviceDashboardIssueSources.Connection &&
            summary.IssueCount == 1 &&
            summary.CriticalIssueCount == 1);
        Assert.Contains(summaries, summary =>
            summary.Source == DeviceDashboardIssueSources.Polling &&
            summary.IssueCount == 1 &&
            summary.CriticalIssueCount == 1);
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
    public void TryGetIssues_can_filter_one_device_by_source()
    {
        var device = new DeviceDefinition("line1-plc", "fake", "127.0.0.1");
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
        using var connections = new DeviceConnectionPool(new FakeConnectionFactory());
        var service = new DeviceDashboardService(
            registry,
            new DeviceHealthService(registry, snapshots, statuses),
            connections,
            new PollingGroupMonitor(new PollingGroupRegistry([group]), statuses),
            snapshots,
            new DeviceSignalSnapshotReader(registry, snapshots));

        var found = service.TryGetIssues(
            "line1-plc",
            new DeviceDashboardIssueFilter(Source: DeviceDashboardIssueSources.Health),
            out var issues);

        Assert.True(found);
        Assert.Single(issues);
        Assert.All(issues, issue => Assert.Equal(DeviceDashboardIssueSources.Health, issue.Source));
    }

    [Fact]
    public void TryGetIssueSummaries_groups_issues_for_one_device()
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
        using var connections = new DeviceConnectionPool(new FakeConnectionFactory());
        var service = new DeviceDashboardService(
            registry,
            new DeviceHealthService(registry, snapshots, statuses),
            connections,
            new PollingGroupMonitor(new PollingGroupRegistry([group1, group2]), statuses),
            snapshots,
            new DeviceSignalSnapshotReader(registry, snapshots));

        var found = service.TryGetIssueSummaries(
            "LINE1-PLC",
            filter: null,
            out var summaries);

        Assert.True(found);
        Assert.Contains(summaries, summary =>
            summary.Source == DeviceDashboardIssueSources.Health &&
            summary.IssueCount == 1 &&
            summary.CriticalIssueCount == 1);
        Assert.Contains(summaries, summary =>
            summary.Source == DeviceDashboardIssueSources.Polling &&
            summary.IssueCount == 1 &&
            summary.CriticalIssueCount == 1);
        Assert.DoesNotContain(summaries, summary =>
            summary.Source == DeviceDashboardIssueSources.Connection);
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

    [Fact]
    public void TryGetIssueSummaries_returns_false_for_unknown_device()
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

        var found = service.TryGetIssueSummaries(
            "missing",
            filter: null,
            out var summaries);

        Assert.False(found);
        Assert.Empty(summaries);
    }

    [Fact]
    public void TryGetRuntimeStatus_returns_false_for_unknown_device()
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

        var found = service.TryGetRuntimeStatus("missing", out _);

        Assert.False(found);
    }

    [Fact]
    public void TryGetSummary_returns_false_for_unknown_device()
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

        var found = service.TryGetSummary("missing", out _);

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

    private sealed class ScopedConnectionPool : IDeviceConnectionPool
    {
        private readonly DeviceConnectionPoolStatus _status;

        public ScopedConnectionPool(DeviceConnectionPoolStatus status)
        {
            _status = status;
        }

        public ValueTask<IDeviceConnectionLease> RentAsync(
            string deviceName,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask<bool> CloseAsync(
            string deviceName,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask<int> CloseAllAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public IReadOnlyList<DeviceConnectionPoolStatus> GetStatus()
        {
            throw new InvalidOperationException("Device-scoped dashboard queries should not read all pool statuses.");
        }

        public bool TryGetStatus(
            string deviceName,
            out DeviceConnectionPoolStatus status)
        {
            if (string.Equals(deviceName, _status.DeviceName, StringComparison.OrdinalIgnoreCase))
            {
                status = _status;
                return true;
            }

            status = default!;
            return false;
        }
    }

    private sealed class ScopedPollingGroupMonitor : IPollingGroupMonitor
    {
        private readonly IReadOnlyList<PollingGroupSummary> _summaries;

        public ScopedPollingGroupMonitor(IReadOnlyList<PollingGroupSummary> summaries)
        {
            _summaries = summaries;
        }

        public IReadOnlyList<PollingGroupSummary> GetAll()
        {
            throw new InvalidOperationException("Device-scoped dashboard queries should not read all polling groups.");
        }

        public IReadOnlyList<PollingGroupSummary> GetForDevice(string deviceName)
        {
            return _summaries
                .Where(summary => string.Equals(
                    summary.DeviceName,
                    deviceName,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        public bool TryGet(string groupName, out PollingGroupSummary summary)
        {
            summary = default!;
            return false;
        }
    }

    private sealed class CountingConnectionPool : IDeviceConnectionPool
    {
        public int GetStatusCount { get; private set; }

        public ValueTask<IDeviceConnectionLease> RentAsync(
            string deviceName,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask<bool> CloseAsync(
            string deviceName,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask<int> CloseAllAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public IReadOnlyList<DeviceConnectionPoolStatus> GetStatus()
        {
            GetStatusCount++;
            return [];
        }

        public bool TryGetStatus(
            string deviceName,
            out DeviceConnectionPoolStatus status)
        {
            status = default!;
            return false;
        }
    }

    private sealed class CountingPollingGroupMonitor : IPollingGroupMonitor
    {
        public int GetAllCount { get; private set; }

        public IReadOnlyList<PollingGroupSummary> GetAll()
        {
            GetAllCount++;
            return [];
        }

        public IReadOnlyList<PollingGroupSummary> GetForDevice(string deviceName)
        {
            return [];
        }

        public bool TryGet(string groupName, out PollingGroupSummary summary)
        {
            summary = default!;
            return false;
        }
    }

    private sealed class CountingWriteAuditStore : IWriteAuditStore
    {
        public int GetDeviceSummaryCount { get; private set; }

        public void Add(WriteAuditRecord record)
        {
            throw new NotSupportedException();
        }

        public IReadOnlyList<WriteAuditRecord> GetRecent(int count = 100)
        {
            throw new NotSupportedException();
        }

        public IReadOnlyList<WriteAuditRecord> GetDeviceRecords(
            string deviceName,
            int count = 100)
        {
            throw new NotSupportedException();
        }

        public WriteAuditSummary GetSummary()
        {
            throw new NotSupportedException();
        }

        public WriteAuditSummary GetDeviceSummary(string deviceName)
        {
            GetDeviceSummaryCount++;
            return new WriteAuditSummary(
                deviceName,
                WriteCount: 0,
                SucceededWriteCount: 0,
                FailedWriteCount: 0,
                LastWriteTimestamp: null,
                LastFailedWriteTimestamp: null,
                LastError: null);
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
