namespace ScadaNet.Runtime;

public sealed record DeviceDashboard(
    DeviceDefinition Device,
    DeviceHealthSummary Health,
    DeviceConnectionPoolStatus? Connection,
    IReadOnlyList<PollingGroupSummary> PollingGroups,
    IReadOnlyList<DeviceSignalSnapshot> Signals);
