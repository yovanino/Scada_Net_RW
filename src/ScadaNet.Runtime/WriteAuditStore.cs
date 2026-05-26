using System.Threading;

namespace ScadaNet.Runtime;

public sealed class WriteAuditStore : IWriteAuditStore
{
    private const int MaxRecords = 1000;

    private readonly object _sync = new();
    private readonly List<WriteAuditRecord> _records = [];
    private long _sequence;

    public void Add(WriteAuditRecord record)
    {
        lock (_sync)
        {
            var sequence = record.Sequence > 0
                ? record.Sequence
                : Interlocked.Increment(ref _sequence);

            _records.Add(record with { Sequence = sequence });

            if (_records.Count > MaxRecords)
            {
                _records.RemoveRange(0, _records.Count - MaxRecords);
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

    private static int NormalizeCount(int count)
    {
        return Math.Clamp(count, 1, MaxRecords);
    }
}
