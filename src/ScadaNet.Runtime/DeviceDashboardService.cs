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
