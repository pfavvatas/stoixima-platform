using Cronos;
using MatchDataService.Configuration;
using MatchDataService.Services;
using Microsoft.Extensions.Options;
using Prometheus;

namespace MatchDataService.Workers;

public class MatchFetchWorker : BackgroundService
{
    // Run at 06:00 UTC daily; retry every hour on failure.
    private static readonly CronExpression DailyCron  = CronExpression.Parse("0 6 * * *");
    private static readonly CronExpression RetryCron  = CronExpression.Parse("0 * * * *");

    private readonly FootballApiClient _apiClient;
    private readonly MatchRepository _repository;
    private readonly FootballApiOptions _options;
    private readonly ILogger<MatchFetchWorker> _logger;

    private bool _lastFetchSucceeded = false;

    private static readonly Counter FetchTotal = Metrics.CreateCounter(
        "matchdata_fetches_total", "Total football API fetch attempts", ["league", "result"]);

    public MatchFetchWorker(
        FootballApiClient apiClient,
        MatchRepository repository,
        IOptions<FootballApiOptions> options,
        ILogger<MatchFetchWorker> logger)
    {
        _apiClient  = apiClient;
        _repository = repository;
        _options    = options.Value;
        _logger     = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        // Fetch immediately on startup
        await FetchAllLeaguesAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var cron = _lastFetchSucceeded ? DailyCron : RetryCron;
            var next = cron.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Utc);
            if (next is null) break;

            var delay = next.Value - DateTimeOffset.UtcNow;
            _logger.LogInformation("Next match fetch at {Next} (in {Delay:hh\\:mm})", next, delay);

            try { await Task.Delay(delay, stoppingToken); }
            catch (OperationCanceledException) { break; }

            await FetchAllLeaguesAsync(stoppingToken);
        }
    }

    private async Task FetchAllLeaguesAsync(CancellationToken ct)
    {
        _logger.LogInformation("Fetching today's matches for {Count} leagues", _options.Leagues.Count);
        var allSucceeded = true;

        foreach (var league in _options.Leagues)
        {
            try
            {
                var matches = await _apiClient.GetTodayMatchesAsync(league, ct);
                await _repository.UpsertAsync(matches, league, ct);
                FetchTotal.WithLabels(league, "success").Inc();
                _logger.LogInformation("League {League}: {Count} matches fetched", league, matches.Count);
            }
            catch (Exception ex)
            {
                allSucceeded = false;
                FetchTotal.WithLabels(league, "error").Inc();
                _logger.LogError(ex, "Failed to fetch matches for league {League}", league);
            }
        }

        _lastFetchSucceeded = allSucceeded;
    }
}
