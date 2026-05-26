namespace ScadaNet.Runtime;

public interface IDeviceDashboardService
{
    bool TryGet(string deviceName, out DeviceDashboard dashboard);
}
