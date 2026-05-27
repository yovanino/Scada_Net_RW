namespace ScadaNet.Runtime;

public sealed record DeviceDashboardIssueSummary(
    string Source,
    int IssueCount,
    int WarningIssueCount,
    int CriticalIssueCount);
