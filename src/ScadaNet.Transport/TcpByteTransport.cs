using System.Net.Sockets;
using ScadaNet.Core;

namespace ScadaNet.Transport;

public sealed class TcpByteTransport : IByteTransport
{
    private readonly TcpByteTransportOptions _options;
    private TcpClient? _client;
    private NetworkStream? _stream;

    public TcpByteTransport(TcpByteTransportOptions options)
    {
        _options = options;
    }

    public async ValueTask ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_stream is not null)
        {
            return;
        }

        var client = new TcpClient();

        try
        {
            using var timeout = CreateTimeout(cancellationToken, _options.ConnectTimeout);
            await client.ConnectAsync(_options.Host, _options.Port, timeout.Token).ConfigureAwait(false);
            _client = client;
            _stream = client.GetStream();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            client.Dispose();
            throw new ScadaNetException($"Timed out connecting to {_options.Host}:{_options.Port}.");
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    public async ValueTask SendAsync(
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default)
    {
        var stream = GetConnectedStream();

        try
        {
            using var timeout = CreateTimeout(cancellationToken, _options.OperationTimeout);
            await stream.WriteAsync(payload, timeout.Token).ConfigureAwait(false);
            await stream.FlushAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ScadaNetException($"Timed out sending data to {_options.Host}:{_options.Port}.");
        }
    }

    public async ValueTask<int> ReceiveAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        var stream = GetConnectedStream();

        try
        {
            using var timeout = CreateTimeout(cancellationToken, _options.OperationTimeout);
            return await stream.ReadAsync(buffer, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ScadaNetException($"Timed out receiving data from {_options.Host}:{_options.Port}.");
        }
    }

    public ValueTask DisposeAsync()
    {
        _stream?.Dispose();
        _client?.Dispose();
        _stream = null;
        _client = null;
        return ValueTask.CompletedTask;
    }

    private NetworkStream GetConnectedStream()
    {
        return _stream ?? throw new InvalidOperationException("The TCP transport is not connected.");
    }

    private static CancellationTokenSource CreateTimeout(
        CancellationToken cancellationToken,
        TimeSpan timeout)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(timeout);
        return source;
    }
}
