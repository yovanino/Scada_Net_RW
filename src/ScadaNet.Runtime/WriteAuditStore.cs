using System.Threading;

namespace ScadaNet.Runtime;

public sealed class WriteAuditStore : IWriteAuditStore
{
    public const int DefaultMaxRecords = 1000;

    private readonly object _sync = new();
    private readonly List<WriteAuditRecord> _records = [];
    private readonly int _maxRecords;
    private long _sequence;

    public WriteAuditStore()
        : this(DefaultMaxRecords)
    {
    }

    public WriteAuditStore(int maxRecords)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxRecords, 1);

        _maxRecords = maxRecords;
    }

    public void Add(WriteAuditRecord record)
    {
        lock (_sync)
        {
            var sequence = record.Sequence > 0
                ? record.Sequence
                : Interlocked.Increment(ref _sequence);

            _records.Add(record with { Sequence = sequence });

            if (_records.Count > _maxRecords)
            {
                _records.RemoveRange(0, _records.Count - _maxRecords);
            }
        }
    }

    public IReadOnlyList<WriteAuditRecord> GetRecent(int count = 100)
    {
        lock (_sync)
        {
            return _records
                .OrderByDescending(record => record.Sequence)
                .Take(NormalizeCount(count))
                .ToArray();
        }
    }

    public IReadOnlyList<WriteAuditRecord> GetDeviceRecords(string deviceName, int count = 100)
    {
        lock (_sync)
        {
            return _records
                .Where(record => string.Equals(
                    record.Signal.DeviceName,
                    deviceName,
                    StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(record => record.Sequence)
                .Take(NormalizeCount(count))
                .ToArray();
        }
    }

    public WriteAuditSummary GetSummary()
    {
        lock (_sync)
        {
            return BuildSummary(deviceName: null, _records);
        }
    }

    public WriteAuditSummary GetDeviceSummary(string deviceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceName);

        lock (_sync)
        {
            var records = _records
                .Where(record => string.Equals(
                    record.Signal.DeviceName,
                    deviceName,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return BuildSummary(deviceName, records);
        }
    }

    private int NormalizeCount(int count)
    {
        return Math.Clamp(count, 1, _maxRecords);
    }

    private static WriteAuditSummary BuildSummary(
        string? deviceName,
        IReadOnlyList<WriteAuditRecord> records)
    {
        var latest = records
            .OrderByDescending(record => record.Sequence)
            .FirstOrDefault();
        var latestFailure = records
            .Where(record => !record.Succeeded)
            .OrderByDescending(record => record.Sequence)
            .FirstOrDefault();

        return new WriteAuditSummary(
            deviceName,
            records.Count,
            records.Count(record => record.Succeeded),
            records.Count(record => !record.Succeeded),
            latest?.Timestamp,
            latestFailure?.Timestamp,
            latestFailure?.Error);
    }
}
