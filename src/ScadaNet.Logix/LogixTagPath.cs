using System.Text;

namespace ScadaNet.Logix;

public sealed record LogixTagPath(string Value)
{
    public byte[] Encode()
    {
        return Encode(Value);
    }

    public static byte[] Encode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var segments = value.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length == 0 || segments.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Logix tag path must contain at least one segment.", nameof(value));
        }

        using var stream = new MemoryStream();

        foreach (var segment in segments)
        {
            WriteAnsiExtendedSymbolSegment(stream, segment);
        }

        return stream.ToArray();
    }

    private static void WriteAnsiExtendedSymbolSegment(Stream stream, string segment)
    {
        if (segment.Length > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(segment),
                segment.Length,
                "Logix tag path segments cannot exceed 255 characters.");
        }

        if (segment.Any(character => character > 0x7F))
        {
            throw new ArgumentException(
                $"Logix tag path segment '{segment}' contains non-ASCII characters.",
                nameof(segment));
        }

        var bytes = Encoding.ASCII.GetBytes(segment);
        stream.WriteByte(0x91);
        stream.WriteByte((byte)bytes.Length);
        stream.Write(bytes);

        if (bytes.Length % 2 != 0)
        {
            stream.WriteByte(0x00);
        }
    }
}
