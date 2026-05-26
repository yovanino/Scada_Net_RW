using System.Buffers.Binary;
using ScadaNet.Core;

namespace ScadaNet.EtherNetIp;

public sealed record SendRRDataRequest(byte[] Data, ushort Timeout = 0);

public sealed record SendRRDataResponse(byte[] Data);

public static class SendRRDataCodec
{
    private const ushort NullAddressItemType = 0x0000;
    private const ushort UnconnectedDataItemType = 0x00B2;

    public static byte[] EncodeRequest(
        uint sessionHandle,
        SendRRDataRequest request)
    {
        ArgumentNullException.ThrowIfNull(request.Data);

        if (request.Data.Length > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.Data.Length,
                "SendRRData payload is too large.");
        }

        var payloadLength = 4 + 2 + 2 + 4 + 4 + request.Data.Length;
        var payload = new byte[payloadLength];
        var span = payload.AsSpan();
        var offset = 0;

        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], 0);
        offset += sizeof(uint);
        BinaryPrimitives.WriteUInt16LittleEndian(span[offset..], request.Timeout);
        offset += sizeof(ushort);
        BinaryPrimitives.WriteUInt16LittleEndian(span[offset..], 2);
        offset += sizeof(ushort);

        BinaryPrimitives.WriteUInt16LittleEndian(span[offset..], NullAddressItemType);
        offset += sizeof(ushort);
        BinaryPrimitives.WriteUInt16LittleEndian(span[offset..], 0);
        offset += sizeof(ushort);

        BinaryPrimitives.WriteUInt16LittleEndian(span[offset..], UnconnectedDataItemType);
        offset += sizeof(ushort);
        BinaryPrimitives.WriteUInt16LittleEndian(span[offset..], (ushort)request.Data.Length);
        offset += sizeof(ushort);
        request.Data.CopyTo(payload, offset);

        return EncapsulationCodec.Encode(new EncapsulationPacket(
            new EncapsulationHeader(
                EtherNetIpCommand.SendRRData,
                (ushort)payload.Length,
                sessionHandle,
                Status: 0,
                SenderContext: new byte[8],
                Options: 0),
            payload));
    }

    public static SendRRDataResponse DecodeResponse(ReadOnlySpan<byte> response)
    {
        var packet = EncapsulationCodec.Decode(response);

        if (packet.Header.Command != EtherNetIpCommand.SendRRData)
        {
            throw new ScadaNetException($"Expected SendRRData response but received {packet.Header.Command}.");
        }

        if (packet.Header.Status != 0)
        {
            throw new ScadaNetException($"SendRRData failed with encapsulation status 0x{packet.Header.Status:X8}.");
        }

        var payload = packet.Payload.AsSpan();

        if (payload.Length < 8)
        {
            throw new ScadaNetException("SendRRData response payload is incomplete.");
        }

        var itemCount = BinaryPrimitives.ReadUInt16LittleEndian(payload[6..8]);
        var offset = 8;

        for (var index = 0; index < itemCount; index++)
        {
            if (payload.Length < offset + 4)
            {
                throw new ScadaNetException("SendRRData response item header is incomplete.");
            }

            var itemType = BinaryPrimitives.ReadUInt16LittleEndian(payload[offset..]);
            var itemLength = BinaryPrimitives.ReadUInt16LittleEndian(payload[(offset + 2)..]);
            offset += 4;

            if (payload.Length < offset + itemLength)
            {
                throw new ScadaNetException("SendRRData response item payload is incomplete.");
            }

            if (itemType == UnconnectedDataItemType)
            {
                return new SendRRDataResponse(payload.Slice(offset, itemLength).ToArray());
            }

            offset += itemLength;
        }

        throw new ScadaNetException("SendRRData response does not contain an unconnected data item.");
    }
}
