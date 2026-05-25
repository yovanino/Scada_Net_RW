using ScadaNet.Protocols;

namespace ScadaNet.Runtime;

public interface IDeviceConnectionFactory
{
    ValueTask<IDeviceConnection> ConnectAsync(
        string deviceName,
        CancellationToken cancellationToken = default);
}
