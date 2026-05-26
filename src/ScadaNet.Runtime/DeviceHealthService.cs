namespace ScadaNet.Runtime;

public sealed class DeviceHealthService : IDeviceHealthService
{
    private readonly IDeviceRegistry _registry;
    private readonly ISignalSnapshotStore _snapshots;
    private readonly IPollingStatusStore _pollingStatuses;

    public DeviceHealthService(
        IDeviceRegistry registry,
        ISignalSnapshotStore snapshots,
        IPollingStatusStore pollingStatuses)
    {
        _registry = registry;
        _snapshots = snapshots;
        _pollingStatuses = pollingStatuses;
    }

    public IReadOnlyList<DeviceHealthSummary> GetAll()
    {
        return _registry.Devices
            .OrderBy(device => device.Name, StringComparer.OrdinalIgnoreCase)
            .Select(BuildHealth)
            .ToArray();
    }

    public bool TryGet(string deviceName, out DeviceHealthSummary health)
    {
        if (!_registry.TryGet(deviceName, out var device))
        {
            health = null!;
            return false;
        }

        health = BuildHealth(device);
        return true;
    }

    private DeviceHealthSummary BuildHealth(DeviceDefinition device)
    {
        var snapshots = _snapshots.GetDeviceSnapshots(device.Name);
        var statuses = _pollingStatuses.GetAll()
            .Where(status => string.Equals(
                status.DeviceName,
                device.Name,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var messages = new List<string>();

        DeviceHealthState state;

        if (statuses.Length == 0)
        {
            state = DeviceHealthState.Unknown;
            messages.Add("No polling status recorded.");
        }
        else if (statuses.Any(status => !status.Healthy))
        {
            state = DeviceHealthState.Degraded;
            messages.AddRange(statuses
                .Where(status => !status.Healthy && !string.IsNullOrWhiteSpace(status.Error))
                .Select(status => $"{status.GroupName}: {status.Error}"));
        }
        else
        {
            state = DeviceHealthState.Healthy;
        }

        if (snapshots.Count == 0)
        {
            messages.Add("No signal snapshots recorded.");
        }

        return new DeviceHealthSummary(
            device.Name,
            device.Driver,
            device.Address,
            state,
            snapshots.Count,
            statuses.Length,
            snapshots.Count == 0 ? null : snapshots.Max(snapshot => snapshot.Timestamp),
            statuses.Length == 0 ? null : statuses.Max(status => status.LastRun),
            messages);
    }
}
