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

    private static ScadaNetWriteSignalRequest DeserializeWriteRequest(string json)
    {
        return JsonSerializer.Deserialize<ScadaNetWriteSignalRequest>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }
}
