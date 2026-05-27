namespace ScadaNet.Runtime;

public sealed record DeviceConnectionPoolStatus(
    string DeviceName,
    bool HasConnection,
    bool IsInUse,
    long RentCount,
    long FailedRentCount,
    long CloseCount,
    DateTimeOffset? ConnectedAt,
    DateTimeOffset? LastRentedAt,
    DateTimeOffset? LastClosedAt,
    DateTimeOffset? LastFailureAt,
    string? LastError);
