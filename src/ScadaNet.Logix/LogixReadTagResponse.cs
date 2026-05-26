namespace ScadaNet.Logix;

public sealed record LogixReadTagResponse(
    LogixResponseStatus Status,
    LogixDataTypeCode? DataType,
    byte[] Data)
{
    public object? DecodeValue()
    {
        return DataType.HasValue
            ? LogixPrimitiveCodec.Decode(DataType.Value, Data)
            : null;
    }
}
