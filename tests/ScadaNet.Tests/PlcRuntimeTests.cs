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
        var pool = new FakeConnectionPool(connection);
        var runtime = new PlcRuntime(pool);
        var signal = new SignalRef("line1-plc", "ProductionCounter");

        var value = await runtime.ReadAsync(signal);

        Assert.Equal("line1-plc", pool.LastDeviceName);
        Assert.Equal(123, value.Value);
        Assert.True(pool.LastLeaseWasDisposed);
        Assert.False(connection.WasDisposed);
    }

    [Fact]
    public async Task WriteAsync_uses_connection_for_signal_device()
    {
        var connection = new FakeConnection();
        var pool = new FakeConnectionPool(connection);
        var runtime = new PlcRuntime(pool);
        var signal = new SignalRef("line1-plc", "ResetCommand");

        await runtime.WriteAsync(signal, true);

        Assert.Equal("line1-plc", pool.LastDeviceName);
        Assert.Equal(signal, connection.LastWrittenSignal);
        Assert.Equal(true, connection.LastWrittenValue);
        Assert.True(pool.LastLeaseWasDisposed);
        Assert.False(connection.WasDisposed);
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
}
