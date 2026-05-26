using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ScadaNet.Model;
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

        group.MapGet("/connections/pool", (IDeviceConnectionPool connections) =>
        {
            return Results.Ok(connections.GetStatus());
        });

        group.MapDelete("/connections/pool/{name}", async (
            string name,
            IDeviceConnectionPool connections,
            CancellationToken cancellationToken) =>
        {
            var closed = await connections.CloseAsync(name, cancellationToken)
                .ConfigureAwait(false);

            return closed
                ? Results.NoContent()
                : Results.NotFound(new
                {
                    Message = $"Device '{name}' has no active pooled connection."
                });
        });

        group.MapGet("/devices/{name}/signals/read", async (
            string name,
            string address,
            IDeviceRegistry registry,
            IPlcRuntime runtime,
            CancellationToken cancellationToken) =>
        {
            if (!registry.TryGet(name, out _))
            {
                return Results.NotFound(new
                {
                    Message = $"Device '{name}' is not registered."
                });
            }

            try
            {
                var signal = ScadaNetSignalRequestValidation.ToSignalRef(name, address);
                var value = await runtime.ReadAsync(
                        signal,
                        cancellationToken)
                    .ConfigureAwait(false);

                return Results.Ok(value);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ScadaNetHttpErrors.ToResult(ex);
            }
        });

        group.MapPost("/devices/{name}/signals/read", async (
            string name,
            ScadaNetReadManyRequest request,
            IDeviceRegistry registry,
            IPlcRuntime runtime,
            CancellationToken cancellationToken) =>
        {
            if (!registry.TryGet(name, out _))
            {
                return Results.NotFound(new
                {
                    Message = $"Device '{name}' is not registered."
                });
            }

            try
            {
                var signals = request.ToSignalRefs(name);
                var values = await runtime.ReadManyAsync(signals, cancellationToken)
                    .ConfigureAwait(false);

                return Results.Ok(values);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ScadaNetHttpErrors.ToResult(ex);
            }
        });

        group.MapGet("/devices/{name}/signals/read-array", async (
            string name,
            string address,
            ushort count,
            IDeviceRegistry registry,
            IPlcRuntime runtime,
            CancellationToken cancellationToken) =>
        {
            if (!registry.TryGet(name, out _))
            {
                return Results.NotFound(new
                {
                    Message = $"Device '{name}' is not registered."
                });
            }

            try
            {
                var signal = ScadaNetSignalRequestValidation.ToSignalRef(name, address);
                var value = await runtime.ReadArrayAsync(
                        signal,
                        count,
                        cancellationToken)
                    .ConfigureAwait(false);

                return Results.Ok(value);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ScadaNetHttpErrors.ToResult(ex);
            }
        });

        group.MapPost("/devices/{name}/signals/write", async (
            string name,
            ScadaNetWriteSignalRequest request,
            IDeviceRegistry registry,
            IPlcRuntime runtime,
            CancellationToken cancellationToken) =>
        {
            if (!registry.TryGet(name, out _))
            {
                return Results.NotFound(new
                {
                    Message = $"Device '{name}' is not registered."
                });
            }

            try
            {
                await runtime.WriteAsync(
                        request.ToSignalRef(name),
                        request.GetValue(),
                        request.DataType,
                        cancellationToken)
                    .ConfigureAwait(false);

                return Results.Accepted();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ScadaNetHttpErrors.ToResult(ex);
            }
        });

        group.MapPost("/devices/{name}/signals/write-array", async (
            string name,
            ScadaNetWriteArrayRequest request,
            IDeviceRegistry registry,
            IPlcRuntime runtime,
            CancellationToken cancellationToken) =>
        {
            if (!registry.TryGet(name, out _))
            {
                return Results.NotFound(new
                {
                    Message = $"Device '{name}' is not registered."
                });
            }

            try
            {
                await runtime.WriteArrayAsync(
                        request.ToSignalRef(name),
                        request.GetValues(),
                        request.DataType,
                        cancellationToken)
                    .ConfigureAwait(false);

                return Results.Accepted();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ScadaNetHttpErrors.ToResult(ex);
            }
        });

        group.MapGet("/devices/{name}/signals/snapshot", (
            string name,
            IDeviceRegistry registry,
            ISignalSnapshotStore snapshots) =>
        {
            if (!registry.TryGet(name, out _))
            {
                return Results.NotFound(new
                {
                    Message = $"Device '{name}' is not registered."
                });
            }

            return Results.Ok(snapshots.GetDeviceSnapshots(name));
        });

        group.MapGet("/devices/{name}/signals/snapshot/{address}", (
            string name,
            string address,
            IDeviceRegistry registry,
            ISignalSnapshotStore snapshots) =>
        {
            if (!registry.TryGet(name, out _))
            {
                return Results.NotFound(new
                {
                    Message = $"Device '{name}' is not registered."
                });
            }

            return snapshots.TryGet(new SignalRef(name, address), out var value)
                ? Results.Ok(value)
                : Results.NotFound(new
                {
                    Message = $"Signal '{address}' has no snapshot for device '{name}'."
                });
        });

        group.MapGet("/polling/status", (IPollingStatusStore statuses) =>
        {
            return Results.Ok(statuses.GetAll());
        });

        group.MapGet("/polling/status/{groupName}", (
            string groupName,
            IPollingStatusStore statuses) =>
        {
            return statuses.TryGet(groupName, out var status)
                ? Results.Ok(status)
                : Results.NotFound(new
                {
                    Message = $"Polling group '{groupName}' has no status yet."
                });
        });

        group.MapGet("/writes/audit", (
            int? count,
            IWriteAuditStore audit) =>
        {
            return Results.Ok(audit.GetRecent(count ?? 100));
        });

        group.MapGet("/devices/{name}/writes/audit", (
            string name,
            int? count,
            IDeviceRegistry registry,
            IWriteAuditStore audit) =>
        {
            if (!registry.TryGet(name, out _))
            {
                return Results.NotFound(new
                {
                    Message = $"Device '{name}' is not registered."
                });
            }

            return Results.Ok(audit.GetDeviceRecords(name, count ?? 100));
        });

        group.MapGet("/health/devices", (IDeviceHealthService health) =>
        {
            return Results.Ok(health.GetAll());
        });

        group.MapGet("/health/devices/{name}", (
            string name,
            IDeviceHealthService health) =>
        {
            return health.TryGet(name, out var summary)
                ? Results.Ok(summary)
                : Results.NotFound(new
                {
                    Message = $"Device '{name}' is not registered."
                });
        });

        return group;
    }
}
