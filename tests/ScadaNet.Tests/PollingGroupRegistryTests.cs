using ScadaNet.Runtime;

namespace ScadaNet.Tests;

public class PollingGroupRegistryTests
{
    [Fact]
    public void TryGet_finds_groups_by_name_case_insensitively()
    {
        var registry = new PollingGroupRegistry([
            new SignalPollingGroupDefinition
            {
                Name = "line1-fast",
                DeviceName = "line1-plc"
            }
        ]);

        var found = registry.TryGet("LINE1-FAST", out var group);

        Assert.True(found);
        Assert.Equal("line1-plc", group.DeviceName);
    }

    [Fact]
    public void Constructor_uses_device_name_when_group_name_is_empty()
    {
        var registry = new PollingGroupRegistry([
            new SignalPollingGroupDefinition
            {
                DeviceName = "line1-plc"
            }
        ]);

        var found = registry.TryGet("line1-plc", out var group);

        Assert.True(found);
        Assert.Equal("line1-plc", group.DeviceName);
    }

    [Fact]
    public void Constructor_rejects_duplicate_group_names()
    {
        var error = Assert.Throws<ArgumentException>(() => new PollingGroupRegistry([
            new SignalPollingGroupDefinition
            {
                Name = "line1-fast",
                DeviceName = "line1-plc"
            },
            new SignalPollingGroupDefinition
            {
                Name = "LINE1-FAST",
                DeviceName = "line2-plc"
            }
        ]));

        Assert.Contains("already registered", error.Message);
    }
}
