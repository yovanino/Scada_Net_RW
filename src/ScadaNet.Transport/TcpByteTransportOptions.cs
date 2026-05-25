namespace ScadaNet.Transport;

public sealed record TcpByteTransportOptions
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(3);
    public TimeSpan OperationTimeout { get; init; } = TimeSpan.FromSeconds(3);
}
