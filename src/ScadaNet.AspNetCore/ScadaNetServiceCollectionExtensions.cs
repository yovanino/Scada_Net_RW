using Microsoft.Extensions.DependencyInjection;
using ScadaNet.Runtime;

namespace ScadaNet.AspNetCore;

public static class ScadaNetServiceCollectionExtensions
{
    public static IServiceCollection AddScadaNet(
        this IServiceCollection services,
        Action<ScadaNetOptions>? configure = null)
    {
        var options = new ScadaNetOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IDeviceRegistry>(_ => new DeviceRegistry(options.Devices));
        services.AddSingleton<IDiscoveryService, DiscoveryService>();
        return services;
    }
}
