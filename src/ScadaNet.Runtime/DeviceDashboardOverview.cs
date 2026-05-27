namespace ScadaNet.Runtime;

public sealed record DeviceDashboardOverview(
    int DeviceCount,
    int HealthyDeviceCount,
    int DegradedDeviceCount,
    int UnknownDeviceCount,
    int ActiveConnectionCount,
    int FailedConnectionCount,
    int PollingGroupCount,
    int StalePollingGroupCount,
    int SignalCount,
    int SignalWithValueCount,
    int WritableSignalCount,
    int ArraySignalCount,
    int IssueCount,
    int WarningIssueCount,
    int CriticalIssueCount,
    int HealthIssueCount,
    int ConnectionIssueCount,
    int PollingIssueCount,
    int WriteAuditIssueCount);
