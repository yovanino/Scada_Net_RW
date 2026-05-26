using ScadaNet.AspNetCore;

namespace ScadaNet.Tests;

public class ScadaNetOptionsValidatorTests
{
    [Fact]
    public void Validate_accepts_valid_configuration()
    {
        var options = new ScadaNetOptions();

        options.AddDevice("line1-plc", "logix", "192.168.0.10", timeout: TimeSpan.FromSeconds(2));
        options.AddPollingGroup(
            "line1-fast",
            "line1-plc",
            ["ProductionCounter"],
            TimeSpan.FromSeconds(1));

        ScadaNetOptionsValidator.Validate(options);
    }

    [Fact]
    public void Validate_rejects_duplicate_device_names()
    {
        var options = new ScadaNetOptions();

        options.AddDevice("line1-plc", "logix", "192.168.0.10");
        options.AddDevice("LINE1-PLC", "logix", "192.168.0.11");

        var error = Assert.Throws<ScadaNetOptionsValidationException>(() =>
            ScadaNetOptionsValidator.Validate(options));

        Assert.Contains(error.Errors, item => item.Contains("registered more than once"));
    }

    [Fact]
    public void Validate_rejects_polling_group_for_unknown_device()
    {
        var options = new ScadaNetOptions();

        options.AddDevice("line1-plc", "logix", "192.168.0.10");
        options.AddPollingGroup(
            "line2-fast",
            "line2-plc",
            ["ProductionCounter"],
            TimeSpan.FromSeconds(1));

        var error = Assert.Throws<ScadaNetOptionsValidationException>(() =>
            ScadaNetOptionsValidator.Validate(options));

        Assert.Contains(error.Errors, item => item.Contains("references unknown device"));
    }

    [Fact]
    public void Validate_rejects_enabled_polling_group_without_addresses()
    {
        var options = new ScadaNetOptions();

        options.AddDevice("line1-plc", "logix", "192.168.0.10");
        options.PollingGroups.Add(new()
        {
            Name = "line1-fast",
            DeviceName = "line1-plc"
        });

        var error = Assert.Throws<ScadaNetOptionsValidationException>(() =>
            ScadaNetOptionsValidator.Validate(options));

        Assert.Contains(error.Errors, item => item.Contains("must contain at least one address or signal name"));
    }

    [Fact]
    public void Validate_accepts_polling_group_with_signal_names()
    {
        var options = new ScadaNetOptions();

        options.AddDevice("line1-plc", "logix", "192.168.0.10");
        options.AddSignal("line1-plc", "counter", "ProductionCounter");
        options.AddPollingGroup(
            "line1-fast",
            "line1-plc",
            [],
            TimeSpan.FromSeconds(1),
            ["counter"]);

        ScadaNetOptionsValidator.Validate(options);
    }

    [Fact]
    public void Validate_rejects_polling_group_with_unknown_signal_name()
    {
        var options = new ScadaNetOptions();

        options.AddDevice("line1-plc", "logix", "192.168.0.10");
        options.AddPollingGroup(
            "line1-fast",
            "line1-plc",
            [],
            TimeSpan.FromSeconds(1),
            ["missing"]);

        var error = Assert.Throws<ScadaNetOptionsValidationException>(() =>
            ScadaNetOptionsValidator.Validate(options));

        Assert.Contains(error.Errors, item => item.Contains("references unknown signal 'missing'"));
    }
}
