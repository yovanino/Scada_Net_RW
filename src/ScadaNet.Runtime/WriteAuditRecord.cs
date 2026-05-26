using ScadaNet.Model;

namespace ScadaNet.Runtime;

public sealed record WriteAuditRecord(
    long Sequence,
    DateTimeOffset Timestamp,
    SignalRef Signal,
    object? Value,
    bool Succeeded,
    string? Error);
