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

        Assert.Equal("EtherNet/IP", metadata.ProtocolFamily);
        Assert.Equal("TCP", metadata.Transport);
        Assert.Equal(["Explicit"], metadata.MessagingModes);
        Assert.Equal(
            [new ProtocolEndpointMetadata(ScadaNet.EtherNetIp.EtherNetIpDefaults.ExplicitMessagingPort, "TCP", "Explicit")],
            metadata.DefaultEndpoints);
        Assert.Equal([ScadaNet.EtherNetIp.EtherNetIpDefaults.ExplicitMessagingPort], metadata.DefaultPorts);
        Assert.Contains("LogixTags", metadata.Capabilities);
        Assert.Contains("ReadMany", metadata.Capabilities);
    }

    [Fact]
    public async Task ProbeAsync_returns_explanatory_result_with_duration()
    {
        var driver = new LogixDriver();

        var result = await driver.ProbeAsync(new ProbeRequest(
            "192.168.0.10",
            [44818],
            TimeSpan.FromSeconds(1)));

        Assert.Equal(0, result.Confidence);
        Assert.Null(result.RecommendedDriver);
        Assert.Equal("TCP", result.Transport);
        Assert.Equal("Explicit", result.MessagingMode);
        Assert.NotNull(result.Duration);
        Assert.Contains(result.Probes, probe =>
            probe.Protocol == "Logix" &&
            !probe.Succeeded &&
            probe.Transport == "TCP" &&
            probe.MessagingMode == "Explicit" &&
            probe.Duration.HasValue);
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
