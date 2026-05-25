using System.Net;
using System.Net.Sockets;
using ScadaNet.Transport;

namespace ScadaNet.Tests;

public class TcpByteTransportTests
{
    [Fact]
    public async Task Send_and_receive_round_trip_over_tcp()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var server = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            using var stream = client.GetStream();
            var buffer = new byte[3];
            var read = await stream.ReadAsync(buffer);
            Assert.Equal(3, read);
            Assert.Equal(new byte[] { 1, 2, 3 }, buffer);
            await stream.WriteAsync(new byte[] { 4, 5, 6 });
        });

        await using var transport = new TcpByteTransport(new TcpByteTransportOptions
        {
            Host = IPAddress.Loopback.ToString(),
            Port = port,
            ConnectTimeout = TimeSpan.FromSeconds(1),
            OperationTimeout = TimeSpan.FromSeconds(1)
        });

        await transport.ConnectAsync();
        await transport.SendAsync(new byte[] { 1, 2, 3 });

        var response = new byte[3];
        var received = await transport.ReceiveAsync(response);

        Assert.Equal(3, received);
        Assert.Equal(new byte[] { 4, 5, 6 }, response);

        await server;
    }
}
