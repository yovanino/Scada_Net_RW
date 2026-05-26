using ScadaNet.Model;
using ScadaNet.Protocols;
using ScadaNet.Runtime;

namespace ScadaNet.Tests;

public class PlcRuntimeTests
{
    [Fact]
    public async Task ReadAsync_connects_by_signal_device_name()
    {
        var connection = new FakeConnection();
        var registry = new DeviceRegistry([
            new DeviceDefinition("line1-plc", "fake", "127.0.0.1")
        ]);
        var pool = new FakeConnectionPool(connection);
        var snapshots = new SignalSnapshotStore();
        var runtime = new PlcRuntime(registry, pool, snapshots, new WriteAuditStore());
        var signal = new SignalRef("line1-plc", "ProductionCounter");

        var value = await runtime.ReadAsync(signal);

        Assert.Equal("line1-plc", pool.LastDeviceName);
        Assert.Equal(123, value.Value);
        Assert.True(pool.LastLeaseWasDisposed);
        Assert.False(connection.WasDisposed);
        Assert.True(snapshots.TryGet(signal, out var snapshot));
        Assert.Equal(123, snapshot.Value);
    }

    [Fact]
    public async Task WriteAsync_uses_connection_for_signal_device()
    {
        var connection = new FakeConnection();
        var registry = new DeviceRegistry([
            new DeviceDefinition("line1-plc", "fake", "127.0.0.1")
            {
                WritesEnabled = true,
                WritableAddresses = { "ResetCommand" }
            }
        ]);
        var pool = new FakeConnectionPool(connection);
        var audit = new WriteAuditStore();
        var runtime = new PlcRuntime(registry, pool, new SignalSnapshotStore(), audit);
        var signal = new SignalRef("line1-plc", "ResetCommand");

        await runtime.WriteAsync(signal, true);

        Assert.Equal("line1-plc", pool.LastDeviceName);
        Assert.Equal(signal, connection.LastWrittenSignal);
        Assert.Equal(true, connection.LastWrittenValue);
        Assert.True(pool.LastLeaseWasDisposed);
        Assert.False(connection.WasDisposed);

        var record = Assert.Single(audit.GetRecent());
        Assert.True(record.Succeeded);
        Assert.Equal(signal, record.Signal);
        Assert.Equal(true, record.Value);
    }

    [Fact]
    public async Task ReadArrayAsync_uses_array_connection_and_updates_snapshot()
    {
        var connection = new FakeArrayConnection();
        var registry = new DeviceRegistry([
            new DeviceDefinition("line1-plc", "fake", "127.0.0.1")
        ]);
        var pool = new FakeConnectionPool(connection);
        var snapshots = new SignalSnapshotStore();
        var runtime = new PlcRuntime(registry, pool, snapshots, new WriteAuditStore());
        var signal = new SignalRef("line1-plc", "Counters");

        var value = await runtime.ReadArrayAsync(signal, 3);

        Assert.Equal("line1-plc", pool.LastDeviceName);
        Assert.Equal(signal, connection.LastArrayReadSignal);
        Assert.Equal((ushort)3, connection.LastArrayReadCount);
        Assert.Equal([1, 2, 3], Assert.IsAssignableFrom<IEnumerable<int>>(value.Value));
        Assert.True(snapshots.TryGet(signal, out var snapshot));
        Assert.Equal([1, 2, 3], Assert.IsAssignableFrom<IEnumerable<int>>(snapshot.Value));
    }

    [Fact]
    public async Task WriteArrayAsync_uses_array_connection_and_audits_write()
    {
        var connection = new FakeArrayConnection();
        var registry = new DeviceRegistry([
            new DeviceDefinition("line1-plc", "fake", "127.0.0.1")
            {
                WritesEnabled = true,
                WritableAddresses = { "Counters" }
            }
        ]);
        var pool = new FakeConnectionPool(connection);
        var audit = new WriteAuditStore();
        var runtime = new PlcRuntime(registry, pool, new SignalSnapshotStore(), audit);
        var signal = new SignalRef("line1-plc", "Counters");
        IReadOnlyList<object?> values = [1, 2, 3];

        await runtime.WriteArrayAsync(signal, values);

        Assert.Equal("line1-plc", pool.LastDeviceName);
        Assert.Equal(signal, connection.LastArrayWrittenSignal);
        Assert.Equal(values, connection.LastArrayWrittenValue);

        var record = Assert.Single(audit.GetRecent());
        Assert.True(record.Succeeded);
        Assert.Equal(signal, record.Signal);
        Assert.Equal(values, record.Value);
    }

    [Fact]
    public async Task ReadArrayAsync_rejects_connections_without_array_support()
    {
        var connection = new FakeConnection();
        var registry = new DeviceRegistry([
            new DeviceDefinition("line1-plc", "fake", "127.0.0.1")
        ]);
        var pool = new FakeConnectionPool(connection);
        var runtime = new PlcRuntime(registry, pool, new SignalSnapshotStore(), new WriteAuditStore());

        var error = await Assert.ThrowsAsync<NotSupportedException>(async () =>
            await runtime.ReadArrayAsync(new SignalRef("line1-plc", "Counters"), 3));

        Assert.Contains("does not support array reads", error.Message);
    }

