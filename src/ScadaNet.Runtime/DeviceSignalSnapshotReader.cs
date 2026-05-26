using ScadaNet.Model;

namespace ScadaNet.Runtime;

public sealed class DeviceSignalSnapshotReader : IDeviceSignalSnapshotReader
{
    private readonly IDeviceRegistry _registry;
    private readonly ISignalSnapshotStore _snapshots;

    public DeviceSignalSnapshotReader(
        IDeviceRegistry registry,
        ISignalSnapshotStore snapshots)
    {
        _registry = registry;
        _snapshots = snapshots;
    }

    public bool TryGetDeviceSnapshots(
        string deviceName,
        out IReadOnlyList<DeviceSignalSnapshot> snapshots)
    {
        if (!_registry.TryGet(deviceName, out var device))
        {
            snapshots = [];
            return false;
        }

        snapshots = device.Signals
            .OrderBy(signal => signal.DisplayOrder ?? int.MaxValue)
            .ThenBy(signal => signal.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(signal => signal.Name, StringComparer.OrdinalIgnoreCase)
            .Select(signal => BuildSnapshot(device, signal))
            .ToArray();
        return true;
    }

    public bool TryGet(
        string deviceName,
        string signalName,
        out DeviceSignalSnapshot snapshot)
    {
        if (!_registry.TryGet(deviceName, out var device) ||
            !device.TryGetSignal(signalName, out var signal))
        {
            snapshot = default!;
            return false;
        }

        snapshot = BuildSnapshot(device, signal);
        return true;
    }

    private DeviceSignalSnapshot BuildSnapshot(
        DeviceDefinition device,
        DeviceSignalDefinition signal)
    {
        var signalRef = new SignalRef(device.Name, signal.Address);
        var hasValue = _snapshots.TryGet(signalRef, out var value);

        return new DeviceSignalSnapshot(
            signal.Name,
            signal.Address,
            signal.DataType,
            signal.Unit,
            signal.Description,
            signal.Category,
            signal.DisplayOrder,
            signal.MinValue,
            signal.MaxValue,
            signal.RawMin,
            signal.RawMax,
            signal.ScaledMin,
            signal.ScaledMax,
            signal.IsArray,
            signal.ElementCount,
            signal.Writable,
            hasValue,
            hasValue ? value : null,
            hasValue ? ScaleValue(signal, value.Value) : null);
    }

    private static object? ScaleValue(
        DeviceSignalDefinition signal,
        object? value)
    {
        if (!HasScaling(signal))
        {
            return null;
        }

        if (TryGetNumber(value, out var numericValue))
        {
            return Scale(signal, numericValue);
        }

        if (value is IEnumerable<object?> values)
        {
            return values
                .Select(item => TryGetNumber(item, out var numericItem)
                    ? Scale(signal, numericItem)
                    : (double?)null)
                .ToArray();
        }

        return null;
    }

    private static bool HasScaling(DeviceSignalDefinition signal)
    {
        return signal.RawMin.HasValue &&
            signal.RawMax.HasValue &&
            signal.ScaledMin.HasValue &&
            signal.ScaledMax.HasValue &&
            signal.RawMax.Value != signal.RawMin.Value;
    }

    private static double Scale(DeviceSignalDefinition signal, double value)
    {
        var rawSpan = signal.RawMax!.Value - signal.RawMin!.Value;
        var scaledSpan = signal.ScaledMax!.Value - signal.ScaledMin!.Value;
        return signal.ScaledMin.Value + ((value - signal.RawMin.Value) / rawSpan * scaledSpan);
    }

    private static bool TryGetNumber(object? value, out double numericValue)
    {
        switch (value)
        {
            case byte byteValue:
                numericValue = byteValue;
                return true;
            case sbyte sbyteValue:
                numericValue = sbyteValue;
                return true;
            case short shortValue:
                numericValue = shortValue;
                return true;
            case ushort ushortValue:
                numericValue = ushortValue;
                return true;
            case int intValue:
                numericValue = intValue;
                return true;
            case uint uintValue:
                numericValue = uintValue;
                return true;
            case long longValue:
                numericValue = longValue;
                return true;
            case ulong ulongValue:
                numericValue = ulongValue;
                return true;
            case float floatValue:
                numericValue = floatValue;
                return true;
            case double doubleValue:
                numericValue = doubleValue;
                return true;
            case decimal decimalValue:
                numericValue = (double)decimalValue;
                return true;
            default:
                numericValue = default;
                return false;
        }
    }
}
