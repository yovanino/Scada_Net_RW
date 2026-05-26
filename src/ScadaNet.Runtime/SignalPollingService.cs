using ScadaNet.Model;

namespace ScadaNet.Runtime;

public sealed class SignalPollingService : ISignalPollingService
{
    private readonly IPlcRuntime _runtime;

    public SignalPollingService(IPlcRuntime runtime)
    {
        _runtime = runtime;
    }

    public async ValueTask<IReadOnlyList<SignalValue>> PollAsync(
        SignalPollingGroupDefinition group,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(group);

        if (!group.Enabled)
        {
            return [];
        }

        var signals = group.ToSignals();

        if (signals.Count == 0)
        {
            return [];
        }

        return await _runtime.ReadManyAsync(signals, cancellationToken)
            .ConfigureAwait(false);
    }
}
