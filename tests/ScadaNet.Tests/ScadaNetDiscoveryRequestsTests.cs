using ScadaNet.AspNetCore;
using ScadaNet.Model;
using ScadaNet.Protocols;

namespace ScadaNet.Tests;

public class ScadaNetDiscoveryRequestsTests
{
    [Fact]
    public void ToProbeRequest_creates_probe_request()
    {
        var request = new ScadaNetDiscoveryRequest(
            " 192.168.0.10 ",
            [44818, 502],
            1500);

        var probe = request.ToProbeRequest();

        Assert.Equal("192.168.0.10", probe.Address);
        Assert.Equal([44818, 502], probe.Ports);
        Assert.Equal(TimeSpan.FromMilliseconds(1500), probe.Timeout);
    }

    [Fact]
    public void ToProbeRequest_uses_default_ports_and_timeout()
    {
        var request = new ScadaNetDiscoveryRequest("192.168.0.10");

        var probe = request.ToProbeRequest();

        Assert.Empty(probe.Ports);
        Assert.Equal(TimeSpan.FromSeconds(3), probe.Timeout);
    }

    [Fact]
    public void ToProbeRequest_removes_duplicate_ports()
    {
        var request = new ScadaNetDiscoveryRequest(
            "192.168.0.10",
            [44818, 502, 44818, 502, 2222]);

        var probe = request.ToProbeRequest();

        Assert.Equal([44818, 502, 2222], probe.Ports);
    }

    [Fact]
    public void ToProbeRequest_rejects_empty_address()
    {
        var request = new ScadaNetDiscoveryRequest(" ");

        var error = Assert.Throws<ArgumentException>(request.ToProbeRequest);

        Assert.Contains("Discovery address cannot be empty", error.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65536)]
    public void ToProbeRequest_rejects_invalid_ports(int port)
    {
        var request = new ScadaNetDiscoveryRequest(
            "192.168.0.10",
            [port]);

        var error = Assert.Throws<ArgumentOutOfRangeException>(request.ToProbeRequest);

        Assert.Contains("Discovery port must be between 1 and 65535", error.Message);
    }

    [Fact]
    public void ToProbeRequest_rejects_non_positive_timeout()
    {
        var request = new ScadaNetDiscoveryRequest(
            "192.168.0.10",
            TimeoutMilliseconds: 0);

        var error = Assert.Throws<ArgumentOutOfRangeException>(request.ToProbeRequest);

        Assert.Contains("Discovery timeout must be greater than zero", error.Message);
    }

    [Fact]
    public void DriverInfo_from_driver_maps_protocol_metadata_for_apis()
    {
        var info = ScadaNetDiscoveryDriverInfo.FromDriver(new TestMetadataDriver());

        Assert.Equal("TestDriver", info.Name);
        Assert.Equal("EtherNet/IP", info.ProtocolFamily);
        Assert.Equal("TCP", info.Transport);
        Assert.Equal(["Explicit"], info.MessagingModes);
        Assert.Equal(
            [new ScadaNetDiscoveryEndpointInfo(44818, "TCP", "Explicit")],
            info.DefaultEndpoints);
        Assert.Equal([44818], info.DefaultPorts);
        Assert.Equal(["Read"], info.Capabilities);
    }

    [Fact]
    public void DriverInfo_from_driver_handles_drivers_without_metadata()
    {
        var info = ScadaNetDiscoveryDriverInfo.FromDriver(new TestDriver());

        Assert.Equal("TestDriver", info.Name);
        Assert.Null(info.ProtocolFamily);
        Assert.Null(info.Transport);
        Assert.Empty(info.MessagingModes);
        Assert.Empty(info.DefaultEndpoints);
        Assert.Empty(info.DefaultPorts);
        Assert.Empty(info.Capabilities);
    }

    private class TestDriver : IDeviceDriver
    {
        public string DriverName => "TestDriver";

        public ValueTask<IDeviceConnection> ConnectAsync(
            DeviceConnectionOptions options,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask<DeviceDetectionResult> ProbeAsync(
            ProbeRequest request,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new DeviceDetectionResult(
                request.Address,
                Port: null,
                Probes: [],
                RecommendedDriver: null,
                Confidence: 0,
                Identity: null,
                Capabilities: []));
        }
    }

    private sealed class TestMetadataDriver : TestDriver, IDeviceDriverMetadata
    {
        public string ProtocolFamily => KnownProtocolFamilies.EtherNetIp;

        public string Transport => KnownTransportProtocols.Tcp;

        public IReadOnlyList<string> MessagingModes { get; } = [KnownMessagingModes.Explicit];

        public IReadOnlyList<ProtocolEndpointMetadata> DefaultEndpoints { get; } =
        [
            new(44818, KnownTransportProtocols.Tcp, KnownMessagingModes.Explicit)
        ];

        public IReadOnlyList<int> DefaultPorts { get; } = [44818];

        public IReadOnlyList<string> Capabilities { get; } = [KnownDiscoveryCapabilities.Read];
    }
}
