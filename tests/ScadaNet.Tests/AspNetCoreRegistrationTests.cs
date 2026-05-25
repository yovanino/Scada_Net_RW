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
        var drivers = provider.GetServices<IDeviceDriver>().ToArray();

        Assert.IsType<DiscoveryService>(discovery);
        Assert.Contains(drivers, driver => driver is EtherNetIpDiscoveryDriver);
    }
}
