using ScadaNet.Model;
using ScadaNet.Runtime;

namespace ScadaNet.Tests;

public class SignalPollingServiceTests
{
    [Fact]
    public async Task PollAsync_reads_configured_group_signals()
    {
        var runtime = new FakeRuntime();
        var statuses = new PollingStatusStore();
        var polling = new SignalPollingService(runtime, statuses);
        var group = new SignalPollingGroupDefinition
        {
            Name = "line1-fast",
            DeviceName = "line1-plc"
        };
        group.Addresses.Add("ProductionCounter");
        group.Addresses.Add("Motor.Speed");

        var values = await polling.PollAsync(group);

        Assert.Equal(["ProductionCounter", "Motor.Speed"], runtime.LastSignals.Select(signal => signal.Address));
        Assert.Equal(2, values.Count);
        Assert.True(statuses.TryGet("line1-fast", out var status));
        Assert.True(status.Healthy);
        Assert.Equal(2, status.SignalCount);
    }

    [Fact]
    public async Task PollAsync_resolves_named_signals_from_catalog()
    {
        var runtime = new FakeRuntime();
        var statuses = new PollingStatusStore();
        var device = new DeviceDefinition("line1-plc", "fake", "127.0.0.1");
        device.Signals.Add(new DeviceSignalDefinition
        {
            Name = "production-counter",
            Address = "ProductionCounter"
        });
        var resolver = new DeviceSignalResolver(new DeviceRegistry([device]));
        var polling = new SignalPollingService(runtime, statuses, resolver);
        var group = new SignalPollingGroupDefinition
        {
            Name = "line1-fast",
            DeviceName = "line1-plc"
        };
        group.SignalNames.Add("production-counter");

        var values = await polling.PollAsync(group);

        Assert.Equal(["ProductionCounter"], runtime.LastSignals.Select(signal => signal.Address));
        Assert.Single(values);
        Assert.True(statuses.TryGet("line1-fast", out var status));
        Assert.True(status.Healthy);
        Assert.Equal(1, status.SignalCount);
    }

    [Fact]
    public async Task PollAsync_skips_disabled_groups()
    {
        var runtime = new FakeRuntime();
        var statuses = new PollingStatusStore();
        var polling = new SignalPollingService(runtime, statuses);
        var group = new SignalPollingGroupDefinition
        {
            Name = "line1-fast",
            DeviceName = "line1-plc",
            Enabled = false
        };
        group.Addresses.Add("ProductionCounter");

        var values = await polling.PollAsync(group);

        Assert.Empty(values);
        Assert.Empty(runtime.LastSignals);
        Assert.True(statuses.TryGet("line1-fast", out var status));
        Assert.True(status.Healthy);
        Assert.Equal("Polling group is disabled.", status.Error);
    }

    [Fact]
    public async Task PollAsync_records_failure_status()
    {
        var runtime = new FakeRuntime
        {
            ReadManyException = new InvalidOperationException("PLC unavailable")
        };
        var statuses = new PollingStatusStore();
        var polling = new SignalPollingService(runtime, statuses);
        var group = new SignalPollingGroupDefinition
        {
            Name = "line1-fast",
            DeviceName = "line1-plc"
        };
        group.Addresses.Add("ProductionCounter");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await polling.PollAsync(group));

        Assert.True(statuses.TryGet("line1-fast", out var status));
        Assert.False(status.Healthy);
        Assert.Equal("PLC unavailable", status.Error);
    }

    private sealed class FakeRuntime : IPlcRuntime
    {
        public IReadOnlyList<SignalRef> LastSignals { get; private set; } = [];
        public Exception? ReadManyException { get; init; }

        public ValueTask<SignalValue> ReadAsync(
            SignalRef signal,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask<IReadOnlyList<SignalValue>> ReadManyAsync(
            IReadOnlyList<SignalRef> signals,
            CancellationToken cancellationToken = default)
        {
            if (ReadManyException is not null)
            {
                throw ReadManyException;
            }

            LastSignals = signals;

            IReadOnlyList<SignalValue> values = signals
                .Select(signal => new SignalValue(
                    signal,
                    123,
                    SignalQuality.Good,
                    DateTimeOffset.UtcNow))
                .ToArray();

            return ValueTask.FromResult(values);
        }

        public ValueTask<SignalValue> ReadArrayAsync(
            SignalRef signal,
            ushort elementCount,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask WriteAsync(
            SignalRef signal,
            object? value,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask WriteAsync(
            SignalRef signal,
            object? value,
            string? dataType,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask WriteArrayAsync(
            SignalRef signal,
            IReadOnlyList<object?> values,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask WriteArrayAsync(
            SignalRef signal,
            IReadOnlyList<object?> values,
            string? dataType,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
