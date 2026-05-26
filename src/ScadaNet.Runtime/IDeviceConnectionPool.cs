namespace ScadaNet.Runtime;

public interface IDeviceConnectionPool
{
    ValueTask<IDeviceConnectionLease> RentAsync(
        string deviceName,
        CancellationToken cancellationToken = default);
}
