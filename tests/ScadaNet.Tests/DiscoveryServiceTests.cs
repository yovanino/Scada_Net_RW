using ScadaNet.Model;
using ScadaNet.Protocols;
using ScadaNet.Runtime;

namespace ScadaNet.Tests;

public class DiscoveryServiceTests
{
    [Fact]
    public async Task DetectAsync_returns_highest_confidence_result_and_combines_probes()
    {
        var low = new FakeDriver("Low", 0.3);
        var high = new FakeDriver("High", 0.9);
        var service = new DiscoveryService([low, high]);

        var result = await service.DetectAsync(new ProbeRequest(
            "192.168.0.10",
            [44818],
            TimeSpan.FromSeconds(1)));

        Assert.Equal("High", result.RecommendedDriver);
        Assert.Equal(0.9, result.Confidence);
        Assert.Equal(2, result.Probes.Count);
        Assert.Contains(result.Probes, probe => probe.Protocol == "Low");
        Assert.Contains(result.Probes, probe => probe.Protocol == "High");
    }

    [Fact]
    public async Task DetectAsync_probes_drivers_concurrently()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = 0;

        void OnStarted()
        {
            if (Interlocked.Increment(ref started) == 2)
            {
                allStarted.SetResult();
            }
        }

        var first = new CoordinatedDriver("First", release, OnStarted);
        var second = new CoordinatedDriver("Second", release, OnStarted);
        var service = new DiscoveryService([first, second]);

        var detection = service.DetectAsync(new ProbeRequest(
                "192.168.0.10",
                [44818],
                TimeSpan.FromSeconds(1)))
            .AsTask();

        await allStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
        release.SetResult();

        var result = await detection;

        Assert.Equal(2, result.Probes.Count);
    }

    [Fact]
    public async Task DetectAsync_keeps_successful_detection_when_driver_probe_fails()
    {
        var failing = new ThrowingDriver("FailingProtocol");
        var successful = new FakeDriver("WorkingProtocol", 0.8);
        var service = new DiscoveryService([failing, successful]);

        var result = await service.DetectAsync(new ProbeRequest(
            "192.168.0.10",
            [44818],
            TimeSpan.FromSeconds(1)));

        Assert.Equal("WorkingProtocol", result.RecommendedDriver);
        Assert.Equal(0.8, result.Confidence);
        Assert.Contains(result.Probes, probe =>
            probe.Protocol == "FailingProtocol" &&
            !probe.Succeeded &&
            probe.Error == "probe failed" &&
            probe.Duration.HasValue);
        Assert.Contains(result.Probes, probe =>
            probe.Protocol == "WorkingProtocol" &&
            probe.Succeeded);
    }

    private sealed class FakeDriver : IDeviceDriver
    {
        private readonly double _confidence;

        public FakeDriver(string driverName, double confidence)
        {
            DriverName = driverName;
            _confidence = confidence;
        }

        public string DriverName { get; }

        public ValueTask<IDeviceConnection> ConnectAsync(
            DeviceConnectionOptions options,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask<DeviceDetectionResult> ProbeAsync(
            ProbeRequest request,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new DeviceDetectionResult(
                request.Address,
                request.Ports.FirstOrDefault(),
                [new ProtocolProbeResult(DriverName, request.Ports.FirstOrDefault(), true, "test", null)],
                DriverName,
                _confidence,
                new DeviceIdentity(DriverName, DriverName, null, null, null),
                [DriverName]));
        }
    }

    private sealed class CoordinatedDriver : IDeviceDriver
    {
        private readonly TaskCompletionSource _release;
        private readonly Action _onStarted;

        public CoordinatedDriver(
            string driverName,
            TaskCompletionSource release,
            Action onStarted)
        {
            DriverName = driverName;
            _release = release;
            _onStarted = onStarted;
        }

        public string DriverName { get; }

        public ValueTask<IDeviceConnection> ConnectAsync(
            DeviceConnectionOptions options,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public async ValueTask<DeviceDetectionResult> ProbeAsync(
            ProbeRequest request,
            CancellationToken cancellationToken = default)
        {
            _onStarted();
            await _release.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

            return new DeviceDetectionResult(
                request.Address,
                request.Ports.FirstOrDefault(),
                [new ProtocolProbeResult(DriverName, request.Ports.FirstOrDefault(), true, "test", null)],
                DriverName,
                0.5,
                new DeviceIdentity(DriverName, DriverName, null, null, null),
                [DriverName]);
        }
    }

    private sealed class ThrowingDriver : IDeviceDriver
    {
        public ThrowingDriver(string driverName)
        {
            DriverName = driverName;
        }

        public string DriverName { get; }

        public ValueTask<IDeviceConnection> ConnectAsync(
            DeviceConnectionOptions options,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask<DeviceDetectionResult> ProbeAsync(
            ProbeRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("probe failed");
        }
    }
}
