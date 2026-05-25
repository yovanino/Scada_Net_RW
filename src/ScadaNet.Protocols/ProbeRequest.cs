namespace ScadaNet.Protocols;

public sealed record ProbeRequest(
    string Address,
    IReadOnlyCollection<int> Ports,
    TimeSpan Timeout);
