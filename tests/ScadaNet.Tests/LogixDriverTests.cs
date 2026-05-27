using ScadaNet.Logix;
using ScadaNet.Protocols;

namespace ScadaNet.Tests;

public class LogixDriverTests
{
    [Fact]
    public void Metadata_describes_default_discovery_ports_and_capabilities()
    {
        var metadata = Assert.IsAssignableFrom<IDeviceDriverMetadata>(
            new LogixDriver());

        Assert.Equal([ScadaNet.EtherNetIp.EtherNetIpDefaults.ExplicitMessagingPort], metadata.DefaultPorts);
        Assert.Contains("LogixTags", metadata.Capabilities);
        Assert.Contains("ReadMany", metadata.Capabilities);
    }

    [Fact]
    public async Task ConnectAsync_returns_logix_device_connection()
    {
        var driver = new LogixDriver();

        await using var connection = await driver.ConnectAsync(new DeviceConnectionOptions
        {
            DeviceName = "line1-plc",
            Address = "192.168.0.10",
            Port = 44818,
            Path = "1,0",
            Timeout = TimeSpan.FromSeconds(2)
        });

        Assert.IsType<LogixDeviceConnection>(connection);
        Assert.True(connection.Capabilities.HasFlag(ScadaNet.Model.DeviceCapabilities.Read));
        Assert.True(connection.Capabilities.HasFlag(ScadaNet.Model.DeviceCapabilities.Write));
    }
}
