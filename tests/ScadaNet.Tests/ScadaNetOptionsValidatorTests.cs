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

    [Fact]
    public void Validate_rejects_incomplete_signal_scaling()
    {
        var options = new ScadaNetOptions();

        options.AddDevice("line1-plc", "logix", "192.168.0.10");
        options.AddSignal(
            "line1-plc",
            "speed",
            "Speed",
            rawMin: 0,
            rawMax: 1000);

        var error = Assert.Throws<ScadaNetOptionsValidationException>(() =>
            ScadaNetOptionsValidator.Validate(options));

        Assert.Contains(error.Errors, item => item.Contains("scaling requires raw min, raw max, scaled min, and scaled max"));
    }

    [Fact]
    public void Validate_rejects_zero_raw_scaling_range()
    {
        var options = new ScadaNetOptions();

        options.AddDevice("line1-plc", "logix", "192.168.0.10");
        options.AddSignal(
            "line1-plc",
            "speed",
            "Speed",
            rawMin: 100,
            rawMax: 100,
            scaledMin: 0,
            scaledMax: 100);

        var error = Assert.Throws<ScadaNetOptionsValidationException>(() =>
            ScadaNetOptionsValidator.Validate(options));

        Assert.Contains(error.Errors, item => item.Contains("raw scaling range cannot be zero"));
    }

    [Fact]
    public void Validate_rejects_non_positive_write_audit_max_records()
    {
        var options = new ScadaNetOptions
        {
            WriteAuditMaxRecords = 0
        };

        var error = Assert.Throws<ScadaNetOptionsValidationException>(() =>
            ScadaNetOptionsValidator.Validate(options));

        Assert.Contains(error.Errors, item => item.Contains("Write audit max records must be greater than zero"));
    }

    [Fact]
    public void Validate_rejects_non_positive_background_polling_max_concurrency()
    {
        var options = new ScadaNetOptions
        {
            BackgroundPollingMaxConcurrency = 0
        };

        var error = Assert.Throws<ScadaNetOptionsValidationException>(() =>
            ScadaNetOptionsValidator.Validate(options));

        Assert.Contains(error.Errors, item => item.Contains("Background polling max concurrency must be greater than zero"));
    }

    [Fact]
    public void Validate_rejects_non_positive_background_polling_tick_interval()
    {
        var options = new ScadaNetOptions
        {
            BackgroundPollingTickInterval = TimeSpan.Zero
        };

        var error = Assert.Throws<ScadaNetOptionsValidationException>(() =>
            ScadaNetOptionsValidator.Validate(options));

        Assert.Contains(error.Errors, item => item.Contains("Background polling tick interval must be greater than zero"));
    }
}
