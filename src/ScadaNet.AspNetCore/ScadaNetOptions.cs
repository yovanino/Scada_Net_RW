using ScadaNet.Runtime;

namespace ScadaNet.AspNetCore;

public sealed class ScadaNetOptions
{
    public const string SectionName = "ScadaNet";

    public IList<DeviceDefinition> Devices { get; } = [];
    public IList<SignalPollingGroupDefinition> PollingGroups { get; } = [];

    public void AddDevice(
        string name,
        string driver,
        string address,
        int? port = null,
        string? path = null,
        TimeSpan? timeout = null,
        bool writesEnabled = false,
        IEnumerable<string>? writableAddresses = null)
    {
        var device = new DeviceDefinition(name, driver, address)
        {
            Port = port,
            Path = path,
            Timeout = timeout ?? TimeSpan.FromSeconds(3),
            WritesEnabled = writesEnabled
        };

        foreach (var writableAddress in writableAddresses ?? [])
        {
            device.WritableAddresses.Add(writableAddress);
        }

        Devices.Add(device);
    }

    public void AddPollingGroup(
        string name,
        string deviceName,
        IEnumerable<string> addresses,
        TimeSpan? interval = null)
    {
        var group = new SignalPollingGroupDefinition
        {
            Name = name,
            DeviceName = deviceName,
            Interval = interval ?? TimeSpan.FromSeconds(1)
        };

        foreach (var address in addresses)
        {
            group.Addresses.Add(address);
        }

        PollingGroups.Add(group);
    }
}
