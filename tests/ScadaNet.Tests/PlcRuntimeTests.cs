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
        var factory = new FakeConnectionFactory(connection);
        var runtime = new PlcRuntime(factory);
        var signal = new SignalRef("line1-plc", "ProductionCounter");

        var value = await runtime.ReadAsync(signal);

        Assert.Equal("line1-plc", factory.LastDeviceName);
        Assert.Equal(123, value.Value);
        Assert.True(connection.WasDisposed);
    }

    [Fact]
    public async Task WriteAsync_uses_connection_for_signal_device()
    {
        var connection = new FakeConnection();
        var factory = new FakeConnectionFactory(connection);
        var runtime = new PlcRuntime(factory);
        var signal = new SignalRef("line1-plc", "ResetCommand");

        await runtime.WriteAsync(signal, true);

        Assert.Equal("line1-plc", factory.LastDeviceName);
        Assert.Equal(signal, connection.LastWrittenSignal);
        Assert.Equal(true, connection.LastWrittenValue);
        Assert.True(connection.WasDisposed);
    }

    private sealed class FakeConnectionFactory : IDeviceConnectionFactory
    {
        private readonly IDeviceConnection _connection;

        public FakeConnectionFactory(IDeviceConnection connection)
        {
            _connection = connection;
        }

        public string? LastDeviceName { get; private set; }

        public ValueTask<IDeviceConnection> ConnectAsync(
            string deviceName,
            CancellationToken cancellationToken = default)
        {
            LastDeviceName = deviceName;
            return ValueTask.FromResult(_connection);
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
