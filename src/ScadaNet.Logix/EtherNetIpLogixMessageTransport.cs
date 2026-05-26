using ScadaNet.EtherNetIp;

namespace ScadaNet.Logix;

public sealed class EtherNetIpLogixMessageTransport : ILogixMessageTransport
{
    private readonly EtherNetIpClient _client;

    public EtherNetIpLogixMessageTransport(LogixClientOptions options)
        : this(new EtherNetIpClient(new EtherNetIpClientOptions
        {
            Host = options.Address,
            Port = options.Port,
            ConnectTimeout = options.Timeout,
            OperationTimeout = options.Timeout
        }))
    {
    }

    public EtherNetIpLogixMessageTransport(EtherNetIpClient client)
    {
        _client = client;
    }

    public ValueTask<byte[]> SendAsync(
        byte[] message,
        CancellationToken cancellationToken = default)
    {
        return _client.SendRRDataAsync(message, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _client.DisposeAsync();
    }
}
