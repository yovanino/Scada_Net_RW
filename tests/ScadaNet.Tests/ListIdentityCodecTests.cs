using System.Text;
using ScadaNet.EtherNetIp;

namespace ScadaNet.Tests;

public class ListIdentityCodecTests
{
    [Fact]
    public void Encode_list_identity_request_matches_expected_bytes()
    {
        var bytes = ListIdentityCodec.EncodeRequest();

        Assert.Equal(
            new byte[]
            {
                0x63, 0x00,
                0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00
            },
            bytes);
    }

    [Fact]
    public void Decode_list_identity_response_reads_identity_item()
    {
        var productName = Encoding.ASCII.GetBytes("CompactLogix");
        var itemLength = 34 + productName.Length;
        var payloadLength = 2 + 4 + itemLength;
        var packet = new byte[24 + payloadLength];

        packet[0] = 0x63;
        packet[2] = (byte)payloadLength;
        packet[24] = 0x01;
        packet[26] = 0x0C;
        packet[28] = (byte)itemLength;

        var itemOffset = 30;
        packet[itemOffset] = 0x01;
        packet[itemOffset + 1] = 0x00;
        packet[itemOffset + 2] = 0x01;
        packet[itemOffset + 3] = 0x00;
        packet[itemOffset + 4] = 0x0E;
        packet[itemOffset + 5] = 0x00;
        packet[itemOffset + 6] = 0x54;
        packet[itemOffset + 7] = 0x00;
        packet[itemOffset + 8] = 0x21;
        packet[itemOffset + 9] = 0x03;
        packet[itemOffset + 10] = 0x34;
        packet[itemOffset + 11] = 0x12;
        packet[itemOffset + 12] = 0x78;
        packet[itemOffset + 13] = 0x56;
        packet[itemOffset + 14] = 0x34;
        packet[itemOffset + 15] = 0x12;
        packet[itemOffset + 32] = (byte)productName.Length;
        productName.CopyTo(packet, itemOffset + 33);
        packet[itemOffset + 33 + productName.Length] = 0x01;

        var identities = ListIdentityCodec.DecodeResponse(packet);

        var identity = Assert.Single(identities);
        Assert.Equal(1, identity.VendorId);
        Assert.Equal(0x000E, identity.DeviceType);
        Assert.Equal(0x0054, identity.ProductCode);
        Assert.Equal(0x21, identity.RevisionMajor);
        Assert.Equal(0x03, identity.RevisionMinor);
        Assert.Equal(0x1234, identity.Status);
        Assert.Equal(0x12345678u, identity.SerialNumber);
        Assert.Equal("CompactLogix", identity.ProductName);
        Assert.Equal(1, identity.State);
    }
}
