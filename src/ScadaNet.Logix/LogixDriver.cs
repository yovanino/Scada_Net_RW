using System.Diagnostics;
using ScadaNet.Model;
using ScadaNet.Protocols;

namespace ScadaNet.Logix;

public sealed class LogixDriver : IDeviceDriver, IDeviceDriverMetadata
{
    public string DriverName => "Logix";

    public string ProtocolFamily => KnownProtocolFamilies.EtherNetIp;

    public string Transport => KnownTransportProtocols.Tcp;

    public IReadOnlyList<string> MessagingModes { get; } =
    [
        KnownMessagingModes.Explicit
    ];

    public IReadOnlyList<ProtocolEndpointMetadata> DefaultEndpoints { get; } =
    [
        new(
            EtherNetIp.EtherNetIpDefaults.ExplicitMessagingPort,
            KnownTransportProtocols.Tcp,
            KnownMessagingModes.Explicit)
    ];

    public IReadOnlyList<int> DefaultPorts { get; } = [EtherNetIp.EtherNetIpDefaults.ExplicitMessagingPort];

    public IReadOnlyList<string> Capabilities { get; } =
    [
        KnownDiscoveryCapabilities.LogixTags,
        KnownDiscoveryCapabilities.Read,
        KnownDiscoveryCapabilities.Write,
        KnownDiscoveryCapabilities.ReadMany,
        KnownDiscoveryCapabilities.Arrays
    ];

    public ValueTask<IDeviceConnection> ConnectAsync(
        DeviceConnectionOptions options,
        CancellationToken cancellationToken = default)
    {
        var client = new LogixClient(new LogixClientOptions
        {
            Address = options.Address,
            Port = options.Port ?? DefaultPorts[0],
            Path = options.Path ?? "1,0",
            Timeout = options.Timeout
        });

        return ValueTask.FromResult<IDeviceConnection>(
            new LogixDeviceConnection(options.DeviceName, client));
    }

    public ValueTask<DeviceDetectionResult> ProbeAsync(
        ProbeRequest request,
        CancellationToken cancellationToken = default)
    {
        var startedAt = Stopwatch.GetTimestamp();
        var duration = Stopwatch.GetElapsedTime(startedAt);

        return ValueTask.FromResult(new DeviceDetectionResult(
            request.Address,
            Port: null,
            Probes: [new ProtocolProbeResult(
                DriverName,
                Port: null,
                Succeeded: false,
                Evidence: "Logix uses EtherNet/IP discovery before tag access.",
                Error: null,
                Duration: duration)],
            RecommendedDriver: null,
            Confidence: 0,
            Identity: null,
            Capabilities: [],
            Duration: duration));
    }
}
