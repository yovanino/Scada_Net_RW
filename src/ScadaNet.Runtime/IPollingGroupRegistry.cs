namespace ScadaNet.Runtime;

public interface IPollingGroupRegistry
{
    IReadOnlyCollection<SignalPollingGroupDefinition> Groups { get; }

    bool TryGet(string name, out SignalPollingGroupDefinition group);
}
