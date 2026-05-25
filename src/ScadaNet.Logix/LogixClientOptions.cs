namespace ScadaNet.Logix;

public sealed record LogixClientOptions
{
    public required string Address { get; init; }
    public string Path { get; init; } = "1,0";
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(3);
}
