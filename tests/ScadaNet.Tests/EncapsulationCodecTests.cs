using ScadaNet.EtherNetIp;

namespace ScadaNet.Tests;

public class EncapsulationCodecTests
{
    [Fact]
    public void Encode_register_session_request_matches_expected_bytes()
    {
        var bytes = RegisterSessionCodec.EncodeRequest(new RegisterSessionRequest());

        Assert.Equal(
            new byte[]
            {
                0x65, 0x00,
                0x04, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x01, 0x00,
                0x00, 0x00
            },
            bytes);
    }

    [Fact]
    public void Decode_register_session_response_reads_session_and_payload()
    {
        var response = new byte[]
        {
            0x65, 0x00,
            0x04, 0x00,
            0x78, 0x56, 0x34, 0x12,
            0x00, 0x00, 0x00, 0x00,
            0xAA, 0xBB, 0xCC, 0xDD,
            0x11, 0x22, 0x33, 0x44,
            0x00, 0x00, 0x00, 0x00,
            0x01, 0x00,
            0x00, 0x00
        };

        var decoded = RegisterSessionCodec.DecodeResponse(response);

        Assert.Equal(0x12345678u, decoded.SessionHandle);
        Assert.Equal(1, decoded.ProtocolVersion);
        Assert.Equal(0, decoded.Options);
    }
}
