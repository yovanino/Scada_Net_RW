using ScadaNet.Core;
using ScadaNet.Transport;

namespace ScadaNet.EtherNetIp;

public sealed class EtherNetIpClient : IAsyncDisposable
{
    private readonly IByteTransport _transport;

    public EtherNetIpClient(EtherNetIpClientOptions options)
        : this(new TcpByteTransport(new TcpByteTransportOptions
        {
            Host = options.Host,
            Port = options.Port,
            ConnectTimeout = options.ConnectTimeout,
            OperationTimeout = options.OperationTimeout
        }))
    {
    }

    public EtherNetIpClient(IByteTransport transport)
    {
        _transport = transport;
    }

    public uint SessionHandle { get; private set; }

    public async ValueTask<RegisterSessionResponse> RegisterSessionAsync(
        CancellationToken cancellationToken = default)
    {
        await _transport.ConnectAsync(cancellationToken).ConfigureAwait(false);

        var request = RegisterSessionCodec.EncodeRequest(new RegisterSessionRequest());
        await _transport.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var packet = await ReceivePacketAsync(cancellationToken).ConfigureAwait(false);
        var response = RegisterSessionCodec.DecodeResponse(packet);
        SessionHandle = response.SessionHandle;
        return response;
    }

    public async ValueTask<IReadOnlyList<EtherNetIpIdentity>> ListIdentityAsync(
        CancellationToken cancellationToken = default)
    {
        await _transport.ConnectAsync(cancellationToken).ConfigureAwait(false);

        var request = ListIdentityCodec.EncodeRequest();
        await _transport.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var packet = await ReceivePacketAsync(cancellationToken).ConfigureAwait(false);
        return ListIdentityCodec.DecodeResponse(packet);
    }

    public ValueTask DisposeAsync()
    {
        return _transport.DisposeAsync();
    }

    private async ValueTask<byte[]> ReceivePacketAsync(CancellationToken cancellationToken)
    {
        var header = new byte[EncapsulationHeader.Size];
        await ReadExactAsync(header, cancellationToken).ConfigureAwait(false);

        var length = BitConverter.ToUInt16(header, 2);
        var packet = new byte[EncapsulationHeader.Size + length];
        header.CopyTo(packet, 0);

        if (length > 0)
        {
            await ReadExactAsync(packet.AsMemory(EncapsulationHeader.Size, length), cancellationToken)
                .ConfigureAwait(false);
        }

        return packet;
    }

    private async ValueTask ReadExactAsync(
        Memory<byte> destination,
        CancellationToken cancellationToken)
    {
        var offset = 0;

        while (offset < destination.Length)
        {
            var read = await _transport.ReceiveAsync(destination[offset..], cancellationToken)
                .ConfigureAwait(false);

            if (read == 0)
            {
                throw new ScadaNetException("Connection closed while reading EtherNet/IP packet.");
            }

            offset += read;
        }
    }
}
