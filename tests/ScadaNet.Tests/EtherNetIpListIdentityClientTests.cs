using System.Net;
using System.Net.Sockets;
using ScadaNet.EtherNetIp;

namespace ScadaNet.Tests;

public class EtherNetIpListIdentityClientTests
{
    [Fact]
    public async Task ListIdentityAsync_sends_request_and_decodes_response()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var server = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            using var stream = client.GetStream();

            var request = new byte[24];
            await ReadExactlyAsync(stream, request);

            Assert.Equal(ListIdentityCodec.EncodeRequest(), request);

            await stream.WriteAsync(BuildListIdentityResponse());
        });

        await using var etherNetIp = new EtherNetIpClient(new EtherNetIpClientOptions
        {
            Host = IPAddress.Loopback.ToString(),
            Port = port,
            ConnectTimeout = TimeSpan.FromSeconds(1),
            OperationTimeout = TimeSpan.FromSeconds(1)
        });

        var identities = await etherNetIp.ListIdentityAsync();

        var identity = Assert.Single(identities);
        Assert.Equal("CompactLogix", identity.ProductName);

        await server;
    }

    private static byte[] BuildListIdentityResponse()
    {
        var productName = "CompactLogix"u8.ToArray();
        var itemLength = 34 + productName.Length;
        var payloadLength = 2 + 4 + itemLength;
        var packet = new byte[24 + payloadLength];

        packet[0] = 0x63;
        packet[2] = (byte)payloadLength;
        packet[24] = 0x01;
        packet[26] = 0x0C;
        packet[28] = (byte)itemLength;

        var itemOffset = 30;
        packet[itemOffset] = 0x01;
        packet[itemOffset + 1] = 0x00;
        packet[itemOffset + 2] = 0x01;
        packet[itemOffset + 4] = 0x0E;
        packet[itemOffset + 6] = 0x54;
        packet[itemOffset + 8] = 0x21;
        packet[itemOffset + 9] = 0x03;
        packet[itemOffset + 32] = (byte)productName.Length;
        productName.CopyTo(packet, itemOffset + 33);
        packet[itemOffset + 33 + productName.Length] = 0x01;

        return packet;
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
