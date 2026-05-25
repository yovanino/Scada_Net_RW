namespace ScadaNet.EtherNetIp;

public enum EtherNetIpCommand : ushort
{
    Nop = 0x0000,
    ListServices = 0x0004,
    ListIdentity = 0x0063,
    ListInterfaces = 0x0064,
    RegisterSession = 0x0065,
    UnregisterSession = 0x0066,
    SendRRData = 0x006F,
    SendUnitData = 0x0070
}
