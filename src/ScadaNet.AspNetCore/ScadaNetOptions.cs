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
        TimeSpan? interval = null,
        IEnumerable<string>? signalNames = null)
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

        foreach (var signalName in signalNames ?? [])
        {
            group.SignalNames.Add(signalName);
        }

        PollingGroups.Add(group);
    }

    public void AddSignal(
        string deviceName,
        string name,
        string address,
        string? dataType = null,
        string? unit = null,
        string? description = null,
        string? category = null,
        int? displayOrder = null,
        double? minValue = null,
        double? maxValue = null,
        bool isArray = false,
        ushort? elementCount = null,
        bool writable = false)
    {
        var device = Devices.FirstOrDefault(device => string.Equals(
            device.Name,
            deviceName,
            StringComparison.OrdinalIgnoreCase));

        if (device is null)
        {
            throw new InvalidOperationException($"Device '{deviceName}' is not registered.");
        }

        device.Signals.Add(new DeviceSignalDefinition
        {
            Name = name,
            Address = address,
            DataType = dataType,
            Unit = unit,
            Description = description,
            Category = category,
            DisplayOrder = displayOrder,
            MinValue = minValue,
            MaxValue = maxValue,
            IsArray = isArray,
            ElementCount = elementCount,
            Writable = writable
        });
    }
}
