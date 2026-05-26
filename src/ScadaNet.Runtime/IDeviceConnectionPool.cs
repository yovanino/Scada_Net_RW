namespace ScadaNet.Runtime;

public interface IDeviceConnectionPool
{
    ValueTask<IDeviceConnectionLease> RentAsync(
        string deviceName,
        CancellationToken cancellationToken = default);

    ValueTask<bool> CloseAsync(
        string deviceName,
        CancellationToken cancellationToken = default);

    IReadOnlyList<DeviceConnectionPoolStatus> GetStatus();
}
