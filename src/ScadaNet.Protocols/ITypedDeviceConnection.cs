using ScadaNet.Model;

namespace ScadaNet.Protocols;

public interface ITypedDeviceConnection
{
    ValueTask WriteAsync(
        SignalRef signal,
        object? value,
        string dataType,
        CancellationToken cancellationToken = default);
}
