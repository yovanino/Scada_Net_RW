using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ScadaNet.Runtime;

namespace ScadaNet.AspNetCore;

public static class ScadaNetEndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapScadaNetEndpoints(
        this IEndpointRouteBuilder endpoints,
        string prefix = "/scadanet")
    {
        var group = endpoints.MapGroup(prefix);

        group.MapGet("/devices", (IDeviceRegistry registry) =>
        {
            return Results.Ok(registry.Devices);
        });

        group.MapGet("/devices/{name}/discovery", async (
            string name,
            IDeviceRegistry registry,
            IDiscoveryService discovery,
            CancellationToken cancellationToken) =>
        {
            if (!registry.TryGet(name, out var device))
            {
                return Results.NotFound(new
                {
                    Message = $"Device '{name}' is not registered."
                });
            }

            var result = await discovery.DetectAsync(
                    device.ToProbeRequest(),
                    cancellationToken)
                .ConfigureAwait(false);

            return Results.Ok(result);
        });

        return group;
    }
}
