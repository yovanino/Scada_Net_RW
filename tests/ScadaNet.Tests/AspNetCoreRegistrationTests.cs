using Microsoft.Extensions.Configuration;
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
        var connectionFactory = provider.GetRequiredService<IDeviceConnectionFactory>();
        var runtime = provider.GetRequiredService<IPlcRuntime>();
        var drivers = provider.GetServices<IDeviceDriver>().ToArray();

        Assert.IsType<DiscoveryService>(discovery);
        Assert.IsType<DeviceRegistry>(registry);
        Assert.IsType<DeviceConnectionFactory>(connectionFactory);
        Assert.IsType<PlcRuntime>(runtime);
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

    [Fact]
    public void AddScadaNet_binds_devices_from_configuration()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationManager();

        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ScadaNet:Devices:0:Name"] = "line1-plc",
            ["ScadaNet:Devices:0:Driver"] = "ethernetip",
            ["ScadaNet:Devices:0:Address"] = "192.168.0.10",
            ["ScadaNet:Devices:0:Port"] = "44818",
            ["ScadaNet:Devices:0:Path"] = "1,0",
            ["ScadaNet:Devices:0:Timeout"] = "00:00:02"
        });

        services.AddScadaNet(configuration);

        using var provider = services.BuildServiceProvider();

        var registry = provider.GetRequiredService<IDeviceRegistry>();
        var device = registry.GetRequired("line1-plc");

        Assert.Equal("ethernetip", device.Driver);
        Assert.Equal("192.168.0.10", device.Address);
        Assert.Equal(44818, device.Port);
        Assert.Equal("1,0", device.Path);
        Assert.Equal(TimeSpan.FromSeconds(2), device.Timeout);
    }
}
