using ScadaNet.Model;

namespace ScadaNet.Runtime;

public sealed record DeviceSignalSnapshot(
    string Name,
    string Address,
    string? DataType,
    string? Unit,
    string? Description,
    string? Category,
    int? DisplayOrder,
    bool IsArray,
    ushort? ElementCount,
    bool Writable,
    bool HasValue,
    SignalValue? Value);