    [Fact]
    public async Task WriteAsync_rejects_when_device_writes_are_disabled()
    {
        var connection = new FakeConnection();
        var registry = new DeviceRegistry([
            new DeviceDefinition("line1-plc", "fake", "127.0.0.1")
        ]);
        var pool = new FakeConnectionPool(connection);
        var audit = new WriteAuditStore();
        var runtime = new PlcRuntime(registry, pool, new SignalSnapshotStore(), audit);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await runtime.WriteAsync(new SignalRef("line1-plc", "ResetCommand"), true));

        Assert.Contains("Writes are disabled", error.Message);
        Assert.Null(connection.LastWrittenSignal);

        var record = Assert.Single(audit.GetRecent());
        Assert.False(record.Succeeded);
        Assert.Contains("Writes are disabled", record.Error);
    }

    [Fact]
    public async Task WriteAsync_rejects_unlisted_writable_signal()
    {
        var connection = new FakeConnection();
        var registry = new DeviceRegistry([
            new DeviceDefinition("line1-plc", "fake", "127.0.0.1")
            {
                WritesEnabled = true,
                WritableAddresses = { "ResetCommand" }
            }
        ]);
        var pool = new FakeConnectionPool(connection);
        var audit = new WriteAuditStore();
        var runtime = new PlcRuntime(registry, pool, new SignalSnapshotStore(), audit);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await runtime.WriteAsync(new SignalRef("line1-plc", "SpeedSetpoint"), 12.5));

        Assert.Contains("not configured as writable", error.Message);
        Assert.Null(connection.LastWrittenSignal);

        var record = Assert.Single(audit.GetRecent());
        Assert.False(record.Succeeded);
        Assert.Contains("not configured as writable", record.Error);
    }

    private sealed class FakeConnectionPool : IDeviceConnectionPool
    {
        private readonly IDeviceConnection _connection;
        private FakeLease? _lastLease;

        public FakeConnectionPool(IDeviceConnection connection)
        {
            _connection = connection;
        }

        public string? LastDeviceName { get; private set; }
        public bool LastLeaseWasDisposed => _lastLease?.WasDisposed == true;

        public ValueTask<IDeviceConnectionLease> RentAsync(
            string deviceName,
            CancellationToken cancellationToken = default)
        {
            LastDeviceName = deviceName;
            _lastLease = new FakeLease(_connection);
            return ValueTask.FromResult<IDeviceConnectionLease>(_lastLease);
        }
    }

    private sealed class FakeLease : IDeviceConnectionLease
    {
        public FakeLease(IDeviceConnection connection)
        {
            Connection = connection;
        }

        public IDeviceConnection Connection { get; }
        public bool WasDisposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            WasDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeConnection : IDeviceConnection
    {
        public DeviceIdentity Identity { get; } = new("Test", "Fake", null, null, null);
        public DeviceCapabilities Capabilities => DeviceCapabilities.Read | DeviceCapabilities.Write;
        public bool WasDisposed { get; private set; }
        public SignalRef? LastWrittenSignal { get; private set; }
        public object? LastWrittenValue { get; private set; }

        public ValueTask DisposeAsync()
        {
            WasDisposed = true;
            return ValueTask.CompletedTask;
        }

        public ValueTask<SignalValue> ReadAsync(
            SignalRef signal,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new SignalValue(
                signal,
                123,
                SignalQuality.Good,
                DateTimeOffset.UtcNow));
        }

        public ValueTask<IReadOnlyList<SignalValue>> ReadManyAsync(
            IReadOnlyList<SignalRef> signals,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<SignalValue> values = signals
                .Select(signal => new SignalValue(
                    signal,
                    123,
                    SignalQuality.Good,
                    DateTimeOffset.UtcNow))
                .ToArray();

            return ValueTask.FromResult(values);
        }

        public ValueTask WriteAsync(
            SignalRef signal,
            object? value,
            CancellationToken cancellationToken = default)
        {
            LastWrittenSignal = signal;
            LastWrittenValue = value;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeArrayConnection : IDeviceConnection, IArrayDeviceConnection
    {
        public DeviceIdentity Identity { get; } = new("Test", "Fake", null, null, null);
        public DeviceCapabilities Capabilities =>
            DeviceCapabilities.Read |
            DeviceCapabilities.Write |
            DeviceCapabilities.ReadArray |
            DeviceCapabilities.WriteArray;

        public SignalRef? LastArrayReadSignal { get; private set; }
        public ushort? LastArrayReadCount { get; private set; }
        public SignalRef? LastArrayWrittenSignal { get; private set; }
        public IReadOnlyList<object?>? LastArrayWrittenValue { get; private set; }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask<SignalValue> ReadAsync(
            SignalRef signal,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask<IReadOnlyList<SignalValue>> ReadManyAsync(
            IReadOnlyList<SignalRef> signals,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask WriteAsync(
            SignalRef signal,
            object? value,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask<SignalValue> ReadArrayAsync(
            SignalRef signal,
            ushort elementCount,
            CancellationToken cancellationToken = default)
        {
            LastArrayReadSignal = signal;
            LastArrayReadCount = elementCount;

            return ValueTask.FromResult(new SignalValue(
                signal,
                new[] { 1, 2, 3 },
                SignalQuality.Good,
                DateTimeOffset.UtcNow));
        }

        public ValueTask WriteArrayAsync(
            SignalRef signal,
            IReadOnlyList<object?> values,
            CancellationToken cancellationToken = default)
        {
            LastArrayWrittenSignal = signal;
            LastArrayWrittenValue = values;
            return ValueTask.CompletedTask;
        }
    }
}
