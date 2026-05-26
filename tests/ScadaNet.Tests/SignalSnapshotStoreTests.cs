using ScadaNet.Model;
using ScadaNet.Runtime;

namespace ScadaNet.Tests;

public class SignalSnapshotStoreTests
{
    [Fact]
    public void Update_and_TryGet_store_latest_value()
    {
        var store = new SignalSnapshotStore();
        var signal = new SignalRef("line1-plc", "ProductionCounter");

        store.Update(new SignalValue(
            signal,
            100,
            SignalQuality.Good,
            DateTimeOffset.UtcNow));
        store.Update(new SignalValue(
            signal,
            101,
            SignalQuality.Good,
            DateTimeOffset.UtcNow));

        var found = store.TryGet(signal, out var value);

        Assert.True(found);
        Assert.Equal(101, value.Value);
    }

    [Fact]
    public void GetDeviceSnapshots_filters_by_device_name_case_insensitively()
    {
        var store = new SignalSnapshotStore();

        store.Update(new SignalValue(
            new SignalRef("line1-plc", "B"),
            2,
            SignalQuality.Good,
            DateTimeOffset.UtcNow));
        store.Update(new SignalValue(
            new SignalRef("LINE1-PLC", "A"),
            1,
            SignalQuality.Good,
            DateTimeOffset.UtcNow));
        store.Update(new SignalValue(
            new SignalRef("line2-plc", "A"),
            3,
            SignalQuality.Good,
            DateTimeOffset.UtcNow));

        var values = store.GetDeviceSnapshots("line1-plc");

        Assert.Equal(["A", "B"], values.Select(value => value.Ref.Address).ToArray());
    }
}
