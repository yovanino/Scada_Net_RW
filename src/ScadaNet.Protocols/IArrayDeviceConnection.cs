using ScadaNet.Model;

namespace ScadaNet.Protocols;

public interface IArrayDeviceConnection
{
    ValueTask<SignalValue> ReadArrayAsync(
        SignalRef signal,
        ushort elementCount,
        CancellationToken cancellationToken = default);

    ValueTask WriteArrayAsync(
        SignalRef signal,
        IReadOnlyList<object?> values,
        string? dataType = null,
        CancellationToken cancellationToken = default);
}
