namespace ScadaNet.Runtime;

public interface IDeviceDashboardService
{
    IReadOnlyList<DeviceDashboard> GetAll();

    bool TryGet(string deviceName, out DeviceDashboard dashboard);
}
