using ScadaNet.Model;
using ScadaNet.Protocols;

namespace ScadaNet.Logix;

public sealed class LogixDeviceConnection : IDeviceConnection, IArrayDeviceConnection, ITypedDeviceConnection
{
    private readonly ILogixClient _client;

    public LogixDeviceConnection(
        string deviceName,
        ILogixClient client)
    {
        DeviceName = deviceName;
        _client = client;
    }

    public string DeviceName { get; }
    public DeviceIdentity Identity { get; } = new("Rockwell Automation", "Logix", null, null, null);

    public DeviceCapabilities Capabilities =>
        DeviceCapabilities.Read |
        DeviceCapabilities.Write |
        DeviceCapabilities.ReadMany |
        DeviceCapabilities.ReadArray |
        DeviceCapabilities.WriteArray;

    public async ValueTask<SignalValue> ReadAsync(
        SignalRef signal,
        CancellationToken cancellationToken = default)
    {
        var value = await _client.ReadAsync(signal.Address, cancellationToken)
            .ConfigureAwait(false);

        return new SignalValue(
            signal,
            value,
            SignalQuality.Good,
            DateTimeOffset.UtcNow);
    }

    public async ValueTask<IReadOnlyList<SignalValue>> ReadManyAsync(
        IReadOnlyList<SignalRef> signals,
        CancellationToken cancellationToken = default)
    {
        var values = new List<SignalValue>(signals.Count);

        foreach (var signal in signals)
        {
            values.Add(await ReadAsync(signal, cancellationToken).ConfigureAwait(false));
        }

        return values;
    }

    public ValueTask WriteAsync(
        SignalRef signal,
        object? value,
        CancellationToken cancellationToken = default)
    {
        return _client.WriteAsync(
            signal.Address,
            InferDataType(value),
            value,
            cancellationToken);
    }

    public ValueTask WriteAsync(
        SignalRef signal,
        object? value,
        string dataType,
        CancellationToken cancellationToken = default)
    {
        return _client.WriteAsync(
            signal.Address,
            ParseDataType(dataType),
            value,
            cancellationToken);
    }

    public async ValueTask<SignalValue> ReadArrayAsync(
        SignalRef signal,
        ushort elementCount,
        CancellationToken cancellationToken = default)
    {
        var value = await _client.ReadArrayAsync<object?>(signal.Address, elementCount, cancellationToken)
            .ConfigureAwait(false);

        return new SignalValue(
            signal,
            value,
            SignalQuality.Good,
            DateTimeOffset.UtcNow);
    }

    public ValueTask WriteArrayAsync(
        SignalRef signal,
        IReadOnlyList<object?> values,
        string? dataType = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Count == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(values),
                values.Count,
                "Logix array write values cannot be empty.");
        }

        var resolvedDataType = dataType is null
            ? InferArrayDataType(values)
            : ParseDataType(dataType);

        return _client.WriteArrayAsync(
            signal.Address,
            resolvedDataType,
            values,
            cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _client.DisposeAsync();
    }

    private static LogixDataTypeCode InferDataType(object? value)
    {
        return value switch
        {
            bool => LogixDataTypeCode.Bool,
            sbyte => LogixDataTypeCode.Sint,
            short => LogixDataTypeCode.Int,
            int => LogixDataTypeCode.Dint,
            long => LogixDataTypeCode.Lint,
            float or double => LogixDataTypeCode.Real,
            string => LogixDataTypeCode.String,
            _ => throw new NotSupportedException(
                $"Cannot infer a Logix primitive data type for value '{value}'.")
        };
    }

    private static LogixDataTypeCode InferArrayDataType(IReadOnlyList<object?> values)
    {
        var dataType = InferDataType(values[0]);

        for (var index = 1; index < values.Count; index++)
        {
            var valueDataType = InferDataType(values[index]);
            if (valueDataType != dataType)
            {
                throw new NotSupportedException(
                    $"Logix array writes require homogeneous primitive values. Element 0 is '{dataType}' but element {index} is '{valueDataType}'.");
            }
        }

        return dataType;
    }

    private static LogixDataTypeCode ParseDataType(string dataType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataType);

        return Enum.TryParse<LogixDataTypeCode>(dataType, ignoreCase: true, out var parsed)
            ? parsed
            : throw new NotSupportedException(
                $"Logix data type '{dataType}' is not supported.");
    }
}
