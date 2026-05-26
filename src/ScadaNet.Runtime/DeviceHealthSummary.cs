namespace ScadaNet.Runtime;

public sealed record DeviceHealthSummary(
    string DeviceName,
    string Driver,
    string Address,
    DeviceHealthState State,
    int SnapshotCount,
    int PollingGroupCount,
    DateTimeOffset? LastSnapshotTimestamp,
    DateTimeOffset? LastPollingTimestamp,
    IReadOnlyList<string> Messages);
