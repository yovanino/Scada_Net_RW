using System.Net;
using System.Net.Sockets;
using ScadaNet.EtherNetIp;
using ScadaNet.Protocols;

namespace ScadaNet.Tests;

public class EtherNetIpDiscoveryDriverTests
{
    [Fact]
    public async Task ProbeAsync_returns_identity_when_list_identity_succeeds()
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
            await stream.WriteAsync(BuildListIdentityResponse());
        });

        var driver = new EtherNetIpDiscoveryDriver();
        var result = await driver.ProbeAsync(new ProbeRequest(
            IPAddress.Loopback.ToString(),
            [port],
            TimeSpan.FromSeconds(1)));

        Assert.Equal("EtherNetIp", result.RecommendedDriver);
        Assert.Equal(port, result.Port);
        Assert.True(result.Confidence > 0.9);
        Assert.NotNull(result.Identity);
        Assert.Equal("CompactLogix", result.Identity.ProductName);
        Assert.Contains("ExplicitMessaging", result.Capabilities);
        Assert.Contains(result.Probes, probe => probe.Succeeded);

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
        packet[itemOffset + 12] = 0x78;
        packet[itemOffset + 13] = 0x56;
        packet[itemOffset + 14] = 0x34;
        packet[itemOffset + 15] = 0x12;
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
