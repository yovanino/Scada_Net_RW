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

        group.MapGet("/runtime/status", (
            int? attentionCount,
            DeviceDashboardIssueSeverity? minimumSeverity,
            IDeviceDashboardService dashboards) =>
        {
            return Results.Ok(dashboards.GetRuntimeStatus(
                attentionCount,
                minimumSeverity));
        });

        group.MapGet("/devices/dashboard", (IDeviceDashboardService dashboards) =>
        {
            return Results.Ok(dashboards.GetAll());
        });

        group.MapGet("/devices/dashboard/summary", (IDeviceDashboardService dashboards) =>
        {
            return Results.Ok(dashboards.GetSummaries());
        });

        group.MapGet("/devices/dashboard/attention", (
            int? count,
            DeviceDashboardIssueSeverity? minimumSeverity,
            IDeviceDashboardService dashboards) =>
        {
            return Results.Ok(dashboards.GetAttentionSummaries(count, minimumSeverity));
        });

        group.MapGet("/devices/dashboard/overview", (IDeviceDashboardService dashboards) =>
        {
            return Results.Ok(dashboards.GetOverview());
        });

        group.MapGet("/devices/dashboard/issues", (
            DeviceDashboardIssueSeverity? minimumSeverity,
            string? source,
            int? count,
            IDeviceDashboardService dashboards) =>
        {
            return Results.Ok(dashboards.GetIssues(new DeviceDashboardIssueFilter(
                minimumSeverity,
                source,
                count)));
        });

        group.MapGet("/devices/dashboard/issues/summary", (
            DeviceDashboardIssueSeverity? minimumSeverity,
            string? source,
            int? count,
            IDeviceDashboardService dashboards) =>
        {
            return Results.Ok(dashboards.GetIssueSummaries(new DeviceDashboardIssueFilter(
                minimumSeverity,
                source,
                count)));
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

        group.MapGet("/devices/{name}/dashboard", (
            string name,
            IDeviceDashboardService dashboards) =>
        {
            return dashboards.TryGet(name, out var dashboard)
                ? Results.Ok(dashboard)
                : Results.NotFound(new
                {
                    Message = $"Device '{name}' is not registered."
                });
        });

        group.MapGet("/devices/{name}/dashboard/summary", (
            string name,
            IDeviceDashboardService dashboards) =>
        {
            return dashboards.TryGetSummary(name, out var summary)
                ? Results.Ok(summary)
                : Results.NotFound(new
                {
                    Message = $"Device '{name}' is not registered."
                });
        });

        group.MapGet("/devices/{name}/runtime/status", (
            string name,
            IDeviceDashboardService dashboards) =>
        {
            return dashboards.TryGetRuntimeStatus(name, out var status)
                ? Results.Ok(status)
                : Results.NotFound(new
                {
                    Message = $"Device '{name}' is not registered."
                });
        });

        group.MapGet("/devices/{name}/dashboard/issues", (
            string name,
            DeviceDashboardIssueSeverity? minimumSeverity,
            string? source,
            int? count,
            IDeviceDashboardService dashboards) =>
        {
            return dashboards.TryGetIssues(
                name,
                new DeviceDashboardIssueFilter(
                    minimumSeverity,
                    source,
                    count),
                out var issues)
                ? Results.Ok(issues)
                : Results.NotFound(new
                {
                    Message = $"Device '{name}' is not registered."
                });
        });

        group.MapGet("/devices/{name}/dashboard/issues/summary", (
            string name,
            DeviceDashboardIssueSeverity? minimumSeverity,
            string? source,
            int? count,
            IDeviceDashboardService dashboards) =>
        {
            return dashboards.TryGetIssueSummaries(
                name,
                new DeviceDashboardIssueFilter(
                    minimumSeverity,
                    source,
                    count),
                out var summaries)
                ? Results.Ok(summaries)
                : Results.NotFound(new
                {
                    Message = $"Device '{name}' is not registered."
                });
        });

        group.MapGet("/devices/{name}/signals/definitions", (
            string name,
            IDeviceRegistry registry) =>
        {
            return registry.TryGet(name, out var device)
                ? Results.Ok(device.Signals
                    .OrderBy(signal => signal.DisplayOrder ?? int.MaxValue)
                    .ThenBy(signal => signal.Category, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(signal => signal.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray())
                : Results.NotFound(new
                {
                    Message = $"Device '{name}' is not registered."
                });
        });

        group.MapGet("/devices/{name}/signals/definitions/{signalName}", (
            string name,
            string signalName,
            IDeviceRegistry registry) =>
        {
            if (!registry.TryGet(name, out var device))
            {
                return Results.NotFound(new
                {
                    Message = $"Device '{name}' is not registered."
                });
            }

            return device.TryGetSignal(signalName, out var signal)
                ? Results.Ok(signal)
                : Results.NotFound(new
                {
                    Message = $"Signal '{signalName}' is not registered for device '{name}'."
                });
        });

        group.MapGet("/connections/pool", (IDeviceConnectionPool connections) =>
        {
            return Results.Ok(connections.GetStatus());
        });

        group.MapGet("/connections/pool/{name}", (
            string name,
            IDeviceConnectionPool connections) =>
        {
            return connections.TryGetStatus(name, out var status)
                ? Results.Ok(status)
                : Results.NotFound(new
                {
                    Message = $"Device '{name}' has no pooled connection status."
                });
        });

        group.MapDelete("/connections/pool", async (
            IDeviceConnectionPool connections,
            CancellationToken cancellationToken) =>
        {
            var closed = await connections.CloseAllAsync(cancellationToken)
                .ConfigureAwait(false);

            return Results.Ok(new
            {
                Closed = closed
            });
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

        group.MapGet("/devices/{name}/signals/{signalName}/read", async (
            string name,
            string signalName,
            IDeviceSignalResolver signals,
            IPlcRuntime runtime,
            CancellationToken cancellationToken) =>
        {
            if (!signals.TryResolve(name, signalName, out var resolution))
            {
                return Results.NotFound(new
                {
                    Message = $"Signal '{signalName}' is not registered for device '{name}'."
                });
            }

            try
            {
                var value = await runtime.ReadAsync(
                        resolution.Signal,
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

        group.MapPost("/devices/{name}/signals/read-named", async (
            string name,
            ScadaNetReadManyNamedRequest request,
            IDeviceSignalResolver signals,
            IPlcRuntime runtime,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var signalNames = request.GetSignalNames();
                if (!signals.TryResolveMany(
                    name,
                    signalNames,
                    out var resolutions,
                    out var missingSignalName))
                {
                    return Results.NotFound(new
                    {
                        Message = $"Signal '{missingSignalName}' is not registered for device '{name}'."
                    });
                }

                var values = await runtime
                    .ReadManyAsync(
                        resolutions.Select(resolution => resolution.Signal).ToArray(),
                        cancellationToken)
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

        group.MapGet("/devices/{name}/signals/{signalName}/read-array", async (
            string name,
            string signalName,
            ushort? count,
            IDeviceSignalResolver signals,
            IPlcRuntime runtime,
            CancellationToken cancellationToken) =>
        {
            if (!signals.TryResolve(name, signalName, out var resolution))
            {
                return Results.NotFound(new
                {
                    Message = $"Signal '{signalName}' is not registered for device '{name}'."
                });
            }

            var elementCount = count ?? resolution.Definition.ElementCount;
            if (!elementCount.HasValue)
            {
                return Results.BadRequest(new
                {
                    Message = $"Signal '{signalName}' requires a count query value or configured element count."
                });
            }

            try
            {
                var value = await runtime.ReadArrayAsync(
                        resolution.Signal,
                        elementCount.Value,
                        cancellationToken)
                    .ConfigureAwait(false);

                return Results.Ok(value);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ScadaNetHttpErrors.ToResult(ex);
            }
        });

        group.MapPost("/devices/{name}/signals/{signalName}/write", async (
            string name,
            string signalName,
            ScadaNetWriteNamedSignalRequest request,
            IDeviceSignalResolver signals,
            IPlcRuntime runtime,
            CancellationToken cancellationToken) =>
        {
            if (!signals.TryResolve(name, signalName, out var resolution))
            {
                return Results.NotFound(new
                {
                    Message = $"Signal '{signalName}' is not registered for device '{name}'."
                });
            }

            if (!resolution.Definition.Writable)
            {
                return Results.BadRequest(new
                {
                    Message = $"Signal '{signalName}' is not configured as writable for device '{name}'."
                });
            }

            try
            {
                var value = request.GetValue();
                ScadaNetSignalValueRangeValidation.Validate(
                    resolution.Definition,
                    value,
                    signalName);

                await runtime.WriteAsync(
                        resolution.Signal,
                        value,
                        request.DataType ?? resolution.Definition.DataType,
                        cancellationToken)
                    .ConfigureAwait(false);

                return Results.Accepted();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ScadaNetHttpErrors.ToResult(ex);
            }
        });

        group.MapPost("/devices/{name}/signals/{signalName}/write-array", async (
            string name,
            string signalName,
            ScadaNetWriteNamedArrayRequest request,
            IDeviceSignalResolver signals,
            IPlcRuntime runtime,
            CancellationToken cancellationToken) =>
        {
            if (!signals.TryResolve(name, signalName, out var resolution))
            {
                return Results.NotFound(new
                {
                    Message = $"Signal '{signalName}' is not registered for device '{name}'."
                });
            }

            if (!resolution.Definition.Writable)
            {
                return Results.BadRequest(new
                {
                    Message = $"Signal '{signalName}' is not configured as writable for device '{name}'."
                });
            }

            try
            {
                var values = request.GetValues();
                ScadaNetSignalValueRangeValidation.ValidateMany(
                    resolution.Definition,
                    values,
                    signalName);

                await runtime.WriteArrayAsync(
                        resolution.Signal,
                        values,
                        request.DataType ?? resolution.Definition.DataType,
                        cancellationToken)
                    .ConfigureAwait(false);

                return Results.Accepted();
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

        group.MapGet("/devices/{name}/signals/snapshot-named", (
            string name,
            IDeviceSignalSnapshotReader snapshots) =>
        {
            return snapshots.TryGetDeviceSnapshots(name, out var values)
                ? Results.Ok(values)
                : Results.NotFound(new
                {
                    Message = $"Device '{name}' is not registered."
                });
        });

        group.MapGet("/devices/{name}/signals/{signalName}/snapshot", (
            string name,
            string signalName,
            IDeviceSignalSnapshotReader snapshots) =>
        {
            return snapshots.TryGet(name, signalName, out var value)
                ? Results.Ok(value)
                : Results.NotFound(new
                {
                    Message = $"Signal '{signalName}' is not registered for device '{name}'."
                });
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

        group.MapGet("/polling/groups", (IPollingGroupRegistry groups) =>
        {
            return Results.Ok(groups.Groups);
        });

        group.MapGet("/polling/groups/summary", (IPollingGroupMonitor monitor) =>
        {
            return Results.Ok(monitor.GetAll());
        });

        group.MapGet("/devices/{name}/polling/groups/summary", (
            string name,
            IDeviceRegistry registry,
            IPollingGroupMonitor monitor) =>
        {
            return registry.TryGet(name, out var device)
                ? Results.Ok(monitor.GetForDevice(device.Name))
                : Results.NotFound(new
                {
                    Message = $"Device '{name}' is not registered."
                });
        });

        group.MapGet("/polling/groups/{groupName}/summary", (
            string groupName,
            IPollingGroupMonitor monitor) =>
        {
            return monitor.TryGet(groupName, out var summary)
                ? Results.Ok(summary)
                : Results.NotFound(new
                {
                    Message = $"Polling group '{groupName}' is not registered."
                });
        });

        group.MapGet("/polling/groups/{groupName}", (
            string groupName,
            IPollingGroupRegistry groups) =>
        {
            return groups.TryGet(groupName, out var groupDefinition)
                ? Results.Ok(groupDefinition)
                : Results.NotFound(new
                {
                    Message = $"Polling group '{groupName}' is not registered."
                });
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

        group.MapGet("/writes/audit/summary", (IWriteAuditStore audit) =>
        {
            return Results.Ok(audit.GetSummary());
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

        group.MapGet("/devices/{name}/writes/audit/summary", (
            string name,
            IDeviceRegistry registry,
            IWriteAuditStore audit) =>
        {
            return registry.TryGet(name, out var device)
                ? Results.Ok(audit.GetDeviceSummary(device.Name))
                : Results.NotFound(new
                {
                    Message = $"Device '{name}' is not registered."
                });
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
