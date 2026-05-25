using System.Buffers.Binary;
using ScadaNet.Core;

namespace ScadaNet.EtherNetIp;

public sealed record RegisterSessionRequest(ushort ProtocolVersion = 1, ushort Options = 0);

public sealed record RegisterSessionResponse(
    uint SessionHandle,
    ushort ProtocolVersion,
    ushort Options);

public static class RegisterSessionCodec
{
    public static byte[] EncodeRequest(RegisterSessionRequest request)
    {
        var payload = new byte[4];
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(0, 2), request.ProtocolVersion);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(2, 2), request.Options);

        return EncapsulationCodec.Encode(new EncapsulationPacket(
            EncapsulationHeader.Empty(EtherNetIpCommand.RegisterSession, (ushort)payload.Length),
            payload));
    }

    public static RegisterSessionResponse DecodeResponse(ReadOnlySpan<byte> response)
    {
        var packet = EncapsulationCodec.Decode(response);

        if (packet.Header.Command != EtherNetIpCommand.RegisterSession)
        {
            throw new ScadaNetException($"Expected RegisterSession response but received {packet.Header.Command}.");
        }

        if (packet.Header.Status != 0)
        {
            throw new ScadaNetException($"RegisterSession failed with encapsulation status 0x{packet.Header.Status:X8}.");
        }

        if (packet.Payload.Length < 4)
        {
            throw new ScadaNetException("RegisterSession response payload is incomplete.");
        }

        return new RegisterSessionResponse(
            packet.Header.SessionHandle,
            BinaryPrimitives.ReadUInt16LittleEndian(packet.Payload.AsSpan(0, 2)),
            BinaryPrimitives.ReadUInt16LittleEndian(packet.Payload.AsSpan(2, 2)));
    }
}
