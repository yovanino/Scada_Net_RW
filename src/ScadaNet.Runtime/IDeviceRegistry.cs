namespace ScadaNet.Runtime;

public interface IDeviceRegistry
{
    IReadOnlyCollection<DeviceDefinition> Devices { get; }

    DeviceDefinition GetRequired(string name);

    bool TryGet(string name, out DeviceDefinition device);
}
