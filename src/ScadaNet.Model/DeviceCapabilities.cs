namespace ScadaNet.Model;

[Flags]
public enum DeviceCapabilities
{
    None = 0,
    Read = 1,
    Write = 2,
    ReadMany = 4,
    Subscribe = 8,
    Poll = 16,
    Discover = 32,
    Metadata = 64,
    UdtMetadata = 128,
    ImplicitIo = 256,
    ReadArray = 512,
    WriteArray = 1024
}
