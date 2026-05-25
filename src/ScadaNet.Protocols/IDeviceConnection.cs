using ScadaNet.Model;

namespace ScadaNet.Protocols;

public interface IDeviceConnection : IAsyncDisposable
{
    DeviceIdentity Identity { get; }
    DeviceCapabilities Capabilities { get; }

    ValueTask<SignalValue> ReadAsync(
        SignalRef signal,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<SignalValue>> ReadManyAsync(
        IReadOnlyList<SignalRef> signals,
        CancellationToken cancellationToken = default);

    ValueTask WriteAsync(
        SignalRef signal,
        object? value,
        CancellationToken cancellationToken = default);
}
