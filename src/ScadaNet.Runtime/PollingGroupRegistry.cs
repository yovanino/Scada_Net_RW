namespace ScadaNet.Runtime;

public sealed class PollingGroupRegistry : IPollingGroupRegistry
{
    private readonly IReadOnlyDictionary<string, SignalPollingGroupDefinition> _groupsByName;

    public PollingGroupRegistry(IEnumerable<SignalPollingGroupDefinition> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);

        var groupsByName = new Dictionary<string, SignalPollingGroupDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var groupName = GetGroupKey(group);
            if (string.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentException("Polling group name cannot be empty.", nameof(groups));
            }

            if (!groupsByName.TryAdd(groupName, group))
            {
                throw new ArgumentException(
                    $"Polling group '{groupName}' is already registered.",
                    nameof(groups));
            }
        }

        _groupsByName = groupsByName;
        Groups = groupsByName.Values.ToArray();
    }

    public IReadOnlyCollection<SignalPollingGroupDefinition> Groups { get; }

    public bool TryGet(string name, out SignalPollingGroupDefinition group)
    {
        return _groupsByName.TryGetValue(name, out group!);
    }

    private static string GetGroupKey(SignalPollingGroupDefinition group)
    {
        return string.IsNullOrWhiteSpace(group.Name)
            ? group.DeviceName
            : group.Name;
    }
}
