using ScadaNet.Model;

namespace ScadaNet.Runtime;

public sealed class PlcRuntime : IPlcRuntime
{
    private readonly IDeviceConnectionPool _connections;

    public PlcRuntime(IDeviceConnectionPool connections)
    {
        _connections = connections;
    }

    public async ValueTask<SignalValue> ReadAsync(
        SignalRef signal,
        CancellationToken cancellationToken = default)
    {
        await using var lease = await _connections
            .RentAsync(signal.DeviceName, cancellationToken)
            .ConfigureAwait(false);

        return await lease.Connection.ReadAsync(signal, cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask<IReadOnlyList<SignalValue>> ReadManyAsync(
        IReadOnlyList<SignalRef> signals,
        CancellationToken cancellationToken = default)
    {
        var values = new List<SignalValue>(signals.Count);

        foreach (var group in signals.GroupBy(signal => signal.DeviceName))
        {
            await using var lease = await _connections
                .RentAsync(group.Key, cancellationToken)
                .ConfigureAwait(false);

            var groupValues = await lease.Connection
                .ReadManyAsync(group.ToArray(), cancellationToken)
                .ConfigureAwait(false);

            values.AddRange(groupValues);
        }

        return values;
    }

    public async ValueTask WriteAsync(
        SignalRef signal,
        object? value,
        CancellationToken cancellationToken = default)
    {
        await using var lease = await _connections
            .RentAsync(signal.DeviceName, cancellationToken)
            .ConfigureAwait(false);

        await lease.Connection.WriteAsync(signal, value, cancellationToken)
            .ConfigureAwait(false);
    }
}
