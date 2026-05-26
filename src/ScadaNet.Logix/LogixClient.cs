using System.Globalization;
using ScadaNet.Core;

namespace ScadaNet.Logix;

public sealed class LogixClient : IAsyncDisposable
{
    private readonly ILogixMessageTransport _transport;

    public LogixClient(LogixClientOptions options)
        : this(new EtherNetIpLogixMessageTransport(options))
    {
    }

    public LogixClient(ILogixMessageTransport transport)
    {
        _transport = transport;
    }

    public async ValueTask<object?> ReadAsync(
        string tagName,
        CancellationToken cancellationToken = default)
    {
        var request = LogixMessageCodec.EncodeReadTag(new LogixReadTagRequest(tagName));
        var responseMessage = await _transport.SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        var response = LogixMessageCodec.DecodeReadTagResponse(responseMessage);

        EnsureSucceeded(response.Status, $"ReadTag '{tagName}' failed");
        return response.DecodeValue();
    }

    public async ValueTask<T> ReadAsync<T>(
        string tagName,
        CancellationToken cancellationToken = default)
    {
        var value = await ReadAsync(tagName, cancellationToken).ConfigureAwait(false);

        if (value is T typed)
        {
            return typed;
        }

        if (value is null)
        {
            throw new ScadaNetException($"ReadTag '{tagName}' returned no value.");
        }

        return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
    }

    public async ValueTask WriteAsync(
        string tagName,
        LogixDataTypeCode dataType,
        object? value,
        CancellationToken cancellationToken = default)
    {
        var request = LogixMessageCodec.EncodeWriteTag(tagName, dataType, value);
        var responseMessage = await _transport.SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        var response = LogixMessageCodec.DecodeWriteTagResponse(responseMessage);

        EnsureSucceeded(response.Status, $"WriteTag '{tagName}' failed");
    }

    public ValueTask DisposeAsync()
    {
        return _transport.DisposeAsync();
    }

    private static void EnsureSucceeded(
        LogixResponseStatus status,
        string message)
    {
        if (status.Succeeded)
        {
            return;
        }

        var additional = status.AdditionalStatus.Count == 0
            ? string.Empty
            : $" Additional status: {string.Join(", ", status.AdditionalStatus.Select(value => $"0x{value:X4}"))}.";

        throw new ScadaNetException(
            $"{message} with general status 0x{status.GeneralStatus:X2}.{additional}");
    }
}
