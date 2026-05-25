namespace ScadaNet.Protocols;

public sealed record ProtocolProbeResult(
    string Protocol,
    int? Port,
    bool Succeeded,
    string? Evidence,
    string? Error);
