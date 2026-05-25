namespace ScadaNet.Protocols;

public interface IDeviceDriver
{
    string DriverName { get; }

    ValueTask<IDeviceConnection> ConnectAsync(
        DeviceConnectionOptions options,
        CancellationToken cancellationToken = default);

    ValueTask<DeviceDetectionResult> ProbeAsync(
        ProbeRequest request,
        CancellationToken cancellationToken = default);
}
