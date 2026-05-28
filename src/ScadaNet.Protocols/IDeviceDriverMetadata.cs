namespace ScadaNet.Protocols;

public interface IDeviceDriverMetadata
{
    string ProtocolFamily { get; }

    string Transport { get; }

    IReadOnlyList<string> MessagingModes { get; }

    IReadOnlyList<ProtocolEndpointMetadata> DefaultEndpoints { get; }

    IReadOnlyList<int> DefaultPorts { get; }

    IReadOnlyList<string> Capabilities { get; }
}
