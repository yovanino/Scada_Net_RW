using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScadaNet.AspNetCore;
using ScadaNet.EtherNetIp;
using ScadaNet.Logix;
using ScadaNet.Model;
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
        var signalResolver = provider.GetRequiredService<IDeviceSignalResolver>();
        var signalSnapshotReader = provider.GetRequiredService<IDeviceSignalSnapshotReader>();
        var connectionFactory = provider.GetRequiredService<IDeviceConnectionFactory>();
        var connectionPool = provider.GetRequiredService<IDeviceConnectionPool>();
        var snapshots = provider.GetRequiredService<ISignalSnapshotStore>();
        var pollingGroups = provider.GetRequiredService<IPollingGroupRegistry>();
        var pollingStatuses = provider.GetRequiredService<IPollingStatusStore>();
        var pollingMonitor = provider.GetRequiredService<IPollingGroupMonitor>();
        var writeAudit = provider.GetRequiredService<IWriteAuditStore>();
        var health = provider.GetRequiredService<IDeviceHealthService>();
        var dashboards = provider.GetRequiredService<IDeviceDashboardService>();
        var polling = provider.GetRequiredService<ISignalPollingService>();
        var runtime = provider.GetRequiredService<IPlcRuntime>();
        var hostedServices = provider.GetServices<IHostedService>().ToArray();
        var drivers = provider.GetServices<IDeviceDriver>().ToArray();

        Assert.IsType<DiscoveryService>(discovery);
        Assert.IsType<DeviceRegistry>(registry);
        Assert.IsType<DeviceSignalResolver>(signalResolver);
        Assert.IsType<DeviceSignalSnapshotReader>(signalSnapshotReader);
        Assert.IsType<DeviceConnectionFactory>(connectionFactory);
        Assert.IsType<DeviceConnectionPool>(connectionPool);
        Assert.IsType<SignalSnapshotStore>(snapshots);
        Assert.IsType<PollingGroupRegistry>(pollingGroups);
        Assert.IsType<PollingStatusStore>(pollingStatuses);
        Assert.IsType<PollingGroupMonitor>(pollingMonitor);
        Assert.IsType<WriteAuditStore>(writeAudit);
        Assert.IsType<DeviceHealthService>(health);
        Assert.IsType<DeviceDashboardService>(dashboards);
        Assert.IsType<SignalPollingService>(polling);
        Assert.IsType<PlcRuntime>(runtime);
        Assert.Contains(hostedServices, service => service is ScadaNetPollingHostedService);
        Assert.Contains(drivers, driver => driver is EtherNetIpDiscoveryDriver);
    }

    [Fact]
    public void AddLogix_registers_logix_driver()
    {
        var services = new ServiceCollection();

        services
            .AddScadaNet()
            .AddLogix();

        using var provider = services.BuildServiceProvider();

        var drivers = provider.GetServices<IDeviceDriver>().ToArray();

        Assert.Contains(drivers, driver => driver is LogixDriver);
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
    public void AddScadaNet_validates_configuration()
    {
        var services = new ServiceCollection();

        var error = Assert.Throws<ScadaNetOptionsValidationException>(() =>
            services.AddScadaNet(options =>
            {
                options.AddDevice("line1-plc", "logix", address: string.Empty);
            }));

        Assert.Contains(error.Errors, item => item.Contains("address cannot be empty"));
    }

    [Fact]
    public void AddScadaNet_binds_devices_from_configuration()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationManager();

        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ScadaNet:BackgroundPollingEnabled"] = "false",
            ["ScadaNet:BackgroundPollingMaxConcurrency"] = "3",
            ["ScadaNet:BackgroundPollingTickInterval"] = "00:00:00.500",
            ["ScadaNet:WriteAuditMaxRecords"] = "250",
            ["ScadaNet:Devices:0:Name"] = "line1-plc",
            ["ScadaNet:Devices:0:Driver"] = "ethernetip",
            ["ScadaNet:Devices:0:Address"] = "192.168.0.10",
            ["ScadaNet:Devices:0:Port"] = "44818",
            ["ScadaNet:Devices:0:Path"] = "1,0",
            ["ScadaNet:Devices:0:Timeout"] = "00:00:02",
            ["ScadaNet:Devices:0:WritesEnabled"] = "true",
            ["ScadaNet:Devices:0:WritableAddresses:0"] = "ResetCommand",
            ["ScadaNet:Devices:0:Signals:0:Name"] = "production-counter",
            ["ScadaNet:Devices:0:Signals:0:Address"] = "ProductionCounter",
            ["ScadaNet:Devices:0:Signals:0:DataType"] = "DINT",
            ["ScadaNet:Devices:0:Signals:0:Unit"] = "parts",
            ["ScadaNet:Devices:0:Signals:0:Description"] = "Good parts counter",
            ["ScadaNet:Devices:0:Signals:0:Category"] = "OEE",
            ["ScadaNet:Devices:0:Signals:0:DisplayOrder"] = "10",
            ["ScadaNet:Devices:0:Signals:0:MinValue"] = "0",
            ["ScadaNet:Devices:0:Signals:0:MaxValue"] = "999999",
            ["ScadaNet:Devices:0:Signals:0:IsArray"] = "true",
            ["ScadaNet:Devices:0:Signals:0:ElementCount"] = "10",
            ["ScadaNet:Devices:0:Signals:0:Writable"] = "false",
            ["ScadaNet:PollingGroups:0:Name"] = "line1-fast",
            ["ScadaNet:PollingGroups:0:DeviceName"] = "line1-plc",
            ["ScadaNet:PollingGroups:0:Interval"] = "00:00:01",
            ["ScadaNet:PollingGroups:0:Addresses:0"] = "ProductionCounter",
            ["ScadaNet:PollingGroups:0:Addresses:1"] = "Motor.Speed",
            ["ScadaNet:PollingGroups:0:SignalNames:0"] = "production-counter"
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
        Assert.True(device.WritesEnabled);
        Assert.Equal(["ResetCommand"], device.WritableAddresses);
        var signal = Assert.Single(device.Signals);
        Assert.Equal("production-counter", signal.Name);
        Assert.Equal("ProductionCounter", signal.Address);
        Assert.Equal("DINT", signal.DataType);
        Assert.Equal("parts", signal.Unit);
        Assert.Equal("Good parts counter", signal.Description);
        Assert.Equal("OEE", signal.Category);
        Assert.Equal(10, signal.DisplayOrder);
        Assert.Equal(0, signal.MinValue);
        Assert.Equal(999999, signal.MaxValue);
        Assert.True(signal.IsArray);
        Assert.Equal((ushort)10, signal.ElementCount);
        Assert.False(signal.Writable);

        var options = provider.GetRequiredService<ScadaNetOptions>();
        Assert.False(options.BackgroundPollingEnabled);
        Assert.Equal(3, options.BackgroundPollingMaxConcurrency);
        Assert.Equal(TimeSpan.FromMilliseconds(500), options.BackgroundPollingTickInterval);
        Assert.Equal(250, options.WriteAuditMaxRecords);
        var group = Assert.Single(options.PollingGroups);
        Assert.Equal("line1-fast", group.Name);
        Assert.Equal("line1-plc", group.DeviceName);
        Assert.Equal(TimeSpan.FromSeconds(1), group.Interval);
        Assert.Equal(["ProductionCounter", "Motor.Speed"], group.Addresses);
        Assert.Equal(["production-counter"], group.SignalNames);

        var pollingGroups = provider.GetRequiredService<IPollingGroupRegistry>();
        Assert.True(pollingGroups.TryGet("LINE1-FAST", out var registeredGroup));
        Assert.Equal("line1-plc", registeredGroup.DeviceName);

        var writeAudit = provider.GetRequiredService<IWriteAuditStore>();
        for (var index = 0; index < 300; index++)
        {
            writeAudit.Add(new WriteAuditRecord(
                0,
                DateTimeOffset.UtcNow,
                new SignalRef("line1-plc", $"Command{index}"),
                index,
                Succeeded: true,
                Error: null));
        }

        Assert.Equal(250, writeAudit.GetRecent(count: 300).Count);
    }

    [Fact]
    public void AddScadaNet_can_disable_background_polling_hosted_service()
    {
        var services = new ServiceCollection();

        services.AddScadaNet(options =>
        {
            options.BackgroundPollingEnabled = false;
        });

        using var provider = services.BuildServiceProvider();

        var hostedServices = provider.GetServices<IHostedService>().ToArray();

        Assert.DoesNotContain(hostedServices, service => service is ScadaNetPollingHostedService);
        Assert.IsType<SignalPollingService>(provider.GetRequiredService<ISignalPollingService>());
    }

    [Fact]
    public void AddScadaNet_validates_duplicate_signal_names()
    {
        var services = new ServiceCollection();

        var error = Assert.Throws<ScadaNetOptionsValidationException>(() =>
            services.AddScadaNet(options =>
            {
                options.AddDevice("line1-plc", "logix", "192.168.0.10");
                options.AddSignal("line1-plc", "counter", "ProductionCounter");
                options.AddSignal("line1-plc", "COUNTER", "OtherCounter");
            }));

        Assert.Contains(error.Errors, item => item.Contains("signal 'COUNTER' more than once"));
    }
}
