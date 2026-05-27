using ScadaNet.Model;

namespace ScadaNet.Protocols;

public sealed record DeviceDetectionResult(
    string Address,
    int? Port,
    IReadOnlyList<ProtocolProbeResult> Probes,
    string? RecommendedDriver,
    double Confidence,
    DeviceIdentity? Identity,
    IReadOnlyList<string> Capabilities,
    TimeSpan? Duration = null);
