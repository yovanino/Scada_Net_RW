using System.Buffers.Binary;
using ScadaNet.Core;

namespace ScadaNet.Logix;

public static class LogixMessageCodec
{
    public static byte[] EncodeReadTag(LogixReadTagRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TagName);

        if (request.ElementCount == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.ElementCount,
                "Read tag element count must be greater than zero.");
        }

        var path = LogixTagPath.Encode(request.TagName);
        var buffer = new byte[2 + path.Length + sizeof(ushort)];
        var span = buffer.AsSpan();

        span[0] = LogixServiceCodes.ReadTag;
        span[1] = GetPathWordCount(path);
        path.CopyTo(buffer, 2);
        BinaryPrimitives.WriteUInt16LittleEndian(span[(2 + path.Length)..], request.ElementCount);

        return buffer;
    }

    public static byte[] EncodeWriteTag(LogixWriteTagRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TagName);
        ArgumentNullException.ThrowIfNull(request.Data);

        if (request.ElementCount == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.ElementCount,
                "Write tag element count must be greater than zero.");
        }

        var path = LogixTagPath.Encode(request.TagName);
        var buffer = new byte[2 + path.Length + sizeof(ushort) + sizeof(ushort) + request.Data.Length];
        var span = buffer.AsSpan();
        var offset = 0;

        span[offset++] = LogixServiceCodes.WriteTag;
        span[offset++] = GetPathWordCount(path);
        path.CopyTo(buffer, offset);
        offset += path.Length;
        BinaryPrimitives.WriteUInt16LittleEndian(span[offset..], (ushort)request.DataType);
        offset += sizeof(ushort);
        BinaryPrimitives.WriteUInt16LittleEndian(span[offset..], request.ElementCount);
        offset += sizeof(ushort);
        request.Data.CopyTo(buffer, offset);

        return buffer;
    }

    public static byte[] EncodeWriteTag(
        string tagName,
        LogixDataTypeCode dataType,
        object? value,
        ushort elementCount = 1)
    {
        return EncodeWriteTag(new LogixWriteTagRequest(
            tagName,
            dataType,
            elementCount,
            LogixPrimitiveCodec.Encode(dataType, value)));
    }

    public static LogixReadTagResponse DecodeReadTagResponse(ReadOnlySpan<byte> response)
    {
        var status = DecodeStatus(response, out var offset);

        if (!status.Succeeded)
        {
            return new LogixReadTagResponse(status, DataType: null, Data: []);
        }

        if (response.Length < offset + sizeof(ushort))
        {
            throw new ScadaNetException("Logix read tag response is missing the data type.");
        }

        var dataType = (LogixDataTypeCode)BinaryPrimitives.ReadUInt16LittleEndian(response[offset..]);
        offset += sizeof(ushort);

        return new LogixReadTagResponse(
            status,
            dataType,
            response[offset..].ToArray());
    }

    public static LogixWriteTagResponse DecodeWriteTagResponse(ReadOnlySpan<byte> response)
    {
        return new LogixWriteTagResponse(DecodeStatus(response, out _));
    }

    private static LogixResponseStatus DecodeStatus(
        ReadOnlySpan<byte> response,
        out int offset)
    {
        if (response.Length < 4)
        {
            throw new ScadaNetException("Logix response is shorter than the CIP response header.");
        }

        var generalStatus = response[2];
        var additionalStatusSize = response[3];
        offset = 4;

        if (response.Length < offset + additionalStatusSize * sizeof(ushort))
        {
            throw new ScadaNetException("Logix response additional status is incomplete.");
        }

        var additionalStatus = new ushort[additionalStatusSize];

        for (var index = 0; index < additionalStatus.Length; index++)
        {
            additionalStatus[index] = BinaryPrimitives.ReadUInt16LittleEndian(response[offset..]);
            offset += sizeof(ushort);
        }

        return new LogixResponseStatus(generalStatus, additionalStatus);
    }

    private static byte GetPathWordCount(byte[] path)
    {
        if (path.Length % 2 != 0)
        {
            throw new ArgumentException("Logix encoded paths must be word aligned.", nameof(path));
        }

        var words = path.Length / 2;

        if (words > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(path),
                words,
                "Logix encoded paths cannot exceed 255 words.");
        }

        return (byte)words;
    }
}
