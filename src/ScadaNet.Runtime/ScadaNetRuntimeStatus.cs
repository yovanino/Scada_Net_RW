namespace ScadaNet.Runtime;

public sealed record ScadaNetRuntimeStatus(
    DeviceDashboardOverview Overview,
    IReadOnlyList<DeviceDashboardSummary> Attention,
    IReadOnlyList<DeviceDashboardIssueSummary> IssueSummaries,
    WriteAuditSummary WriteAudit);
