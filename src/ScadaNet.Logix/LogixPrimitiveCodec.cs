using System.Buffers.Binary;

namespace ScadaNet.Logix;

public static class LogixPrimitiveCodec
{
    public static byte[] Encode(LogixDataTypeCode type, object? value)
    {
        return type switch
        {
            LogixDataTypeCode.Bool => [(byte)((bool)value! ? 1 : 0)],
            LogixDataTypeCode.Dint => EncodeDint(Convert.ToInt32(value)),
            LogixDataTypeCode.Real => EncodeReal(Convert.ToSingle(value)),
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
}
