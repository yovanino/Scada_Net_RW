namespace ScadaNet.AspNetCore;

public sealed class ScadaNetOptions
{
    public IList<DeviceRegistration> Devices { get; } = [];

    public void AddDevice(string name, string driver, string address)
    {
        Devices.Add(new DeviceRegistration(name, driver, address));
    }
}

public sealed record DeviceRegistration(
    string Name,
    string Driver,
    string Address);
