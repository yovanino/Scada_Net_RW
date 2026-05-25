using ScadaNet.AspNetCore;
using ScadaNet.Protocols;
using ScadaNet.Runtime;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddScadaNet()
    .AddEtherNetIpDiscovery();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    Name = "ScadaNet sample API",
    Endpoints = new[] { "/discovery/ethernetip?address=192.168.0.10" }
}));

app.MapGet("/discovery/ethernetip", async (
    string address,
    int? port,
    IDiscoveryService discovery,
    CancellationToken cancellationToken) =>
{
    var ports = port.HasValue
        ? new[] { port.Value }
        : Array.Empty<int>();

    var result = await discovery.DetectAsync(
        new ProbeRequest(address, ports, TimeSpan.FromSeconds(2)),
        cancellationToken);

    return Results.Ok(result);
});

app.Run();
