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

    [Fact]
    public async Task CloseAsync_disposes_cached_connection()
    {
        var factory = new FakeConnectionFactory();
        await using var pool = new DeviceConnectionPool(factory);

        await using (await pool.RentAsync("line1-plc"))
        {
        }

        var closed = await pool.CloseAsync("line1-plc");

        Assert.True(closed);
        Assert.True(factory.Connections[0].WasDisposed);

        var status = Assert.Single(pool.GetStatus());
        Assert.False(status.HasConnection);
        Assert.Null(status.ConnectedAt);
        Assert.Equal(1, status.RentCount);
    }

    [Fact]
    public async Task CloseAsync_allows_reconnect_on_next_rent()
    {
        var factory = new FakeConnectionFactory();
        await using var pool = new DeviceConnectionPool(factory);

        await using (await pool.RentAsync("line1-plc"))
        {
        }

        await pool.CloseAsync("line1-plc");

        await using (await pool.RentAsync("line1-plc"))
        {
        }

        Assert.Equal(2, factory.ConnectCount);
        Assert.True(factory.Connections[0].WasDisposed);
        Assert.False(factory.Connections[1].WasDisposed);
    }

    [Fact]
    public async Task CloseAsync_returns_false_for_missing_connection()
    {
        var factory = new FakeConnectionFactory();
        await using var pool = new DeviceConnectionPool(factory);

        var closed = await pool.CloseAsync("line1-plc");

        Assert.False(closed);
    }

    [Fact]
    public async Task GetStatus_reports_cached_connections()
    {
        var factory = new FakeConnectionFactory();
        await using var pool = new DeviceConnectionPool(factory);

        await using (await pool.RentAsync("line2-plc"))
        {
        }

        await using (await pool.RentAsync("line1-plc"))
        {
        }

        var statuses = pool.GetStatus();

        Assert.Equal(["line1-plc", "line2-plc"], statuses.Select(status => status.DeviceName));
        Assert.All(statuses, status =>
        {
            Assert.True(status.HasConnection);
            Assert.False(status.IsInUse);
            Assert.Equal(1, status.RentCount);
            Assert.Equal(0, status.FailedRentCount);
            Assert.NotNull(status.ConnectedAt);
            Assert.NotNull(status.LastRentedAt);
            Assert.Null(status.LastFailureAt);
            Assert.Null(status.LastError);
        });
    }

    [Fact]
    public async Task GetStatus_reports_in_use_connection()
    {
        var factory = new FakeConnectionFactory();
        await using var pool = new DeviceConnectionPool(factory);

        await using var lease = await pool.RentAsync("line1-plc");

        var status = Assert.Single(pool.GetStatus());
        Assert.True(status.IsInUse);
    }

    [Fact]
    public async Task GetStatus_reports_connection_failures()
    {
        var factory = new FakeConnectionFactory
        {
            ConnectException = new InvalidOperationException("PLC unavailable")
        };
        await using var pool = new DeviceConnectionPool(factory);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await pool.RentAsync("line1-plc"));

        var status = Assert.Single(pool.GetStatus());
        Assert.Equal("line1-plc", status.DeviceName);
        Assert.False(status.HasConnection);
        Assert.False(status.IsInUse);
        Assert.Equal(0, status.RentCount);
        Assert.Equal(1, status.FailedRentCount);
        Assert.Null(status.ConnectedAt);
        Assert.Null(status.LastRentedAt);
        Assert.NotNull(status.LastFailureAt);
        Assert.Equal("PLC unavailable", status.LastError);
    }

    private sealed class FakeConnectionFactory : IDeviceConnectionFactory
    {
        public int ConnectCount { get; private set; }
        public List<FakeConnection> Connections { get; } = [];
        public Exception? ConnectException { get; init; }

        public ValueTask<IDeviceConnection> ConnectAsync(
            string deviceName,
            CancellationToken cancellationToken = default)
        {
            ConnectCount++;
            if (ConnectException is not null)
            {
                throw ConnectException;
            }

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
