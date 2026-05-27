namespace ScadaNet.Runtime;

public interface IWriteAuditStore
{
    void Add(WriteAuditRecord record);

    IReadOnlyList<WriteAuditRecord> GetRecent(int count = 100);

    IReadOnlyList<WriteAuditRecord> GetDeviceRecords(string deviceName, int count = 100);

    WriteAuditSummary GetSummary();

    WriteAuditSummary GetDeviceSummary(string deviceName);
}
