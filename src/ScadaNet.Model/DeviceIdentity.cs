namespace ScadaNet.Model;

public sealed record DeviceIdentity(
    string? VendorName,
    string? ProductName,
    string? ProductCode,
    string? Revision,
    string? SerialNumber);
