namespace ScadaNet.Runtime;

public sealed record PollingGroupStatus(
    string GroupName,
    string DeviceName,
    bool Healthy,
    DateTimeOffset LastRun,
    TimeSpan Duration,
    int SignalCount,
    string? Error);
