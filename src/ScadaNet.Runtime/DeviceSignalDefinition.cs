namespace ScadaNet.Runtime;

public sealed record DeviceSignalDefinition
{
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string? DataType { get; init; }
    public string? Unit { get; init; }
    public string? Description { get; init; }
    public bool Writable { get; init; }
}
