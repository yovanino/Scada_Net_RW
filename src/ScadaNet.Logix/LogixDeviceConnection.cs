using ScadaNet.Model;
using ScadaNet.Protocols;

namespace ScadaNet.Logix;

public sealed class LogixDeviceConnection : IDeviceConnection
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
        DeviceCapabilities.ReadMany;

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

    public ValueTask DisposeAsync()
    {
        return _client.DisposeAsync();
    }

    private static LogixDataTypeCode InferDataType(object? value)
    {
        return value switch
        {
            bool => LogixDataTypeCode.Bool,
            int => LogixDataTypeCode.Dint,
            float or double => LogixDataTypeCode.Real,
            _ => throw new NotSupportedException(
                $"Cannot infer a Logix primitive data type for value '{value}'.")
        };
    }
}
