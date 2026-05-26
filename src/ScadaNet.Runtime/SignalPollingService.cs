using ScadaNet.Model;

namespace ScadaNet.Runtime;

public sealed class SignalPollingService : ISignalPollingService
{
    private readonly IPlcRuntime _runtime;
    private readonly IPollingStatusStore _statuses;
    private readonly IDeviceSignalResolver? _signalResolver;

    public SignalPollingService(
        IPlcRuntime runtime,
        IPollingStatusStore statuses)
        : this(runtime, statuses, signalResolver: null)
    {
    }

    public SignalPollingService(
        IPlcRuntime runtime,
        IPollingStatusStore statuses,
        IDeviceSignalResolver? signalResolver)
    {
        _runtime = runtime;
        _statuses = statuses;
        _signalResolver = signalResolver;
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

        try
        {
            var signals = GetSignals(group);

            if (signals.Count == 0)
            {
                _statuses.MarkSkipped(group, "Polling group has no signals.");
                return [];
            }

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

    private IReadOnlyList<SignalRef> GetSignals(SignalPollingGroupDefinition group)
    {
        var signals = new List<SignalRef>(group.Addresses.Count + group.SignalNames.Count);
        signals.AddRange(group.ToSignals());

        if (group.SignalNames.Count == 0)
        {
            return signals;
        }

        if (_signalResolver is null)
        {
            throw new InvalidOperationException(
                $"Polling group '{group.Name}' uses signal names, but no signal resolver is configured.");
        }

        if (!_signalResolver.TryResolveMany(
            group.DeviceName,
            group.SignalNames.ToArray(),
            out var resolutions,
            out var missingSignalName))
        {
            throw new KeyNotFoundException(
                $"Signal '{missingSignalName}' is not registered for device '{group.DeviceName}'.");
        }

        signals.AddRange(resolutions.Select(resolution => resolution.Signal));
        return signals;
    }
}
