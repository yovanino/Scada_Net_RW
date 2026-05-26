namespace ScadaNet.Runtime;

public interface IDeviceSignalSnapshotReader
{
    bool TryGetDeviceSnapshots(
        string deviceName,
        out IReadOnlyList<DeviceSignalSnapshot> snapshots);

    bool TryGet(
        string deviceName,
        string signalName,
        out DeviceSignalSnapshot snapshot);
}
