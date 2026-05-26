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
            LogixDataTypeCode.Dint => EncodeDint(Convert.ToInt32(value)),
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
            LogixDataTypeCode.Dint => BinaryPrimitives.ReadInt32LittleEndian(data),
            LogixDataTypeCode.Real => BitConverter.Int32BitsToSingle(
                BinaryPrimitives.ReadInt32LittleEndian(data)),
            LogixDataTypeCode.String => DecodeString(data),
            _ => throw new NotSupportedException($"Logix type '{type}' is not supported.")
        };
    }

    private static byte[] EncodeDint(int value)
    {
        var data = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(data, value);
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
