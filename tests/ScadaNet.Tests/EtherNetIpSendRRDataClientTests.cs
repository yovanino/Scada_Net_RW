using System.Net;
using System.Net.Sockets;
using ScadaNet.EtherNetIp;

namespace ScadaNet.Tests;

public class EtherNetIpSendRRDataClientTests
{
    [Fact]
    public async Task SendRRDataAsync_registers_session_and_sends_request()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var server = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            using var stream = client.GetStream();

            var registerRequest = new byte[28];
            await ReadExactlyAsync(stream, registerRequest);
            Assert.Equal(RegisterSessionCodec.EncodeRequest(new RegisterSessionRequest()), registerRequest);

            await stream.WriteAsync(BuildRegisterSessionResponse());

            var sendRequest = new byte[42];
            await ReadExactlyAsync(stream, sendRequest);
            Assert.Equal(
                SendRRDataCodec.EncodeRequest(0x12345678, new SendRRDataRequest([0x4C, 0x00])),
                sendRequest);

            await stream.WriteAsync(BuildSendRRDataResponse());
        });

        await using var etherNetIp = new EtherNetIpClient(new EtherNetIpClientOptions
        {
            Host = IPAddress.Loopback.ToString(),
            Port = port,
            ConnectTimeout = TimeSpan.FromSeconds(1),
            OperationTimeout = TimeSpan.FromSeconds(1)
        });

        var response = await etherNetIp.SendRRDataAsync([0x4C, 0x00]);

        Assert.Equal([0xCC, 0x00], response);

        await server;
    }

    private static byte[] BuildRegisterSessionResponse()
    {
        return
        [
            0x65, 0x00,
            0x04, 0x00,
            0x78, 0x56, 0x34, 0x12,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x01, 0x00,
            0x00, 0x00
        ];
    }

    private static byte[] BuildSendRRDataResponse()
    {
        return
        [
            0x6F, 0x00,
            0x12, 0x00,
            0x78, 0x56, 0x34, 0x12,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00,
            0x02, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0xB2, 0x00,
            0x02, 0x00,
            0xCC, 0x00
        ];
    }

    private static async Task ReadExactlyAsync(Stream stream, byte[] buffer)
    {
        var offset = 0;

        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset));
            Assert.NotEqual(0, read);
            offset += read;
        }
    }
}
