using ScadaNet.Model;

namespace ScadaNet.Runtime;

public sealed class DeviceDashboardService : IDeviceDashboardService
{
    private readonly IDeviceRegistry _devices;
    private readonly IDeviceHealthService _health;
    private readonly IDeviceConnectionPool _connections;
    private readonly IPollingGroupMonitor _pollingGroups;
    private readonly ISignalSnapshotStore _snapshotStore;
    private readonly IDeviceSignalSnapshotReader _snapshots;

    public DeviceDashboardService(
        IDeviceRegistry devices,
        IDeviceHealthService health,
        IDeviceConnectionPool connections,
        IPollingGroupMonitor pollingGroups,
        ISignalSnapshotStore snapshotStore,
        IDeviceSignalSnapshotReader snapshots)
    {
        _devices = devices;
        _health = health;
        _connections = connections;
        _pollingGroups = pollingGroups;
        _snapshotStore = snapshotStore;
        _snapshots = snapshots;
    }

    public IReadOnlyList<DeviceDashboard> GetAll()
    {
        return _devices.Devices
            .OrderBy(device => device.Name, StringComparer.OrdinalIgnoreCase)
            .Select(device => TryGet(device.Name, out var dashboard)
                ? dashboard
                : null)
            .Where(dashboard => dashboard is not null)
            .ToArray()!;
    }

    public IReadOnlyList<DeviceDashboardSummary> GetSummaries()
    {
        var connections = _connections.GetStatus();
        var pollingGroups = _pollingGroups.GetAll();

        return _devices.Devices
            .OrderBy(device => device.Name, StringComparer.OrdinalIgnoreCase)
            .Select(device => TryBuildSummary(device, connections, pollingGroups, out var summary)
                ? summary
                : null)
            .Where(summary => summary is not null)
            .Select(summary => summary!)
            .ToArray();
    }

