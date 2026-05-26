namespace ScadaNet.Logix;

public interface ILogixMessageTransport : IAsyncDisposable
{
    ValueTask<byte[]> SendAsync(
        byte[] message,
        CancellationToken cancellationToken = default);
}
