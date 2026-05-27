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
                .Select(driver => ProbeDriverAsync(driver, request, cancellationToken)))
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

    private static async Task<DeviceDetectionResult> ProbeDriverAsync(
        IDeviceDriver driver,
        ProbeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await driver.ProbeAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var port = request.Ports.FirstOrDefault();

            return new DeviceDetectionResult(
                request.Address,
                Port: null,
                Probes:
                [
                    new ProtocolProbeResult(
                        driver.DriverName,
                        port == 0 ? null : port,
                        Succeeded: false,
                        Evidence: null,
                        Error: ex.Message)
                ],
                RecommendedDriver: null,
                Confidence: 0,
                Identity: null,
                Capabilities: []);
        }
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
