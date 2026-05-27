namespace ScadaNet.Runtime;

public sealed record DeviceDashboardSummary(
    string DeviceName,
    string Driver,
    string Address,
    DeviceHealthState HealthState,
    bool HasConnection,
    bool IsConnectionInUse,
    int PollingGroupCount,
    int StalePollingGroupCount,
    int SignalCount,
    int SignalWithValueCount,
    int IssueCount,
    int WarningIssueCount,
    int CriticalIssueCount,
    DateTimeOffset? LastSnapshotTimestamp,
    DateTimeOffset? LastPollingTimestamp);
