using ScadaNet.Model;

namespace ScadaNet.Runtime;

public interface IPlcRuntime
{
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
