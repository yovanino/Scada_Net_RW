namespace ScadaNet.Runtime;

public sealed class PollingGroupMonitor : IPollingGroupMonitor
{
    private readonly IPollingGroupRegistry _groups;
    private readonly IPollingStatusStore _statuses;

    public PollingGroupMonitor(
        IPollingGroupRegistry groups,
        IPollingStatusStore statuses)
    {
        _groups = groups;
        _statuses = statuses;
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

        return new PollingGroupSummary(
            groupName,
            group.DeviceName,
            group.Enabled,
            group.Interval,
            group.Addresses.Count,
            group.Addresses.ToArray(),
            hasStatus,
            hasStatus ? status.Healthy : null,
            hasStatus ? status.LastRun : null,
            hasStatus ? status.Duration : null,
            hasStatus ? status.Error : null);
    }

    private static string GetGroupKey(SignalPollingGroupDefinition group)
    {
        return string.IsNullOrWhiteSpace(group.Name)
            ? group.DeviceName
            : group.Name;
    }
}
