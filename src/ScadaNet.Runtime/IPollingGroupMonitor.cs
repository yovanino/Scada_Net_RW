namespace ScadaNet.Runtime;

public interface IPollingGroupMonitor
{
    IReadOnlyList<PollingGroupSummary> GetAll();

    IReadOnlyList<PollingGroupSummary> GetForDevice(string deviceName);

    bool TryGet(string groupName, out PollingGroupSummary summary);
}
