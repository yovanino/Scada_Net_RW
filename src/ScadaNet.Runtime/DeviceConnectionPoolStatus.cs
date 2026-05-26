namespace ScadaNet.Runtime;

public sealed record DeviceConnectionPoolStatus(
    string DeviceName,
    bool HasConnection,
    bool IsInUse,
    long RentCount,
    long FailedRentCount,
    DateTimeOffset? ConnectedAt,
    DateTimeOffset? LastRentedAt,
    DateTimeOffset? LastFailureAt,
    string? LastError);
