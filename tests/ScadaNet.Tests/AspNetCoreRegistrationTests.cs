using Microsoft.Extensions.DependencyInjection;
using ScadaNet.AspNetCore;
using ScadaNet.EtherNetIp;
using ScadaNet.Protocols;
using ScadaNet.Runtime;

namespace ScadaNet.Tests;

public class AspNetCoreRegistrationTests
{
    [Fact]
    public void AddScadaNet_and_AddEtherNetIpDiscovery_register_discovery_services()
    {
        var services = new ServiceCollection();

        services
            .AddScadaNet()
            .AddEtherNetIpDiscovery();

        using var provider = services.BuildServiceProvider();

        var discovery = provider.GetRequiredService<IDiscoveryService>();
        var registry = provider.GetRequiredService<IDeviceRegistry>();
        var drivers = provider.GetServices<IDeviceDriver>().ToArray();

        Assert.IsType<DiscoveryService>(discovery);
        Assert.IsType<DeviceRegistry>(registry);
        Assert.Contains(drivers, driver => driver is EtherNetIpDiscoveryDriver);
    }

    [Fact]
    public void AddScadaNet_registers_configured_devices()
    {
        var services = new ServiceCollection();

        services.AddScadaNet(options =>
        {
            options.AddDevice("line1-plc", "ethernetip", "192.168.0.10", port: 44818);
        });

        using var provider = services.BuildServiceProvider();

        var registry = provider.GetRequiredService<IDeviceRegistry>();
        var device = registry.GetRequired("LINE1-PLC");

        Assert.Equal("ethernetip", device.Driver);
        Assert.Equal("192.168.0.10", device.Address);
        Assert.Equal(44818, device.Port);
    }
}
