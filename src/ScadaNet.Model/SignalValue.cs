namespace ScadaNet.Model;

public sealed record SignalValue(
    SignalRef Ref,
    object? Value,
    SignalQuality Quality,
    DateTimeOffset Timestamp);
