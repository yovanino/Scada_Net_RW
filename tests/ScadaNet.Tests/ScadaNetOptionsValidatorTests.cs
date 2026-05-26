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

    [Fact]
    public void Validate_rejects_zero_signal_element_count()
    {
        var options = new ScadaNetOptions();

        options.AddDevice("line1-plc", "logix", "192.168.0.10");
        options.AddSignal(
            "line1-plc",
            "history",
            "History",
            isArray: true,
            elementCount: 0);

        var error = Assert.Throws<ScadaNetOptionsValidationException>(() =>
            ScadaNetOptionsValidator.Validate(options));

        Assert.Contains(error.Errors, item => item.Contains("element count must be greater than zero"));
    }

    [Fact]
    public void Validate_rejects_writable_signal_when_device_writes_are_disabled()
    {
        var options = new ScadaNetOptions();

        options.AddDevice("line1-plc", "logix", "192.168.0.10");
        options.AddSignal(
            "line1-plc",
            "reset-command",
            "ResetCommand",
            writable: true);

        var error = Assert.Throws<ScadaNetOptionsValidationException>(() =>
            ScadaNetOptionsValidator.Validate(options));

        Assert.Contains(error.Errors, item => item.Contains("device writes are disabled"));
    }

    [Fact]
    public void Validate_rejects_writable_signal_outside_device_write_policy()
    {
        var options = new ScadaNetOptions();

        options.AddDevice(
            "line1-plc",
            "logix",
            "192.168.0.10",
            writesEnabled: true,
            writableAddresses: ["ResetCommand"]);
        options.AddSignal(
            "line1-plc",
            "speed-setpoint",
            "SpeedSetpoint",
            writable: true);

        var error = Assert.Throws<ScadaNetOptionsValidationException>(() =>
            ScadaNetOptionsValidator.Validate(options));

        Assert.Contains(error.Errors, item => item.Contains("not allowed by the device write policy"));
    }

    [Fact]
    public void Validate_rejects_signal_min_value_greater_than_max_value()
    {
        var options = new ScadaNetOptions();

        options.AddDevice("line1-plc", "logix", "192.168.0.10");
        options.AddSignal(
            "line1-plc",
            "speed-setpoint",
            "SpeedSetpoint",
            minValue: 100,
            maxValue: 10);

        var error = Assert.Throws<ScadaNetOptionsValidationException>(() =>
            ScadaNetOptionsValidator.Validate(options));

        Assert.Contains(error.Errors, item => item.Contains("minimum value cannot be greater than maximum value"));
    }
}
