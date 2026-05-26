using System.Collections.Concurrent;

namespace ScadaNet.Runtime;

public sealed class PollingStatusStore : IPollingStatusStore
{
    private readonly ConcurrentDictionary<string, PollingGroupStatus> _statuses = new(
        StringComparer.OrdinalIgnoreCase);

    public void MarkSuccess(
        SignalPollingGroupDefinition group,
        TimeSpan duration,
        int signalCount)
    {
        _statuses[GetGroupKey(group)] = new PollingGroupStatus(
            GetGroupKey(group),
            group.DeviceName,
            Healthy: true,
            DateTimeOffset.UtcNow,
            duration,
            signalCount,
            Error: null);
    }

    public void MarkSkipped(
        SignalPollingGroupDefinition group,
        string reason)
    {
        _statuses[GetGroupKey(group)] = new PollingGroupStatus(
            GetGroupKey(group),
            group.DeviceName,
            Healthy: true,
            DateTimeOffset.UtcNow,
            Duration: TimeSpan.Zero,
            SignalCount: 0,
            Error: reason);
    }

    public void MarkFailure(
        SignalPollingGroupDefinition group,
        TimeSpan duration,
        Exception exception)
    {
        _statuses[GetGroupKey(group)] = new PollingGroupStatus(
            GetGroupKey(group),
            group.DeviceName,
            Healthy: false,
            DateTimeOffset.UtcNow,
            duration,
            SignalCount: 0,
            Error: exception.Message);
    }

    public IReadOnlyList<PollingGroupStatus> GetAll()
    {
        return _statuses.Values
            .OrderBy(status => status.GroupName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool TryGet(string groupName, out PollingGroupStatus status)
    {
        return _statuses.TryGetValue(groupName, out status!);
    }

    private static string GetGroupKey(SignalPollingGroupDefinition group)
    {
        return string.IsNullOrWhiteSpace(group.Name)
            ? group.DeviceName
            : group.Name;
    }
}
