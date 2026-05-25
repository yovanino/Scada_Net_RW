using ScadaNet.Model;
using ScadaNet.Protocols;
using ScadaNet.Runtime;

namespace ScadaNet.Tests;

public class DiscoveryServiceTests
{
    [Fact]
    public async Task DetectAsync_returns_highest_confidence_result_and_combines_probes()
    {
        var low = new FakeDriver("Low", 0.3);
        var high = new FakeDriver("High", 0.9);
        var service = new DiscoveryService([low, high]);

        var result = await service.DetectAsync(new ProbeRequest(
            "192.168.0.10",
            [44818],
            TimeSpan.FromSeconds(1)));

        Assert.Equal("High", result.RecommendedDriver);
        Assert.Equal(0.9, result.Confidence);
        Assert.Equal(2, result.Probes.Count);
        Assert.Contains(result.Probes, probe => probe.Protocol == "Low");
        Assert.Contains(result.Probes, probe => probe.Protocol == "High");
    }

    private sealed class FakeDriver : IDeviceDriver
    {
        private readonly double _confidence;

        public FakeDriver(string driverName, double confidence)
        {
            DriverName = driverName;
            _confidence = confidence;
        }

        public string DriverName { get; }

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
                request.Ports.FirstOrDefault(),
                [new ProtocolProbeResult(DriverName, request.Ports.FirstOrDefault(), true, "test", null)],
                DriverName,
                _confidence,
                new DeviceIdentity(DriverName, DriverName, null, null, null),
                [DriverName]));
        }
    }
}
