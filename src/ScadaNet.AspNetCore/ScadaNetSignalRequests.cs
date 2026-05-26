using System.Text.Json;
using ScadaNet.Model;
using ScadaNet.Runtime;

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

public static class ScadaNetSignalValueRangeValidation
{
    public static void Validate(
        DeviceSignalDefinition definition,
        object? value,
        string signalName)
    {
        if (!definition.MinValue.HasValue && !definition.MaxValue.HasValue)
        {
            return;
        }

        if (!TryGetNumber(value, out var numericValue))
        {
            throw new ArgumentException(
                $"Signal '{signalName}' requires a numeric value because it has configured engineering limits.",
                nameof(value));
        }

        if (definition.MinValue.HasValue && numericValue < definition.MinValue.Value)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                numericValue,
                $"Signal '{signalName}' value must be greater than or equal to {definition.MinValue.Value}.");
        }

        if (definition.MaxValue.HasValue && numericValue > definition.MaxValue.Value)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                numericValue,
                $"Signal '{signalName}' value must be less than or equal to {definition.MaxValue.Value}.");
        }
    }

    public static void ValidateMany(
        DeviceSignalDefinition definition,
        IReadOnlyList<object?> values,
        string signalName)
    {
        for (var index = 0; index < values.Count; index++)
        {
            try
            {
                Validate(definition, values[index], signalName);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException(
                    $"{ex.Message} Array index: {index}.",
                    $"values[{index}]",
                    ex);
            }
        }
    }

    private static bool TryGetNumber(object? value, out double numericValue)
    {
        switch (value)
        {
            case byte byteValue:
                numericValue = byteValue;
                return true;
            case sbyte sbyteValue:
                numericValue = sbyteValue;
                return true;
            case short shortValue:
                numericValue = shortValue;
                return true;
            case ushort ushortValue:
                numericValue = ushortValue;
                return true;
            case int intValue:
                numericValue = intValue;
                return true;
            case uint uintValue:
                numericValue = uintValue;
                return true;
            case long longValue:
                numericValue = longValue;
                return true;
            case ulong ulongValue:
                numericValue = ulongValue;
                return true;
            case float floatValue:
                numericValue = floatValue;
                return true;
            case double doubleValue:
                numericValue = doubleValue;
                return true;
            case decimal decimalValue:
                numericValue = (double)decimalValue;
                return true;
            default:
                numericValue = default;
                return false;
        }
    }
}
