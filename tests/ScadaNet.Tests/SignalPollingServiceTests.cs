using ScadaNet.Model;
using ScadaNet.Runtime;

namespace ScadaNet.Tests;

public class SignalPollingServiceTests
{
    [Fact]
    public async Task PollAsync_reads_configured_group_signals()
    {
        var runtime = new FakeRuntime();
        var polling = new SignalPollingService(runtime);
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
    }

    [Fact]
    public async Task PollAsync_skips_disabled_groups()
    {
        var runtime = new FakeRuntime();
        var polling = new SignalPollingService(runtime);
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
    }

    private sealed class FakeRuntime : IPlcRuntime
    {
        public IReadOnlyList<SignalRef> LastSignals { get; private set; } = [];

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

        public ValueTask WriteAsync(
            SignalRef signal,
            object? value,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
