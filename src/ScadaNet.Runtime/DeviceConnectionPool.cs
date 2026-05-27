using System.Collections.Concurrent;
using ScadaNet.Protocols;

namespace ScadaNet.Runtime;

public sealed class DeviceConnectionPool : IDeviceConnectionPool, IAsyncDisposable, IDisposable
{
    private readonly IDeviceConnectionFactory _factory;
    private readonly ConcurrentDictionary<string, DeviceConnectionPoolEntry> _entries = new(
        StringComparer.OrdinalIgnoreCase);

    public DeviceConnectionPool(IDeviceConnectionFactory factory)
    {
        _factory = factory;
    }

    public async ValueTask<IDeviceConnectionLease> RentAsync(
        string deviceName,
        CancellationToken cancellationToken = default)
    {
        var entry = _entries.GetOrAdd(
            deviceName,
            static name => new DeviceConnectionPoolEntry(name));

        await entry.Lock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            entry.Connection ??= await _factory
                .ConnectAsync(deviceName, cancellationToken)
                .ConfigureAwait(false);
            entry.ConnectedAt ??= DateTimeOffset.UtcNow;
            entry.LastRentedAt = DateTimeOffset.UtcNow;
            entry.RentCount++;

            return new DeviceConnectionLease(entry);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            entry.FailedRentCount++;
            entry.LastFailureAt = DateTimeOffset.UtcNow;
            entry.LastError = ex.Message;
            entry.Lock.Release();
            throw;
        }
        catch
        {
            entry.Lock.Release();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var entry in _entries.Values)
        {
            await entry.Lock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (entry.Connection is not null)
                {
                    await entry.Connection.DisposeAsync().ConfigureAwait(false);
                    entry.Connection = null;
                }
            }
            finally
            {
                entry.Lock.Release();
                entry.Lock.Dispose();
            }
        }

        _entries.Clear();
    }

    public async ValueTask<bool> CloseAsync(
        string deviceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceName);

        if (!_entries.TryGetValue(deviceName, out var entry))
        {
            return false;
        }

        await entry.Lock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (entry.Connection is null)
            {
                return false;
            }

            await entry.Connection.DisposeAsync().ConfigureAwait(false);
            entry.Connection = null;
            entry.ConnectedAt = null;
            return true;
        }
        finally
        {
            entry.Lock.Release();
        }
    }

    public async ValueTask<int> CloseAllAsync(CancellationToken cancellationToken = default)
    {
        var closed = 0;

        foreach (var entry in _entries.Values.OrderBy(entry => entry.DeviceName, StringComparer.OrdinalIgnoreCase))
        {
            await entry.Lock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (entry.Connection is null)
                {
                    continue;
                }

                await entry.Connection.DisposeAsync().ConfigureAwait(false);
                entry.Connection = null;
                entry.ConnectedAt = null;
                closed++;
            }
            finally
            {
                entry.Lock.Release();
            }
        }

        return closed;
    }

    public IReadOnlyList<DeviceConnectionPoolStatus> GetStatus()
    {
        return _entries.Values
            .Select(ToStatus)
            .OrderBy(status => status.DeviceName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool TryGetStatus(
        string deviceName,
        out DeviceConnectionPoolStatus status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceName);

        if (!_entries.TryGetValue(deviceName, out var entry))
        {
            status = default!;
            return false;
        }

        status = ToStatus(entry);
        return true;
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private sealed class DeviceConnectionPoolEntry
    {
        public DeviceConnectionPoolEntry(string deviceName)
        {
            DeviceName = deviceName;
        }

        public string DeviceName { get; }
        public SemaphoreSlim Lock { get; } = new(1, 1);
        public IDeviceConnection? Connection { get; set; }
        public DateTimeOffset? ConnectedAt { get; set; }
        public DateTimeOffset? LastRentedAt { get; set; }
        public long RentCount { get; set; }
        public long FailedRentCount { get; set; }
        public DateTimeOffset? LastFailureAt { get; set; }
        public string? LastError { get; set; }
    }

    private static DeviceConnectionPoolStatus ToStatus(DeviceConnectionPoolEntry entry)
    {
        return new DeviceConnectionPoolStatus(
            entry.DeviceName,
            entry.Connection is not null,
            entry.Lock.CurrentCount == 0,
            entry.RentCount,
            entry.FailedRentCount,
            entry.ConnectedAt,
            entry.LastRentedAt,
            entry.LastFailureAt,
            entry.LastError);
    }

    private sealed class DeviceConnectionLease : IDeviceConnectionLease
    {
        private readonly DeviceConnectionPoolEntry _entry;
        private bool _disposed;

        public DeviceConnectionLease(DeviceConnectionPoolEntry entry)
        {
            _entry = entry;
            Connection = entry.Connection
                ?? throw new InvalidOperationException(
                    $"Device '{entry.DeviceName}' has no active pooled connection.");
        }

        public IDeviceConnection Connection { get; }

        public ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _entry.Lock.Release();
                _disposed = true;
            }

            return ValueTask.CompletedTask;
        }
    }
}
