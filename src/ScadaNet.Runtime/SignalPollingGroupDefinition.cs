using ScadaNet.Model;

namespace ScadaNet.Runtime;

public sealed class SignalPollingGroupDefinition
{
    public string Name { get; init; } = string.Empty;
    public string DeviceName { get; init; } = string.Empty;
    public IList<string> Addresses { get; } = [];
    public IList<string> SignalNames { get; } = [];
    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(1);
    public bool Enabled { get; init; } = true;

    public IReadOnlyList<SignalRef> ToSignals()
    {
        return Addresses
            .Where(address => !string.IsNullOrWhiteSpace(address))
            .Select(address => new SignalRef(DeviceName, address))
            .ToArray();
    }
}
