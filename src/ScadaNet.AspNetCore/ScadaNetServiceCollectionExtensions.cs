using Microsoft.Extensions.Configuration;
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

        return AddScadaNet(services, options);
    }

    public static IServiceCollection AddScadaNet(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<ScadaNetOptions>? configure = null)
    {
        var options = new ScadaNetOptions();
        configuration.GetSection(ScadaNetOptions.SectionName).Bind(options);
        configure?.Invoke(options);

        return AddScadaNet(services, options);
    }

    private static IServiceCollection AddScadaNet(
        IServiceCollection services,
        ScadaNetOptions options)
    {
        ScadaNetOptionsValidator.Validate(options);

        services.AddLogging();
        services.AddSingleton(options);
        services.AddSingleton<IDeviceRegistry>(_ => new DeviceRegistry(options.Devices));
        services.AddSingleton<IDeviceSignalResolver, DeviceSignalResolver>();
        services.AddSingleton<IDeviceSignalSnapshotReader, DeviceSignalSnapshotReader>();
        services.AddSingleton<IPollingGroupRegistry>(_ => new PollingGroupRegistry(options.PollingGroups));
        services.AddSingleton<IDeviceConnectionFactory, DeviceConnectionFactory>();
        services.AddSingleton<IDeviceConnectionPool, DeviceConnectionPool>();
        services.AddSingleton<ISignalSnapshotStore, SignalSnapshotStore>();
        services.AddSingleton<IPollingStatusStore, PollingStatusStore>();
        services.AddSingleton<IPollingGroupMonitor, PollingGroupMonitor>();
        services.AddSingleton<IWriteAuditStore, WriteAuditStore>();
        services.AddSingleton<IDeviceHealthService, DeviceHealthService>();
        services.AddSingleton<IDiscoveryService, DiscoveryService>();
        services.AddSingleton<IPlcRuntime, PlcRuntime>();
        services.AddSingleton<ISignalPollingService>(provider => new SignalPollingService(
            provider.GetRequiredService<IPlcRuntime>(),
            provider.GetRequiredService<IPollingStatusStore>(),
            provider.GetRequiredService<IDeviceSignalResolver>()));
        services.AddHostedService<ScadaNetPollingHostedService>();
        return services;
    }
}
