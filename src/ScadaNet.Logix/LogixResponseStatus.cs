namespace ScadaNet.Logix;

public sealed record LogixResponseStatus(
    byte GeneralStatus,
    IReadOnlyList<ushort> AdditionalStatus)
{
    public bool Succeeded => GeneralStatus == 0;
}
