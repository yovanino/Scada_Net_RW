using ScadaNet.Logix;

namespace ScadaNet.Tests;

public class LogixMessageCodecTests
{
    [Fact]
    public void EncodeReadTag_creates_read_tag_request()
    {
        var encoded = LogixMessageCodec.EncodeReadTag(new LogixReadTagRequest("Counter"));

        Assert.Equal(
            [
                0x4C, 0x05,
                0x91, 0x07, (byte)'C', (byte)'o', (byte)'u', (byte)'n', (byte)'t', (byte)'e', (byte)'r', 0x00,
                0x01, 0x00
            ],
            encoded);
    }

    [Fact]
    public void EncodeReadTag_supports_udt_member_path()
    {
        var encoded = LogixMessageCodec.EncodeReadTag(new LogixReadTagRequest("Motor.Speed"));

        Assert.Equal(0x4C, encoded[0]);
        Assert.Equal(0x08, encoded[1]);
    }

    [Fact]
    public void EncodeReadTag_supports_indexed_udt_member_path()
    {
        var encoded = LogixMessageCodec.EncodeReadTag(new LogixReadTagRequest("Recipe.Steps[260].Target"));

        Assert.Equal(0x4C, encoded[0]);
        Assert.Equal(0x0E, encoded[1]);
        Assert.Equal([0x01, 0x00], encoded[^2..]);
    }

    [Fact]
    public void EncodeWriteTag_creates_dint_write_tag_request()
    {
        var encoded = LogixMessageCodec.EncodeWriteTag(
            "Counter",
            LogixDataTypeCode.Dint,
            123456);

        Assert.Equal(
            [
                0x4D, 0x05,
                0x91, 0x07, (byte)'C', (byte)'o', (byte)'u', (byte)'n', (byte)'t', (byte)'e', (byte)'r', 0x00,
                0xC4, 0x00,
                0x01, 0x00,
                0x40, 0xE2, 0x01, 0x00
            ],
            encoded);
    }

    [Fact]
    public void DecodeReadTagResponse_returns_data_type_and_data()
    {
        byte[] response =
        [
            0xCC, 0x00, 0x00, 0x00,
            0xC4, 0x00,
            0x40, 0xE2, 0x01, 0x00
        ];

        var decoded = LogixMessageCodec.DecodeReadTagResponse(response);

        Assert.True(decoded.Status.Succeeded);
        Assert.Equal(LogixDataTypeCode.Dint, decoded.DataType);
        Assert.Equal(123456, decoded.DecodeValue());
    }

    [Fact]
    public void DecodeWriteTagResponse_returns_success_status()
    {
        byte[] response = [0xCD, 0x00, 0x00, 0x00];

        var decoded = LogixMessageCodec.DecodeWriteTagResponse(response);

        Assert.True(decoded.Status.Succeeded);
    }

    [Fact]
    public void DecodeWriteTagResponse_preserves_additional_status()
    {
        byte[] response = [0xCD, 0x00, 0x05, 0x01, 0x34, 0x12];

        var decoded = LogixMessageCodec.DecodeWriteTagResponse(response);

        Assert.False(decoded.Status.Succeeded);
        Assert.Equal(0x05, decoded.Status.GeneralStatus);
        Assert.Equal([0x1234], decoded.Status.AdditionalStatus);
    }
}
