using ScadaNet.AspNetCore;

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
}
