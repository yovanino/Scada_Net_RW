using ScadaNet.Model;

namespace ScadaNet.Runtime;

public sealed class PlcRuntime : IPlcRuntime
{
    private readonly IDeviceConnectionPool _connections;
    private readonly ISignalSnapshotStore _snapshots;

    public PlcRuntime(
        IDeviceConnectionPool connections,
        ISignalSnapshotStore snapshots)
    {
        _connections = connections;
        _snapshots = snapshots;
    }

    public async ValueTask<SignalValue> ReadAsync(
        SignalRef signal,
        CancellationToken cancellationToken = default)
    {
        await using var lease = await _connections
            .RentAsync(signal.DeviceName, cancellationToken)
            .ConfigureAwait(false);

        var value = await lease.Connection.ReadAsync(signal, cancellationToken)
            .ConfigureAwait(false);

        _snapshots.Update(value);
        return value;
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

            _snapshots.UpdateMany(groupValues);
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
