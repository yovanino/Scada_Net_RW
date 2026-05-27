namespace ScadaNet.Runtime;

public interface IDeviceDashboardService
{
    IReadOnlyList<DeviceDashboard> GetAll();

    DeviceDashboardOverview GetOverview();

    IReadOnlyList<DeviceDashboardIssue> GetIssues();

    bool TryGet(string deviceName, out DeviceDashboard dashboard);
}
