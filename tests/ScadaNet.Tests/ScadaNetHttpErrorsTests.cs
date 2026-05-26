using ScadaNet.AspNetCore;
using ScadaNet.Core;

namespace ScadaNet.Tests;

public class ScadaNetHttpErrorsTests
{
    [Fact]
    public void FromException_maps_scadanet_exception_to_bad_gateway()
    {
        var error = ScadaNetHttpErrors.FromException(new ScadaNetException("PLC rejected request."));

        Assert.Equal(502, error.StatusCode);
        Assert.Equal("scadanet.operation_failed", error.Code);
        Assert.Equal("PLC rejected request.", error.Message);
    }

    [Fact]
    public void FromException_maps_timeout_to_gateway_timeout()
    {
        var error = ScadaNetHttpErrors.FromException(new TimeoutException("PLC timeout."));

        Assert.Equal(504, error.StatusCode);
        Assert.Equal("scadanet.timeout", error.Code);
    }

    [Fact]
    public void FromException_maps_bad_request_errors()
    {
        var error = ScadaNetHttpErrors.FromException(new NotSupportedException("Unsupported value."));

        Assert.Equal(400, error.StatusCode);
        Assert.Equal("scadanet.bad_request", error.Code);
    }
}
