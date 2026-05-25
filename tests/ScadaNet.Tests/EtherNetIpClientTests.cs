using System.Net;
using System.Net.Sockets;
using ScadaNet.EtherNetIp;

namespace ScadaNet.Tests;

public class EtherNetIpClientTests
{
    [Fact]
    public async Task RegisterSessionAsync_sends_request_and_stores_session_handle()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var server = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            using var stream = client.GetStream();

            var request = new byte[28];
            await ReadExactlyAsync(stream, request);

            Assert.Equal(RegisterSessionCodec.EncodeRequest(new RegisterSessionRequest()), request);

            await stream.WriteAsync(new byte[]
            {
                0x65, 0x00,
                0x04, 0x00,
                0x78, 0x56, 0x34, 0x12,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x01, 0x00,
                0x00, 0x00
            });
        });

        await using var etherNetIp = new EtherNetIpClient(new EtherNetIpClientOptions
        {
            Host = IPAddress.Loopback.ToString(),
            Port = port,
            ConnectTimeout = TimeSpan.FromSeconds(1),
            OperationTimeout = TimeSpan.FromSeconds(1)
        });

        var response = await etherNetIp.RegisterSessionAsync();

        Assert.Equal(0x12345678u, response.SessionHandle);
        Assert.Equal(0x12345678u, etherNetIp.SessionHandle);

        await server;
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
