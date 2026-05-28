using ScadaNet.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddScadaNet(builder.Configuration)
    .AddLogix()
    .AddEtherNetIpDiscovery();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    Name = "ScadaNet sample API",
    Endpoints = new[]
    {
        "/scadanet/devices",
        "/scadanet/devices/line1-plc/discovery",
        "/scadanet/devices/line1-plc/signals/read?address=ProductionCounter",
        "/scadanet/devices/line1-plc/signals/snapshot",
        "/scadanet/polling/status",
        "/scadanet/health/devices",
        "/scadanet/writes/audit",
        "/scadanet/devices/line1-plc/signals/write",
        "/scadanet/discovery/detect?address=192.168.0.10"
    }
}));

app.MapScadaNetEndpoints();

app.Run();
