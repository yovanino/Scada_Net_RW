using System.Text.Json;
using ScadaNet.Model;

namespace ScadaNet.AspNetCore;

public sealed record ScadaNetReadManyRequest(IReadOnlyList<string> Addresses)
{
    public IReadOnlyList<SignalRef> ToSignalRefs(string deviceName)
    {
        ArgumentNullException.ThrowIfNull(Addresses);

        var signals = new SignalRef[Addresses.Count];
        for (var index = 0; index < Addresses.Count; index++)
        {
            signals[index] = ScadaNetSignalRequestValidation.ToSignalRef(
                deviceName,
                Addresses[index],
                $"addresses[{index}]");
        }

        return signals;
    }
}

public sealed record ScadaNetReadManyNamedRequest(IReadOnlyList<string> SignalNames)
{
    public IReadOnlyList<string> GetSignalNames()
    {
        ArgumentNullException.ThrowIfNull(SignalNames);

        var signalNames = new string[SignalNames.Count];
        for (var index = 0; index < SignalNames.Count; index++)
        {
            signalNames[index] = ScadaNetSignalRequestValidation.ToSignalName(
                SignalNames[index],
                $"signalNames[{index}]");
        }

        return signalNames;
    }
}

public sealed record ScadaNetWriteArrayRequest(
    string Address,
    JsonElement Values,
    string? DataType = null)
{
    public SignalRef ToSignalRef(string deviceName)
    {
        return ScadaNetSignalRequestValidation.ToSignalRef(deviceName, Address);
    }

    public IReadOnlyList<object?> GetValues()
    {
        if (Values.ValueKind != JsonValueKind.Array)
        {
            throw new NotSupportedException(
                $"JSON value kind '{Values.ValueKind}' is not supported for array writes.");
        }

        return Values.EnumerateArray()
            .Select(ScadaNetJsonSignalValue.ToObject)
            .ToArray();
    }
}

public sealed record ScadaNetWriteSignalRequest(
    string Address,
    JsonElement Value,
    string? DataType = null)
{
    public SignalRef ToSignalRef(string deviceName)
    {
        return ScadaNetSignalRequestValidation.ToSignalRef(deviceName, Address);
    }

    public object? GetValue()
    {
        return ScadaNetJsonSignalValue.ToObject(Value);
    }
}

public sealed record ScadaNetWriteNamedSignalRequest(
    JsonElement Value,
    string? DataType = null)
{
    public object? GetValue()
    {
        return ScadaNetJsonSignalValue.ToObject(Value);
    }
}

public sealed record ScadaNetWriteNamedArrayRequest(
    JsonElement Values,
    string? DataType = null)
{
    public IReadOnlyList<object?> GetValues()
    {
        if (Values.ValueKind != JsonValueKind.Array)
        {
            throw new NotSupportedException(
                $"JSON value kind '{Values.ValueKind}' is not supported for array writes.");
        }

        return Values.EnumerateArray()
            .Select(ScadaNetJsonSignalValue.ToObject)
            .ToArray();
    }
}

internal static class ScadaNetSignalRequestValidation
{
    public static string ToSignalName(
        string? signalName,
        string fieldName = "signalName")
    {
        if (string.IsNullOrWhiteSpace(signalName))
        {
            throw new ArgumentException("Signal name cannot be empty.", fieldName);
        }

        return signalName;
    }

    public static SignalRef ToSignalRef(
        string deviceName,
        string? address,
        string fieldName = "address")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceName);

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException("Signal address cannot be empty.", fieldName);
        }

        return new SignalRef(deviceName, address);
    }
}

internal static class ScadaNetJsonSignalValue
{
    public static object? ToObject(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when value.TryGetInt32(out var intValue) => intValue,
            JsonValueKind.Number => value.GetDouble(),
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Null => null,
            _ => throw new NotSupportedException(
                $"JSON value kind '{value.ValueKind}' is not supported for signal values.")
        };
    }
}
