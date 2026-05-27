namespace ScadaNet.Runtime;

public sealed record DeviceDashboardIssueFilter(
    DeviceDashboardIssueSeverity? MinimumSeverity = null,
    string? Source = null,
    int? Count = null);
