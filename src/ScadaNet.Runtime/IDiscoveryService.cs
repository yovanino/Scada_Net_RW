using ScadaNet.Protocols;

namespace ScadaNet.Runtime;

public interface IDiscoveryService
{
    ValueTask<DeviceDetectionResult> DetectAsync(
        ProbeRequest request,
        CancellationToken cancellationToken = default);
}
