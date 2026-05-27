using ScadaNet.Protocols;

namespace ScadaNet.Runtime;

public sealed class DiscoveryService : IDiscoveryService
{
    private readonly IReadOnlyList<IDeviceDriver> _drivers;

    public DiscoveryService(IEnumerable<IDeviceDriver> drivers)
    {
        _drivers = drivers.ToArray();
    }

    public async ValueTask<DeviceDetectionResult> DetectAsync(
        ProbeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_drivers.Count == 0)
        {
            return NoMatch(request, []);
        }

        var results = await Task.WhenAll(_drivers
                .Select(driver => driver.ProbeAsync(request, cancellationToken).AsTask()))
            .ConfigureAwait(false);

        var probes = results.SelectMany(result => result.Probes).ToArray();
        var best = results
            .Where(result => result.Confidence > 0)
            .OrderByDescending(result => result.Confidence)
            .FirstOrDefault();

        if (best is null)
        {
            return NoMatch(request, probes);
        }

        return best with { Probes = probes };
    }

    private static DeviceDetectionResult NoMatch(
        ProbeRequest request,
        IReadOnlyList<ProtocolProbeResult> probes)
    {
        return new DeviceDetectionResult(
            request.Address,
            Port: null,
            probes,
            RecommendedDriver: null,
            Confidence: 0,
            Identity: null,
            Capabilities: []);
    }
}
