using ScadaNet.Model;
using ScadaNet.Protocols;
using ScadaNet.Runtime;

namespace ScadaNet.Tests;

public class DeviceConnectionPoolTests
{
    [Fact]
    public async Task RentAsync_reuses_connection_for_same_device()
    {
        var factory = new FakeConnectionFactory();
        await using var pool = new DeviceConnectionPool(factory);

        await using (var first = await pool.RentAsync("line1-plc"))
        {
            Assert.NotNull(first.Connection);
        }

        await using (var second = await pool.RentAsync("line1-plc"))
        {
            Assert.NotNull(second.Connection);
        }

        Assert.Equal(1, factory.ConnectCount);
    }

    [Fact]
    public async Task RentAsync_creates_separate_connections_for_different_devices()
    {
        var factory = new FakeConnectionFactory();
        await using var pool = new DeviceConnectionPool(factory);

        await using (await pool.RentAsync("line1-plc"))
        {
        }

        await using (await pool.RentAsync("line2-plc"))
        {
        }

        Assert.Equal(2, factory.ConnectCount);
    }

    [Fact]
    public async Task DisposeAsync_disposes_cached_connections()
    {
        var factory = new FakeConnectionFactory();
        var pool = new DeviceConnectionPool(factory);

        await using (await pool.RentAsync("line1-plc"))
        {
        }

        await pool.DisposeAsync();

        var connection = Assert.Single(factory.Connections);
        Assert.True(connection.WasDisposed);
    }

    private sealed class FakeConnectionFactory : IDeviceConnectionFactory
    {
        public int ConnectCount { get; private set; }
        public List<FakeConnection> Connections { get; } = [];

        public ValueTask<IDeviceConnection> ConnectAsync(
            string deviceName,
            CancellationToken cancellationToken = default)
        {
            ConnectCount++;
            var connection = new FakeConnection(deviceName);
            Connections.Add(connection);
            return ValueTask.FromResult<IDeviceConnection>(connection);
        }
    }

    private sealed class FakeConnection : IDeviceConnection
    {
        public FakeConnection(string deviceName)
        {
            Identity = new DeviceIdentity("Test", deviceName, null, null, null);
        }

        public DeviceIdentity Identity { get; }
        public DeviceCapabilities Capabilities => DeviceCapabilities.Read;
        public bool WasDisposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            WasDisposed = true;
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
    }
}
