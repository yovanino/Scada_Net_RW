namespace ScadaNet.Transport;

public interface IByteTransport : IAsyncDisposable
{
    ValueTask ConnectAsync(CancellationToken cancellationToken = default);

    ValueTask SendAsync(
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default);

    ValueTask<int> ReceiveAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default);
}
