namespace ScadaNet.Runtime;

public interface IDeviceDashboardService
{
    IReadOnlyList<DeviceDashboard> GetAll();

    DeviceDashboardOverview GetOverview();

    bool TryGet(string deviceName, out DeviceDashboard dashboard);
}
