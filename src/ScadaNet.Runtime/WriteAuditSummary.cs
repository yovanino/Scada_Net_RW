namespace ScadaNet.Runtime;

public sealed record WriteAuditSummary(
    string? DeviceName,
    int WriteCount,
    int SucceededWriteCount,
    int FailedWriteCount,
    DateTimeOffset? LastWriteTimestamp,
    DateTimeOffset? LastFailedWriteTimestamp,
    string? LastError);
