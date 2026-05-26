using ScadaNet.Logix;

namespace ScadaNet.Tests;

public class LogixPrimitiveCodecTests
{
    [Fact]
    public void Encode_and_decode_bool()
    {
        var encoded = LogixPrimitiveCodec.Encode(LogixDataTypeCode.Bool, true);
        var decoded = LogixPrimitiveCodec.Decode(LogixDataTypeCode.Bool, encoded);

        Assert.Equal([0x01], encoded);
        Assert.Equal(true, decoded);
    }

    [Fact]
    public void Encode_and_decode_sint()
    {
        var encoded = LogixPrimitiveCodec.Encode(LogixDataTypeCode.Sint, (sbyte)-5);
        var decoded = LogixPrimitiveCodec.Decode(LogixDataTypeCode.Sint, encoded);

        Assert.Equal([0xFB], encoded);
        Assert.Equal((sbyte)-5, decoded);
    }

    [Fact]
    public void Encode_and_decode_int_little_endian()
    {
        var encoded = LogixPrimitiveCodec.Encode(LogixDataTypeCode.Int, (short)1234);
        var decoded = LogixPrimitiveCodec.Decode(LogixDataTypeCode.Int, encoded);

        Assert.Equal([0xD2, 0x04], encoded);
        Assert.Equal((short)1234, decoded);
    }

    [Fact]
    public void Encode_and_decode_dint_little_endian()
    {
        var encoded = LogixPrimitiveCodec.Encode(LogixDataTypeCode.Dint, 123456);
        var decoded = LogixPrimitiveCodec.Decode(LogixDataTypeCode.Dint, encoded);

        Assert.Equal([0x40, 0xE2, 0x01, 0x00], encoded);
        Assert.Equal(123456, decoded);
    }

    [Fact]
    public void Encode_and_decode_lint_little_endian()
    {
        var encoded = LogixPrimitiveCodec.Encode(LogixDataTypeCode.Lint, 1234567890123L);
        var decoded = LogixPrimitiveCodec.Decode(LogixDataTypeCode.Lint, encoded);

        Assert.Equal([0xCB, 0x04, 0xFB, 0x71, 0x1F, 0x01, 0x00, 0x00], encoded);
        Assert.Equal(1234567890123L, decoded);
    }

    [Fact]
    public void DecodeMany_decodes_dint_values()
    {
        byte[] data =
        [
            0x01, 0x00, 0x00, 0x00,
            0x02, 0x00, 0x00, 0x00,
            0x03, 0x00, 0x00, 0x00
        ];

        var decoded = LogixPrimitiveCodec.DecodeMany(LogixDataTypeCode.Dint, data, 3);

        Assert.Equal([1, 2, 3], decoded);
    }

    [Fact]
    public void EncodeMany_encodes_dint_values()
    {
        var encoded = LogixPrimitiveCodec.EncodeMany(
            LogixDataTypeCode.Dint,
            [1, 2, 3]);

        Assert.Equal(
            [
                0x01, 0x00, 0x00, 0x00,
                0x02, 0x00, 0x00, 0x00,
                0x03, 0x00, 0x00, 0x00
            ],
            encoded);
    }

    [Fact]
    public void EncodeMany_rejects_empty_values()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LogixPrimitiveCodec.EncodeMany(LogixDataTypeCode.Dint, []));
    }

    [Fact]
    public void DecodeMany_rejects_incomplete_data()
    {
        Assert.Throws<ArgumentException>(() =>
            LogixPrimitiveCodec.DecodeMany(LogixDataTypeCode.Dint, [0x01, 0x00], 1));
    }

    [Fact]
    public void DecodeMany_decodes_int_values()
    {
        byte[] data =
        [
            0x01, 0x00,
            0x02, 0x00,
            0x03, 0x00
        ];

        var decoded = LogixPrimitiveCodec.DecodeMany(LogixDataTypeCode.Int, data, 3);

        Assert.Equal([(short)1, (short)2, (short)3], decoded);
    }

    [Fact]
    public void Encode_and_decode_real_little_endian()
    {
        var encoded = LogixPrimitiveCodec.Encode(LogixDataTypeCode.Real, 12.5f);
        var decoded = LogixPrimitiveCodec.Decode(LogixDataTypeCode.Real, encoded);

        Assert.Equal([0x00, 0x00, 0x48, 0x41], encoded);
        Assert.Equal(12.5f, decoded);
    }

    [Fact]
    public void Encode_and_decode_logix_string()
    {
        var encoded = LogixPrimitiveCodec.Encode(LogixDataTypeCode.String, "RUN");
        var decoded = LogixPrimitiveCodec.Decode(LogixDataTypeCode.String, encoded);

        Assert.Equal(sizeof(int) + LogixPrimitiveCodec.LogixStringMaxLength, encoded.Length);
        Assert.Equal([0x03, 0x00, 0x00, 0x00, 0x52, 0x55, 0x4E], encoded[..7]);
        Assert.Equal("RUN", decoded);
    }

    [Fact]
    public void Encode_string_rejects_values_longer_than_logix_string_buffer()
    {
        var value = new string('A', LogixPrimitiveCodec.LogixStringMaxLength + 1);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LogixPrimitiveCodec.Encode(LogixDataTypeCode.String, value));
    }

    [Fact]
    public void Encode_string_rejects_non_ascii_values()
    {
        Assert.Throws<ArgumentException>(() =>
            LogixPrimitiveCodec.Encode(LogixDataTypeCode.String, "linea-ñ"));
    }
}
