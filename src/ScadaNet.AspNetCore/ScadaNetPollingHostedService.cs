using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScadaNet.Runtime;

namespace ScadaNet.AspNetCore;

public sealed class ScadaNetPollingHostedService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(250);

    private readonly ScadaNetOptions _options;
    private readonly ISignalPollingService _polling;
    private readonly ILogger<ScadaNetPollingHostedService> _logger;

    public ScadaNetPollingHostedService(
        ScadaNetOptions options,
        ISignalPollingService polling,
        ILogger<ScadaNetPollingHostedService> logger)
    {
        _options = options;
        _polling = polling;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var lastRuns = new Dictionary<string, DateTimeOffset>(StringComparer.OrdinalIgnoreCase);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            foreach (var group in _options.PollingGroups.Where(group => group.Enabled))
            {
                if (!ShouldRun(group, now, lastRuns))
                {
                    continue;
                }

                lastRuns[GetGroupKey(group)] = now;

                try
                {
                    await _polling.PollAsync(group, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Polling group '{PollingGroup}' failed.",
                        group.Name);
                }
            }

            await Task.Delay(TickInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    private static bool ShouldRun(
        SignalPollingGroupDefinition group,
        DateTimeOffset now,
        Dictionary<string, DateTimeOffset> lastRuns)
    {
        return !lastRuns.TryGetValue(GetGroupKey(group), out var lastRun) ||
            now - lastRun >= NormalizeInterval(group.Interval);
    }

    private static TimeSpan NormalizeInterval(TimeSpan interval)
    {
        return interval <= TimeSpan.Zero
            ? TimeSpan.FromSeconds(1)
            : interval;
    }

    private static string GetGroupKey(SignalPollingGroupDefinition group)
    {
        return string.IsNullOrWhiteSpace(group.Name)
            ? group.DeviceName
            : group.Name;
    }
}
