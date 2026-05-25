namespace ScadaNet.Protocols;

public sealed record DeviceConnectionOptions
{
    public required string DeviceName { get; init; }
    public required string Address { get; init; }
    public int? Port { get; init; }
    public string? Path { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(3);
}
