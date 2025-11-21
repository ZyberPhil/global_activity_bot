using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GlobalStatsBot.Services;

/// <summary>
/// Periodically synchronizes the GlobalXpCache via <see cref="StatsService"/> so manual commands can rely on fresh data.
/// </summary>
public sealed class GlobalXpSyncService : BackgroundService
{
    private static readonly TimeSpan MinimumDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GlobalXpSyncService> _logger;

    public GlobalXpSyncService(IServiceScopeFactory scopeFactory, ILogger<GlobalXpSyncService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunSyncAsync(stoppingToken);

            try
            {
                var delay = CalculateDelayToNextInterval();
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // ignore cancellation during delay and end gracefully
            }
        }
    }

    private async Task RunSyncAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var statsService = scope.ServiceProvider.GetRequiredService<StatsService>();

        try
        {
            var affected = await statsService.SynchronizeGlobalXpCacheAsync(ct);
            _logger.LogInformation("Global XP Cache Sync abgeschlossen: {Affected} Nutzer aktualisiert.", affected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim automatischen Global XP Sync.");
        }
    }

    private static TimeSpan CalculateDelayToNextInterval()
    {
        var nowUtc = DateTime.UtcNow;
        var currentAlignedMinute = nowUtc.Minute - nowUtc.Minute % 5;
        var aligned = new DateTime(
            nowUtc.Year,
            nowUtc.Month,
            nowUtc.Day,
            nowUtc.Hour,
            currentAlignedMinute,
            0,
            DateTimeKind.Utc);

        var next = aligned.AddMinutes(5);
        var delay = next - nowUtc;

        if (delay < MinimumDelay)
        {
            delay = MinimumDelay;
        }

        return delay;
    }
}
