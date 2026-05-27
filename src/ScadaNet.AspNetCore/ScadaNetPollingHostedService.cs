using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScadaNet.Runtime;

namespace ScadaNet.AspNetCore;

public sealed class ScadaNetPollingHostedService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(250);

    private readonly IPollingGroupRegistry _groups;
    private readonly ISignalPollingService _polling;
    private readonly ILogger<ScadaNetPollingHostedService> _logger;
    private readonly int _maxConcurrency;

    public ScadaNetPollingHostedService(
        IPollingGroupRegistry groups,
        ISignalPollingService polling,
        ILogger<ScadaNetPollingHostedService> logger)
        : this(groups, polling, logger, new ScadaNetOptions())
    {
    }

    public ScadaNetPollingHostedService(
        IPollingGroupRegistry groups,
        ISignalPollingService polling,
        ILogger<ScadaNetPollingHostedService> logger,
        ScadaNetOptions options)
    {
        _groups = groups;
        _polling = polling;
        _logger = logger;
        _maxConcurrency = Math.Max(1, options.BackgroundPollingMaxConcurrency);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var lastRuns = new Dictionary<string, DateTimeOffset>(StringComparer.OrdinalIgnoreCase);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var dueGroups = _groups.Groups
                .Where(group => group.Enabled && ShouldRun(group, now, lastRuns))
                .ToArray();

            foreach (var group in dueGroups)
            {
                lastRuns[GetGroupKey(group)] = now;
            }

            if (_maxConcurrency == 1)
            {
                foreach (var group in dueGroups)
                {
                    await PollGroupAsync(group, stoppingToken).ConfigureAwait(false);
                }
            }
            else if (dueGroups.Length > 0)
            {
                await Parallel.ForEachAsync(
                    dueGroups,
                    new ParallelOptions
                    {
                        CancellationToken = stoppingToken,
                        MaxDegreeOfParallelism = _maxConcurrency
                    },
                    async (group, cancellationToken) =>
                    {
                        await PollGroupAsync(group, cancellationToken).ConfigureAwait(false);
                    }).ConfigureAwait(false);
            }

            await Task.Delay(TickInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async ValueTask PollGroupAsync(
        SignalPollingGroupDefinition group,
        CancellationToken stoppingToken)
    {
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
