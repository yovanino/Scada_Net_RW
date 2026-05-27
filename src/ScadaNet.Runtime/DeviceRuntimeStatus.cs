namespace ScadaNet.Runtime;

public sealed record DeviceRuntimeStatus(
    DeviceDashboardSummary Summary,
    DeviceConnectionPoolStatus? Connection,
    IReadOnlyList<PollingGroupSummary> PollingGroups,
    IReadOnlyList<DeviceDashboardIssueSummary> IssueSummaries,
    WriteAuditSummary WriteAudit,
    DateTimeOffset GeneratedAt);
