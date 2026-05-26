using System.Buffers.Binary;
using System.Text;

namespace ScadaNet.Logix;

public static class LogixPrimitiveCodec
{
    public const int LogixStringMaxLength = 82;

    public static byte[] Encode(LogixDataTypeCode type, object? value)
    {
        return type switch
        {
            LogixDataTypeCode.Bool => [(byte)((bool)value! ? 1 : 0)],
            LogixDataTypeCode.Sint => [(byte)Convert.ToSByte(value)],
            LogixDataTypeCode.Int => EncodeInt(Convert.ToInt16(value)),
            LogixDataTypeCode.Dint => EncodeDint(Convert.ToInt32(value)),
            LogixDataTypeCode.Lint => EncodeLint(Convert.ToInt64(value)),
            LogixDataTypeCode.Real => EncodeReal(Convert.ToSingle(value)),
            LogixDataTypeCode.String => EncodeString((string)value!),
            _ => throw new NotSupportedException($"Logix type '{type}' is not supported.")
        };
    }

    public static object Decode(LogixDataTypeCode type, ReadOnlySpan<byte> data)
    {
        return type switch
        {
            LogixDataTypeCode.Bool => data.Length > 0 && data[0] != 0,
            LogixDataTypeCode.Sint => unchecked((sbyte)data[0]),
            LogixDataTypeCode.Int => BinaryPrimitives.ReadInt16LittleEndian(data),
            LogixDataTypeCode.Dint => BinaryPrimitives.ReadInt32LittleEndian(data),
            LogixDataTypeCode.Lint => BinaryPrimitives.ReadInt64LittleEndian(data),
            LogixDataTypeCode.Real => BitConverter.Int32BitsToSingle(
                BinaryPrimitives.ReadInt32LittleEndian(data)),
            LogixDataTypeCode.String => DecodeString(data),
            _ => throw new NotSupportedException($"Logix type '{type}' is not supported.")
        };
    }

    public static byte[] EncodeMany(
        LogixDataTypeCode type,
        IReadOnlyList<object?> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Count == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(values),
                values.Count,
                "Logix primitive encode element count must be greater than zero.");
        }

        using var stream = new MemoryStream();

        foreach (var value in values)
        {
            stream.Write(Encode(type, value));
        }

        return stream.ToArray();
    }

    public static IReadOnlyList<object?> DecodeMany(
        LogixDataTypeCode type,
        ReadOnlySpan<byte> data,
        ushort elementCount)
    {
        if (elementCount == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(elementCount),
                elementCount,
                "Logix primitive decode element count must be greater than zero.");
        }

        var values = new object?[elementCount];
        var elementSize = GetFixedElementSize(type);

        if (elementSize is null)
        {
            throw new NotSupportedException($"Logix type '{type}' cannot be decoded as a fixed-size array.");
        }

        var requiredLength = elementSize.Value * elementCount;
        if (data.Length < requiredLength)
        {
            throw new ArgumentException(
                $"Logix data contains {data.Length} bytes but {requiredLength} bytes are required.",
                nameof(data));
        }

        for (var index = 0; index < values.Length; index++)
        {
            var offset = index * elementSize.Value;
            values[index] = Decode(type, data.Slice(offset, elementSize.Value));
        }

        return values;
    }

    private static byte[] EncodeInt(short value)
    {
        var data = new byte[sizeof(short)];
        BinaryPrimitives.WriteInt16LittleEndian(data, value);
        return data;
    }

    private static byte[] EncodeDint(int value)
    {
        var data = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(data, value);
        return data;
    }

    private static byte[] EncodeLint(long value)
    {
        var data = new byte[sizeof(long)];
        BinaryPrimitives.WriteInt64LittleEndian(data, value);
        return data;
    }

    private static byte[] EncodeReal(float value)
    {
        var data = new byte[sizeof(float)];
        BinaryPrimitives.WriteInt32LittleEndian(data, BitConverter.SingleToInt32Bits(value));
        return data;
    }

    private static byte[] EncodeString(string value)
    {
        if (!AsciiOnly(value))
        {
            throw new ArgumentException("Logix STRING values must contain ASCII characters only.", nameof(value));
        }

        var stringData = Encoding.ASCII.GetBytes(value);
        if (stringData.Length > LogixStringMaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                $"Logix STRING values cannot exceed {LogixStringMaxLength} bytes.");
        }

        var data = new byte[sizeof(int) + LogixStringMaxLength];
        BinaryPrimitives.WriteInt32LittleEndian(data, stringData.Length);
        stringData.CopyTo(data.AsSpan(sizeof(int)));
        return data;
    }

    private static string DecodeString(ReadOnlySpan<byte> data)
    {
        if (data.Length < sizeof(int))
        {
            throw new ArgumentException("Logix STRING data must include a length prefix.", nameof(data));
        }

        var length = BinaryPrimitives.ReadInt32LittleEndian(data);
        if (length < 0 || length > data.Length - sizeof(int))
        {
            throw new ArgumentException("Logix STRING length prefix exceeds available data.", nameof(data));
        }

        return Encoding.ASCII.GetString(data.Slice(sizeof(int), length));
    }

    private static int? GetFixedElementSize(LogixDataTypeCode type)
    {
        return type switch
        {
            LogixDataTypeCode.Bool => sizeof(byte),
            LogixDataTypeCode.Sint => sizeof(sbyte),
            LogixDataTypeCode.Int => sizeof(short),
            LogixDataTypeCode.Dint => sizeof(int),
            LogixDataTypeCode.Lint => sizeof(long),
            LogixDataTypeCode.Real => sizeof(float),
            LogixDataTypeCode.String => sizeof(int) + LogixStringMaxLength,
            _ => null
        };
    }

    private static bool AsciiOnly(string value)
    {
        foreach (var character in value)
        {
            if (character > 0x7F)
            {
                return false;
            }
        }

        return true;
    }
}
