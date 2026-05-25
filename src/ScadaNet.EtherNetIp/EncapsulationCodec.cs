using System.Buffers.Binary;
using ScadaNet.Core;

namespace ScadaNet.EtherNetIp;

public static class EncapsulationCodec
{
    public static byte[] Encode(EncapsulationPacket packet)
    {
        if (packet.Header.SenderContext.Length != 8)
        {
            throw new ArgumentException("Sender context must be exactly 8 bytes.", nameof(packet));
        }

        if (packet.Payload.Length > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(packet), "Payload is too large for an EtherNet/IP encapsulation packet.");
        }

        var buffer = new byte[EncapsulationHeader.Size + packet.Payload.Length];
        var span = buffer.AsSpan();

        BinaryPrimitives.WriteUInt16LittleEndian(span[0..2], (ushort)packet.Header.Command);
        BinaryPrimitives.WriteUInt16LittleEndian(span[2..4], (ushort)packet.Payload.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(span[4..8], packet.Header.SessionHandle);
        BinaryPrimitives.WriteUInt32LittleEndian(span[8..12], packet.Header.Status);
        packet.Header.SenderContext.CopyTo(buffer, 12);
        BinaryPrimitives.WriteUInt32LittleEndian(span[20..24], packet.Header.Options);
        packet.Payload.CopyTo(buffer, EncapsulationHeader.Size);

        return buffer;
    }

    public static EncapsulationPacket Decode(ReadOnlySpan<byte> packet)
    {
        if (packet.Length < EncapsulationHeader.Size)
        {
            throw new ScadaNetException("EtherNet/IP encapsulation packet is shorter than the header size.");
        }

        var command = (EtherNetIpCommand)BinaryPrimitives.ReadUInt16LittleEndian(packet[0..2]);
        var length = BinaryPrimitives.ReadUInt16LittleEndian(packet[2..4]);
        var totalLength = EncapsulationHeader.Size + length;

        if (packet.Length < totalLength)
        {
            throw new ScadaNetException("EtherNet/IP encapsulation packet payload is incomplete.");
        }

        var senderContext = packet[12..20].ToArray();
        var payload = packet[EncapsulationHeader.Size..totalLength].ToArray();

        var header = new EncapsulationHeader(
            command,
            length,
            BinaryPrimitives.ReadUInt32LittleEndian(packet[4..8]),
            BinaryPrimitives.ReadUInt32LittleEndian(packet[8..12]),
            senderContext,
            BinaryPrimitives.ReadUInt32LittleEndian(packet[20..24]));

        return new EncapsulationPacket(header, payload);
    }
}
