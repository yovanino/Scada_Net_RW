using System.Text.Json;

namespace ScadaNet.AspNetCore;

public sealed record ScadaNetReadManyRequest(IReadOnlyList<string> Addresses);

public sealed record ScadaNetWriteSignalRequest(
    string Address,
    JsonElement Value)
{
    public object? GetValue()
    {
        return Value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when Value.TryGetInt32(out var intValue) => intValue,
            JsonValueKind.Number => Value.GetDouble(),
            JsonValueKind.String => Value.GetString(),
            JsonValueKind.Null => null,
            _ => throw new NotSupportedException(
                $"JSON value kind '{Value.ValueKind}' is not supported for signal writes.")
        };
    }
}
