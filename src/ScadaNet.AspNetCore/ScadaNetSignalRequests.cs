using System.Text.Json;

namespace ScadaNet.AspNetCore;

public sealed record ScadaNetReadManyRequest(IReadOnlyList<string> Addresses);

public sealed record ScadaNetWriteArrayRequest(
    string Address,
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

public sealed record ScadaNetWriteSignalRequest(
    string Address,
    JsonElement Value,
    string? DataType = null)
{
    public object? GetValue()
    {
        return ScadaNetJsonSignalValue.ToObject(Value);
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
