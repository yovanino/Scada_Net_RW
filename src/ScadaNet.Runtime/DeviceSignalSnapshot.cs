using ScadaNet.Model;

namespace ScadaNet.Runtime;

public sealed record DeviceSignalSnapshot(
    string Name,
    string Address,
    string? DataType,
    string? Unit,
    string? Description,
    bool Writable,
    bool HasValue,
    SignalValue? Value);
