using ScadaNet.Runtime;

namespace ScadaNet.AspNetCore;

public sealed class ScadaNetOptions
{
    public const string SectionName = "ScadaNet";

    public IList<DeviceDefinition> Devices { get; } = [];

    public void AddDevice(
        string name,
        string driver,
        string address,
        int? port = null,
        string? path = null,
        TimeSpan? timeout = null)
    {
        Devices.Add(new DeviceDefinition(name, driver, address)
        {
            Port = port,
            Path = path,
            Timeout = timeout ?? TimeSpan.FromSeconds(3)
        });
    }
}
