namespace ScadaNet.Cip;

public static class CipRoutePath
{
    public static byte[] Encode(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var parts = path
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseByte)
            .ToArray();

        if (parts.Length == 0 || parts.Length % 2 != 0)
        {
            throw new ArgumentException(
                "CIP route path must contain port/link pairs, for example '1,0'.",
                nameof(path));
        }

        return parts;
    }

    private static byte ParseByte(string value)
    {
        if (!byte.TryParse(value, out var parsed))
        {
            throw new ArgumentException(
                $"CIP route path segment '{value}' is not a valid byte value.",
                nameof(value));
        }

        return parsed;
    }
}
