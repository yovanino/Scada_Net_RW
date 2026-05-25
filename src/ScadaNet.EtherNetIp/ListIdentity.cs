using System.Buffers.Binary;
using System.Text;
using ScadaNet.Core;

namespace ScadaNet.EtherNetIp;

public static class ListIdentityCodec
{
    public static byte[] EncodeRequest()
    {
        return EncapsulationCodec.Encode(new EncapsulationPacket(
            EncapsulationHeader.Empty(EtherNetIpCommand.ListIdentity, 0),
            []));
    }

    public static IReadOnlyList<EtherNetIpIdentity> DecodeResponse(ReadOnlySpan<byte> response)
    {
        var packet = EncapsulationCodec.Decode(response);

        if (packet.Header.Command != EtherNetIpCommand.ListIdentity)
        {
            throw new ScadaNetException($"Expected ListIdentity response but received {packet.Header.Command}.");
        }

        if (packet.Header.Status != 0)
        {
            throw new ScadaNetException($"ListIdentity failed with encapsulation status 0x{packet.Header.Status:X8}.");
        }

        var payload = packet.Payload.AsSpan();
        if (payload.Length < 2)
        {
            throw new ScadaNetException("ListIdentity response payload is incomplete.");
        }

        var itemCount = BinaryPrimitives.ReadUInt16LittleEndian(payload[0..2]);
        var offset = 2;
        var identities = new List<EtherNetIpIdentity>(itemCount);

        for (var index = 0; index < itemCount; index++)
        {
            if (payload.Length < offset + 4)
            {
                throw new ScadaNetException("ListIdentity item header is incomplete.");
            }

            var itemType = BinaryPrimitives.ReadUInt16LittleEndian(payload[offset..(offset + 2)]);
            var itemLength = BinaryPrimitives.ReadUInt16LittleEndian(payload[(offset + 2)..(offset + 4)]);
            offset += 4;

            if (payload.Length < offset + itemLength)
            {
                throw new ScadaNetException("ListIdentity item payload is incomplete.");
            }

            if (itemType == 0x000C)
            {
                identities.Add(DecodeIdentityItem(payload.Slice(offset, itemLength)));
            }

            offset += itemLength;
        }

        return identities;
    }

    private static EtherNetIpIdentity DecodeIdentityItem(ReadOnlySpan<byte> item)
    {
        const int fixedLengthBeforeName = 33;

        if (item.Length < fixedLengthBeforeName)
        {
            throw new ScadaNetException("Identity item is incomplete.");
        }

        var productNameLength = item[32];
        if (item.Length < fixedLengthBeforeName + productNameLength)
        {
            throw new ScadaNetException("Identity item product name is incomplete.");
        }

        return new EtherNetIpIdentity(
            BinaryPrimitives.ReadUInt16LittleEndian(item[2..4]),
            BinaryPrimitives.ReadUInt16LittleEndian(item[4..6]),
            BinaryPrimitives.ReadUInt16LittleEndian(item[6..8]),
            item[8],
            item[9],
            BinaryPrimitives.ReadUInt16LittleEndian(item[10..12]),
            BinaryPrimitives.ReadUInt32LittleEndian(item[12..16]),
            Encoding.ASCII.GetString(item.Slice(33, productNameLength)),
            item[33 + productNameLength]);
    }
}
