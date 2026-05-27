namespace ScadaNet.Protocols;

public interface IDeviceDriverMetadata
{
    IReadOnlyList<int> DefaultPorts { get; }

    IReadOnlyList<string> Capabilities { get; }
}
