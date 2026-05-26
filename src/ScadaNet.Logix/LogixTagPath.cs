using System.Buffers.Binary;
using System.Globalization;
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
            WriteSymbolAndElementSegments(stream, segment);
        }

        return stream.ToArray();
    }

    private static void WriteSymbolAndElementSegments(Stream stream, string segment)
    {
        var indexStart = segment.IndexOf('[');
        if (indexStart < 0)
        {
            WriteAnsiExtendedSymbolSegment(stream, segment);
            return;
        }

        var symbol = segment[..indexStart];
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("Logix indexed tag segments must start with a symbol.", nameof(segment));
        }

        WriteAnsiExtendedSymbolSegment(stream, symbol);

        var offset = indexStart;
        while (offset < segment.Length)
        {
            if (segment[offset] != '[')
            {
                throw new ArgumentException(
                    $"Logix tag path segment '{segment}' contains invalid index syntax.",
                    nameof(segment));
            }

            var indexEnd = segment.IndexOf(']', offset + 1);
            if (indexEnd < 0)
            {
                throw new ArgumentException(
                    $"Logix tag path segment '{segment}' has an unclosed index.",
                    nameof(segment));
            }

            WriteElementSegments(stream, segment[(offset + 1)..indexEnd], segment);
            offset = indexEnd + 1;
        }
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

    private static void WriteElementSegments(Stream stream, string indexes, string segment)
    {
        var values = indexes.Split(',', StringSplitOptions.TrimEntries);

        if (values.Length == 0 || values.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                $"Logix tag path segment '{segment}' contains an empty index.",
                nameof(segment));
        }

        foreach (var value in values)
        {
            if (!uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var index))
            {
                throw new ArgumentException(
                    $"Logix tag path segment '{segment}' contains invalid index '{value}'.",
                    nameof(segment));
            }

            WriteElementSegment(stream, index);
        }
    }

    private static void WriteElementSegment(Stream stream, uint index)
    {
        if (index <= byte.MaxValue)
        {
            stream.WriteByte(0x28);
            stream.WriteByte((byte)index);
            return;
        }

        if (index <= ushort.MaxValue)
        {
            Span<byte> buffer = stackalloc byte[4];
            buffer[0] = 0x29;
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[2..], (ushort)index);
            stream.Write(buffer);
            return;
        }

        Span<byte> longBuffer = stackalloc byte[6];
        longBuffer[0] = 0x2A;
        BinaryPrimitives.WriteUInt32LittleEndian(longBuffer[2..], index);
        stream.Write(longBuffer);
    }
}
