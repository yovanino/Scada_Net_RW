namespace ScadaNet.Runtime;

public interface IDeviceDashboardService
{
    IReadOnlyList<DeviceDashboard> GetAll();

    IReadOnlyList<DeviceDashboardSummary> GetSummaries();

    IReadOnlyList<DeviceDashboardSummary> GetAttentionSummaries(
        int? count = null,
        DeviceDashboardIssueSeverity? minimumSeverity = null);

    bool TryGetSummary(string deviceName, out DeviceDashboardSummary summary);

    DeviceDashboardOverview GetOverview();

    IReadOnlyList<DeviceDashboardIssue> GetIssues();

    bool TryGetIssues(string deviceName, out IReadOnlyList<DeviceDashboardIssue> issues);

    bool TryGet(string deviceName, out DeviceDashboard dashboard);
}
