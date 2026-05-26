using ScadaNet.Core;
using ScadaNet.Logix;

namespace ScadaNet.Tests;

public class LogixClientTests
{
    [Fact]
    public async Task ReadAsync_sends_read_tag_request_and_decodes_value()
    {
        var transport = new FakeLogixTransport(
            [
                0xCC, 0x00, 0x00, 0x00,
                0xC4, 0x00,
                0x40, 0xE2, 0x01, 0x00
            ]);
        await using var client = new LogixClient(transport);

        var value = await client.ReadAsync<int>("Counter");

        Assert.Equal(123456, value);
        Assert.Equal(
            LogixMessageCodec.EncodeReadTag(new LogixReadTagRequest("Counter")),
            transport.LastMessage);
    }

    [Fact]
    public async Task ReadArrayAsync_sends_read_tag_request_with_element_count_and_decodes_values()
    {
        var transport = new FakeLogixTransport(
            [
                0xCC, 0x00, 0x00, 0x00,
                0xC4, 0x00,
                0x01, 0x00, 0x00, 0x00,
                0x02, 0x00, 0x00, 0x00,
                0x03, 0x00, 0x00, 0x00
            ]);
        await using var client = new LogixClient(transport);

        var values = await client.ReadArrayAsync<int>("Counters", 3);

        Assert.Equal([1, 2, 3], values);
        Assert.Equal(
            LogixMessageCodec.EncodeReadTag(new LogixReadTagRequest("Counters", 3)),
            transport.LastMessage);
    }

    [Fact]
    public async Task ReadArrayAsync_rejects_zero_element_count()
    {
        var transport = new FakeLogixTransport([]);
        await using var client = new LogixClient(transport);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await client.ReadArrayAsync<int>("Counters", 0));
    }

    [Fact]
    public async Task WriteAsync_sends_write_tag_request()
    {
        var transport = new FakeLogixTransport([0xCD, 0x00, 0x00, 0x00]);
        await using var client = new LogixClient(transport);

        await client.WriteAsync("Counter", LogixDataTypeCode.Dint, 123456);

        Assert.Equal(
            LogixMessageCodec.EncodeWriteTag("Counter", LogixDataTypeCode.Dint, 123456),
            transport.LastMessage);
    }

    [Fact]
    public async Task WriteArrayAsync_sends_write_tag_request_with_element_count()
    {
        var transport = new FakeLogixTransport([0xCD, 0x00, 0x00, 0x00]);
        await using var client = new LogixClient(transport);

        await client.WriteArrayAsync("Counters", LogixDataTypeCode.Dint, [1, 2, 3]);

        Assert.Equal(
            LogixMessageCodec.EncodeWriteTag(new LogixWriteTagRequest(
                "Counters",
                LogixDataTypeCode.Dint,
                3,
                LogixPrimitiveCodec.EncodeMany(LogixDataTypeCode.Dint, [1, 2, 3]))),
            transport.LastMessage);
    }

    [Fact]
    public async Task WriteArrayAsync_rejects_empty_values()
    {
        var transport = new FakeLogixTransport([]);
        await using var client = new LogixClient(transport);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await client.WriteArrayAsync("Counters", LogixDataTypeCode.Dint, []));
    }

    [Fact]
    public async Task ReadAsync_throws_when_controller_returns_error_status()
    {
        var transport = new FakeLogixTransport([0xCC, 0x00, 0x05, 0x01, 0x34, 0x12]);
        await using var client = new LogixClient(transport);

        var error = await Assert.ThrowsAsync<ScadaNetException>(async () =>
            await client.ReadAsync<int>("MissingTag"));

        Assert.Contains("general status 0x05", error.Message);
        Assert.Contains("0x1234", error.Message);
    }

    private sealed class FakeLogixTransport : ILogixMessageTransport
    {
        private readonly byte[] _response;

        public FakeLogixTransport(byte[] response)
        {
            _response = response;
        }

        public byte[]? LastMessage { get; private set; }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask<byte[]> SendAsync(
            byte[] message,
            CancellationToken cancellationToken = default)
        {
            LastMessage = message;
            return ValueTask.FromResult(_response);
        }
    }
}
