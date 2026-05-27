using ScadaNet.Protocols;

namespace ScadaNet.AspNetCore;

public sealed record ScadaNetDiscoveryRequest(
    string Address,
    IReadOnlyList<int>? Ports = null,
    int? TimeoutMilliseconds = null)
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);

    public ProbeRequest ToProbeRequest()
    {
        if (string.IsNullOrWhiteSpace(Address))
        {
            throw new ArgumentException("Discovery address cannot be empty.", nameof(Address));
        }

        var timeout = TimeoutMilliseconds.HasValue
            ? TimeSpan.FromMilliseconds(TimeoutMilliseconds.Value)
            : DefaultTimeout;

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(TimeoutMilliseconds),
                TimeoutMilliseconds,
                "Discovery timeout must be greater than zero.");
        }

        return new ProbeRequest(
            Address.Trim(),
            ValidatePorts(),
            timeout);
    }

    private IReadOnlyList<int> ValidatePorts()
    {
        if (Ports is null || Ports.Count == 0)
        {
            return [];
        }

        var ports = new List<int>(Ports.Count);
        var seen = new HashSet<int>();
        for (var index = 0; index < Ports.Count; index++)
        {
            var port = Ports[index];
            if (port is < 1 or > 65535)
            {
                throw new ArgumentOutOfRangeException(
                    $"ports[{index}]",
                    port,
                    "Discovery port must be between 1 and 65535.");
            }

            if (seen.Add(port))
            {
                ports.Add(port);
            }
        }

        return ports.ToArray();
    }
}

public sealed record ScadaNetDiscoveryDriverInfo(
    string Name,
    IReadOnlyList<int> DefaultPorts,
    IReadOnlyList<string> Capabilities);
