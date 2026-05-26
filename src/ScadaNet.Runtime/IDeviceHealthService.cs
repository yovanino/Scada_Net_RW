namespace ScadaNet.Runtime;

public interface IDeviceHealthService
{
    IReadOnlyList<DeviceHealthSummary> GetAll();

    bool TryGet(string deviceName, out DeviceHealthSummary health);
}
