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
            signals.Count(signal => signal.IsArray));
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
}
