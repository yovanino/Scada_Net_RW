namespace ScadaNet.Logix;

public sealed record LogixReadTagRequest(
    string TagName,
    ushort ElementCount = 1);
