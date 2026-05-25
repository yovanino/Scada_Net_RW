using ScadaNet.Model;

namespace ScadaNet.Runtime;

public sealed class PlcRuntime : IPlcRuntime
{
    private readonly IDeviceConnectionFactory _connections;

    public PlcRuntime(IDeviceConnectionFactory connections)
    {
        _connections = connections;
    }

    public async ValueTask<SignalValue> ReadAsync(
        SignalRef signal,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connections
            .ConnectAsync(signal.DeviceName, cancellationToken)
            .ConfigureAwait(false);

        return await connection.ReadAsync(signal, cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask<IReadOnlyList<SignalValue>> ReadManyAsync(
        IReadOnlyList<SignalRef> signals,
        CancellationToken cancellationToken = default)
    {
        var values = new List<SignalValue>(signals.Count);

        foreach (var group in signals.GroupBy(signal => signal.DeviceName))
        {
            await using var connection = await _connections
                .ConnectAsync(group.Key, cancellationToken)
                .ConfigureAwait(false);

            var groupValues = await connection
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
        await using var connection = await _connections
            .ConnectAsync(signal.DeviceName, cancellationToken)
            .ConfigureAwait(false);

        await connection.WriteAsync(signal, value, cancellationToken)
            .ConfigureAwait(false);
    }
}
