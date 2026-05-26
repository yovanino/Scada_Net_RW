using Microsoft.Extensions.DependencyInjection;
using ScadaNet.Logix;
using ScadaNet.Protocols;

namespace ScadaNet.AspNetCore;

public static class LogixServiceCollectionExtensions
{
    public static IServiceCollection AddLogix(
        this IServiceCollection services)
    {
        services.AddSingleton<IDeviceDriver, LogixDriver>();
        return services;
    }
}
