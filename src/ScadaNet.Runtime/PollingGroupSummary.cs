namespace ScadaNet.Runtime;

public sealed record PollingGroupSummary(
    string GroupName,
    string DeviceName,
    bool Enabled,
    TimeSpan Interval,
    int ConfiguredSignalCount,
    IReadOnlyList<string> Addresses,
    IReadOnlyList<string> SignalNames,
    bool HasStatus,
    bool? Healthy,
    DateTimeOffset? LastRun,
    TimeSpan? LastRunAge,
    TimeSpan? Duration,
    TimeSpan StaleAfter,
    bool IsStale,
    string? Error);
