using ScadaNet.Logix;

namespace ScadaNet.Tests;

public class LogixUnconnectedSendCodecTests
{
    [Fact]
    public void EncodeRequest_wraps_logix_message_for_connection_manager()
    {
        var encoded = LogixUnconnectedSendCodec.EncodeRequest(
            new LogixUnconnectedSendRequest([0x4C, 0x00], "1,0"));

        Assert.Equal(
            [
                0x52, 0x02,
                0x20, 0x06,
                0x24, 0x01,
                0x0A, 0x0E,
                0x02, 0x00,
                0x4C, 0x00,
                0x01, 0x00,
                0x01, 0x00
            ],
            encoded);
    }

    [Fact]
    public void EncodeRequest_pads_odd_message_size()
    {
        var encoded = LogixUnconnectedSendCodec.EncodeRequest(
            new LogixUnconnectedSendRequest([0x4C], "1,0"));

        Assert.Equal(
            [
                0x52, 0x02,
                0x20, 0x06,
                0x24, 0x01,
                0x0A, 0x0E,
                0x01, 0x00,
                0x4C, 0x00,
                0x01, 0x00,
                0x01, 0x00
            ],
            encoded);
    }

    [Fact]
    public void DecodeResponse_unwraps_embedded_logix_response()
    {
        var decoded = LogixUnconnectedSendCodec.DecodeResponse(
            [0xD2, 0x00, 0x00, 0x00, 0xCC, 0x00]);

        Assert.Equal([0xCC, 0x00], decoded);
    }
}
