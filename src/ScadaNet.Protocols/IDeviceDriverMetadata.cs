namespace ScadaNet.Protocols;

public interface IDeviceDriverMetadata
{
    string ProtocolFamily { get; }

    IReadOnlyList<int> DefaultPorts { get; }

    IReadOnlyList<string> Capabilities { get; }
}
