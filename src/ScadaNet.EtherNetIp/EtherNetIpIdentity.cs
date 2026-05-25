namespace ScadaNet.EtherNetIp;

public sealed record EtherNetIpIdentity(
    ushort VendorId,
    ushort DeviceType,
    ushort ProductCode,
    byte RevisionMajor,
    byte RevisionMinor,
    ushort Status,
    uint SerialNumber,
    string ProductName,
    byte State);
