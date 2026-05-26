namespace ScadaNet.Runtime;

public interface IPollingGroupMonitor
{
    IReadOnlyList<PollingGroupSummary> GetAll();

    bool TryGet(string groupName, out PollingGroupSummary summary);
}
