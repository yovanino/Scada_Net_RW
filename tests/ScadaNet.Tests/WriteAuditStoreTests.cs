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

    [Fact]
    public void GetSummary_counts_writes_and_last_failure()
    {
        var store = new WriteAuditStore();
        var firstTimestamp = new DateTimeOffset(2026, 5, 26, 10, 0, 0, TimeSpan.Zero);
        var secondTimestamp = firstTimestamp.AddSeconds(1);
        var thirdTimestamp = firstTimestamp.AddSeconds(2);

        store.Add(new WriteAuditRecord(
            0,
            firstTimestamp,
            new SignalRef("line1-plc", "ResetCommand"),
            true,
            Succeeded: true,
            Error: null));
        store.Add(new WriteAuditRecord(
            0,
            secondTimestamp,
            new SignalRef("line1-plc", "SpeedSetpoint"),
            12.5,
            Succeeded: false,
            Error: "blocked"));
        store.Add(new WriteAuditRecord(
            0,
            thirdTimestamp,
            new SignalRef("line2-plc", "ResetCommand"),
            true,
            Succeeded: true,
            Error: null));

        var summary = store.GetSummary();

        Assert.Null(summary.DeviceName);
        Assert.Equal(3, summary.WriteCount);
        Assert.Equal(2, summary.SucceededWriteCount);
        Assert.Equal(1, summary.FailedWriteCount);
        Assert.Equal(thirdTimestamp, summary.LastWriteTimestamp);
        Assert.Equal(secondTimestamp, summary.LastFailedWriteTimestamp);
        Assert.Equal("blocked", summary.LastError);
    }

    [Fact]
    public void GetDeviceSummary_counts_one_device_case_insensitively()
    {
        var store = new WriteAuditStore();
        var timestamp = new DateTimeOffset(2026, 5, 26, 10, 0, 0, TimeSpan.Zero);

        store.Add(new WriteAuditRecord(
            0,
            timestamp,
            new SignalRef("LINE1-PLC", "ResetCommand"),
            true,
            Succeeded: false,
            Error: "PLC rejected write."));
        store.Add(new WriteAuditRecord(
            0,
            timestamp.AddSeconds(1),
            new SignalRef("line2-plc", "ResetCommand"),
            true,
            Succeeded: true,
            Error: null));

        var summary = store.GetDeviceSummary("line1-plc");

        Assert.Equal("line1-plc", summary.DeviceName);
        Assert.Equal(1, summary.WriteCount);
        Assert.Equal(0, summary.SucceededWriteCount);
        Assert.Equal(1, summary.FailedWriteCount);
        Assert.Equal(timestamp, summary.LastWriteTimestamp);
        Assert.Equal(timestamp, summary.LastFailedWriteTimestamp);
        Assert.Equal("PLC rejected write.", summary.LastError);
    }

    [Fact]
    public void Add_trims_to_configured_max_records()
    {
        var store = new WriteAuditStore(maxRecords: 3);
        var timestamp = new DateTimeOffset(2026, 5, 26, 10, 0, 0, TimeSpan.Zero);

        for (var index = 0; index < 5; index++)
        {
            store.Add(new WriteAuditRecord(
                0,
                timestamp.AddSeconds(index),
                new SignalRef("line1-plc", $"Command{index}"),
                index,
                Succeeded: true,
                Error: null));
        }

        var records = store.GetRecent(count: 10);
        var summary = store.GetSummary();

        Assert.Equal(3, records.Count);
        Assert.Equal(["Command4", "Command3", "Command2"], records.Select(record => record.Signal.Address));
        Assert.Equal(3, summary.WriteCount);
        Assert.Equal(timestamp.AddSeconds(4), summary.LastWriteTimestamp);
    }
}
