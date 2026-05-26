using ScadaNet.Model;

namespace ScadaNet.Runtime;

public interface ISignalPollingService
{
    ValueTask<IReadOnlyList<SignalValue>> PollAsync(
        SignalPollingGroupDefinition group,
        CancellationToken cancellationToken = default);
}
