namespace ScadaNet.Runtime;

public sealed record DeviceSignalDefinition
{
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string? DataType { get; init; }
    public string? Unit { get; init; }
    public string? Description { get; init; }
    public string? Category { get; init; }
    public int? DisplayOrder { get; init; }
    public double? MinValue { get; init; }
    public double? MaxValue { get; init; }
    public bool IsArray { get; init; }
    public ushort? ElementCount { get; init; }
    public bool Writable { get; init; }
}
