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
    public void Encode_and_decode_dint_little_endian()
    {
        var encoded = LogixPrimitiveCodec.Encode(LogixDataTypeCode.Dint, 123456);
        var decoded = LogixPrimitiveCodec.Decode(LogixDataTypeCode.Dint, encoded);

        Assert.Equal([0x40, 0xE2, 0x01, 0x00], encoded);
        Assert.Equal(123456, decoded);
    }

    [Fact]
    public void Encode_and_decode_real_little_endian()
    {
        var encoded = LogixPrimitiveCodec.Encode(LogixDataTypeCode.Real, 12.5f);
        var decoded = LogixPrimitiveCodec.Decode(LogixDataTypeCode.Real, encoded);

        Assert.Equal([0x00, 0x00, 0x48, 0x41], encoded);
        Assert.Equal(12.5f, decoded);
    }
}
