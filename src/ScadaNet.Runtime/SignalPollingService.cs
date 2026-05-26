using ScadaNet.Model;

namespace ScadaNet.Runtime;

public sealed class SignalPollingService : ISignalPollingService
{
    private readonly IPlcRuntime _runtime;
    private readonly IPollingStatusStore _statuses;

    public SignalPollingService(
        IPlcRuntime runtime,
        IPollingStatusStore statuses)
    {
        _runtime = runtime;
        _statuses = statuses;
    }

    public async ValueTask<IReadOnlyList<SignalValue>> PollAsync(
        SignalPollingGroupDefinition group,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(group);

        var started = DateTimeOffset.UtcNow;

        if (!group.Enabled)
        {
            _statuses.MarkSkipped(group, "Polling group is disabled.");
            return [];
        }

        var signals = group.ToSignals();

        if (signals.Count == 0)
        {
            _statuses.MarkSkipped(group, "Polling group has no signals.");
            return [];
        }

        try
        {
            var values = await _runtime.ReadManyAsync(signals, cancellationToken)
                .ConfigureAwait(false);

            _statuses.MarkSuccess(group, DateTimeOffset.UtcNow - started, values.Count);
            return values;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _statuses.MarkFailure(group, DateTimeOffset.UtcNow - started, ex);
            throw;
        }
    }
}
