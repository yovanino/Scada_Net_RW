using ScadaNet.Model;

namespace ScadaNet.Tests;

public class ModelTests
{
    [Fact]
    public void SignalValue_keeps_reference_quality_and_timestamp()
    {
        var signal = new SignalRef("Line1", "Motor.Speed");
        var timestamp = DateTimeOffset.UtcNow;

        var value = new SignalValue(signal, 42, SignalQuality.Good, timestamp);

        Assert.Equal(signal, value.Ref);
        Assert.Equal(42, value.Value);
        Assert.Equal(SignalQuality.Good, value.Quality);
        Assert.Equal(timestamp, value.Timestamp);
    }
}
