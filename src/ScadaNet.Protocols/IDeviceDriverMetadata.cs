namespace ScadaNet.Protocols;

public interface IDeviceDriverMetadata
{
    string ProtocolFamily { get; }

    string Transport { get; }

    IReadOnlyList<int> DefaultPorts { get; }

    IReadOnlyList<string> Capabilities { get; }
}
