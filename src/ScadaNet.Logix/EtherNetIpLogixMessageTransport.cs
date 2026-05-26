using ScadaNet.EtherNetIp;

namespace ScadaNet.Logix;

public sealed class EtherNetIpLogixMessageTransport : ILogixMessageTransport
{
    private readonly EtherNetIpClient _client;
    private readonly string _path;

    public EtherNetIpLogixMessageTransport(LogixClientOptions options)
        : this(new EtherNetIpClient(new EtherNetIpClientOptions
        {
            Host = options.Address,
            Port = options.Port,
            ConnectTimeout = options.Timeout,
            OperationTimeout = options.Timeout
        }), options.Path)
    {
    }

    public EtherNetIpLogixMessageTransport(EtherNetIpClient client, string path = "1,0")
    {
        _client = client;
        _path = path;
    }

    public async ValueTask<byte[]> SendAsync(
        byte[] message,
        CancellationToken cancellationToken = default)
    {
        var request = LogixUnconnectedSendCodec.EncodeRequest(
            new LogixUnconnectedSendRequest(message, _path));
        var response = await _client.SendRRDataAsync(request, cancellationToken)
            .ConfigureAwait(false);

        return LogixUnconnectedSendCodec.DecodeResponse(response);
    }

    public ValueTask DisposeAsync()
    {
        return _client.DisposeAsync();
    }
}
