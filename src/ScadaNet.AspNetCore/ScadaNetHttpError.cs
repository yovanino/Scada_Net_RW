using Microsoft.AspNetCore.Http;
using ScadaNet.Core;

namespace ScadaNet.AspNetCore;

public sealed record ScadaNetHttpError(
    int StatusCode,
    string Code,
    string Message);

public static class ScadaNetHttpErrors
{
    public static ScadaNetHttpError FromException(Exception exception)
    {
        return exception switch
        {
            ScadaNetException => new ScadaNetHttpError(
                StatusCodes.Status502BadGateway,
                "scadanet.operation_failed",
                exception.Message),

            TimeoutException => new ScadaNetHttpError(
                StatusCodes.Status504GatewayTimeout,
                "scadanet.timeout",
                exception.Message),

            ArgumentException or NotSupportedException => new ScadaNetHttpError(
                StatusCodes.Status400BadRequest,
                "scadanet.bad_request",
                exception.Message),

            InvalidOperationException => new ScadaNetHttpError(
                StatusCodes.Status409Conflict,
                "scadanet.invalid_operation",
                exception.Message),

            _ => new ScadaNetHttpError(
                StatusCodes.Status500InternalServerError,
                "scadanet.unhandled_error",
                "Unhandled ScadaNet operation error.")
        };
    }

    public static IResult ToResult(Exception exception)
    {
        var error = FromException(exception);
        return Results.Json(error, statusCode: error.StatusCode);
    }
}
