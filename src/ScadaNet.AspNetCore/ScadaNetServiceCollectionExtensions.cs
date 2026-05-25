using Microsoft.Extensions.DependencyInjection;

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
        return services;
    }
}
