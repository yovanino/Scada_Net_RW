namespace ScadaNet.EtherNetIp;

public sealed record EncapsulationHeader(
    EtherNetIpCommand Command,
    ushort Length,
    uint SessionHandle,
    uint Status,
    byte[] SenderContext,
    uint Options)
{
    public const int Size = 24;

    public static EncapsulationHeader Empty(EtherNetIpCommand command, ushort length)
    {
        return new EncapsulationHeader(
            command,
            length,
            SessionHandle: 0,
            Status: 0,
            SenderContext: new byte[8],
            Options: 0);
    }
}
