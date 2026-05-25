namespace ScadaNet.EtherNetIp;

public sealed record EtherNetIpClientOptions
{
    public required string Host { get; init; }
    public int Port { get; init; } = EtherNetIpDefaults.ExplicitMessagingPort;
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(3);
    public TimeSpan OperationTimeout { get; init; } = TimeSpan.FromSeconds(3);
}
