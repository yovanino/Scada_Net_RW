using System.Text.Json;
using ScadaNet.AspNetCore;

namespace ScadaNet.Tests;

public class ScadaNetSignalRequestsTests
{
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

    private static ScadaNetWriteSignalRequest DeserializeWriteRequest(string json)
    {
        return JsonSerializer.Deserialize<ScadaNetWriteSignalRequest>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }
}
