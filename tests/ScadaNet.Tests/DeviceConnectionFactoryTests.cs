using ScadaNet.Model;
using ScadaNet.Protocols;
using ScadaNet.Runtime;

namespace ScadaNet.Tests;

public class DeviceConnectionFactoryTests
{
    [Fact]
    public async Task ConnectAsync_resolves_device_and_driver_case_insensitively()
    {
        var driver = new FakeDriver("EtherNet/IP");
        var registry = new DeviceRegistry([
            new DeviceDefinition("line1-plc", "ethernetip", "192.168.0.10")
        ]);
        var factory = new DeviceConnectionFactory(registry, [driver]);

        await using var connection = await factory.ConnectAsync("LINE1-PLC");

        Assert.NotNull(connection);
        Assert.Equal("line1-plc", driver.LastOptions?.DeviceName);
        Assert.Equal("192.168.0.10", driver.LastOptions?.Address);
    }

    [Fact]
    public async Task ConnectAsync_throws_when_driver_is_not_registered()
    {
        var registry = new DeviceRegistry([
            new DeviceDefinition("line1-plc", "logix", "192.168.0.10")
        ]);
        var factory = new DeviceConnectionFactory(registry, []);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await factory.ConnectAsync("line1-plc"));

        Assert.Contains("Driver 'logix' is not registered", error.Message);
    }

    private sealed class FakeDriver : IDeviceDriver
    {
        public FakeDriver(string driverName)
        {
            DriverName = driverName;
        }

        public string DriverName { get; }
        public DeviceConnectionOptions? LastOptions { get; private set; }

        public ValueTask<IDeviceConnection> ConnectAsync(
            DeviceConnectionOptions options,
            CancellationToken cancellationToken = default)
        {
            LastOptions = options;
            return ValueTask.FromResult<IDeviceConnection>(new FakeConnection());
        }

        public ValueTask<DeviceDetectionResult> ProbeAsync(
            ProbeRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeConnection : IDeviceConnection
    {
        public DeviceIdentity Identity { get; } = new("Test", "Fake", null, null, null);
        public DeviceCapabilities Capabilities => DeviceCapabilities.Read;

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
    }
}
