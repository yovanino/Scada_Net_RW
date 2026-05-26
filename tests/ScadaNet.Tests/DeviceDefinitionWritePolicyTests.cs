using ScadaNet.Runtime;

namespace ScadaNet.Tests;

public class DeviceDefinitionWritePolicyTests
{
    [Fact]
    public void CanWrite_returns_false_when_writes_are_disabled()
    {
        var device = new DeviceDefinition("line1-plc", "logix", "192.168.0.10");

        Assert.False(device.CanWrite("ResetCommand"));
    }

    [Fact]
    public void CanWrite_allows_any_address_when_enabled_without_allow_list()
    {
        var device = new DeviceDefinition("line1-plc", "logix", "192.168.0.10")
        {
            WritesEnabled = true
        };

        Assert.True(device.CanWrite("ResetCommand"));
    }

    [Fact]
    public void CanWrite_enforces_allow_list_case_insensitively()
    {
        var device = new DeviceDefinition("line1-plc", "logix", "192.168.0.10")
        {
            WritesEnabled = true,
            WritableAddresses = { "ResetCommand" }
        };

        Assert.True(device.CanWrite("resetcommand"));
        Assert.False(device.CanWrite("SpeedSetpoint"));
    }
}
