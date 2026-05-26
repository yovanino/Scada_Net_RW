namespace ScadaNet.Runtime;

public sealed record DeviceConnectionPoolStatus(
    string DeviceName,
    bool HasConnection,
    bool IsInUse,
    long RentCount,
    DateTimeOffset? ConnectedAt,
    DateTimeOffset? LastRentedAt);
