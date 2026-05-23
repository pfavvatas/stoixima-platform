using AggregationService.Services;
using Confluent.Kafka;
using Contracts;
using Contracts.Events;
using Messaging;
using Prometheus;
using StackExchange.Redis;

namespace AggregationService.Workers;

public class AggregationWorker : BackgroundService
{
    private readonly KafkaConsumerFactory _consumerFactory;
    private readonly MatchAggregator _aggregator;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<AggregationWorker> _logger;

    private const string GroupId = "aggregation-service-group";

    private static readonly Counter TipsConsumed = Metrics.CreateCounter(
        "aggregation_tips_consumed_total", "Total ProcessedTipEvents consumed from Kafka");

    public AggregationWorker(
        KafkaConsumerFactory consumerFactory,
        MatchAggregator aggregator,
        IConnectionMultiplexer redis,
        ILogger<AggregationWorker> logger)
    {
        _consumerFactory = consumerFactory;
        _aggregator      = aggregator;
        _redis           = redis;
        _logger          = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        using var consumer = _consumerFactory.Create(GroupId);
        consumer.Subscribe(KafkaTopics.ProcessedMessages);
        _logger.LogInformation("AggregationWorker subscribed to {Topic}", KafkaTopics.ProcessedMessages);

        // Periodic re-aggregation for matches happening soon (every minute)
        _ = RunPeriodicReaggregationAsync(stoppingToken);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));
                if (result is null or { IsPartitionEOF: true })
                    continue;

                var tip = KafkaConsumerFactory.Deserialize<ProcessedTipEvent>(result);
                TipsConsumed.Inc();

                try
                {
                    await _aggregator.OnTipReceivedAsync(tip, stoppingToken);
                    consumer.Commit(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Aggregation failed for match {MatchId}", tip.MatchId);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally
        {
            consumer.Close();
        }
    }

    // Re-aggregates matches with kick-off within 3 hours, every minute.
    private async Task RunPeriodicReaggregationAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), ct);
                var matchIds = await GetUpcomingMatchIdsAsync(ct);
                foreach (var matchId in matchIds)
                {
                    try { await _aggregator.AggregateAndPublishAsync(matchId, ct); }
                    catch (Exception ex) { _logger.LogError(ex, "Re-aggregation failed for match {MatchId}", matchId); }
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Periodic re-aggregation loop error"); }
        }
    }

    private async Task<List<int>> GetUpcomingMatchIdsAsync(CancellationToken ct)
    {
        var db  = _redis.GetDatabase();
        var key = $"matches:today:{DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}";
        var members = await db.SetMembersAsync(key);
        return members.Select(m => (int)(long)m).ToList();
    }
}
