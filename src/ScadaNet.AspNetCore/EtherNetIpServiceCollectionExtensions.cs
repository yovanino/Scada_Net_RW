using Microsoft.Extensions.DependencyInjection;
using ScadaNet.EtherNetIp;
using ScadaNet.Protocols;

namespace ScadaNet.AspNetCore;

public static class EtherNetIpServiceCollectionExtensions
{
    public static IServiceCollection AddEtherNetIpDiscovery(
        this IServiceCollection services)
    {
        services.AddSingleton<IDeviceDriver, EtherNetIpDiscoveryDriver>();
        return services;
    }
}
