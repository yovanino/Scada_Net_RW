namespace ScadaNet.Runtime;

public interface IDeviceDashboardService
{
    IReadOnlyList<DeviceDashboard> GetAll();

    IReadOnlyList<DeviceDashboardSummary> GetSummaries();

    DeviceDashboardOverview GetOverview();

    IReadOnlyList<DeviceDashboardIssue> GetIssues();

    bool TryGetIssues(string deviceName, out IReadOnlyList<DeviceDashboardIssue> issues);

    bool TryGet(string deviceName, out DeviceDashboard dashboard);
}
