namespace ScadaNet.Protocols;

public sealed record ProtocolEndpointMetadata(
    int Port,
    string Transport,
    string? MessagingMode = null);
