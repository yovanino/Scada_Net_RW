using ScadaNet.Logix;
using ScadaNet.Model;

namespace ScadaNet.Tests;

public class LogixDeviceConnectionTests
{
    [Fact]
    public async Task ReadAsync_reads_tag_address_and_returns_signal_value()
    {
        var client = new FakeLogixClient(readValue: 123);
        await using var connection = new LogixDeviceConnection("line1-plc", client);
        var signal = new SignalRef("line1-plc", "Counter");

        var value = await connection.ReadAsync(signal);

        Assert.Equal("Counter", client.LastReadTag);
        Assert.Equal(signal, value.Ref);
        Assert.Equal(123, value.Value);
        Assert.Equal(SignalQuality.Good, value.Quality);
    }

    [Fact]
    public async Task WriteAsync_infers_dint_for_int_values()
    {
        var client = new FakeLogixClient(readValue: null);
        await using var connection = new LogixDeviceConnection("line1-plc", client);

        await connection.WriteAsync(new SignalRef("line1-plc", "Counter"), 123);

        Assert.Equal("Counter", client.LastWriteTag);
        Assert.Equal(LogixDataTypeCode.Dint, client.LastWriteType);
        Assert.Equal(123, client.LastWriteValue);
    }

    [Fact]
    public async Task WriteAsync_rejects_unsupported_values()
    {
        var client = new FakeLogixClient(readValue: null);
        await using var connection = new LogixDeviceConnection("line1-plc", client);

        await Assert.ThrowsAsync<NotSupportedException>(async () =>
            await connection.WriteAsync(new SignalRef("line1-plc", "Timestamp"), DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task WriteAsync_infers_string_for_string_values()
    {
        var client = new FakeLogixClient(readValue: null);
        await using var connection = new LogixDeviceConnection("line1-plc", client);

        await connection.WriteAsync(new SignalRef("line1-plc", "Message"), "hello");

        Assert.Equal("Message", client.LastWriteTag);
        Assert.Equal(LogixDataTypeCode.String, client.LastWriteType);
        Assert.Equal("hello", client.LastWriteValue);
    }

    private sealed class FakeLogixClient : ILogixClient
    {
        private readonly object? _readValue;

        public FakeLogixClient(object? readValue)
        {
            _readValue = readValue;
        }

        public string? LastReadTag { get; private set; }
        public string? LastWriteTag { get; private set; }
        public LogixDataTypeCode? LastWriteType { get; private set; }
        public object? LastWriteValue { get; private set; }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask<object?> ReadAsync(
            string tagName,
            CancellationToken cancellationToken = default)
        {
            LastReadTag = tagName;
            return ValueTask.FromResult(_readValue);
        }

        public async ValueTask<T> ReadAsync<T>(
            string tagName,
            CancellationToken cancellationToken = default)
        {
            var value = await ReadAsync(tagName, cancellationToken);
            return (T)value!;
        }

        public ValueTask<IReadOnlyList<T>> ReadArrayAsync<T>(
            string tagName,
            ushort elementCount,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<IReadOnlyList<T>>([]);
        }

        public ValueTask WriteAsync(
            string tagName,
            LogixDataTypeCode dataType,
            object? value,
            CancellationToken cancellationToken = default)
        {
            LastWriteTag = tagName;
            LastWriteType = dataType;
            LastWriteValue = value;
            return ValueTask.CompletedTask;
        }

        public ValueTask WriteArrayAsync(
            string tagName,
            LogixDataTypeCode dataType,
            IReadOnlyList<object?> values,
            CancellationToken cancellationToken = default)
        {
            LastWriteTag = tagName;
            LastWriteType = dataType;
            LastWriteValue = values;
            return ValueTask.CompletedTask;
        }
    }
}
