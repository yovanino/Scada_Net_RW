namespace ScadaNet.Runtime;

public sealed class DeviceDashboardService : IDeviceDashboardService
{
    private readonly IDeviceRegistry _devices;
    private readonly IDeviceHealthService _health;
    private readonly IDeviceConnectionPool _connections;
    private readonly IPollingGroupMonitor _pollingGroups;
    private readonly IDeviceSignalSnapshotReader _snapshots;

    public DeviceDashboardService(
        IDeviceRegistry devices,
        IDeviceHealthService health,
        IDeviceConnectionPool connections,
        IPollingGroupMonitor pollingGroups,
        IDeviceSignalSnapshotReader snapshots)
    {
        _devices = devices;
        _health = health;
        _connections = connections;
        _pollingGroups = pollingGroups;
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

    public DeviceDashboardOverview GetOverview()
    {
        var dashboards = GetAll();
        var connections = dashboards
            .Select(dashboard => dashboard.Connection)
            .Where(connection => connection is not null)
            .ToArray();
        var pollingGroups = dashboards
            .SelectMany(dashboard => dashboard.PollingGroups)
            .ToArray();
        var signals = dashboards
            .SelectMany(dashboard => dashboard.Signals)
            .ToArray();
        var issues = dashboards
            .SelectMany(GetIssues)
            .ToArray();

        return new DeviceDashboardOverview(
            dashboards.Count,
            dashboards.Count(dashboard => dashboard.Health.State == DeviceHealthState.Healthy),
            dashboards.Count(dashboard => dashboard.Health.State == DeviceHealthState.Degraded),
            dashboards.Count(dashboard => dashboard.Health.State == DeviceHealthState.Unknown),
            connections.Count(connection => connection!.HasConnection),
            connections.Count(connection => connection!.FailedRentCount > 0),
            pollingGroups.Length,
            pollingGroups.Count(group => group.IsStale),
            signals.Length,
            signals.Count(signal => signal.HasValue),
            signals.Count(signal => signal.Writable),
            signals.Count(signal => signal.IsArray),
            issues.Length,
            issues.Count(issue => issue.Severity == DeviceDashboardIssueSeverity.Warning),
            issues.Count(issue => issue.Severity == DeviceDashboardIssueSeverity.Critical));
    }

    public IReadOnlyList<DeviceDashboardIssue> GetIssues()
    {
        return GetAll()
            .SelectMany(GetIssues)
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
        if (!TryGet(deviceName, out var dashboard))
        {
            issues = [];
            return false;
        }

        issues = GetIssues(dashboard)
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

    private static IEnumerable<DeviceDashboardIssue> GetIssues(DeviceDashboard dashboard)
    {
        if (dashboard.Health.State is DeviceHealthState.Degraded or DeviceHealthState.Unknown)
        {
            yield return new DeviceDashboardIssue(
                dashboard.Device.Name,
                dashboard.Health.State == DeviceHealthState.Degraded
                    ? DeviceDashboardIssueSeverity.Critical
                    : DeviceDashboardIssueSeverity.Warning,
                "health",
                dashboard.Health.State == DeviceHealthState.Degraded
                    ? "device-health-degraded"
                    : "device-health-unknown",
                GetHealthMessage(dashboard.Health));
        }

        if (dashboard.Connection is { FailedRentCount: > 0 } connection)
        {
            yield return new DeviceDashboardIssue(
                dashboard.Device.Name,
                connection.HasConnection
                    ? DeviceDashboardIssueSeverity.Warning
                    : DeviceDashboardIssueSeverity.Critical,
                "connection",
                "connection-rent-failed",
                string.IsNullOrWhiteSpace(connection.LastError)
                    ? "Connection pool recorded one or more rent failures."
                    : connection.LastError);
        }

        foreach (var group in dashboard.PollingGroups)
        {
            if (group.IsStale)
            {
                yield return new DeviceDashboardIssue(
                    dashboard.Device.Name,
                    DeviceDashboardIssueSeverity.Warning,
                    "polling",
                    "polling-group-stale",
                    $"Polling group '{group.GroupName}' has not run within {group.StaleAfter}.");
            }
            else if (group is { HasStatus: true, Healthy: false })
            {
                yield return new DeviceDashboardIssue(
                    dashboard.Device.Name,
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
                    dashboard.Device.Name,
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
