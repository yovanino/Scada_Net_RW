namespace ScadaNet.Runtime;

public interface IPollingStatusStore
{
    void MarkSuccess(
        SignalPollingGroupDefinition group,
        TimeSpan duration,
        int signalCount);

    void MarkSkipped(
        SignalPollingGroupDefinition group,
        string reason);

    void MarkFailure(
        SignalPollingGroupDefinition group,
        TimeSpan duration,
        Exception exception);

    IReadOnlyList<PollingGroupStatus> GetAll();

    bool TryGet(string groupName, out PollingGroupStatus status);
}
