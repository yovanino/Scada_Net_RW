using ScadaNet.Protocols;

namespace ScadaNet.Runtime;

public interface IDeviceConnectionLease : IAsyncDisposable
{
    IDeviceConnection Connection { get; }
}
