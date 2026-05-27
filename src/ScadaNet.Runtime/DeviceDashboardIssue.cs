namespace ScadaNet.Runtime;

public sealed record DeviceDashboardIssue(
    string DeviceName,
    DeviceDashboardIssueSeverity Severity,
    string Source,
    string Code,
    string Message);
