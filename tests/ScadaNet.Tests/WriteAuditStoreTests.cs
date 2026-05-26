using ScadaNet.Model;
using ScadaNet.Runtime;

namespace ScadaNet.Tests;

public class WriteAuditStoreTests
{
    [Fact]
    public void Add_assigns_sequence_and_returns_recent_first()
    {
        var store = new WriteAuditStore();

        store.Add(new WriteAuditRecord(
            0,
            DateTimeOffset.UtcNow,
            new SignalRef("line1-plc", "ResetCommand"),
            true,
            Succeeded: true,
            Error: null));
        store.Add(new WriteAuditRecord(
            0,
            DateTimeOffset.UtcNow,
            new SignalRef("line1-plc", "SpeedSetpoint"),
            12.5,
            Succeeded: false,
            Error: "blocked"));

        var records = store.GetRecent();

        Assert.Equal(2, records.Count);
        Assert.True(records[0].Sequence > records[1].Sequence);
        Assert.Equal("SpeedSetpoint", records[0].Signal.Address);
    }

    [Fact]
    public void GetDeviceRecords_filters_by_device_case_insensitively()
    {
        var store = new WriteAuditStore();

        store.Add(new WriteAuditRecord(
            0,
            DateTimeOffset.UtcNow,
            new SignalRef("LINE1-PLC", "ResetCommand"),
            true,
            Succeeded: true,
            Error: null));
        store.Add(new WriteAuditRecord(
            0,
            DateTimeOffset.UtcNow,
            new SignalRef("line2-plc", "ResetCommand"),
            true,
            Succeeded: true,
            Error: null));

        var records = store.GetDeviceRecords("line1-plc");

        var record = Assert.Single(records);
        Assert.Equal("ResetCommand", record.Signal.Address);
    }
}