    public IReadOnlyList<DeviceDashboardSummary> GetAttentionSummaries(
        int? count = null,
        DeviceDashboardIssueSeverity? minimumSeverity = null)
    {
        var summaries = GetSummaries()
            .Where(summary => HasMinimumSeverity(summary, minimumSeverity))
            .OrderByDescending(summary => summary.CriticalIssueCount)
            .ThenByDescending(summary => summary.WarningIssueCount)
            .ThenBy(summary => summary.DeviceName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return count.HasValue
            ? summaries.Take(Math.Max(0, count.Value)).ToArray()
            : summaries;
    }

    private static bool HasMinimumSeverity(
        DeviceDashboardSummary summary,
        DeviceDashboardIssueSeverity? minimumSeverity)
    {
        return minimumSeverity switch
        {
            DeviceDashboardIssueSeverity.Critical => summary.CriticalIssueCount > 0,
            DeviceDashboardIssueSeverity.Warning => summary.WarningIssueCount > 0 ||
                summary.CriticalIssueCount > 0,
            DeviceDashboardIssueSeverity.Info => summary.IssueCount > 0,
            _ => summary.IssueCount > 0
        };
    }

    public bool TryGetSummary(
        string deviceName,
        out DeviceDashboardSummary summary)
    {
        if (!_devices.TryGet(deviceName, out var device))
        {
            summary = default!;
            return false;
        }

        return TryBuildSummary(
            device,
            _connections.GetStatus(),
            _pollingGroups.GetAll(),
            out summary);
    }

    public DeviceDashboardOverview GetOverview()
    {
        var summaries = GetSummaries();
        var devices = _devices.Devices.ToArray();
        var deviceNames = new HashSet<string>(
            devices.Select(device => device.Name),
            StringComparer.OrdinalIgnoreCase);
        var failedConnectionCount = _connections.GetStatus()
            .Count(connection => deviceNames.Contains(connection.DeviceName) &&
                connection.FailedRentCount > 0);

        return new DeviceDashboardOverview(
            summaries.Count,
            summaries.Count(summary => summary.HealthState == DeviceHealthState.Healthy),
            summaries.Count(summary => summary.HealthState == DeviceHealthState.Degraded),
            summaries.Count(summary => summary.HealthState == DeviceHealthState.Unknown),
            summaries.Count(summary => summary.HasConnection),
            failedConnectionCount,
            summaries.Sum(summary => summary.PollingGroupCount),
            summaries.Sum(summary => summary.StalePollingGroupCount),
            summaries.Sum(summary => summary.SignalCount),
            summaries.Sum(summary => summary.SignalWithValueCount),
            devices.Sum(device => device.Signals.Count(signal => signal.Writable)),
            devices.Sum(device => device.Signals.Count(signal => signal.IsArray)),
            summaries.Sum(summary => summary.IssueCount),
            summaries.Sum(summary => summary.WarningIssueCount),
            summaries.Sum(summary => summary.CriticalIssueCount));
    }

    public IReadOnlyList<DeviceDashboardIssue> GetIssues()
    {
        var connections = _connections.GetStatus();
        var pollingGroups = _pollingGroups.GetAll();

        return _devices.Devices
            .SelectMany(device => GetIssues(device, connections, pollingGroups))
            .OrderByDescending(issue => issue.Severity)
            .ThenBy(issue => issue.DeviceName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(issue => issue.Source, StringComparer.OrdinalIgnoreCase)
            .ThenBy(issue => issue.Code, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool TryGetIssues(
        string deviceName,
        out IReadOnlyList<DeviceDashboardIssue> issues)
    {
        if (!_devices.TryGet(deviceName, out var device))
        {
            issues = [];
            return false;
        }

        issues = GetIssues(device, _connections.GetStatus(), _pollingGroups.GetAll())
            .OrderByDescending(issue => issue.Severity)
            .ThenBy(issue => issue.Source, StringComparer.OrdinalIgnoreCase)
            .ThenBy(issue => issue.Code, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return true;
    }

    public bool TryGet(string deviceName, out DeviceDashboard dashboard)
    {
        if (!_devices.TryGet(deviceName, out var device) ||
            !_health.TryGet(device.Name, out var health) ||
            !_snapshots.TryGetDeviceSnapshots(device.Name, out var snapshots))
        {
            dashboard = default!;
            return false;
        }

        var connection = _connections.GetStatus()
            .FirstOrDefault(status => string.Equals(
                status.DeviceName,
                device.Name,
                StringComparison.OrdinalIgnoreCase));
        var pollingGroups = _pollingGroups.GetAll()
            .Where(group => string.Equals(
                group.DeviceName,
                device.Name,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        dashboard = new DeviceDashboard(
            device,
            health,
            connection,
            pollingGroups,
            snapshots);
        return true;
    }

    private bool TryBuildSummary(
        DeviceDefinition device,
        IReadOnlyList<DeviceConnectionPoolStatus> connections,
        IReadOnlyList<PollingGroupSummary> pollingGroups,
        out DeviceDashboardSummary summary)
    {
        if (!_health.TryGet(device.Name, out var health))
        {
            summary = default!;
            return false;
        }

        var connection = connections.FirstOrDefault(status => string.Equals(
            status.DeviceName,
            device.Name,
            StringComparison.OrdinalIgnoreCase));
        var devicePollingGroups = pollingGroups
            .Where(group => string.Equals(
                group.DeviceName,
                device.Name,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var issues = BuildIssues(device.Name, health, connection, devicePollingGroups).ToArray();

        summary = new DeviceDashboardSummary(
            device.Name,
            device.Driver,
            device.Address,
            health.State,
            connection?.HasConnection ?? false,
            connection?.IsInUse ?? false,
            devicePollingGroups.Length,
            devicePollingGroups.Count(group => group.IsStale),
            device.Signals.Count,
            device.Signals.Count(signal => _snapshotStore.TryGet(
                new SignalRef(device.Name, signal.Address),
                out _)),
            issues.Length,
            issues.Count(issue => issue.Severity == DeviceDashboardIssueSeverity.Warning),
            issues.Count(issue => issue.Severity == DeviceDashboardIssueSeverity.Critical),
            health.LastSnapshotTimestamp,
            health.LastPollingTimestamp);
        return true;
    }

    private static IEnumerable<DeviceDashboardIssue> GetIssues(DeviceDashboard dashboard)
    {
        return BuildIssues(
            dashboard.Device.Name,
            dashboard.Health,
            dashboard.Connection,
            dashboard.PollingGroups);
    }

    private IEnumerable<DeviceDashboardIssue> GetIssues(
        DeviceDefinition device,
        IReadOnlyList<DeviceConnectionPoolStatus> connections,
        IReadOnlyList<PollingGroupSummary> pollingGroups)
    {
        if (!_health.TryGet(device.Name, out var health))
        {
            return [];
        }

        var connection = connections.FirstOrDefault(status => string.Equals(
                status.DeviceName,
                device.Name,
                StringComparison.OrdinalIgnoreCase));
        var devicePollingGroups = pollingGroups
            .Where(group => string.Equals(
                group.DeviceName,
                device.Name,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return BuildIssues(device.Name, health, connection, devicePollingGroups);
    }

    private static IEnumerable<DeviceDashboardIssue> BuildIssues(
        string deviceName,
        DeviceHealthSummary health,
        DeviceConnectionPoolStatus? connection,
        IReadOnlyList<PollingGroupSummary> pollingGroups)
    {
        if (health.State is DeviceHealthState.Degraded or DeviceHealthState.Unknown)
        {
            yield return new DeviceDashboardIssue(
                deviceName,
                health.State == DeviceHealthState.Degraded
                    ? DeviceDashboardIssueSeverity.Critical
                    : DeviceDashboardIssueSeverity.Warning,
                "health",
                health.State == DeviceHealthState.Degraded
                    ? "device-health-degraded"
                    : "device-health-unknown",
                GetHealthMessage(health));
        }

        if (connection is { FailedRentCount: > 0 })
        {
            yield return new DeviceDashboardIssue(
                deviceName,
                connection.HasConnection
                    ? DeviceDashboardIssueSeverity.Warning
                    : DeviceDashboardIssueSeverity.Critical,
                "connection",
                "connection-rent-failed",
                string.IsNullOrWhiteSpace(connection.LastError)
                    ? "Connection pool recorded one or more rent failures."
                    : connection.LastError);
        }

        foreach (var group in pollingGroups)
        {
            if (group.IsStale)
            {
                yield return new DeviceDashboardIssue(
                    deviceName,
                    DeviceDashboardIssueSeverity.Warning,
                    "polling",
                    "polling-group-stale",
                    $"Polling group '{group.GroupName}' has not run within {group.StaleAfter}.");
            }
            else if (group is { HasStatus: true, Healthy: false })
            {
                yield return new DeviceDashboardIssue(
                    deviceName,
                    DeviceDashboardIssueSeverity.Critical,
                    "polling",
                    "polling-group-failed",
                    string.IsNullOrWhiteSpace(group.Error)
                        ? $"Polling group '{group.GroupName}' failed."
                        : group.Error);
            }
            else if (group.Enabled && !group.HasStatus)
            {
                yield return new DeviceDashboardIssue(
                    deviceName,
                    DeviceDashboardIssueSeverity.Warning,
                    "polling",
                    "polling-group-no-status",
                    $"Polling group '{group.GroupName}' has no recorded status.");
            }
        }
    }

    private static string GetHealthMessage(DeviceHealthSummary health)
    {
        return health.Messages.Count == 0
            ? $"Device health is {health.State}."
            : string.Join(" ", health.Messages);
    }
}
