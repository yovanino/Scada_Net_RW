using ScadaNet.Model;

namespace ScadaNet.Runtime;

public sealed record DeviceSignalResolution(
    DeviceDefinition Device,
    DeviceSignalDefinition Definition,
    SignalRef Signal);
