namespace ScadaNet.Logix;

public sealed record LogixWriteTagRequest(
    string TagName,
    LogixDataTypeCode DataType,
    ushort ElementCount,
    byte[] Data);
