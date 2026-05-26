using ScadaNet.Model;

namespace ScadaNet.Runtime;

public sealed class DeviceSignalSnapshotReader : IDeviceSignalSnapshotReader
{
    private readonly IDeviceRegistry _registry;
    private readonly ISignalSnapshotStore _snapshots;

    public DeviceSignalSnapshotReader(
        IDeviceRegistry registry,
        ISignalSnapshotStore snapshots)
    {
        _registry = registry;
        _snapshots = snapshots;
    }

    public bool TryGetDeviceSnapshots(
        string deviceName,
        out IReadOnlyList<DeviceSignalSnapshot> snapshots)
    {
        if (!_registry.TryGet(deviceName, out var device))
        {
            snapshots = [];
            return false;
        }

        snapshots = device.Signals
            .OrderBy(signal => signal.DisplayOrder ?? int.MaxValue)
            .ThenBy(signal => signal.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(signal => signal.Name, StringComparer.OrdinalIgnoreCase)
            .Select(signal => BuildSnapshot(device, signal))
            .ToArray();
        return true;
    }

    public bool TryGet(
        string deviceName,
        string signalName,
        out DeviceSignalSnapshot snapshot)
    {
        if (!_registry.TryGet(deviceName, out var device) ||
            !device.TryGetSignal(signalName, out var signal))
        {
            snapshot = default!;
            return false;
        }

        snapshot = BuildSnapshot(device, signal);
        return true;
    }

    private DeviceSignalSnapshot BuildSnapshot(
        DeviceDefinition device,
        DeviceSignalDefinition signal)
    {
        var signalRef = new SignalRef(device.Name, signal.Address);
        var hasValue = _snapshots.TryGet(signalRef, out var value);

        return new DeviceSignalSnapshot(
            signal.Name,
            signal.Address,
            signal.DataType,
            signal.Unit,
            signal.Description,
            signal.Category,
            signal.DisplayOrder,
            signal.IsArray,
            signal.ElementCount,
            signal.Writable,
            hasValue,
            hasValue ? value : null);
    }
}
