using System.Diagnostics;
using ScadaNet.Model;
using ScadaNet.Protocols;

namespace ScadaNet.EtherNetIp;

public sealed class EtherNetIpDiscoveryDriver : IDeviceDriver, IDeviceDriverMetadata
{
    public string DriverName => "EtherNetIp";

    public string ProtocolFamily => KnownProtocolFamilies.EtherNetIp;

    public IReadOnlyList<int> DefaultPorts { get; } = [EtherNetIpDefaults.ExplicitMessagingPort];

    public IReadOnlyList<string> Capabilities { get; } =
    [
        KnownDiscoveryCapabilities.ReadIdentity,
        KnownDiscoveryCapabilities.ExplicitMessaging
    ];

    public ValueTask<IDeviceConnection> ConnectAsync(
        DeviceConnectionOptions options,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("EtherNet/IP generic device connections are not implemented yet.");
    }

    public async ValueTask<DeviceDetectionResult> ProbeAsync(
        ProbeRequest request,
        CancellationToken cancellationToken = default)
    {
        var detectionStartedAt = Stopwatch.GetTimestamp();
        var ports = request.Ports.Count == 0
            ? DefaultPorts
            : request.Ports;

        var probes = new List<ProtocolProbeResult>();

        foreach (var port in ports)
        {
            var startedAt = Stopwatch.GetTimestamp();

            try
            {
                await using var client = new EtherNetIpClient(new EtherNetIpClientOptions
                {
                    Host = request.Address,
                    Port = port,
                    ConnectTimeout = request.Timeout,
                    OperationTimeout = request.Timeout
                });

                var identities = await client.ListIdentityAsync(cancellationToken).ConfigureAwait(false);
                var identity = identities.FirstOrDefault();

                if (identity is null)
                {
                    probes.Add(new ProtocolProbeResult(
                        KnownProtocolFamilies.EtherNetIp,
                        port,
                        Succeeded: false,
                        Evidence: "Device responded to ListIdentity but returned no identity items.",
                        Error: null,
                        Duration: Stopwatch.GetElapsedTime(startedAt)));
                    continue;
                }

                probes.Add(new ProtocolProbeResult(
                    KnownProtocolFamilies.EtherNetIp,
                    port,
                    Succeeded: true,
                    Evidence: $"ListIdentity returned '{identity.ProductName}'.",
                    Error: null,
                    Duration: Stopwatch.GetElapsedTime(startedAt)));

                return new DeviceDetectionResult(
                    request.Address,
                    port,
                    probes,
                    RecommendedDriver: DriverName,
                    Confidence: 0.95,
                    Identity: ToDeviceIdentity(identity),
                    Capabilities: Capabilities,
                    Duration: Stopwatch.GetElapsedTime(detectionStartedAt));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                probes.Add(new ProtocolProbeResult(
                    KnownProtocolFamilies.EtherNetIp,
                    port,
                    Succeeded: false,
                    Evidence: null,
                    Error: ex.Message,
                    Duration: Stopwatch.GetElapsedTime(startedAt)));
            }
        }

        return new DeviceDetectionResult(
            request.Address,
            null,
            probes,
            RecommendedDriver: null,
            Confidence: 0,
            Identity: null,
            Capabilities: [],
            Duration: Stopwatch.GetElapsedTime(detectionStartedAt));
    }

    private static DeviceIdentity ToDeviceIdentity(EtherNetIpIdentity identity)
    {
        return new DeviceIdentity(
            VendorName: GetVendorName(identity.VendorId),
            ProductName: identity.ProductName,
            ProductCode: identity.ProductCode.ToString(),
            Revision: $"{identity.RevisionMajor}.{identity.RevisionMinor}",
            SerialNumber: identity.SerialNumber.ToString("X8"),
            VendorCode: identity.VendorId.ToString(),
            DeviceTypeCode: identity.DeviceType.ToString(),
            StatusCode: $"0x{identity.Status:X4}",
            StateCode: identity.State.ToString());
    }

    private static string GetVendorName(ushort vendorId)
    {
        return vendorId switch
        {
            1 => "Rockwell Automation",
            _ => vendorId.ToString()
        };
    }
}
