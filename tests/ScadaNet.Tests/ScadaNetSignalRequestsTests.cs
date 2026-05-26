using System.Text.Json;
using ScadaNet.AspNetCore;

namespace ScadaNet.Tests;

public class ScadaNetSignalRequestsTests
{
    [Fact]
    public void Read_many_request_creates_signal_refs()
    {
        var request = new ScadaNetReadManyRequest(["Counter", "Motor.Speed"]);

        var signals = request.ToSignalRefs("line1-plc");

        Assert.Equal(["Counter", "Motor.Speed"], signals.Select(signal => signal.Address));
        Assert.All(signals, signal => Assert.Equal("line1-plc", signal.DeviceName));
    }

    [Fact]
    public void Read_many_request_rejects_empty_addresses()
    {
        var request = new ScadaNetReadManyRequest(["Counter", " "]);

        var error = Assert.Throws<ArgumentException>(() => request.ToSignalRefs("line1-plc"));

        Assert.Contains("Signal address cannot be empty", error.Message);
    }

    [Fact]
    public void Read_many_named_request_rejects_empty_signal_names()
    {
        var request = new ScadaNetReadManyNamedRequest(["counter", " "]);

        var error = Assert.Throws<ArgumentException>(request.GetSignalNames);

        Assert.Contains("Signal name cannot be empty", error.Message);
    }

    [Fact]
    public void GetValue_returns_bool()
    {
        var request = DeserializeWriteRequest("""
            {
              "address": "ResetCommand",
              "value": true
            }
            """);

        Assert.Equal(true, request.GetValue());
    }

    [Fact]
    public void GetValue_returns_int_for_integer_numbers()
    {
        var request = DeserializeWriteRequest("""
            {
              "address": "ProductionCounter",
              "value": 123
            }
            """);

        Assert.Equal(123, request.GetValue());
    }

    [Fact]
    public void GetValue_returns_double_for_decimal_numbers()
    {
        var request = DeserializeWriteRequest("""
            {
              "address": "SpeedSetpoint",
              "value": 12.5
            }
            """);

        Assert.Equal(12.5, request.GetValue());
    }

    [Fact]
    public void Write_request_preserves_optional_data_type()
    {
        var request = DeserializeWriteRequest("""
            {
              "address": "SpeedSetpoint",
              "value": 1,
              "dataType": "Real"
            }
            """);

        Assert.Equal("Real", request.DataType);
        Assert.Equal(1, request.GetValue());
    }

    [Fact]
    public void Write_request_creates_signal_ref()
    {
        var request = new ScadaNetWriteSignalRequest("ResetCommand", JsonDocument.Parse("true").RootElement);

        var signal = request.ToSignalRef("line1-plc");

        Assert.Equal("line1-plc", signal.DeviceName);
        Assert.Equal("ResetCommand", signal.Address);
    }

    [Fact]
    public void Write_request_rejects_empty_address()
    {
        var request = new ScadaNetWriteSignalRequest(" ", JsonDocument.Parse("true").RootElement);

        var error = Assert.Throws<ArgumentException>(() => request.ToSignalRef("line1-plc"));

        Assert.Contains("Signal address cannot be empty", error.Message);
    }

    [Fact]
    public void GetValues_returns_array_values()
    {
        var request = JsonSerializer.Deserialize<ScadaNetWriteArrayRequest>(
            """
            {
              "address": "Counters",
              "values": [1, 2.5, true, "ready", null]
            }
            """,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        Assert.Equal([1, 2.5, true, "ready", null], request.GetValues());
    }

    [Fact]
    public void Write_array_request_preserves_optional_data_type()
    {
        var request = JsonSerializer.Deserialize<ScadaNetWriteArrayRequest>(
            """
            {
              "address": "Speeds",
              "values": [1, 2, 3],
              "dataType": "Real"
            }
            """,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        Assert.Equal("Real", request.DataType);
        Assert.Equal([1, 2, 3], request.GetValues());
    }

    [Fact]
    public void Write_named_array_request_returns_array_values()
    {
        var request = JsonSerializer.Deserialize<ScadaNetWriteNamedArrayRequest>(
            """
            {
              "values": [1, 2.5, true, "ready"],
              "dataType": "Real"
            }
            """,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        Assert.Equal("Real", request.DataType);
        Assert.Equal([1, 2.5, true, "ready"], request.GetValues());
    }

    [Fact]
    public void Write_array_request_rejects_empty_address()
    {
        using var document = JsonDocument.Parse("[1,2,3]");
        var request = new ScadaNetWriteArrayRequest(" ", document.RootElement);

        var error = Assert.Throws<ArgumentException>(() => request.ToSignalRef("line1-plc"));

        Assert.Contains("Signal address cannot be empty", error.Message);
    }

    private static ScadaNetWriteSignalRequest DeserializeWriteRequest(string json)
    {
        return JsonSerializer.Deserialize<ScadaNetWriteSignalRequest>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }
}
