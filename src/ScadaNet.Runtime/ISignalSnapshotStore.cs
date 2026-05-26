using ScadaNet.Model;

namespace ScadaNet.Runtime;

public interface ISignalSnapshotStore
{
    void Update(SignalValue value);

    void UpdateMany(IEnumerable<SignalValue> values);

    bool TryGet(SignalRef signal, out SignalValue value);

    IReadOnlyList<SignalValue> GetDeviceSnapshots(string deviceName);
}
