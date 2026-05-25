namespace ScadaNet.EtherNetIp;

public sealed record EncapsulationPacket(
    EncapsulationHeader Header,
    byte[] Payload);
