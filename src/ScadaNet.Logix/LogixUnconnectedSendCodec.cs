using System.Buffers.Binary;
using ScadaNet.Cip;
using ScadaNet.Core;

namespace ScadaNet.Logix;

public sealed record LogixUnconnectedSendRequest(
    byte[] Message,
    string RoutePath,
    byte PriorityTimeTick = 0x0A,
    byte TimeoutTicks = 0x0E);

public static class LogixUnconnectedSendCodec
{
    private const byte UnconnectedSendService = 0x52;
    private static readonly byte[] ConnectionManagerPath = [0x20, 0x06, 0x24, 0x01];

    public static byte[] EncodeRequest(LogixUnconnectedSendRequest request)
    {
        ArgumentNullException.ThrowIfNull(request.Message);

        if (request.Message.Length > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.Message.Length,
                "Logix unconnected message is too large.");
        }

        var routePath = CipRoutePath.Encode(request.RoutePath);

        if (routePath.Length % 2 != 0)
        {
            throw new ArgumentException("CIP route path must be word aligned.", nameof(request));
        }

        var messagePad = request.Message.Length % 2 == 0 ? 0 : 1;
        var buffer = new byte[
            2 +
            ConnectionManagerPath.Length +
            2 +
            sizeof(ushort) +
            request.Message.Length +
            messagePad +
            2 +
            routePath.Length];
        var span = buffer.AsSpan();
        var offset = 0;

        span[offset++] = UnconnectedSendService;
        span[offset++] = (byte)(ConnectionManagerPath.Length / 2);
        ConnectionManagerPath.CopyTo(buffer, offset);
        offset += ConnectionManagerPath.Length;

        span[offset++] = request.PriorityTimeTick;
        span[offset++] = request.TimeoutTicks;
        BinaryPrimitives.WriteUInt16LittleEndian(span[offset..], (ushort)request.Message.Length);
        offset += sizeof(ushort);
        request.Message.CopyTo(buffer, offset);
        offset += request.Message.Length + messagePad;

        span[offset++] = (byte)(routePath.Length / 2);
        span[offset++] = 0x00;
        routePath.CopyTo(buffer, offset);

        return buffer;
    }

    public static byte[] DecodeResponse(ReadOnlySpan<byte> response)
    {
        if (response.Length < 4)
        {
            throw new ScadaNetException("Logix unconnected send response is shorter than the CIP response header.");
        }

        if (response[0] != (UnconnectedSendService | 0x80))
        {
            throw new ScadaNetException(
                $"Expected Unconnected Send response but received service 0x{response[0]:X2}.");
        }

        var generalStatus = response[2];
        var additionalStatusSize = response[3];
        var offset = 4;

        if (response.Length < offset + additionalStatusSize * sizeof(ushort))
        {
            throw new ScadaNetException("Logix unconnected send additional status is incomplete.");
        }

        if (generalStatus != 0)
        {
            throw new ScadaNetException(
                $"Logix unconnected send failed with general status 0x{generalStatus:X2}.");
        }

        offset += additionalStatusSize * sizeof(ushort);
        return response[offset..].ToArray();
    }
}
