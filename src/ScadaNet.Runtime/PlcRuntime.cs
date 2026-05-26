using ScadaNet.Model;
using ScadaNet.Protocols;

namespace ScadaNet.Runtime;

public sealed class PlcRuntime : IPlcRuntime
{
    private readonly IDeviceRegistry _registry;
    private readonly IDeviceConnectionPool _connections;
    private readonly ISignalSnapshotStore _snapshots;
    private readonly IWriteAuditStore _writeAudit;

    public PlcRuntime(
        IDeviceRegistry registry,
        IDeviceConnectionPool connections,
        ISignalSnapshotStore snapshots,
        IWriteAuditStore writeAudit)
    {
        _registry = registry;
        _connections = connections;
        _snapshots = snapshots;
        _writeAudit = writeAudit;
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

    public async ValueTask<SignalValue> ReadArrayAsync(
        SignalRef signal,
        ushort elementCount,
        CancellationToken cancellationToken = default)
    {
        if (elementCount == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(elementCount),
                elementCount,
                "Array read element count must be greater than zero.");
        }

        await using var lease = await _connections
            .RentAsync(signal.DeviceName, cancellationToken)
            .ConfigureAwait(false);

        if (lease.Connection is not IArrayDeviceConnection arrayConnection)
        {
            throw new NotSupportedException(
                $"Device '{signal.DeviceName}' does not support array reads.");
        }

        var value = await arrayConnection
            .ReadArrayAsync(signal, elementCount, cancellationToken)
            .ConfigureAwait(false);

        _snapshots.Update(value);
        return value;
    }

    public async ValueTask WriteAsync(
        SignalRef signal,
        object? value,
        CancellationToken cancellationToken = default)
    {
        await WriteAsync(signal, value, dataType: null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask WriteAsync(
        SignalRef signal,
        object? value,
        string? dataType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureWriteAllowed(signal);

            await using var lease = await _connections
                .RentAsync(signal.DeviceName, cancellationToken)
                .ConfigureAwait(false);

            if (dataType is null)
            {
                await lease.Connection.WriteAsync(signal, value, cancellationToken)
                    .ConfigureAwait(false);
            }
            else if (lease.Connection is ITypedDeviceConnection typedConnection)
            {
                await typedConnection.WriteAsync(signal, value, dataType, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                throw new NotSupportedException(
                    $"Device '{signal.DeviceName}' does not support typed writes.");
            }

            AuditWrite(signal, value, succeeded: true, error: null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AuditWrite(signal, value, succeeded: false, ex.Message);
            throw;
        }
    }

    public async ValueTask WriteArrayAsync(
        SignalRef signal,
        IReadOnlyList<object?> values,
        CancellationToken cancellationToken = default)
    {
        await WriteArrayAsync(signal, values, dataType: null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask WriteArrayAsync(
        SignalRef signal,
        IReadOnlyList<object?> values,
        string? dataType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureWriteAllowed(signal);

            await using var lease = await _connections
                .RentAsync(signal.DeviceName, cancellationToken)
                .ConfigureAwait(false);

            if (lease.Connection is not IArrayDeviceConnection arrayConnection)
            {
                throw new NotSupportedException(
                    $"Device '{signal.DeviceName}' does not support array writes.");
            }

            await arrayConnection.WriteArrayAsync(signal, values, dataType, cancellationToken)
                .ConfigureAwait(false);

            AuditWrite(signal, values, succeeded: true, error: null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AuditWrite(signal, values, succeeded: false, ex.Message);
            throw;
        }
    }

    private void EnsureWriteAllowed(SignalRef signal)
    {
        var device = _registry.GetRequired(signal.DeviceName);

        if (!device.WritesEnabled)
        {
            throw new InvalidOperationException(
                $"Writes are disabled for device '{device.Name}'.");
        }

        if (!device.CanWrite(signal.Address))
        {
            throw new InvalidOperationException(
                $"Signal '{signal.Address}' is not configured as writable for device '{device.Name}'.");
        }
    }

    private void AuditWrite(
        SignalRef signal,
        object? value,
        bool succeeded,
        string? error)
    {
        _writeAudit.Add(new WriteAuditRecord(
            Sequence: 0,
            DateTimeOffset.UtcNow,
            signal,
            value,
            succeeded,
            error));
    }
}
