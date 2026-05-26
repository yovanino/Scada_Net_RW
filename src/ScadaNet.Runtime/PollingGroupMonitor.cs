namespace ScadaNet.Runtime;

public sealed class PollingGroupMonitor : IPollingGroupMonitor
{
    private const int StaleIntervalMultiplier = 3;

    private readonly IPollingGroupRegistry _groups;
    private readonly IPollingStatusStore _statuses;
    private readonly TimeProvider _timeProvider;

    public PollingGroupMonitor(
        IPollingGroupRegistry groups,
        IPollingStatusStore statuses)
        : this(groups, statuses, TimeProvider.System)
    {
    }

    public PollingGroupMonitor(
        IPollingGroupRegistry groups,
        IPollingStatusStore statuses,
        TimeProvider timeProvider)
    {
        _groups = groups;
        _statuses = statuses;
        _timeProvider = timeProvider;
    }

    public IReadOnlyList<PollingGroupSummary> GetAll()
    {
        return _groups.Groups
            .Select(BuildSummary)
            .OrderBy(summary => summary.GroupName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool TryGet(string groupName, out PollingGroupSummary summary)
    {
        if (!_groups.TryGet(groupName, out var group))
        {
            summary = default!;
            return false;
        }

        summary = BuildSummary(group);
        return true;
    }

    private PollingGroupSummary BuildSummary(SignalPollingGroupDefinition group)
    {
        var groupName = GetGroupKey(group);
        var hasStatus = _statuses.TryGet(groupName, out var status);
        var interval = NormalizeInterval(group.Interval);
        var staleAfter = TimeSpan.FromTicks(interval.Ticks * StaleIntervalMultiplier);
        var lastRunAge = hasStatus
            ? GetAge(status.LastRun)
            : (TimeSpan?)null;

        return new PollingGroupSummary(
            groupName,
            group.DeviceName,
            group.Enabled,
            interval,
            group.Addresses.Count + group.SignalNames.Count,
            group.Addresses.ToArray(),
            group.SignalNames.ToArray(),
            hasStatus,
            hasStatus ? status.Healthy : null,
            hasStatus ? status.LastRun : null,
            lastRunAge,
            hasStatus ? status.Duration : null,
            staleAfter,
            group.Enabled && lastRunAge > staleAfter,
            hasStatus ? status.Error : null);
    }

    private TimeSpan GetAge(DateTimeOffset lastRun)
    {
        var age = _timeProvider.GetUtcNow() - lastRun;
        return age < TimeSpan.Zero
            ? TimeSpan.Zero
            : age;
    }

    private static TimeSpan NormalizeInterval(TimeSpan interval)
    {
        return interval <= TimeSpan.Zero
            ? TimeSpan.FromSeconds(1)
            : interval;
    }

    private static string GetGroupKey(SignalPollingGroupDefinition group)
    {
        return string.IsNullOrWhiteSpace(group.Name)
            ? group.DeviceName
            : group.Name;
    }
}
