using System.Collections.Concurrent;
using ScadaNet.Model;

namespace ScadaNet.Runtime;

public sealed class SignalSnapshotStore : ISignalSnapshotStore
{
    private readonly ConcurrentDictionary<SignalRef, SignalValue> _values = [];

    public void Update(SignalValue value)
    {
        _values[value.Ref] = value;
    }

    public void UpdateMany(IEnumerable<SignalValue> values)
    {
        foreach (var value in values)
        {
            Update(value);
        }
    }

    public bool TryGet(SignalRef signal, out SignalValue value)
    {
        return _values.TryGetValue(signal, out value!);
    }

    public IReadOnlyList<SignalValue> GetDeviceSnapshots(string deviceName)
    {
        return _values
            .Where(pair => string.Equals(
                pair.Key.DeviceName,
                deviceName,
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(pair => pair.Key.Address, StringComparer.OrdinalIgnoreCase)
            .Select(pair => pair.Value)
            .ToArray();
    }
}
